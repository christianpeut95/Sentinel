using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface IBackupService
    {
        Task<BackupResult> CreateBackupAsync(BackupType backupType);
        Task<List<BackupInfo>> GetBackupHistoryAsync();
        Task<bool> RestoreBackupAsync(string backupFileName);
        Task<bool> DeleteBackupAsync(string backupFileName);
    }

    public class BackupService : IBackupService
    {
        private readonly string _connectionString;
        private readonly string _backupPath;
        private readonly ILogger<BackupService> _logger;
        private readonly IConfiguration _configuration;

        public BackupService(
            IConfiguration configuration,
            ILogger<BackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");
            _backupPath = configuration["Backup:Path"] ?? @"C:\DatabaseBackups\SurveillanceMVP";

            // Ensure backup directory exists
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
        }

        private async Task<bool> SupportsCompressionAsync(SqlConnection connection)
        {
            try
            {
                const string query = @"
                    SELECT CASE 
                        WHEN SERVERPROPERTY('EngineEdition') = 2 THEN 0  -- Express Edition
                        WHEN SERVERPROPERTY('EngineEdition') = 4 THEN 0  -- Express Edition with Advanced Services
                        ELSE 1 
                    END AS SupportsCompression";

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine compression support, assuming not supported");
                return false;
            }
        }

        public async Task<BackupResult> CreateBackupAsync(BackupType backupType)
        {
            var result = new BackupResult
            {
                BackupType = backupType,
                StartTime = DateTime.Now
            };

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"SurveillanceMVP_{backupType}_{timestamp}.bak";
                var fullPath = Path.Combine(_backupPath, backupFileName);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if compression is supported
                bool supportsCompression = await SupportsCompressionAsync(connection);
                _logger.LogInformation("SQL Server compression support: {Supported}", supportsCompression);

                string backupScript = GetFullBackupScript(fullPath, supportsCompression);

                using var command = new SqlCommand(backupScript, connection);
                command.CommandTimeout = 600; // 10 minutes
                await command.ExecuteNonQueryAsync();

                result.Success = true;
                result.BackupFileName = backupFileName;
                result.BackupFilePath = fullPath;
                result.EndTime = DateTime.Now;
                result.SizeInBytes = new FileInfo(fullPath).Length;

                _logger.LogInformation("Backup created successfully: {BackupType} - {FileName} - {SizeMB} MB",
                    backupType, backupFileName, result.SizeInMB);

                // Log to database
                await LogBackupAsync(result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;

                _logger.LogError(ex, "Backup failed: {BackupType}", backupType);
            }

            return result;
        }

        private string GetFullBackupScript(string backupPath, bool useCompression)
        {
            var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
            var compressionOption = useCompression ? "COMPRESSION," : "";
            
            return $@"
                BACKUP DATABASE [{dbName}]
                TO DISK = N'{backupPath}'
                WITH FORMAT,
                     INIT,
                     NAME = N'Surveillance MVP Full Backup',
                     SKIP,
                     NOREWIND,
                     NOUNLOAD,
                     {compressionOption}
                     STATS = 10;
            ";
        }

        public async Task<List<BackupInfo>> GetBackupHistoryAsync()
        {
            var backups = new List<BackupInfo>();

            // Get from database log
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP 50
                    BackupType,
                    BackupFileName,
                    BackupFilePath,
                    SizeInBytes,
                    StartTime,
                    EndTime,
                    Success,
                    ErrorMessage,
                    CreatedBy
                FROM BackupHistory
                ORDER BY StartTime DESC
            ";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                backups.Add(new BackupInfo
                {
                    BackupType = Enum.Parse<BackupType>(reader.GetString(0)),
                    BackupFileName = reader.GetString(1),
                    BackupFilePath = reader.GetString(2),
                    SizeInBytes = reader.GetInt64(3),
                    StartTime = reader.GetDateTime(4),
                    EndTime = reader.GetDateTime(5),
                    Success = reader.GetBoolean(6),
                    ErrorMessage = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                    FileExists = File.Exists(reader.GetString(2))
                });
            }

            return backups;
        }

        public async Task<bool> RestoreBackupAsync(string backupFileName)
        {
            try
            {
                var fullPath = Path.Combine(_backupPath, backupFileName);
                if (!File.Exists(fullPath))
                {
                    _logger.LogError("Backup file not found: {FileName}", backupFileName);
                    return false;
                }

                var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Set database to single user mode
                var setSingleUserScript = $@"
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                ";

                using var setSingleUserCommand = new SqlCommand(setSingleUserScript, connection);
                await setSingleUserCommand.ExecuteNonQueryAsync();

                // Restore backup
                var restoreScript = $@"
                    RESTORE DATABASE [{dbName}]
                    FROM DISK = N'{fullPath}'
                    WITH REPLACE,
                         RECOVERY,
                         STATS = 10;
                ";

                using var restoreCommand = new SqlCommand(restoreScript, connection);
                restoreCommand.CommandTimeout = 600;
                await restoreCommand.ExecuteNonQueryAsync();

                // Set back to multi-user mode
                var setMultiUserScript = $@"
                    ALTER DATABASE [{dbName}] SET MULTI_USER;
                ";

                using var setMultiUserCommand = new SqlCommand(setMultiUserScript, connection);
                await setMultiUserCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Database restored successfully from: {FileName}", backupFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore failed for: {FileName}", backupFileName);
                return false;
            }
        }

        public async Task<bool> DeleteBackupAsync(string backupFileName)
        {
            try
            {
                var fullPath = Path.Combine(_backupPath, backupFileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                // Delete from database log
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "DELETE FROM BackupHistory WHERE BackupFileName = @FileName";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FileName", backupFileName);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Backup deleted: {FileName}", backupFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup: {FileName}", backupFileName);
                return false;
            }
        }

        private async Task LogBackupAsync(BackupResult result)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO BackupHistory 
                        (BackupType, BackupFileName, BackupFilePath, SizeInBytes, 
                         StartTime, EndTime, Success, ErrorMessage, CreatedBy, CreatedAt)
                    VALUES 
                        (@BackupType, @BackupFileName, @BackupFilePath, @SizeInBytes,
                         @StartTime, @EndTime, @Success, @ErrorMessage, @CreatedBy, @CreatedAt)
                ";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@BackupType", result.BackupType.ToString());
                command.Parameters.AddWithValue("@BackupFileName", result.BackupFileName ?? "");
                command.Parameters.AddWithValue("@BackupFilePath", result.BackupFilePath ?? "");
                command.Parameters.AddWithValue("@SizeInBytes", result.SizeInBytes);
                command.Parameters.AddWithValue("@StartTime", result.StartTime);
                command.Parameters.AddWithValue("@EndTime", result.EndTime);
                command.Parameters.AddWithValue("@Success", result.Success);
                command.Parameters.AddWithValue("@ErrorMessage", (object?)result.ErrorMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", "System"); // TODO: Get from user context
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log backup to database");
            }
        }
    }

    // DTOs
    public enum BackupType
    {
        Full
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public BackupType BackupType { get; set; }
        public string? BackupFileName { get; set; }
        public string? BackupFilePath { get; set; }
        public long SizeInBytes { get; set; }
        public double SizeInMB => SizeInBytes / (1024.0 * 1024.0);
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string? ErrorMessage { get; set; }
    }

    public class BackupInfo
    {
        public BackupType BackupType { get; set; }
        public string BackupFileName { get; set; } = string.Empty;
        public string BackupFilePath { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public double SizeInMB => SizeInBytes / (1024.0 * 1024.0);
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CreatedBy { get; set; }
        public bool FileExists { get; set; }
    }
}
