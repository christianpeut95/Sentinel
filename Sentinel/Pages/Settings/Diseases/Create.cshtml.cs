using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(Guid? parentId)
        {
            // Initialize Disease object
            Disease = new Disease();
            
            
            // If parentId is provided, pre-select it
            if (parentId.HasValue)
            {
                Disease.ParentDiseaseId = parentId.Value;
            }
            
            await LoadParentDiseases();
            await LoadCategories();
            return Page();
        }

        [BindProperty]
        public Disease Disease { get; set; } = default!;

        public SelectList ParentDiseases { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!ModelState.IsValid)
            {
                await LoadParentDiseases();
                await LoadCategories();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                if (await _context.Diseases.AnyAsync(d => d.Code == Disease.Code))
                {
                    ModelState.AddModelError("Disease.Code", "A disease with this code already exists.");
                    await LoadParentDiseases();
                    await LoadCategories();
                    return Page();
                }

                // Ensure ID is generated before saving
                if (Disease.Id == Guid.Empty)
                {
                    Disease.Id = Guid.NewGuid();
                }

                _context.Diseases.Add(Disease);
                await _context.SaveChangesAsync();
                
                var diseaseName = Disease.Name;
                var diseaseId = Disease.Id;
                
                TempData["SuccessMessage"] = $"Disease '{diseaseName}' has been created successfully.";
                
                // If "Create & Add Child" was clicked, redirect to create page with parent set
                if (action == "createAndAddChild")
                {
                    return RedirectToPage("./Create", new { parentId = diseaseId });
                }
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadParentDiseases();
                await LoadCategories();
                return Page();
            }
        }

        private async Task LoadParentDiseases()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    DisplayName = new string('—', d.Level) + " " + d.Name
                })
                .ToListAsync();

            ParentDiseases = new SelectList(diseases, "Id", "DisplayName");
        }

        private async Task LoadCategories()
        {
            ViewData["CategoryId"] = new SelectList(
                await _context.DiseaseCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name");
        }
    }
}
