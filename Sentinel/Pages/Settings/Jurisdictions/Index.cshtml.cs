using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Jurisdictions
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IJurisdictionService _jurisdictionService;
        private const int PageSize = 50;

        public IndexModel(ApplicationDbContext context, IJurisdictionService jurisdictionService)
        {
            _context = context;
            _jurisdictionService = jurisdictionService;
        }

        public List<Jurisdiction> Jurisdictions { get; set; } = new();
        public List<JurisdictionType> JurisdictionTypes { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public int? FilterTypeId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public async Task OnGetAsync()
        {
            // Load jurisdiction types for filter dropdown
            JurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

            // Build query
            var query = _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Include(j => j.ParentJurisdiction)
                .AsQueryable();

            // Apply type filter
            if (FilterTypeId.HasValue)
            {
                query = query.Where(j => j.JurisdictionTypeId == FilterTypeId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(j => 
                    j.Name.Contains(SearchTerm) || 
                    (j.Code != null && j.Code.Contains(SearchTerm)));
            }

            // Get total count
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Order and execute with pagination
            Jurisdictions = await query
                .OrderBy(j => j.JurisdictionType!.FieldNumber)
                .ThenBy(j => j.DisplayOrder)
                .ThenBy(j => j.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
