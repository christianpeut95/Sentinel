using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.DiseaseCategories
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public DiseaseCategory Category { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _context.DiseaseCategories.AnyAsync(c => c.Name == Category.Name))
            {
                ModelState.AddModelError("Category.Name", "A category with this name already exists.");
                return Page();
            }

            if (await _context.DiseaseCategories.AnyAsync(c => c.ReportingId == Category.ReportingId))
            {
                ModelState.AddModelError("Category.ReportingId", "A category with this Reporting ID already exists.");
                return Page();
            }

            _context.DiseaseCategories.Add(Category);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Category '{Category.Name}' has been created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
