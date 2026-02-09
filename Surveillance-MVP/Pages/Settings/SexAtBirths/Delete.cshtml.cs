using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.SexAtBirths
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
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
            else
            {
                SexAtBirth = sexAtBirth;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sexAtBirth = await _context.SexAtBirths.FindAsync(id);
            if (sexAtBirth != null)
            {
                SexAtBirth = sexAtBirth;
                _context.SexAtBirths.Remove(SexAtBirth);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sex at Birth '{SexAtBirth.Name}' has been deleted successfully.";
            }

            return RedirectToPage("./Index");
        }
    }
}
