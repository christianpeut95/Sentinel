using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Organizations;

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
    public OrganizationInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue)
            return NotFound();

        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.Value, cancellationToken);

        if (organization == null)
            return NotFound();

        Input = OrganizationInputModel.FromOrganization(organization);
        await LoadFormAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var existingOrganization = await _context.Organizations
            .FirstOrDefaultAsync(organization => organization.Id == Input.Id, cancellationToken);

        if (existingOrganization == null)
            return NotFound();

        await OrganizationFormSupport.ValidateReferencesAsync(
            _context,
            Input,
            ModelState,
            existingOrganization.Id,
            cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadFormAsync(cancellationToken);
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        try
        {
            Input.ApplyTo(existingOrganization);
            existingOrganization.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogChangeAsync(
                entityType: "Organization",
                entityId: existingOrganization.Id.ToString(),
                fieldName: "Organization Updated",
                oldValue: null,
                newValue: existingOrganization.Name,
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Organization updated successfully.";
            return RedirectToPage("./Details", new { id = existingOrganization.Id });
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
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Organizations.AnyAsync(
                    organization => organization.Id == Input.Id,
                    cancellationToken))
            {
                return NotFound();
            }

            ModelState.AddModelError(
                string.Empty,
                "This organization was changed by another user. Reload the page and try again.");
            TempData["ErrorMessage"] = "The organization could not be updated because it changed while you were editing it.";
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
