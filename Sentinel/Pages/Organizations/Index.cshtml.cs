using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Organizations
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Organization> Organizations { get; set; } = default!;
        public SelectList OrganizationTypesList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? OrganizationTypeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Organizations
                .Include(o => o.OrganizationType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(o => o.Name.Contains(SearchTerm) || 
                                        (o.ContactPerson != null && o.ContactPerson.Contains(SearchTerm)));
            }

            if (OrganizationTypeId.HasValue)
            {
                query = query.Where(o => o.OrganizationTypeId == OrganizationTypeId.Value);
            }

            if (IsActive.HasValue)
            {
                query = query.Where(o => o.IsActive == IsActive.Value);
            }
            else
            {
                query = query.Where(o => o.IsActive);
            }

            Organizations = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            OrganizationTypesList = new SelectList(
                await _context.OrganizationTypes
                    .Where(ot => ot.IsActive)
                    .OrderBy(ot => ot.DisplayOrder)
                    .ThenBy(ot => ot.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                OrganizationTypeId
            );
        }
    }
}
