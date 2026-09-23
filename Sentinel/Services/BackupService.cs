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
        bool IsBackupConfigured { get; }
        string? BackupConfigurationIssue { get; }
        bool IsRestoreConfigured { get; }
        Task<BackupResult> CreateBackupAsync(BackupType backupType);
        Task<List<BackupInfo>> GetBackupHistoryAsync();
        Task<bool> RestoreBackupAsync(int backupId);
        Task<bool> DeleteBackupAsync(int backupId);
    }

    public class BackupService : IBackupService
    {
        private readonly string _applicationConnectionString;
        private readonly string? _backupConnectionString;
        private readonly string? _restoreConnectionString;
        private readonly string _backupPath;
        private readonly string _backupRoot;
        private readonly string _sqlServerBackupRoot;
        private readonly ILogger<BackupService> _logger;
        private readonly IConfiguration _configuration;

        public bool IsBackupConfigured { get; }
        public string? BackupConfigurationIssue { get; }
        public bool IsRestoreConfigured { get; }

        public BackupService(
            IConfiguration configuration,
            ILogger<BackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _applicationConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found");
            _backupConnectionString = configuration.GetConnectionString("BackupConnection");
            _restoreConnectionString = configuration.GetConnectionString("BackupRestoreConnection");

            // Get backup path from configuration or use default, handling both null and empty values
            var configuredPath = configuration["Backup:Path"];
            _backupPath = string.IsNullOrWhiteSpace(configuredPath) 
                ? @"C:\DatabaseBackups\SurveillanceMVP" 
                : configuredPath;
            _backupRoot = Path.GetFullPath(_backupPath);
            var configuredSqlServerPath = configuration["Backup:SqlServerPath"];
            _sqlServerBackupRoot = (string.IsNullOrWhiteSpace(configuredSqlServerPath)
                ? _backupPath
                : configuredSqlServerPath).Trim();

            // Ensure backup directory exists
            if (!Directory.Exists(_backupRoot))
            {
                Directory.CreateDirectory(_backupRoot);
            }

            if (string.IsNullOrWhiteSpace(_backupConnectionString))
            {
                BackupConfigurationIssue = "A dedicated database backup connection has not been configured.";
                return;
            }

            if (_sqlServerBackupRoot.IndexOfAny(new[] { '\r', '\n', '\'' }) >= 0)
            {
                BackupConfigurationIssue = "The SQL Server backup path is invalid.";
                return;
            }

            try
            {
                var applicationDatabase = new SqlConnectionStringBuilder(_applicationConnectionString).InitialCatalog;
                var backupDatabase = new SqlConnectionStringBuilder(_backupConnectionString).InitialCatalog;
                if (!string.Equals(applicationDatabase, backupDatabase, StringComparison.OrdinalIgnoreCase))
                {
                    BackupConfigurationIssue = "The dedicated backup connection must target the Sentinel application database.";
                    return;
                }
            }
            catch (ArgumentException)
            {
                BackupConfigurationIssue = "The dedicated database backup connection is invalid.";
                return;
            }

            IsBackupConfigured = true;
            IsRestoreConfigured = configuration.GetValue<bool>("Backup:RestoreEnabled")
                && !string.IsNullOrWhiteSpace(_restoreConnectionString);
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
                StartTime = DateTime.UtcNow
            };

            try
            {
                if (!IsBackupConfigured || _backupConnectionString is null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Backups require administrator configuration.";
                    result.EndTime = DateTime.UtcNow;
                    _logger.LogWarning("Backup was requested but is not configured: {Reason}", BackupConfigurationIssue);
                    return result;
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"SurveillanceMVP_{backupType}_{timestamp}.bak";
                var fullPath = GetOwnedBackupPath(backupFileName);
                var sqlServerPath = GetOwnedSqlServerBackupPath(backupFileName);

                using var connection = new SqlConnection(_backupConnectionString);
                await connection.OpenAsync();

                // Check if compression is supported
                bool supportsCompression = await SupportsCompressionAsync(connection);
                _logger.LogInformation("SQL Server compression support: {Supported}", supportsCompression);

                string backupScript = GetFullBackupScript(sqlServerPath, supportsCompression);

                using var command = new SqlCommand(backupScript, connection);
                command.CommandTimeout = 600; // 10 minutes
                await command.ExecuteNonQueryAsync();

                result.Success = true;
                result.BackupFileName = backupFileName;
                result.BackupFilePath = fullPath;
                result.EndTime = DateTime.UtcNow;
                result.SizeInBytes = new FileInfo(fullPath).Length;

                _logger.LogInformation("Backup created successfully: {BackupType} - {FileName} - {SizeMB} MB",
                    backupType, backupFileName, result.SizeInMB);

                // Log to database
                await LogBackupAsync(result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                // BackupResult is rendered by the settings UI and written to backup
                // history. Keep provider, path and connection details in the log only.
                result.ErrorMessage = "Backup failed. Check the application logs for details.";
                result.EndTime = DateTime.UtcNow;

                _logger.LogError(ex, "Backup failed: {BackupType}", backupType);
            }

            return result;
        }

        private string GetFullBackupScript(string backupPath, bool useCompression)
        {
            var dbName = new SqlConnectionStringBuilder(_applicationConnectionString).InitialCatalog;
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
            using var connection = new SqlConnection(_applicationConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP 50
                    Id,
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
                var backupFileName = reader.GetString(2);
                backups.Add(new BackupInfo
                {
                    Id = reader.GetInt32(0),
                    BackupType = Enum.Parse<BackupType>(reader.GetString(1)),
                    BackupFileName = backupFileName,
                    BackupFilePath = reader.GetString(3),
                    SizeInBytes = reader.GetInt64(4),
                    StartTime = reader.GetDateTime(5),
                    EndTime = reader.GetDateTime(6),
                    Success = reader.GetBoolean(7),
                    ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CreatedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
                    FileExists = TryGetOwnedBackupPath(backupFileName, out var backupPath) && File.Exists(backupPath)
                });
            }

            return backups;
        }

        public async Task<bool> RestoreBackupAsync(int backupId)
        {
            try
            {
                var backupFileName = await GetRecordedBackupFileNameAsync(backupId);
                if (backupFileName == null || !TryGetOwnedBackupPath(backupFileName, out var fullPath))
                {
                    _logger.LogWarning("Restore requested for an invalid or unavailable backup record {BackupId}", backupId);
                    return false;
                }

                if (!File.Exists(fullPath))
                {
                    _logger.LogError("Backup file not found for backup record {BackupId}", backupId);
                    return false;
                }

                if (!IsRestoreConfigured || _restoreConnectionString is null)
                {
                    _logger.LogWarning("Restore requested but no dedicated restore connection is configured.");
                    return false;
                }

                var dbName = new SqlConnectionStringBuilder(_applicationConnectionString).InitialCatalog;
                var sqlServerPath = GetOwnedSqlServerBackupPath(backupFileName);

                using var connection = new SqlConnection(_restoreConnectionString);
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
                    FROM DISK = N'{sqlServerPath}'
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

                _logger.LogInformation("Database restored successfully from backup record {BackupId}", backupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore failed for backup record {BackupId}", backupId);
                return false;
            }
        }

        public async Task<bool> DeleteBackupAsync(int backupId)
        {
            try
            {
                var backupFileName = await GetRecordedBackupFileNameAsync(backupId);
                if (backupFileName == null || !TryGetOwnedBackupPath(backupFileName, out var fullPath))
                {
                    _logger.LogWarning("Delete requested for an invalid or unavailable backup record {BackupId}", backupId);
                    return false;
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                // Delete from database log
                using var connection = new SqlConnection(_applicationConnectionString);
                await connection.OpenAsync();

                var query = "DELETE FROM BackupHistory WHERE Id = @BackupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@BackupId", backupId);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Backup record {BackupId} deleted", backupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup record {BackupId}", backupId);
                return false;
            }
        }

        private async Task<string?> GetRecordedBackupFileNameAsync(int backupId)
        {
            using var connection = new SqlConnection(_applicationConnectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT BackupFileName
                FROM BackupHistory
                WHERE Id = @BackupId AND Success = 1";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BackupId", backupId);
            return await command.ExecuteScalarAsync() as string;
        }

        private string GetOwnedBackupPath(string backupFileName)
        {
            if (!TryGetOwnedBackupPath(backupFileName, out var fullPath))
            {
                throw new InvalidOperationException("The backup filename is invalid.");
            }

            return fullPath;
        }

        private bool TryGetOwnedBackupPath(string? backupFileName, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrWhiteSpace(backupFileName) ||
                !string.Equals(backupFileName, Path.GetFileName(backupFileName), StringComparison.Ordinal) ||
                !backupFileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = Path.GetFullPath(Path.Combine(_backupRoot, backupFileName));
            var rootWithSeparator = _backupRoot.EndsWith(Path.DirectorySeparatorChar)
                ? _backupRoot
                : _backupRoot + Path.DirectorySeparatorChar;

            return fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private string GetOwnedSqlServerBackupPath(string backupFileName)
        {
            if (string.IsNullOrWhiteSpace(backupFileName) ||
                !string.Equals(backupFileName, Path.GetFileName(backupFileName), StringComparison.Ordinal) ||
                !backupFileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The backup filename is invalid.");
            }

            // This path is evaluated by SQL Server, which can run on a different
            // operating system from Sentinel. Do not use Path.GetFullPath here:
            // it would reinterpret a Windows SQL Server path on a Linux app host.
            var separator = _sqlServerBackupRoot.Contains('\\') ? '\\' : '/';
            return _sqlServerBackupRoot.TrimEnd('\\', '/') + separator + backupFileName;
        }

        private async Task LogBackupAsync(BackupResult result)
        {
            try
            {
                using var connection = new SqlConnection(_applicationConnectionString);
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
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

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
        public int Id { get; set; }
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
