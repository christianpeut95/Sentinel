using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Ethnicities
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public IndexModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Ethnicity> Ethnicity { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Ethnicity = await _context.Ethnicities.ToListAsync();
        }
    }
}
