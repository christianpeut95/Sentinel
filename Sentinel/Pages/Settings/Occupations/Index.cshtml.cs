using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Occupations
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 20;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Occupation> Occupation { get;set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? MajorGroupFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync()
        {
            var query = _context.Occupations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(o => 
                    o.Code.Contains(SearchTerm) || 
                    o.Name.Contains(SearchTerm) ||
                    o.MajorGroupName!.Contains(SearchTerm) ||
                    o.MinorGroupName!.Contains(SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(MajorGroupFilter))
            {
                query = query.Where(o => o.MajorGroupCode == MajorGroupFilter);
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (PageIndex < 1) PageIndex = 1;
            if (PageIndex > TotalPages && TotalPages > 0) PageIndex = TotalPages;

            Occupation = await query
                .OrderBy(o => o.Code)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
