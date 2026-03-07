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
    public class DetailsModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;

        public DetailsModel(Sentinel.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Ancestry Ancestry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ancestry = await _context.Ancestries.FirstOrDefaultAsync(m => m.Id == id);

            if (ancestry is not null)
            {
                Ancestry = ancestry;
                return Page();
            }

            return NotFound();
        }
    }
}
