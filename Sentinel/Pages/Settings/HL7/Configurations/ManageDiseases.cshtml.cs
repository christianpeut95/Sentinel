using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.HL7.Configurations
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class ManageDiseasesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageDiseasesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public HL7Configuration Configuration { get; set; } = null!;
        public List<HL7ConfigurationDisease> ConfigurationDiseases { get; set; } = new();
        public List<HL7FieldMapping> DefaultMappings { get; set; } = new();
        public List<Disease> AvailableDiseases { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Configuration = await _context.HL7Configurations
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Configuration == null)
            {
                return NotFound();
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddDiseaseAsync(Guid id, Guid diseaseId, int priority, bool isDefault, string? notes)
        {
            Configuration = await _context.HL7Configurations.FindAsync(id);
            if (Configuration == null)
            {
                return NotFound();
            }

            // Check if association already exists
            var existing = await _context.HL7ConfigurationDiseases
                .FirstOrDefaultAsync(cd => cd.ConfigurationId == id && cd.DiseaseId == diseaseId);

            if (existing != null)
            {
                TempData["ErrorMessage"] = "This disease is already associated with this configuration.";
                await LoadDataAsync();
                return Page();
            }

            // If this is being set as default, remove default from others
            if (isDefault)
            {
                var currentDefaults = await _context.HL7ConfigurationDiseases
                    .Where(cd => cd.ConfigurationId == id && cd.IsDefault)
                    .ToListAsync();

                foreach (var cd in currentDefaults)
                {
                    cd.IsDefault = false;
                }
            }

            var configDisease = new HL7ConfigurationDisease
            {
                ConfigurationId = id,
                DiseaseId = diseaseId,
                Priority = priority,
                IsDefault = isDefault,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.HL7ConfigurationDiseases.Add(configDisease);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Disease association added successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveDiseaseAsync(Guid id, Guid diseaseId)
        {
            var configDisease = await _context.HL7ConfigurationDiseases
                .FirstOrDefaultAsync(cd => cd.ConfigurationId == id && cd.DiseaseId == diseaseId);

            if (configDisease == null)
            {
                return NotFound();
            }

            // Check if there are field mappings using this disease
            var mappingsCount = await _context.HL7FieldMappings
                .CountAsync(m => m.ConfigurationId == id && m.DiseaseId == diseaseId);

            if (mappingsCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot remove disease: {mappingsCount} field mapping(s) are configured for this disease. Remove the mappings first.";
                await LoadDataAsync();
                return Page();
            }

            _context.HL7ConfigurationDiseases.Remove(configDisease);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Disease association removed successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEnableTestModeAsync(Guid id)
        {
            var config = await _context.HL7Configurations.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            config.IsTestMode = true;
            config.TestModeDescription = $"Test mode enabled by {User.Identity?.Name} on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            config.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test mode enabled. HL7 messages will be staged but not committed.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDisableTestModeAsync(Guid id)
        {
            var config = await _context.HL7Configurations.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            config.IsTestMode = false;
            config.TestModeDescription = $"Test mode disabled by {User.Identity?.Name} on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            config.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test mode disabled. HL7 messages will now be processed normally.";
            return RedirectToPage(new { id });
        }

        private async Task LoadDataAsync()
        {
            ConfigurationDiseases = await _context.HL7ConfigurationDiseases
                .Include(cd => cd.Disease)
                .Where(cd => cd.ConfigurationId == Configuration.Id)
                .OrderByDescending(cd => cd.Priority)
                .ToListAsync();

            DefaultMappings = await _context.HL7FieldMappings
                .Where(m => m.ConfigurationId == Configuration.Id && m.DiseaseId == null && m.IsActive)
                .OrderByDescending(m => m.Priority)
                .Take(10)
                .ToListAsync();

            var associatedDiseaseIds = ConfigurationDiseases.Select(cd => cd.DiseaseId).ToList();
            AvailableDiseases = await _context.Diseases
                .Where(d => !associatedDiseaseIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}
