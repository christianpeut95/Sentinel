using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Genders
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Gender> Genders { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Genders = await _context.Genders
                .OrderBy(g => g.DisplayOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();
        }
    }
}
