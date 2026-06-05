using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateLabConfigurationModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateLabConfigurationModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string ConfigurationName { get; set; } = string.Empty;

        [BindProperty]
        public string? Notes { get; set; }

        public void OnGet()
        {
            // Just show the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationName))
            {
                ModelState.AddModelError(nameof(ConfigurationName), "Lab name is required");
                return Page();
            }

            // Create new configuration
            var configuration = new HL7Configuration
            {
                ConfigurationName = ConfigurationName.Trim(),
                Notes = Notes?.Trim(),
                IsActive = false,
                Priority = 100,
                CreatedAt = DateTime.UtcNow
            };

            _context.HL7Configurations.Add(configuration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lab configuration '{ConfigurationName}' created. Now upload a sample message to configure field mappings.";

            // Redirect to ConfigureLab with the new ID
            return RedirectToPage("/Settings/HL7/FieldMappings/ConfigureLab", new { configId = configuration.Id });
        }
    }
}
