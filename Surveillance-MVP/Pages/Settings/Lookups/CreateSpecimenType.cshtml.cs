using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateSpecimenTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateSpecimenTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SpecimenType SpecimenType { get; set; } = default!;

        public IActionResult OnGet()
        {
            SpecimenType = new SpecimenType { IsActive = true };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name
            var exists = await _context.SpecimenTypes
                .AnyAsync(s => s.Name == SpecimenType.Name);

            if (exists)
            {
                ModelState.AddModelError("SpecimenType.Name", "A specimen type with this name already exists.");
                TempData["ErrorMessage"] = "A specimen type with this name already exists.";
                return Page();
            }

            try
            {
                _context.SpecimenTypes.Add(SpecimenType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Specimen type '{SpecimenType.Name}' created successfully.";
                return RedirectToPage("./SpecimenTypes");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the specimen type: {ex.Message}";
                return Page();
            }
        }
    }
}
