using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Countries
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public IndexModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Country> Country { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Country = await _context.Countries.ToListAsync();
        }
    }
}
