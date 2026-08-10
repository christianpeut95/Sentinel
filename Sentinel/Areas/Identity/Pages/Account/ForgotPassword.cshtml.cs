using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using Sentinel.Services.Email;

namespace Sentinel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
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
            // Password reset feature disabled - return to login
            return RedirectToPage("./Login");

            // Uncomment below and remove redirect above to enable password reset
            // EmailSent = false;
            // return Page();
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

            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Always show success message to prevent email enumeration attacks
            // Don't reveal whether the account exists or not
            EmailSent = true;

            // Only send email if user actually exists
            if (user != null)
            {
                try
                {
                    // Generate password reset token
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // Build callback URL for password reset
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    if (!string.IsNullOrEmpty(callbackUrl))
                    {
                        var userName = !string.IsNullOrEmpty(user.FirstName)
                            ? $"{user.FirstName} {user.LastName}".Trim()
                            : user.Email ?? "User";

                        var emailSent = await _emailService.SendPasswordResetEmailAsync(
                            user.Email ?? Input.Email,
                            callbackUrl,
                            userName);

                        if (emailSent)
                        {
                            _logger.LogInformation(
                                "Password reset email sent to user {Email}",
                                user.Email);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to send password reset email to {Email}. SMTP may not be configured.",
                                user.Email);
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to generate callback URL for password reset");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing password reset for {Email}", Input.Email);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Password reset requested for non-existent email: {Email}",
                    Input.Email);
            }

            return Page();
            */
        }
    }
}
