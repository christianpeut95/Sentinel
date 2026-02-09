using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Diseases
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
        public Disease Disease { get; set; } = default!;
        public bool HasSubDiseases { get; set; }
        public int SubDiseaseCount { get; set; }
        public int CaseCount { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disease = await _context.Diseases
                .Include(d => d.ParentDisease)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (disease == null)
            {
                return NotFound();
            }

            Disease = disease;

            SubDiseaseCount = await _context.Diseases.CountAsync(d => d.ParentDiseaseId == id);
            HasSubDiseases = SubDiseaseCount > 0;

            CaseCount = await _context.Cases
                .Where(c => c.Disease.PathIds.Contains($"/{id}/"))
                .CountAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disease = await _context.Diseases.FindAsync(id);
            if (disease == null)
            {
                return NotFound();
            }

            var subDiseaseCount = await _context.Diseases.CountAsync(d => d.ParentDiseaseId == id);
            if (subDiseaseCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{disease.Name}'. It has {subDiseaseCount} sub-type(s).";
                return RedirectToPage("./Index");
            }

            var caseCount = await _context.Cases
                .Where(c => c.Disease.PathIds.Contains($"/{id}/"))
                .CountAsync();

            if (caseCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{disease.Name}'. It has {caseCount} associated case(s).";
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Diseases.Remove(disease);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Disease '{disease.Name}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToPage("./Index");
        }
    }
}
