using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool ResetSuccessful { get; set; }

        public class InputModel
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Code { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            ResetSuccessful = false;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.UserId) || string.IsNullOrWhiteSpace(Input.Code))
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "The password reset link is invalid or incomplete. Please request a new one.");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!PasswordResetTokenEncoding.TryDecode(Input.Code, out var decodedToken))
            {
                ModelState.AddModelError(string.Empty, "The password reset link is invalid or has expired. Please request a new one.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user is not { IsEnabled: true })
            {
                ModelState.AddModelError(string.Empty, "Invalid password reset attempt.");
                return Page();
            }

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
                    ResetSuccessful = true;
                    return Page();
                }

                foreach (var error in result.Errors)
                {
                    if (error.Code == "InvalidToken")
                    {
                        _logger.LogWarning("Invalid or expired token used for password reset: {UserId}", user.Id);
                        ModelState.AddModelError(string.Empty, 
                            "The password reset link is invalid or has expired. Please request a new one.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                _logger.LogWarning(
                    "Password reset failed for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during password reset for user {UserId}", Input.UserId);
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
            }

            return Page();
        }
    }
}
