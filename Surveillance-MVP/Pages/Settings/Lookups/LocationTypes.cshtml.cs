using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class LocationTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LocationTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LocationType> LocationTypes { get; set; } = default!;
        public Dictionary<int, int> LocationCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.LocationTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(l => l.Name.Contains(SearchTerm) || 
                                        (l.Description != null && l.Description.Contains(SearchTerm)));
            }

            if (IsActive.HasValue)
            {
                query = query.Where(l => l.IsActive == IsActive.Value);
            }

            LocationTypes = await query
                .OrderBy(l => l.DisplayOrder)
                .ThenBy(l => l.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.Locations
                .Where(l => l.LocationTypeId != null)
                .GroupBy(l => l.LocationTypeId!.Value)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            LocationCounts = counts.ToDictionary(x => x.TypeId, x => x.Count);
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var locationType = await _context.LocationTypes.FindAsync(id);

            if (locationType == null)
            {
                TempData["ErrorMessage"] = "Location type not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.Locations
                .CountAsync(l => l.LocationTypeId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{locationType.Name}' because it is used by {usageCount} location(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.LocationTypes.Remove(locationType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Location type '{locationType.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the location type: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
