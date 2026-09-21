using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account.Manage;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EnableAuthenticatorModel : PageModel
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly ILogger<EnableAuthenticatorModel> _logger;

    public EnableAuthenticatorModel(
        UserManager<ApplicationUser> userManager,
        UrlEncoder urlEncoder,
        ILogger<EnableAuthenticatorModel> logger)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string SharedKey { get; private set; } = string.Empty;
    public string AuthenticatorUri { get; private set; } = string.Empty;
    public string QrCodeImageUrl { get; private set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Enter the 6-digit code from your authenticator app.")]
        [DataType(DataType.Text)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load the signed-in user.");
        }

        if (!ModelState.IsValid)
        {
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(Input.Code), "The verification code is invalid. Check the time on your device and try again.");
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []).ToArray();

        TempData["RecoveryCodesJson"] = JsonSerializer.Serialize(recoveryCodes);
        _logger.LogInformation("Authenticator application enabled for user {UserId}", user.Id);
        return RedirectToPage("./ShowRecoveryCodes");
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(unformattedKey!);
        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? user.Id;
        AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey!);
        QrCodeImageUrl = GenerateQrCodeImage(AuthenticatorUri);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey) =>
        string.Format(
            AuthenticatorUriFormat,
            _urlEncoder.Encode("Sentinel"),
            _urlEncoder.Encode(email),
            unformattedKey);

    private static string GenerateQrCodeImage(string authenticatorUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
