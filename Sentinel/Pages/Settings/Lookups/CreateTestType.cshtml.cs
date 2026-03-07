using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateTestTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTestTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestType TestType { get; set; } = default!;

        public IActionResult OnGet()
        {
            TestType = new TestType { IsActive = true };
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
            var exists = await _context.TestTypes
                .AnyAsync(t => t.Name == TestType.Name);

            if (exists)
            {
                ModelState.AddModelError("TestType.Name", "A test type with this name already exists.");
                TempData["ErrorMessage"] = "A test type with this name already exists.";
                return Page();
            }

            try
            {
                _context.TestTypes.Add(TestType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test type '{TestType.Name}' created successfully.";
                return RedirectToPage("./TestTypes");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the test type: {ex.Message}";
                return Page();
            }
        }
    }
}
