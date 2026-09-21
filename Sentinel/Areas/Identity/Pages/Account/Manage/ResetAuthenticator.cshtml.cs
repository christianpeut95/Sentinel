using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account.Manage;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResetAuthenticatorModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ResetAuthenticatorModel> _logger;

    public ResetAuthenticatorModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ResetAuthenticatorModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        return await _userManager.GetTwoFactorEnabledAsync(user)
            ? Page()
            : RedirectToPage("./TwoFactorAuthentication");
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

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        TempData["StatusMessage"] = "Your authenticator app has been reset. Configure it again before two-factor authentication can be enabled.";
        _logger.LogInformation("Authenticator application reset for user {UserId}", user.Id);
        return RedirectToPage("./EnableAuthenticator");
    }
}
