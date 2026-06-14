using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class TestResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TestResult> TestResults { get; set; } = new();
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.TestResults.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(t =>
                    t.Name.Contains(SearchTerm) ||
                    (t.Description != null && t.Description.Contains(SearchTerm)) ||
                    (t.SnomedCode != null && t.SnomedCode.Contains(SearchTerm)) ||
                    (t.SnomedDisplay != null && t.SnomedDisplay.Contains(SearchTerm))
                );
            }

            if (IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == IsActive.Value);
            }

            TestResults = await query
                .OrderBy(t => t.DisplayOrder ?? int.MaxValue)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Get usage counts from LabResultMarkers
            var labResultMarkerCounts = await _context.LabResultMarkers
                .Where(m => m.TestResultId != null)
                .GroupBy(m => m.TestResultId!.Value)
                .Select(g => new { TestResultId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = labResultMarkerCounts.ToDictionary(x => x.TestResultId, x => x.Count);
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);

            if (testResult == null)
            {
                return NotFound();
            }

            // Check if it's used in any lab result markers
            var labMarkerUsage = await _context.LabResultMarkers
                .Where(m => m.TestResultId == id)
                .CountAsync();

            if (labMarkerUsage > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{testResult.Name}' because it is referenced in {labMarkerUsage} lab result marker(s).";
                return RedirectToPage();
            }

            _context.TestResults.Remove(testResult);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Test result '{testResult.Name}' has been deleted successfully.";
            return RedirectToPage();
        }
    }
}
