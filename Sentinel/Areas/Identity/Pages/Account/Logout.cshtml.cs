using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account;

/// <summary>
/// Ends the current browser session and invalidates the user's security stamp so a
/// copied authentication cookie cannot be replayed after logout.
/// </summary>
[Authorize]
public sealed class LogoutModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<LogoutModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            var invalidationResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!invalidationResult.Succeeded)
            {
                _logger.LogError(
                    "Unable to invalidate active sessions during logout for user {UserId}",
                    user.Id);
            }
        }

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {UserId} signed out", user?.Id ?? "unknown");

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
