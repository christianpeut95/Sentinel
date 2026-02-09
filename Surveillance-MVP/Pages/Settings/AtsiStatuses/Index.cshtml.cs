using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.AtsiStatuses
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<AboriginalTorresStraitIslanderStatus> AtsiStatuses { get;set; } = default!;

        public async Task OnGetAsync()
        {
            AtsiStatuses = await _context.AtsiStatuses
                .OrderBy(a => a.DisplayOrder)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }
    }
}
