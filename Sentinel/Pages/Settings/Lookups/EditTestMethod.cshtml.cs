using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditTestMethodModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTestMethodModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TestMethod TestMethod { get; set; } = null!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            TestMethod = await _context.TestMethods.FirstOrDefaultAsync(m => m.Id == id);

            if (TestMethod == null)
            {
                return NotFound();
            }

            UsageCount = await _context.LabResultMarkers
                .Where(m => m.TestMethodId == id)
                .CountAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var testMethodToUpdate = await _context.TestMethods.FindAsync(TestMethod.Id);
            if (testMethodToUpdate == null)
            {
                return NotFound();
            }

            // Standard codes are maintained through their dedicated configuration;
            // this form deliberately changes only its rendered fields.
            testMethodToUpdate.Name = TestMethod.Name;
            testMethodToUpdate.Description = TestMethod.Description;
            testMethodToUpdate.ExportCode = TestMethod.ExportCode;
            testMethodToUpdate.DisplayOrder = TestMethod.DisplayOrder;
            testMethodToUpdate.IsActive = TestMethod.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TestMethodExistsAsync(TestMethod.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            TempData["SuccessMessage"] = $"Test method '{testMethodToUpdate.Name}' has been updated successfully.";
            return RedirectToPage("./TestMethods");
        }

        private async Task<bool> TestMethodExistsAsync(int id)
        {
            return await _context.TestMethods.AnyAsync(e => e.Id == id);
        }
    }
}
