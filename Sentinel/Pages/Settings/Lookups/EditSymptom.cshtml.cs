using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditSymptomModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditSymptomModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Symptom Symptom { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var symptom = await _context.Symptoms
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (symptom == null)
            {
                return NotFound();
            }

            Symptom = symptom;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var symptomToUpdate = await _context.Symptoms
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == Symptom.Id);

            if (symptomToUpdate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            symptomToUpdate.Name = Symptom.Name;
            symptomToUpdate.Code = Symptom.Code;
            symptomToUpdate.ExportCode = Symptom.ExportCode;
            symptomToUpdate.Description = Symptom.Description;
            symptomToUpdate.SortOrder = Symptom.SortOrder;
            symptomToUpdate.IsActive = Symptom.IsActive;
            symptomToUpdate.UpdatedAt = DateTime.UtcNow;
            symptomToUpdate.UpdatedBy = userId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SymptomExists(Symptom.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = $"Symptom '{Symptom.Name}' updated successfully.";
            return RedirectToPage("./Symptoms");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var symptom = await _context.Symptoms
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == Symptom.Id);

            if (symptom == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            symptom.IsDeleted = true;
            symptom.DeletedAt = DateTime.UtcNow;
            symptom.DeletedByUserId = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Symptom '{symptom.Name}' deleted successfully.";
            return RedirectToPage("./Symptoms");
        }

        private bool SymptomExists(int id)
        {
            return _context.Symptoms.Any(e => e.Id == id);
        }
    }
}
