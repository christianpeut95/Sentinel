using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Disease> Diseases { get; set; } = default!;
        public IList<DiseaseCategory> Categories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Bypass disease access filter for admin disease management
            // Admins need to see all diseases including restricted ones
            Categories = await _context.DiseaseCategories
                .Include(dc => dc.Diseases.OrderBy(d => d.PathIds))
                .OrderBy(dc => dc.DisplayOrder)
                .ThenBy(dc => dc.Name)
                .IgnoreQueryFilters()
                .ToListAsync();

            // Get uncategorized diseases
            Diseases = await _context.Diseases
                .Where(d => d.DiseaseCategoryId == null)
                .OrderBy(d => d.PathIds)
                .ThenBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .IgnoreQueryFilters()
                .ToListAsync();
        }
    }
}
