using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Organizations;

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
    public OrganizationInputModel Input { get; set; } = new() { IsActive = true };

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadFormAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await OrganizationFormSupport.ValidateReferencesAsync(
            _context,
            Input,
            ModelState,
            cancellationToken: cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadFormAsync(cancellationToken);
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        try
        {
            var organizationToCreate = new Organization
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            Input.ApplyTo(organizationToCreate);

            _context.Organizations.Add(organizationToCreate);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogChangeAsync(
                entityType: "Organization",
                entityId: organizationToCreate.Id.ToString(),
                fieldName: "Organization Created",
                oldValue: null,
                newValue: organizationToCreate.Name,
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Organization created successfully.";
            return RedirectToPage("./Details", new { id = organizationToCreate.Id });
        }
        catch (DbUpdateException ex)
            when (ProviderIdentityResolutionService.IsProviderIdentityConflict(ex))
        {
            ModelState.AddModelError(
                $"Input.{nameof(Input.ProviderIdentifier)}",
                "That provider identifier is already assigned to another organization with the same authority.");
            TempData["ErrorMessage"] = "Please correct the provider identifier and try again.";
            await LoadFormAsync(cancellationToken);
            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = UserFacingError.Create(HttpContext, ex);
            await LoadFormAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadFormAsync(CancellationToken cancellationToken)
    {
        ViewData["OrganizationTypeId"] =
            await OrganizationFormSupport.LoadOrganizationTypesAsync(
                _context,
                Input.OrganizationTypeId,
                cancellationToken);
    }
}
