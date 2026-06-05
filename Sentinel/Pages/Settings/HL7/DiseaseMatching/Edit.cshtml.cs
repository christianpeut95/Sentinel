using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.HL7;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.HL7.DiseaseMatching
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public Guid DiseaseId { get; set; }

        [BindProperty]
        public DiseaseHL7MatchingConfig Config { get; set; } = new();

        public Disease? Disease { get; set; }
        public string? ParentDiseaseName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Disease = await _context.Diseases
                .Include(d => d.ParentDisease)
                .Include(d => d.HL7MatchingConfig)
                .FirstOrDefaultAsync(d => d.Id == DiseaseId);

            if (Disease == null)
            {
                TempData["ErrorMessage"] = "Disease not found.";
                return RedirectToPage("./Index");
            }

            if (Disease.ParentDisease != null)
            {
                ParentDiseaseName = Disease.ParentDisease.Name;
            }

            Config = Disease.HL7MatchingConfig ?? new DiseaseHL7MatchingConfig
            {
                DiseaseId = DiseaseId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Debug: Log what we received
            System.Diagnostics.Debug.WriteLine($"POST - DiseaseId: {DiseaseId}");
            System.Diagnostics.Debug.WriteLine($"POST - TestMethod_UseTextFallback: {Config.TestMethod_UseTextFallback}");
            System.Diagnostics.Debug.WriteLine($"POST - ModelState.IsValid: {ModelState.IsValid}");

            // Debug: Log all ModelState errors
            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state?.Errors.Count > 0)
                {
                    foreach (var error in state.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error - Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            // Reload disease for display
            Disease = await _context.Diseases
                .Include(d => d.ParentDisease)
                .FirstOrDefaultAsync(d => d.Id == DiseaseId);

            if (Disease == null)
            {
                TempData["ErrorMessage"] = "Disease not found.";
                return RedirectToPage("./Index");
            }

            if (Disease.ParentDisease != null)
            {
                ParentDiseaseName = Disease.ParentDisease.Name;
            }

            // Remove Config.Disease from ModelState - it's a required navigation property that won't be posted
            ModelState.Remove("Config.Disease");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed. Please check your inputs.";
                return Page();
            }

            var existingConfig = await _context.DiseaseHL7MatchingConfigs
                .FirstOrDefaultAsync(c => c.DiseaseId == DiseaseId);

            if (existingConfig != null)
            {
                // Update existing
                existingConfig.OverrideParentRules = Config.OverrideParentRules;
                existingConfig.TestMethod_UseTextFallback = Config.TestMethod_UseTextFallback;
                existingConfig.TestMethod_NormalizeWhitespace = Config.TestMethod_NormalizeWhitespace;
                existingConfig.TestMethod_IgnorePunctuation = Config.TestMethod_IgnorePunctuation;
                existingConfig.TestMethod_CaseInsensitive = Config.TestMethod_CaseInsensitive;
                existingConfig.SpecimenType_UseTextFallback = Config.SpecimenType_UseTextFallback;
                existingConfig.SpecimenType_NormalizeWhitespace = Config.SpecimenType_NormalizeWhitespace;
                existingConfig.SpecimenType_IgnorePunctuation = Config.SpecimenType_IgnorePunctuation;
                existingConfig.SpecimenType_CaseInsensitive = Config.SpecimenType_CaseInsensitive;
                existingConfig.Pathogen_UseTextFallback = Config.Pathogen_UseTextFallback;
                existingConfig.Pathogen_NormalizeWhitespace = Config.Pathogen_NormalizeWhitespace;
                existingConfig.Pathogen_IgnorePunctuation = Config.Pathogen_IgnorePunctuation;
                existingConfig.Pathogen_CaseInsensitive = Config.Pathogen_CaseInsensitive;
                existingConfig.TestResult_UseTextFallback = Config.TestResult_UseTextFallback;
                existingConfig.TestResult_NormalizeWhitespace = Config.TestResult_NormalizeWhitespace;
                existingConfig.TestResult_IgnorePunctuation = Config.TestResult_IgnorePunctuation;
                existingConfig.TestResult_CaseInsensitive = Config.TestResult_CaseInsensitive;
                existingConfig.UpdatedAt = DateTime.UtcNow;
                existingConfig.UpdatedBy = User.Identity?.Name;
            }
            else
            {
                // Create new
                Config.DiseaseId = DiseaseId;
                Config.CreatedAt = DateTime.UtcNow;
                Config.CreatedBy = User.Identity?.Name;
                Config.UpdatedAt = null;
                Config.UpdatedBy = null;
                _context.DiseaseHL7MatchingConfigs.Add(Config);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "HL7 matching configuration saved successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
                TempData["ErrorMessage"] = $"Failed to save configuration: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var config = await _context.DiseaseHL7MatchingConfigs
                .FirstOrDefaultAsync(c => c.DiseaseId == DiseaseId);

            if (config != null)
            {
                _context.DiseaseHL7MatchingConfigs.Remove(config);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Configuration deleted. Disease will now inherit from parent.";
            }

            return RedirectToPage("./Index");
        }
    }
}
