using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditResultUnitModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditResultUnitModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ResultUnits ResultUnit { get; set; } = default!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resultUnit = await _context.ResultUnits.FindAsync(id);

            if (resultUnit == null)
            {
                return NotFound();
            }

            ResultUnit = resultUnit;

            // Get usage count
            UsageCount = await _context.LabResults
                .CountAsync(lr => lr.ResultUnitsId == id);

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
            var exists = await _context.ResultUnits
                .AnyAsync(r => r.Name == ResultUnit.Name && r.Id != ResultUnit.Id);

            if (exists)
            {
                ModelState.AddModelError("ResultUnit.Name", "A result unit with this name already exists.");
                TempData["ErrorMessage"] = "A result unit with this name already exists.";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.ResultUnitsId == ResultUnit.Id);
                return Page();
            }

            try
            {
                _context.Attach(ResultUnit).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Result unit '{ResultUnit.Name}' updated successfully.";
                return RedirectToPage("./ResultUnits");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ResultUnitExists(ResultUnit.Id))
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
                TempData["ErrorMessage"] = $"An error occurred while updating the result unit: {ex.Message}";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.ResultUnitsId == ResultUnit.Id);
                return Page();
            }
        }

        private async Task<bool> ResultUnitExists(int id)
        {
            return await _context.ResultUnits.AnyAsync(e => e.Id == id);
        }
    }
}
