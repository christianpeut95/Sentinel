using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditTestResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTestResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestResult TestResult { get; set; } = null!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TestResult = await _context.TestResults.FirstOrDefaultAsync(m => m.Id == id);

            if (TestResult == null)
            {
                return NotFound();
            }

            // Count usage in LabResultMarkers
            var labMarkerUsage = await _context.LabResultMarkers
                .Where(m => m.TestResultId == id)
                .CountAsync();

            UsageCount = labMarkerUsage;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var testResultToUpdate = await _context.TestResults.FindAsync(TestResult.Id);
            if (testResultToUpdate == null)
            {
                return NotFound();
            }

            // TestTypeId is configured separately and is not a field on this form.
            testResultToUpdate.Name = TestResult.Name;
            testResultToUpdate.Description = TestResult.Description;
            testResultToUpdate.SnomedCode = TestResult.SnomedCode;
            testResultToUpdate.SnomedDisplay = TestResult.SnomedDisplay;
            testResultToUpdate.Hl7Code = TestResult.Hl7Code;
            testResultToUpdate.ExportCode = TestResult.ExportCode;
            testResultToUpdate.DisplayOrder = TestResult.DisplayOrder;
            testResultToUpdate.IsActive = TestResult.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TestResultExistsAsync(TestResult.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = $"Test result '{testResultToUpdate.Name}' has been updated successfully.";
            return RedirectToPage("./TestResults");
        }

        private async Task<bool> TestResultExistsAsync(int id)
        {
            return await _context.TestResults.AnyAsync(e => e.Id == id);
        }
    }
}
