using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Ethnicities
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditModel : PageModel
    {
        private readonly Surveillance_MVP.Data.ApplicationDbContext _context;

        public EditModel(Surveillance_MVP.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Ethnicity Ethnicity { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ethnicity =  await _context.Ethnicities.FirstOrDefaultAsync(m => m.Id == id);
            if (ethnicity == null)
            {
                return NotFound();
            }
            Ethnicity = ethnicity;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Ethnicity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EthnicityExists(Ethnicity.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool EthnicityExists(int id)
        {
            return _context.Ethnicities.Any(e => e.Id == id);
        }
    }
}
