using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class OrganizationTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrganizationTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<OrganizationType> OrganizationTypes { get; set; } = default!;
        public Dictionary<int, int> OrganizationCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.OrganizationTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(o => o.Name.Contains(SearchTerm) || 
                                        (o.Description != null && o.Description.Contains(SearchTerm)));
            }

            if (IsActive.HasValue)
            {
                query = query.Where(o => o.IsActive == IsActive.Value);
            }

            OrganizationTypes = await query
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.Organizations
                .Where(o => o.OrganizationTypeId != null)
                .GroupBy(o => o.OrganizationTypeId!.Value)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            OrganizationCounts = counts.ToDictionary(x => x.TypeId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var organizationType = await _context.OrganizationTypes.FindAsync(id);

            if (organizationType == null)
            {
                TempData["ErrorMessage"] = "Organization type not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.Organizations
                .CountAsync(o => o.OrganizationTypeId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{organizationType.Name}' because it is used by {usageCount} organization(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.OrganizationTypes.Remove(organizationType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Organization type '{organizationType.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting organization type: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
