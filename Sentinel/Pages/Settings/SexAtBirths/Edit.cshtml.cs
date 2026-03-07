using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.SexAtBirths
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SexAtBirth SexAtBirth { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sexAtBirth = await _context.SexAtBirths.FirstOrDefaultAsync(m => m.Id == id);
            if (sexAtBirth == null)
            {
                return NotFound();
            }
            SexAtBirth = sexAtBirth;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(SexAtBirth).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sex at Birth '{SexAtBirth.Name}' has been updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SexAtBirthExists(SexAtBirth.Id))
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

        private bool SexAtBirthExists(int id)
        {
            return _context.SexAtBirths.Any(e => e.Id == id);
        }
    }
}
