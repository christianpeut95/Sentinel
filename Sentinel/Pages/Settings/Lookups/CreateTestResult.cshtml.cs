using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateTestResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTestResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestResult TestResult { get; set; } = new TestResult
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

            _context.TestResults.Add(TestResult);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Test result '{TestResult.Name}' has been created successfully.";
            return RedirectToPage("./TestResults");
        }
    }
}
