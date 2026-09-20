using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models;
using Sentinel.Services;
using Sentinel.Services.Email;

namespace Sentinel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("password-reset")]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool EmailSent { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            EmailSent = false;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Always return the same confirmation to prevent account enumeration.
            EmailSent = true;

            // Disabled accounts cannot sign in, so do not issue reset tokens for them.
            if (user is not { IsEnabled: true } || string.IsNullOrWhiteSpace(user.Email))
            {
                return Page();
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetPageUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity" },
                    protocol: Request.Scheme);

                if (string.IsNullOrWhiteSpace(resetPageUrl))
                {
                    _logger.LogError("Failed to generate a password reset callback URL");
                    return Page();
                }

                var callbackUrl = PasswordResetTokenEncoding.AddToResetLinkFragment(
                    resetPageUrl,
                    user.Id,
                    token);

                var userName = !string.IsNullOrWhiteSpace(user.FirstName)
                    ? $"{user.FirstName} {user.LastName}".Trim()
                    : user.Email;
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl, userName);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent for user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Password reset email delivery failed for user {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing a password reset request for user {UserId}", user.Id);
            }

            return Page();
        }
    }
}
