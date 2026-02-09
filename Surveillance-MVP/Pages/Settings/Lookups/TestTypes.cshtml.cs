using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class TestTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TestTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TestType> TestTypes { get; set; } = default!;
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.TestTypes.AsQueryable();

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

            TestTypes = await query
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.LabResults
                .Where(lr => lr.TestTypeId != null)
                .GroupBy(lr => lr.TestTypeId!.Value)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = counts.ToDictionary(x => x.TypeId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var testType = await _context.TestTypes.FindAsync(id);

            if (testType == null)
            {
                TempData["ErrorMessage"] = "Test type not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.LabResults
                .CountAsync(lr => lr.TestTypeId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{testType.Name}' because it is used in {usageCount} laboratory result(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.TestTypes.Remove(testType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test type '{testType.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting test type: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
