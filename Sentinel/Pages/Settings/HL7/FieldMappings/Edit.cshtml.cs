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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HL7CustomFieldMapping Mapping { get; set; } = null!;

        public SelectList DiseaseSelectList { get; set; } = null!;
        public SelectList CustomFieldSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var mapping = await _context.HL7CustomFieldMappings.FindAsync(id);
            if (mapping == null)
            {
                return NotFound();
            }

            Mapping = mapping;
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

            var existingMapping = await _context.HL7CustomFieldMappings
                .FirstOrDefaultAsync(m => m.Id == Mapping.Id);

            if (existingMapping == null)
            {
                return NotFound();
            }

            // Check for duplicate mapping (excluding current)
            var duplicateExists = await _context.HL7CustomFieldMappings
                .AnyAsync(m => m.Id != Mapping.Id 
                            && m.DiseaseId == Mapping.DiseaseId 
                            && m.HL7TestCode == Mapping.HL7TestCode 
                            && m.CustomFieldDefinitionId == Mapping.CustomFieldDefinitionId);

            if (duplicateExists)
            {
                ModelState.AddModelError("Mapping.HL7TestCode", 
                    "A mapping for this test code, disease, and custom field already exists.");
                await LoadSelectListsAsync();
                return Page();
            }

            // Update properties
            existingMapping.DiseaseId = Mapping.DiseaseId;
            existingMapping.HL7TestCode = Mapping.HL7TestCode;
            existingMapping.TestCodeDescription = Mapping.TestCodeDescription;
            existingMapping.CustomFieldDefinitionId = Mapping.CustomFieldDefinitionId;
            existingMapping.ExtractQualitativeResult = Mapping.ExtractQualitativeResult;
            existingMapping.ExtractQuantitativeResult = Mapping.ExtractQuantitativeResult;
            existingMapping.ValueTransformation = Mapping.ValueTransformation;
            existingMapping.Priority = Mapping.Priority;
            existingMapping.Notes = Mapping.Notes;
            existingMapping.IsActive = Mapping.IsActive;
            existingMapping.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Field mapping for '{Mapping.HL7TestCode}' has been updated successfully.";
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
