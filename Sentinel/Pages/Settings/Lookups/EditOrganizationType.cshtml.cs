using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditOrganizationTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditOrganizationTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public OrganizationType OrganizationType { get; set; } = default!;

        public int UsageCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organizationType = await _context.OrganizationTypes.FindAsync(id);

            if (organizationType == null)
            {
                return NotFound();
            }

            OrganizationType = organizationType;

            // Get usage count
            UsageCount = await _context.Organizations
                .CountAsync(o => o.OrganizationTypeId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name (excluding current record)
            var exists = await _context.OrganizationTypes
                .AnyAsync(o => o.Name == OrganizationType.Name && o.Id != OrganizationType.Id);

            if (exists)
            {
                ModelState.AddModelError("OrganizationType.Name", "An organization type with this name already exists.");
                TempData["ErrorMessage"] = "An organization type with this name already exists.";
                UsageCount = await _context.Organizations.CountAsync(o => o.OrganizationTypeId == OrganizationType.Id);
                return Page();
            }

            try
            {
                _context.Attach(OrganizationType).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Organization type '{OrganizationType.Name}' updated successfully.";
                return RedirectToPage("./OrganizationTypes");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await OrganizationTypeExists(OrganizationType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the organization type: {ex.Message}";
                UsageCount = await _context.Organizations.CountAsync(o => o.OrganizationTypeId == OrganizationType.Id);
                return Page();
            }
        }

        private async Task<bool> OrganizationTypeExists(int id)
        {
            return await _context.OrganizationTypes.AnyAsync(e => e.Id == id);
        }
    }
}
