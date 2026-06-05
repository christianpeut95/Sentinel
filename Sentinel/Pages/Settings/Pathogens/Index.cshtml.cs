using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;

namespace Sentinel.Pages.Settings.Pathogens
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Pathogen> Pathogens { get; set; } = new();
        public List<Disease> Diseases { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? DiseaseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int ActiveCount { get; set; }
        public int WithLoincCount { get; set; }
        public int DiseasesCount { get; set; }

        public async Task OnGetAsync()
        {
            // Load diseases for filter dropdown
            Diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Build query
            var query = _context.Pathogens
                .Include(p => p.Disease)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(SearchTerm) ||
                    (p.ShortName != null && p.ShortName.Contains(SearchTerm)) ||
                    (p.LOINCCode != null && p.LOINCCode.Contains(SearchTerm))
                );
            }

            if (DiseaseId.HasValue)
            {
                query = query.Where(p => p.DiseaseId == DiseaseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(Category) && Enum.TryParse<PathogenCategory>(Category, out var categoryEnum))
            {
                query = query.Where(p => p.Category == categoryEnum);
            }

            // Get counts
            TotalCount = await query.CountAsync();
            ActiveCount = await query.Where(p => p.IsActive).CountAsync();
            WithLoincCount = await query.Where(p => !string.IsNullOrEmpty(p.LOINCCode)).CountAsync();
            DiseasesCount = await query.Where(p => p.DiseaseId != null).Select(p => p.DiseaseId).Distinct().CountAsync();

            // Calculate pagination
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages > 0 ? TotalPages : 1));

            // Get paginated results
            Pathogens = await query
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
