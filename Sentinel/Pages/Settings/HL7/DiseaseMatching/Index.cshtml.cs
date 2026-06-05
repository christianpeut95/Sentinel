using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.HL7.DiseaseMatching
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Disease> Diseases { get; set; } = new();

        public async Task OnGetAsync()
        {
            Diseases = await _context.Diseases
                .Include(d => d.HL7MatchingConfig)
                .Include(d => d.SubDiseases)
                .Where(d => d.IsActive)
                .OrderBy(d => d.Level)
                .ThenBy(d => d.DisplayOrder)
                .ToListAsync();
        }
    }
}
