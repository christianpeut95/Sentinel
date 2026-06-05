using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class TestMethodsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestMethodsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TestMethod> TestMethods { get; set; } = new();
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.TestMethods.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(t =>
                    t.Name.Contains(SearchTerm) ||
                    (t.Description != null && t.Description.Contains(SearchTerm))
                );
            }

            if (IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == IsActive.Value);
            }

            TestMethods = await query
                .OrderBy(t => t.DisplayOrder ?? int.MaxValue)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Get usage counts
            var usageCounts = await _context.LabResultMarkers
                .Where(m => m.TestMethodId != null)
                .GroupBy(m => m.TestMethodId!.Value)
                .Select(g => new { TestMethodId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = usageCounts.ToDictionary(x => x.TestMethodId, x => x.Count);
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var testMethod = await _context.TestMethods.FindAsync(id);

            if (testMethod == null)
            {
                return NotFound();
            }

            // Check if it's used in any lab results
            var usageCount = await _context.LabResultMarkers
                .Where(m => m.TestMethodId == id)
                .CountAsync();

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{testMethod.Name}' because it is referenced in {usageCount} lab result(s).";
                return RedirectToPage();
            }

            _context.TestMethods.Remove(testMethod);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Test method '{testMethod.Name}' has been deleted successfully.";
            return RedirectToPage();
        }
    }
}
