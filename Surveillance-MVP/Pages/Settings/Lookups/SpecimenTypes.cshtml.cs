using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class SpecimenTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SpecimenTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SpecimenType> SpecimenTypes { get; set; } = default!;
        public Dictionary<int, int> LabResultCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsInvasive { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.SpecimenTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(s => s.Name.Contains(SearchTerm) || 
                                        (s.Description != null && s.Description.Contains(SearchTerm)) ||
                                        (s.ExportCode != null && s.ExportCode.Contains(SearchTerm)));
            }

            if (IsInvasive.HasValue)
            {
                query = query.Where(s => s.IsInvasive == IsInvasive.Value);
            }

            if (IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == IsActive.Value);
            }

            SpecimenTypes = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.LabResults
                .Where(lr => lr.SpecimenTypeId != null)
                .GroupBy(lr => lr.SpecimenTypeId!.Value)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            LabResultCounts = counts.ToDictionary(x => x.TypeId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var specimenType = await _context.SpecimenTypes.FindAsync(id);

            if (specimenType == null)
            {
                TempData["ErrorMessage"] = "Specimen type not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.LabResults
                .CountAsync(lr => lr.SpecimenTypeId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{specimenType.Name}' because it is used in {usageCount} laboratory result(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.SpecimenTypes.Remove(specimenType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Specimen type '{specimenType.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting specimen type: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
