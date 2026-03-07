using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class ResultUnitsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResultUnitsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ResultUnits> ResultUnits { get; set; } = default!;
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.ResultUnits.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(r => r.Name.Contains(SearchTerm) || 
                                        (r.Abbreviation != null && r.Abbreviation.Contains(SearchTerm)) ||
                                        (r.Description != null && r.Description.Contains(SearchTerm)));
            }

            if (IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == IsActive.Value);
            }

            ResultUnits = await query
                .OrderBy(r => r.DisplayOrder)
                .ThenBy(r => r.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.LabResults
                .Where(lr => lr.ResultUnitsId != null)
                .GroupBy(lr => lr.ResultUnitsId!.Value)
                .Select(g => new { UnitsId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = counts.ToDictionary(x => x.UnitsId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var resultUnit = await _context.ResultUnits.FindAsync(id);

            if (resultUnit == null)
            {
                TempData["ErrorMessage"] = "Result unit not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.LabResults
                .CountAsync(lr => lr.ResultUnitsId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{resultUnit.Name}' because it is used in {usageCount} laboratory result(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.ResultUnits.Remove(resultUnit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Result unit '{resultUnit.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting result unit: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
