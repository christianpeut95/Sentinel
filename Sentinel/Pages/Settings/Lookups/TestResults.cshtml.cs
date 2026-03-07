using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class TestResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TestResult> TestResults { get; set; } = default!;
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.TestResults.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(t => t.Name.Contains(SearchTerm) || 
                                        (t.Description != null && t.Description.Contains(SearchTerm)) ||
                                        (t.ExportCode != null && t.ExportCode.Contains(SearchTerm)));
            }

            if (IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == IsActive.Value);
            }

            TestResults = await query
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.LabResults
                .Where(lr => lr.TestResultId != null)
                .GroupBy(lr => lr.TestResultId!.Value)
                .Select(g => new { ResultId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = counts.ToDictionary(x => x.ResultId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);

            if (testResult == null)
            {
                TempData["ErrorMessage"] = "Test result not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.LabResults
                .CountAsync(lr => lr.TestResultId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{testResult.Name}' because it is used in {usageCount} laboratory result(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.TestResults.Remove(testResult);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test result '{testResult.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting test result: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
