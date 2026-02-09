using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Organizations
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CreateModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public Organization Organization { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
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

            Organization = new Organization { IsActive = true };

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
                Organization.Id = Guid.NewGuid();
                Organization.CreatedAt = DateTime.UtcNow;

                _context.Organizations.Add(Organization);
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "Organization",
                    entityId: Organization.Id.ToString(),
                    fieldName: "Organization Created",
                    oldValue: null,
                    newValue: Organization.Name,
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Organization created successfully.";
                return RedirectToPage("./Details", new { id = Organization.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the organization: {ex.Message}";

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
    }
}
