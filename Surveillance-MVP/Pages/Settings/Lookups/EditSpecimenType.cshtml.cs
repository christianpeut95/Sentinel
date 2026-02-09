using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
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
                _context.Attach(SpecimenType).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Specimen type '{SpecimenType.Name}' updated successfully.";
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
                TempData["ErrorMessage"] = $"An error occurred while updating the specimen type: {ex.Message}";
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
