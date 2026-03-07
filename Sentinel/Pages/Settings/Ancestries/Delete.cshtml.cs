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
    public class DeleteModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;

        public DeleteModel(Sentinel.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ancestry = await _context.Ancestries.FindAsync(id);
            if (ancestry != null)
            {
                Ancestry = ancestry;
                _context.Ancestries.Remove(Ancestry);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
