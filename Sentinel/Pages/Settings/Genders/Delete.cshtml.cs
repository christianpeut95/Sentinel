using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Genders
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
        public Gender Gender { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gender = await _context.Genders.FirstOrDefaultAsync(m => m.Id == id);

            if (gender == null)
            {
                return NotFound();
            }
            else
            {
                Gender = gender;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gender = await _context.Genders.FindAsync(id);
            if (gender != null)
            {
                Gender = gender;
                _context.Genders.Remove(Gender);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Gender '{Gender.Name}' has been deleted successfully.";
            }

            return RedirectToPage("./Index");
        }
    }
}
