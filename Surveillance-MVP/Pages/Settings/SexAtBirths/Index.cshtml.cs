using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.SexAtBirths
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SexAtBirth> SexAtBirths { get;set; } = default!;

        public async Task OnGetAsync()
        {
            SexAtBirths = await _context.SexAtBirths
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
    }
}
