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

namespace Sentinel.Pages.Settings.Ancestries
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;

        public IndexModel(Sentinel.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Ancestry> Ancestries { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Ancestries = await _context.Ancestries.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name).ToListAsync();
        }
    }
}
