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

namespace Surveillance_MVP.Pages.Settings.Languages
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DetailsModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public DetailsModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Language Language { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await _context.Languages.FirstOrDefaultAsync(m => m.Id == id);

            if (language is not null)
            {
                Language = language;

                return Page();
            }

            return NotFound();
        }
    }
}
