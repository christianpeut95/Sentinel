using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account.Manage;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class GenerateRecoveryCodesModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GenerateRecoveryCodesModel> _logger;

    public GenerateRecoveryCodesModel(UserManager<ApplicationUser> userManager, ILogger<GenerateRecoveryCodesModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public int RecoveryCodesLeft { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();
        TempData["RecoveryCodesJson"] = JsonSerializer.Serialize(recoveryCodes);
        _logger.LogInformation("Recovery codes regenerated for user {UserId}", user.Id);
        return RedirectToPage("./ShowRecoveryCodes");
    }
}
