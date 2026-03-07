using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Jurisdictions
{
    // TEMPORARY: Authorization disabled for testing - re-enable in production
    // [Authorize(Policy = "Permission.Settings.Delete")]
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Jurisdiction Jurisdiction { get; set; } = default!;

        public int PatientCount { get; set; }
        public int CaseCount { get; set; }
        public bool CanDelete => PatientCount == 0 && CaseCount == 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jurisdiction = await _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Include(j => j.ParentJurisdiction)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jurisdiction == null)
            {
                return NotFound();
            }

            Jurisdiction = jurisdiction;

            // Check usage across all 5 jurisdiction fields
            PatientCount = await _context.Patients.CountAsync(p =>
                p.Jurisdiction1Id == id ||
                p.Jurisdiction2Id == id ||
                p.Jurisdiction3Id == id ||
                p.Jurisdiction4Id == id ||
                p.Jurisdiction5Id == id);

            CaseCount = await _context.Cases.CountAsync(c =>
                c.Jurisdiction1Id == id ||
                c.Jurisdiction2Id == id ||
                c.Jurisdiction3Id == id ||
                c.Jurisdiction4Id == id ||
                c.Jurisdiction5Id == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jurisdiction = await _context.Jurisdictions.FindAsync(id);

            if (jurisdiction == null)
            {
                return NotFound();
            }

            // Re-check usage before deletion
            var patientCount = await _context.Patients.CountAsync(p =>
                p.Jurisdiction1Id == id ||
                p.Jurisdiction2Id == id ||
                p.Jurisdiction3Id == id ||
                p.Jurisdiction4Id == id ||
                p.Jurisdiction5Id == id);

            var caseCount = await _context.Cases.CountAsync(c =>
                c.Jurisdiction1Id == id ||
                c.Jurisdiction2Id == id ||
                c.Jurisdiction3Id == id ||
                c.Jurisdiction4Id == id ||
                c.Jurisdiction5Id == id);

            if (patientCount > 0 || caseCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{jurisdiction.Name}' because it is assigned to {patientCount} patient(s) and {caseCount} case(s). Consider deactivating it instead.";
                return RedirectToPage("./Index");
            }

            _context.Jurisdictions.Remove(jurisdiction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Jurisdiction '{jurisdiction.Name}' deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
