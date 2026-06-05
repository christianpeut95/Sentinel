using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Pathogens;

namespace Sentinel.Pages.Settings.Pathogens
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
        public Pathogen Pathogen { get; set; } = null!;

        public SelectList DiseaseSelectList { get; set; } = null!;
        public int UsageCount { get; set; }

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

            // Get usage count
            UsageCount = await _context.LabResultMarkers
                .Where(m => m.PathogenId == id)
                .CountAsync();

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // Check for duplicate LOINC code if provided
            if (!string.IsNullOrWhiteSpace(Pathogen.LOINCCode))
            {
                var existingLoinc = await _context.Pathogens
                    .AnyAsync(p => p.LOINCCode == Pathogen.LOINCCode && p.Id != Pathogen.Id);

                if (existingLoinc)
                {
                    ModelState.AddModelError("Pathogen.LOINCCode", "A pathogen with this LOINC code already exists.");
                    await LoadSelectListsAsync();
                    return Page();
                }
            }

            // Get usage count for validation
            UsageCount = await _context.LabResultMarkers
                .Where(m => m.PathogenId == Pathogen.Id)
                .CountAsync();

            _context.Attach(Pathogen).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PathogenExistsAsync(Pathogen.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = $"Pathogen '{Pathogen.Name}' has been updated successfully.";
            return RedirectToPage("./Index");
        }

        private async Task<bool> PathogenExistsAsync(Guid id)
        {
            return await _context.Pathogens.AnyAsync(e => e.Id == id);
        }

        private async Task LoadSelectListsAsync()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            DiseaseSelectList = new SelectList(diseases, "Id", "Name");
        }
    }
}
