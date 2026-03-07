using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateResultUnitModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateResultUnitModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ResultUnits ResultUnit { get; set; } = default!;

        public IActionResult OnGet()
        {
            ResultUnit = new ResultUnits { IsActive = true };
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
            var exists = await _context.ResultUnits
                .AnyAsync(r => r.Name == ResultUnit.Name);

            if (exists)
            {
                ModelState.AddModelError("ResultUnit.Name", "A result unit with this name already exists.");
                TempData["ErrorMessage"] = "A result unit with this name already exists.";
                return Page();
            }

            try
            {
                _context.ResultUnits.Add(ResultUnit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Result unit '{ResultUnit.Name}' created successfully.";
                return RedirectToPage("./ResultUnits");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the result unit: {ex.Message}";
                return Page();
            }
        }
    }
}
