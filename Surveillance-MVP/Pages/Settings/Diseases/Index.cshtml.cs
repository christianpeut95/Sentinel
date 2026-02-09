using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Diseases
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
            Categories = await _context.DiseaseCategories
                .Include(dc => dc.Diseases.OrderBy(d => d.PathIds))
                .OrderBy(dc => dc.DisplayOrder)
                .ThenBy(dc => dc.Name)
                .ToListAsync();

            // Get uncategorized diseases
            Diseases = await _context.Diseases
                .Where(d => d.DiseaseCategoryId == null)
                .OrderBy(d => d.PathIds)
                .ThenBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .ToListAsync();
        }
    }
}
