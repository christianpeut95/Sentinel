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
        private const string TimelineDataFolder = "data/timeline-entries";
        private const string BackupFolder = "data/timeline-backups";

        public TimelineStorageService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TimelineStorageService> logger)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            EnsureDirectoriesExist();
        }

        private void EnsureDirectoriesExist()
        {
            var timelinePath = Path.Combine(_environment.WebRootPath, TimelineDataFolder);
            var backupPath = Path.Combine(_environment.WebRootPath, BackupFolder);
            
            if (!Directory.Exists(timelinePath))
                Directory.CreateDirectory(timelinePath);
            
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);
        }

        private string GetTimelineFilePath(Guid caseId)
        {
            return Path.Combine(_environment.WebRootPath, TimelineDataFolder, $"{caseId}_timeline.json");
        }

        private string GetBackupFilePath(Guid caseId, DateTime timestamp)
        {
            return Path.Combine(_environment.WebRootPath, BackupFolder, $"{caseId}_{timestamp:yyyyMMddHHmmss}_backup.json");
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
