using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    /// <summary>
    /// DEPRECATED: This Razor Page has been replaced by the Blazor-based wizard at /Settings/HL7/ConfigureLab/{id}
    /// This file only exists to redirect old bookmarks/links to the new location.
    /// </summary>
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    [Obsolete("Use the new Blazor wizard at /Settings/HL7/ConfigureLab/{id} instead")]
    public class ConfigureLabModel : PageModel
    {
        private readonly ILogger<ConfigureLabModel> _logger;

        public ConfigureLabModel(ILogger<ConfigureLabModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet(Guid? configId)
        {
            _logger.LogInformation("Old ConfigureLab Razor Page accessed - redirecting to new Blazor wizard");

            // Redirect to new Blazor-based wizard
            if (configId.HasValue && configId.Value != Guid.Empty)
            {
                return Redirect($"/Settings/HL7/ConfigureLab/{configId.Value}");
            }

            // If no configId, redirect to lab selection
            return RedirectToPage("/Settings/HL7/FieldMappings/SelectLab");
        }

        public IActionResult OnPost(Guid configId)
        {
            _logger.LogInformation("Old ConfigureLab POST handler - redirecting to new Blazor wizard");
            return Redirect($"/Settings/HL7/ConfigureLab/{configId}");
        }
    }
}
