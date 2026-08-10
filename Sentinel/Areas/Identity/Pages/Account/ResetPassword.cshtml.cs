using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;

namespace Sentinel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
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
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
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

        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            // Password reset feature disabled - return to login
            return RedirectToPage("./Login");

            /* Uncomment below and remove redirect above to enable password reset
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("Password reset attempted with missing userId or code");
                return RedirectToPage("./ForgotPassword");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent user: {UserId}", userId);
                // Don't reveal that the user doesn't exist
                return RedirectToPage("./ForgotPassword");
            }

            Input = new InputModel
            {
                UserId = userId,
                Code = code,
                Email = user.Email ?? string.Empty
            };

            ResetSuccessful = false;
            return Page();
            */
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Password reset feature disabled - return to login
            return RedirectToPage("./Login");

            /* Uncomment below and remove redirect above to enable password reset
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                _logger.LogWarning("Password reset POST attempted for non-existent user: {UserId}", Input.UserId);
                // Don't reveal that the user doesn't exist
                ResetSuccessful = true;
                return Page();
            }

            // Verify email matches
            if (user.Email != Input.Email)
            {
                _logger.LogWarning(
                    "Password reset attempted with mismatched email. User: {UserId}, Expected: {UserEmail}, Provided: {InputEmail}",
                    Input.UserId, user.Email, Input.Email);
                ModelState.AddModelError(string.Empty, "Invalid password reset attempt.");
                return Page();
            }

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user: {UserId} ({Email})", user.Id, user.Email);
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
                    "Password reset failed for user {UserId}. Errors: {Errors}",
                    user.Id,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during password reset for user {UserId}", Input.UserId);
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
            }

            return Page();
            */
        }
    }
}
