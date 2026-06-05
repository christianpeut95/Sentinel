using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.HL7;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HL7CustomFieldMapping Mapping { get; set; } = new HL7CustomFieldMapping
        {
            IsActive = true,
            Priority = 10,
            ExtractQualitativeResult = true,
            ExtractQuantitativeResult = false
        };

        public SelectList DiseaseSelectList { get; set; } = null!;
        public SelectList CustomFieldSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid? diseaseId)
        {
            if (diseaseId.HasValue)
            {
                Mapping.DiseaseId = diseaseId.Value;
            }

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // Check for duplicate mapping
            var existingMapping = await _context.HL7CustomFieldMappings
                .AnyAsync(m => m.DiseaseId == Mapping.DiseaseId 
                            && m.HL7TestCode == Mapping.HL7TestCode 
                            && m.CustomFieldDefinitionId == Mapping.CustomFieldDefinitionId);

            if (existingMapping)
            {
                ModelState.AddModelError("Mapping.HL7TestCode", 
                    "A mapping for this test code, disease, and custom field already exists.");
                await LoadSelectListsAsync();
                return Page();
            }

            Mapping.CreatedAt = DateTime.UtcNow;
            Mapping.ModifiedAt = DateTime.UtcNow;

            _context.HL7CustomFieldMappings.Add(Mapping);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Field mapping for '{Mapping.HL7TestCode}' has been created successfully.";
            return RedirectToPage("./Index", new { diseaseId = Mapping.DiseaseId });
        }

        private async Task LoadSelectListsAsync()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            DiseaseSelectList = new SelectList(diseases, "Id", "Name");

            var customFields = await _context.CustomFieldDefinitions
                .Where(cf => cf.IsActive && cf.ShowOnCaseForm)
                .OrderBy(cf => cf.Category)
                .ThenBy(cf => cf.DisplayOrder)
                .ThenBy(cf => cf.Label)
                .ToListAsync();

            CustomFieldSelectList = new SelectList(
                customFields.Select(cf => new 
                { 
                    cf.Id, 
                    DisplayText = $"{cf.Category} - {cf.Label}" 
                }), 
                "Id", 
                "DisplayText"
            );
        }
    }
}
