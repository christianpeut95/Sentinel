using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateOrganizationTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateOrganizationTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public OrganizationType OrganizationType { get; set; } = new OrganizationType { IsActive = true };

        public IActionResult OnGet()
        {
            // Ensure the model is initialized
            if (OrganizationType == null)
            {
                OrganizationType = new OrganizationType { IsActive = true };
            }
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
            var exists = await _context.OrganizationTypes
                .AnyAsync(o => o.Name == OrganizationType.Name);

            if (exists)
            {
                ModelState.AddModelError("OrganizationType.Name", "An organization type with this name already exists.");
                TempData["ErrorMessage"] = "An organization type with this name already exists.";
                return Page();
            }

            try
            {
                _context.OrganizationTypes.Add(OrganizationType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Organization type '{OrganizationType.Name}' created successfully.";
                return RedirectToPage("./OrganizationTypes");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the organization type: {ex.Message}";
                return Page();
            }
        }
    }
}
