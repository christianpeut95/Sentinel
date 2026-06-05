using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateTestMethodModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTestMethodModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestMethod TestMethod { get; set; } = new TestMethod
        {
            IsActive = true,
            DisplayOrder = 100
        };

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.TestMethods.Add(TestMethod);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Test method '{TestMethod.Name}' has been created successfully.";
            return RedirectToPage("./TestMethods");
        }
    }
}
