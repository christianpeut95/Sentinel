using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Reports;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class WebDataRocksTermsModel : PageModel
{
    private readonly IWebDataRocksLicenseService _webDataRocksLicenseService;

    public WebDataRocksTermsModel(IWebDataRocksLicenseService webDataRocksLicenseService)
    {
        _webDataRocksLicenseService = webDataRocksLicenseService;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public bool AcceptLicenseTerms { get; set; }

    public WebDataRocksLicenseStatus LicenseStatus { get; private set; } = new(false, false, false);

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (LicenseStatus.CanUse)
        {
            return RedirectToReturnUrl();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!LicenseStatus.OrganizationAccepted)
        {
            ModelState.AddModelError(string.Empty, "Interactive pivot reports have not been enabled by an organisation administrator.");
            return Page();
        }

        if (!AcceptLicenseTerms)
        {
            ModelState.AddModelError(nameof(AcceptLicenseTerms), "Accept the WebDataRocks Licence Agreement to use interactive pivot reports.");
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        await _webDataRocksLicenseService.AcceptForUserAsync(userId);
        return RedirectToReturnUrl();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        LicenseStatus = await _webDataRocksLicenseService.GetStatusAsync(userId);
    }

    private IActionResult RedirectToReturnUrl()
    {
        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl!) : RedirectToPage("/Reports/Index");
    }
}
