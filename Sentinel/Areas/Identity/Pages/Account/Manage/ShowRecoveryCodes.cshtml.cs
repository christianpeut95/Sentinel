using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Areas.Identity.Pages.Account.Manage;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ShowRecoveryCodesModel : PageModel
{
    public string[] RecoveryCodes { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (TempData["RecoveryCodesJson"] is not string recoveryCodesJson)
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        RecoveryCodes = JsonSerializer.Deserialize<string[]>(recoveryCodesJson) ?? [];
        return RecoveryCodes.Length == 0
            ? RedirectToPage("./TwoFactorAuthentication")
            : Page();
    }
}
