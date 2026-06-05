using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class ReinfectionRulesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReinfectionRulesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DiseaseReinfectionRule> Rules { get; set; } = new();
        public List<Disease> AllDiseases { get; set; } = new();
        public Guid? SelectedDiseaseId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? diseaseId)
        {
            SelectedDiseaseId = diseaseId;

            // Load all active diseases for filter
            AllDiseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Load rules based on filter
            var query = _context.DiseaseReinfectionRules
                .Include(r => r.Disease)
                .AsQueryable();

            if (diseaseId.HasValue)
            {
                query = query.Where(r => r.DiseaseId == diseaseId.Value);
            }

            Rules = await query
                .OrderBy(r => r.Disease!.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid? diseaseId)
        {
            var rule = await _context.DiseaseReinfectionRules.FindAsync(id);
            if (rule != null)
            {
                _context.DiseaseReinfectionRules.Remove(rule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reinfection rule has been deleted successfully.";
            }

            return RedirectToPage(new { diseaseId });
        }
    }
}
