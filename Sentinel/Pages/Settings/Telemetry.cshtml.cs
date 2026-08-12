using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Sentinel.Services;
using Dapper;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings
{
    [Authorize(Roles = "Admin")]
    public class TelemetryModel : PageModel
    {
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelemetryModel> _logger;

        public TelemetryModel(
            ISystemSettingsService systemSettingsService,
            IConfiguration configuration,
            ILogger<TelemetryModel> logger)
        {
            _systemSettingsService = systemSettingsService;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public bool TelemetryEnabled { get; set; }

        [BindProperty]
        public bool LocalLoggingEnabled { get; set; }

        [BindProperty]
        [Range(1, 365, ErrorMessage = "Retention days must be between 1 and 365")]
        public int LogRetentionDays { get; set; } = 30;

        [BindProperty]
        public string MinimumLogLevel { get; set; } = "Information";

        [BindProperty]
        public bool IncludeUserInformation { get; set; } = true;

        [BindProperty]
        public bool IncludeSystemInformation { get; set; } = true;

        public string? InstallationId { get; set; }
        public int CurrentLogCount { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var settings = await _systemSettingsService.GetSettingsAsync();
                if (settings != null)
                {
                    InstallationId = settings.InstallationId ?? "Not Set";
                    TelemetryEnabled = settings.TelemetryEnabled;
                    LocalLoggingEnabled = settings.LocalLoggingEnabled;
                    LogRetentionDays = settings.LogRetentionDays ?? 30;
                    MinimumLogLevel = settings.MinimumLogLevel ?? "Information";
                    IncludeUserInformation = settings.IncludeUserInformation;
                    IncludeSystemInformation = settings.IncludeSystemInformation;
                }

                // Get current log count
                CurrentLogCount = await GetLogCountAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading telemetry settings");
                ErrorMessage = "Error loading settings. Please try again.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CurrentLogCount = await GetLogCountAsync();
                ErrorMessage = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                var settings = await _systemSettingsService.GetSettingsAsync();
                if (settings == null)
                {
                    ErrorMessage = "System settings not found. Please complete initial setup.";
                    return Page();
                }

                settings.TelemetryEnabled = TelemetryEnabled;
                settings.LocalLoggingEnabled = LocalLoggingEnabled;
                settings.LogRetentionDays = LogRetentionDays;
                settings.MinimumLogLevel = MinimumLogLevel;
                settings.IncludeUserInformation = IncludeUserInformation;
                settings.IncludeSystemInformation = IncludeSystemInformation;

                await _systemSettingsService.UpdateSettingsAsync(settings);

                InstallationId = settings.InstallationId ?? "Not Set";
                CurrentLogCount = await GetLogCountAsync();
                Message = "Telemetry settings updated successfully.";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving telemetry settings");
                ErrorMessage = "Error saving settings. Please try again.";
                CurrentLogCount = await GetLogCountAsync();
                return Page();
            }
        }

        private async Task<int> GetLogCountAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    return 0;

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table exists
                var tableExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SentinelLogs'");

                if (tableExists == 0)
                    return 0;

                var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SentinelLogs");
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
