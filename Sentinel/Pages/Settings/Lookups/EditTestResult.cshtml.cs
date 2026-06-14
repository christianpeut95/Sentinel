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

            _context.Attach(TestResult).State = EntityState.Modified;

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

            TempData["SuccessMessage"] = $"Test result '{TestResult.Name}' has been updated successfully.";
            return RedirectToPage("./TestResults");
        }

        private async Task<bool> TestResultExistsAsync(int id)
        {
            return await _context.TestResults.AnyAsync(e => e.Id == id);
        }
    }
}
