using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Pathogens;

namespace Sentinel.Pages.Settings.Pathogens
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
        public Pathogen Pathogen { get; set; } = null!;

        public int UsageCount { get; set; }
        public bool CannotDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Pathogen = await _context.Pathogens
                .Include(p => p.Disease)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Pathogen == null)
            {
                return NotFound();
            }

            // Check if pathogen is used in any lab results
            UsageCount = await _context.LabResultMarkers
                .Where(m => m.PathogenId == id)
                .CountAsync();

            CannotDelete = UsageCount > 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Pathogen = await _context.Pathogens.FindAsync(id);

            if (Pathogen == null)
            {
                return NotFound();
            }

            // Double-check usage before deletion
            UsageCount = await _context.LabResultMarkers
                .Where(m => m.PathogenId == id)
                .CountAsync();

            if (UsageCount > 0)
            {
                CannotDelete = true;
                TempData["ErrorMessage"] = "Cannot delete this pathogen because it is referenced in lab results.";
                return RedirectToPage("./Index");
            }

            _context.Pathogens.Remove(Pathogen);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Pathogen '{Pathogen.Name}' has been deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
