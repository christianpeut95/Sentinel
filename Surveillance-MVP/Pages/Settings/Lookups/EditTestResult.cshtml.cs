using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditTestResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTestResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestResult TestResult { get; set; } = default!;

        public int UsageCount { get; set; }
        public SelectList TestTypesList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testResult = await _context.TestResults
                .Include(tr => tr.TestType)
                .FirstOrDefaultAsync(tr => tr.Id == id);

            if (testResult == null)
            {
                return NotFound();
            }

            TestResult = testResult;

            // Get usage count
            UsageCount = await _context.LabResults
                .CountAsync(lr => lr.TestResultId == id);

            await LoadTestTypes();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTestTypes();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name within the same test type (excluding current record)
            var exists = await _context.TestResults
                .AnyAsync(t => t.Name == TestResult.Name && t.TestTypeId == TestResult.TestTypeId && t.Id != TestResult.Id);

            if (exists)
            {
                ModelState.AddModelError("TestResult.Name", "A test result with this name already exists for this test type.");
                await LoadTestTypes();
                TempData["ErrorMessage"] = "A test result with this name already exists for this test type.";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.TestResultId == TestResult.Id);
                return Page();
            }

            try
            {
                _context.Attach(TestResult).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test result '{TestResult.Name}' updated successfully.";
                return RedirectToPage("./TestResults");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TestResultExists(TestResult.Id))
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
                await LoadTestTypes();
                TempData["ErrorMessage"] = $"An error occurred while updating the test result: {ex.Message}";
                UsageCount = await _context.LabResults.CountAsync(lr => lr.TestResultId == TestResult.Id);
                return Page();
            }
        }

        private async Task LoadTestTypes()
        {
            TestTypesList = new SelectList(
                await _context.TestTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );
        }

        private async Task<bool> TestResultExists(int id)
        {
            return await _context.TestResults.AnyAsync(e => e.Id == id);
        }
    }
}
