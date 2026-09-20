using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditSpecimenTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditSpecimenTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SpecimenType SpecimenType { get; set; } = default!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var specimenType = await _context.SpecimenTypes.FindAsync(id);

            if (specimenType == null)
            {
                return NotFound();
            }

            SpecimenType = specimenType;

            // Get usage count
            UsageCount = await _context.LabResults
                .CountAsync(lr => lr.SpecimenTypeId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name (excluding current record)
            var exists = await _context.SpecimenTypes
                .AnyAsync(s => s.Name == SpecimenType.Name && s.Id != SpecimenType.Id);

            if (exists)
            {
                ModelState.AddModelError("SpecimenType.Name", "A specimen type with this name already exists.");
                TempData["ErrorMessage"] = "A specimen type with this name already exists.";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.SpecimenTypeId == SpecimenType.Id);
                return Page();
            }

            try
            {
                var specimenTypeToUpdate = await _context.SpecimenTypes.FindAsync(SpecimenType.Id);
                if (specimenTypeToUpdate == null)
                {
                    return NotFound();
                }

                specimenTypeToUpdate.Name = SpecimenType.Name;
                specimenTypeToUpdate.Description = SpecimenType.Description;
                specimenTypeToUpdate.SnomedCode = SpecimenType.SnomedCode;
                specimenTypeToUpdate.SnomedDisplay = SpecimenType.SnomedDisplay;
                specimenTypeToUpdate.LoincSystemCode = SpecimenType.LoincSystemCode;
                specimenTypeToUpdate.Hl7Code = SpecimenType.Hl7Code;
                specimenTypeToUpdate.BodySite = SpecimenType.BodySite;
                specimenTypeToUpdate.CollectionMethod = SpecimenType.CollectionMethod;
                specimenTypeToUpdate.ExportCode = SpecimenType.ExportCode;
                specimenTypeToUpdate.IsInvasive = SpecimenType.IsInvasive;
                specimenTypeToUpdate.IsSterileSite = SpecimenType.IsSterileSite;
                specimenTypeToUpdate.DisplayOrder = SpecimenType.DisplayOrder;
                specimenTypeToUpdate.IsActive = SpecimenType.IsActive;
                specimenTypeToUpdate.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Specimen type '{specimenTypeToUpdate.Name}' updated successfully.";
                return RedirectToPage("./SpecimenTypes");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SpecimenTypeExists(SpecimenType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
                UsageCount = await _context.LabResults.CountAsync(lr => lr.SpecimenTypeId == SpecimenType.Id);
                return Page();
            }
        }

        private async Task<bool> SpecimenTypeExists(int id)
        {
            return await _context.SpecimenTypes.AnyAsync(e => e.Id == id);
        }
    }
}
