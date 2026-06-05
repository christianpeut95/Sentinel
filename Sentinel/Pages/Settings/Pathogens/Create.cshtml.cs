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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Pathogen Pathogen { get; set; } = new Pathogen
        {
            IsActive = true,
            DisplayOrder = 100,
            ResultType = ResultType.Qualitative
        };

        public SelectList DiseaseSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
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
                    .AnyAsync(p => p.LOINCCode == Pathogen.LOINCCode);

                if (existingLoinc)
                {
                    ModelState.AddModelError("Pathogen.LOINCCode", "A pathogen with this LOINC code already exists.");
                    await LoadSelectListsAsync();
                    return Page();
                }
            }

            Pathogen.Id = Guid.NewGuid();
            _context.Pathogens.Add(Pathogen);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Pathogen '{Pathogen.Name}' has been created successfully.";
            return RedirectToPage("./Index");
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
