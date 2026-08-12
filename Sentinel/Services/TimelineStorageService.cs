using Sentinel.Models.Timeline;
using System.Text.Json;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for storing and retrieving timeline data as JSON files
    /// </summary>
    public interface ITimelineStorageService
    {
        /// <summary>
        /// Load timeline data for a case
        /// </summary>
        Task<CaseTimelineData?> LoadTimelineAsync(Guid caseId);

        /// <summary>
        /// Save timeline data for a case
        /// </summary>
        Task SaveTimelineAsync(CaseTimelineData timelineData);

        /// <summary>
        /// Check if timeline exists for a case
        /// </summary>
        Task<bool> TimelineExistsAsync(Guid caseId);

        /// <summary>
        /// Delete timeline data for a case
        /// </summary>
        Task DeleteTimelineAsync(Guid caseId);

        /// <summary>
        /// Create a backup of the timeline
        /// </summary>
        Task<string> CreateBackupAsync(Guid caseId);
    }

    public class TimelineStorageService : ITimelineStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TimelineStorageService> _logger;
        private readonly string _storageRoot;
        private const string TimelineDataFolder = "timelines";
        private const string BackupFolder = "timeline-backups";

        public TimelineStorageService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<TimelineStorageService> logger)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            var configuredRoot = configuration["FileStorage:RootPath"];
            configuredRoot = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine("App_Data", "SentinelFiles")
                : configuredRoot;
            _storageRoot = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(_environment.ContentRootPath, configuredRoot));

            var webRoot = Path.GetFullPath(_environment.WebRootPath);
            if (_storageRoot.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
                _storageRoot.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("FileStorage:RootPath must be outside wwwroot.");
            }

            EnsureDirectoriesExist();
            MigrateLegacyTimelineFiles();
        }

        private void EnsureDirectoriesExist()
        {
            var timelinePath = Path.Combine(_storageRoot, TimelineDataFolder);
            var backupPath = Path.Combine(_storageRoot, BackupFolder);
            
            if (!Directory.Exists(timelinePath))
                Directory.CreateDirectory(timelinePath);
            
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);
        }

        private string GetTimelineFilePath(Guid caseId)
        {
            return Path.Combine(_storageRoot, TimelineDataFolder, $"{caseId}_timeline.json");
        }

        private string GetBackupFilePath(Guid caseId, DateTime timestamp)
        {
            return Path.Combine(_storageRoot, BackupFolder, $"{caseId}_{timestamp:yyyyMMddHHmmss}_backup.json");
        }

        private void MigrateLegacyTimelineFiles()
        {
            MoveLegacyFiles("data/timeline-entries", TimelineDataFolder);
            MoveLegacyFiles("data/timeline-backups", BackupFolder);
        }

        private void MoveLegacyFiles(string legacyRelativeFolder, string protectedRelativeFolder)
        {
            var legacyDirectory = Path.Combine(_environment.WebRootPath, legacyRelativeFolder);
            if (!Directory.Exists(legacyDirectory))
                return;

            var destinationDirectory = Path.Combine(_storageRoot, protectedRelativeFolder);
            Directory.CreateDirectory(destinationDirectory);

            foreach (var sourceFile in Directory.EnumerateFiles(legacyDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
                if (File.Exists(destinationFile))
                {
                    _logger.LogWarning("Legacy timeline file {SourceFile} was not moved because the protected destination exists", sourceFile);
                    continue;
                }

                File.Move(sourceFile, destinationFile);
                _logger.LogInformation("Moved legacy timeline file to protected storage: {FileName}", Path.GetFileName(sourceFile));
            }
        }

        public async Task<CaseTimelineData?> LoadTimelineAsync(Guid caseId)
        {
            try
            {
                var filePath = GetTimelineFilePath(caseId);
                
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                return JsonSerializer.Deserialize<CaseTimelineData>(json, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading timeline for case {CaseId}", caseId);
                throw;
            }
        }

        public async Task SaveTimelineAsync(CaseTimelineData timelineData)
        {
            try
            {
                var filePath = GetTimelineFilePath(timelineData.CaseId);
                
                // Create backup if file exists
                if (File.Exists(filePath))
                {
                    await CreateBackupAsync(timelineData.CaseId);
                }

                // Update metadata
                if (timelineData.CreatedDate == default)
                {
                    timelineData.CreatedDate = DateTime.UtcNow;
                    timelineData.CreatedByUserId = GetCurrentUserId();
                }
                
                timelineData.LastModified = DateTime.UtcNow;
                timelineData.LastModifiedByUserId = GetCurrentUserId();
                timelineData.Version++;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(timelineData, options);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("Timeline saved for case {CaseId}, version {Version}", 
                    timelineData.CaseId, timelineData.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving timeline for case {CaseId}", timelineData.CaseId);
                throw;
            }
        }

        public async Task<bool> TimelineExistsAsync(Guid caseId)
        {
            var filePath = GetTimelineFilePath(caseId);
            return await Task.FromResult(File.Exists(filePath));
        }

        public async Task DeleteTimelineAsync(Guid caseId)
        {
            try
            {
                var filePath = GetTimelineFilePath(caseId);
                
                if (File.Exists(filePath))
                {
                    // Create final backup before deletion
                    await CreateBackupAsync(caseId);
                    File.Delete(filePath);
                    _logger.LogInformation("Timeline deleted for case {CaseId}", caseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timeline for case {CaseId}", caseId);
                throw;
            }
        }

        public async Task<string> CreateBackupAsync(Guid caseId)
        {
            try
            {
                var sourceFile = GetTimelineFilePath(caseId);
                
                if (!File.Exists(sourceFile))
                    return string.Empty;

                var backupFile = GetBackupFilePath(caseId, DateTime.UtcNow);
                File.Copy(sourceFile, backupFile, overwrite: false);

                _logger.LogInformation("Backup created for case {CaseId} at {BackupPath}", caseId, backupFile);
                return backupFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup for case {CaseId}", caseId);
                throw;
            }
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                ?? "system";
        }
    }
}
