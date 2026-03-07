using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateTestResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTestResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestResult TestResult { get; set; } = default!;
        
        public SelectList TestTypesList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            TestResult = new TestResult { IsActive = true };
            await LoadTestTypes();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTestTypes();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name within the same test type
            var exists = await _context.TestResults
                .AnyAsync(t => t.Name == TestResult.Name && t.TestTypeId == TestResult.TestTypeId);

            if (exists)
            {
                ModelState.AddModelError("TestResult.Name", "A test result with this name already exists for this test type.");
                await LoadTestTypes();
                TempData["ErrorMessage"] = "A test result with this name already exists for this test type.";
                return Page();
            }

            try
            {
                _context.TestResults.Add(TestResult);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Test result '{TestResult.Name}' created successfully.";
                return RedirectToPage("./TestResults");
            }
            catch (Exception ex)
            {
                await LoadTestTypes();
                TempData["ErrorMessage"] = $"An error occurred while creating the test result: {ex.Message}";
                return Page();
            }
        }

        private async Task LoadTestTypes()
        {
            TestTypesList = new SelectList(
                await _context.TestTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );
        }
    }
}

