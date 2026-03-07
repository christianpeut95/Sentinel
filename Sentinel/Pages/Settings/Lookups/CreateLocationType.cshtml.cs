using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateLocationTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateLocationTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LocationType LocationType { get; set; } = new LocationType { IsActive = true };

        public IActionResult OnGet()
        {
            if (LocationType == null)
            {
                LocationType = new LocationType { IsActive = true };
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
            var exists = await _context.LocationTypes
                .AnyAsync(l => l.Name == LocationType.Name);

            if (exists)
            {
                ModelState.AddModelError("LocationType.Name", "A location type with this name already exists.");
                TempData["ErrorMessage"] = "A location type with this name already exists.";
                return Page();
            }

            try
            {
                _context.LocationTypes.Add(LocationType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Location type '{LocationType.Name}' created successfully.";
                return RedirectToPage("./LocationTypes");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the location type: {ex.Message}";
                return Page();
            }
        }
    }
}
