using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Organizations
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public EditModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public Organization Organization { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(m => m.Id == id);

            if (organization == null)
            {
                return NotFound();
            }

            Organization = organization;

            ViewData["OrganizationTypeId"] = new SelectList(
                await _context.OrganizationTypes
                    .Where(ot => ot.IsActive)
                    .OrderBy(ot => ot.DisplayOrder)
                    .ThenBy(ot => ot.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["OrganizationTypeId"] = new SelectList(
                    await _context.OrganizationTypes
                        .Where(ot => ot.IsActive)
                        .OrderBy(ot => ot.DisplayOrder)
                        .ThenBy(ot => ot.Name)
                        .ToListAsync(),
                    "Id",
                    "Name"
                );

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                var existingOrganization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Id == Organization.Id);

                if (existingOrganization == null)
                {
                    TempData["ErrorMessage"] = "Organization not found.";
                    return RedirectToPage("./Index");
                }

                // Only fields presented by this form may be changed.  Do not attach the
                // request-bound entity: hidden/audit/navigation fields are not a client API.
                existingOrganization.Name = Organization.Name;
                existingOrganization.OrganizationTypeId = Organization.OrganizationTypeId;
                existingOrganization.ExportCode = Organization.ExportCode;
                existingOrganization.IsActive = Organization.IsActive;
                existingOrganization.Address = Organization.Address;
                existingOrganization.ContactPerson = Organization.ContactPerson;
                existingOrganization.Phone = Organization.Phone;
                existingOrganization.Email = Organization.Email;
                existingOrganization.Notes = Organization.Notes;
                existingOrganization.ModifiedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();

                    await _auditService.LogChangeAsync(
                        entityType: "Organization",
                        entityId: existingOrganization.Id.ToString(),
                        fieldName: "Organization Updated",
                        oldValue: null,
                        newValue: existingOrganization.Name,
                        userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    TempData["SuccessMessage"] = "Organization updated successfully.";
                    return RedirectToPage("./Details", new { id = existingOrganization.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OrganizationExists(Organization.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
                
                ViewData["OrganizationTypeId"] = new SelectList(
                    await _context.OrganizationTypes
                        .Where(ot => ot.IsActive)
                        .OrderBy(ot => ot.DisplayOrder)
                        .ThenBy(ot => ot.Name)
                        .ToListAsync(),
                    "Id",
                    "Name"
                );

                return Page();
            }
        }

        private async Task<bool> OrganizationExists(Guid id)
        {
            return await _context.Organizations.AnyAsync(e => e.Id == id);
        }
    }
}
