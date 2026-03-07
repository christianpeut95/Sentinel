using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Disease Disease { get; set; } = default!;
        public IList<Disease> SubDiseases { get; set; } = default!;
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

            SubDiseases = await _context.Diseases
                .Where(d => d.ParentDiseaseId == id)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .ToListAsync();

            CaseCount = await _context.Cases
                .Where(c => c.Disease.PathIds.Contains($"/{id}/"))
                .CountAsync();

            return Page();
        }
    }
}
