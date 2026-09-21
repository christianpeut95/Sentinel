using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account.Manage;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TwoFactorAuthenticationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<TwoFactorAuthenticationModel> _logger;

    public TwoFactorAuthenticationModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<TwoFactorAuthenticationModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public bool HasAuthenticator { get; private set; }
    public int RecoveryCodesLeft { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public bool IsMachineRemembered { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostDisable2faAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return BadRequest("Two-factor authentication is not enabled for this account.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _signInManager.ForgetTwoFactorClientAsync();
        _logger.LogWarning("Two-factor authentication disabled for user {UserId}", user.Id);
        StatusMessage = "Two-factor authentication has been disabled for your account.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForgetBrowserAsync()
    {
        await _signInManager.ForgetTwoFactorClientAsync();
        StatusMessage = "This browser will require a two-factor code the next time you sign in.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
        IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
    }
}
