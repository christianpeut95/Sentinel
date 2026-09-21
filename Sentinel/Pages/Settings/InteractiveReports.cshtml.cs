using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings;

/// <summary>
/// Controls the organisation-level consent required for the optional
/// WebDataRocks interactive reporting component.
/// </summary>
[Authorize(Roles = "Admin")]
public class InteractiveReportsModel : PageModel
{
    private readonly IWebDataRocksLicenseService _webDataRocksLicenseService;

    public InteractiveReportsModel(IWebDataRocksLicenseService webDataRocksLicenseService)
    {
        _webDataRocksLicenseService = webDataRocksLicenseService;
    }

    [BindProperty]
    public bool EnableWebDataRocks { get; set; }

    [BindProperty]
    public bool AcceptLicenseTerms { get; set; }

    public WebDataRocksLicenseStatus LicenseStatus { get; private set; } = new(false, false, false);

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // LoadAsync populates display-only status and the persisted switch
        // value, so retain the submitted value before doing that lookup.
        var requestedEnableWebDataRocks = EnableWebDataRocks;
        await LoadAsync();
        EnableWebDataRocks = requestedEnableWebDataRocks;

        // New consent is required when enabling, and again whenever a later
        // vendor revision invalidates the recorded organisation acceptance.
        if (EnableWebDataRocks && !LicenseStatus.OrganizationAccepted && !AcceptLicenseTerms)
        {
            ModelState.AddModelError(nameof(AcceptLicenseTerms), "Accept the WebDataRocks Licence Agreement before enabling interactive pivot reports.");
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        await _webDataRocksLicenseService.SetOrganizationAcceptanceAsync(EnableWebDataRocks, userId);

        if (EnableWebDataRocks)
        {
            // An administrator who has accepted on behalf of the organisation
            // has also accepted before using the component personally.
            await _webDataRocksLicenseService.AcceptForUserAsync(userId);
        }

        TempData["SuccessMessage"] = EnableWebDataRocks
            ? "Interactive pivot reports are enabled. Users will be asked to accept the current WebDataRocks terms before first use."
            : "Interactive pivot reports are disabled. No WebDataRocks scripts will be loaded.";

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        LicenseStatus = await _webDataRocksLicenseService.GetStatusAsync(userId);
        EnableWebDataRocks = LicenseStatus.OrganizationEnabled;
    }
}
