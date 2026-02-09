using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditTestTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTestTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestType TestType { get; set; } = default!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testType = await _context.TestTypes.FindAsync(id);

            if (testType == null)
            {
                return NotFound();
            }

            TestType = testType;

            // Get usage count
            UsageCount = await _context.LabResults
                .CountAsync(lr => lr.TestTypeId == id);

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
            var exists = await _context.TestTypes
                .AnyAsync(t => t.Name == TestType.Name && t.Id != TestType.Id);

            if (exists)
            {
                ModelState.AddModelError("TestType.Name", "A test type with this name already exists.");
                TempData["ErrorMessage"] = "A test type with this name already exists.";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.TestTypeId == TestType.Id);
                return Page();
            }

            try
            {
                _context.Attach(TestType).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test type '{TestType.Name}' updated successfully.";
                return RedirectToPage("./TestTypes");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TestTypeExists(TestType.Id))
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
                TempData["ErrorMessage"] = $"An error occurred while updating the test type: {ex.Message}";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.TestTypeId == TestType.Id);
                return Page();
            }
        }

        private async Task<bool> TestTypeExists(int id)
        {
            return await _context.TestTypes.AnyAsync(e => e.Id == id);
        }
    }
}
