using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings
{
    [Authorize(Roles = "Admin")]
    public class FeedbackModel : PageModel
    {
        private readonly ISystemSettingsService _settingsService;
        private readonly ILogger<FeedbackModel> _logger;

        public FeedbackModel(
            ISystemSettingsService settingsService,
            ILogger<FeedbackModel> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [BindProperty]
        public bool EnableFeedbackWidget { get; set; }

        [BindProperty]
        public bool EnableUsageMonitoring { get; set; }

        public string? InstallationId { get; set; }
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                EnableFeedbackWidget = await _settingsService.GetFeedbackWidgetEnabledAsync();
                EnableUsageMonitoring = await _settingsService.GetUsageMonitoringEnabledAsync();
                InstallationId = await _settingsService.GetInstallationIdAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load feedback settings");
                Message = "Failed to load settings. Please try again.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _settingsService.UpdateFeedbackWidgetSettingAsync(EnableFeedbackWidget);
                await _settingsService.UpdateUsageMonitoringSettingAsync(EnableUsageMonitoring);

                _logger.LogInformation(
                    "Feedback settings updated - Widget: {Widget}, Usage Monitoring: {Usage} by {User}",
                    EnableFeedbackWidget,
                    EnableUsageMonitoring,
                    User.Identity?.Name);

                Message = "Feedback settings saved successfully.";

                // Reload installation ID for display
                InstallationId = await _settingsService.GetInstallationIdAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save feedback settings");
                ModelState.AddModelError(string.Empty, "Failed to save settings. Please try again.");
                return Page();
            }
        }
    }
}
