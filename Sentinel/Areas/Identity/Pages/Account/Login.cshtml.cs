using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Find user by email to get their actual username
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: No user found with email {Input.Email}");
                    ModelState.AddModelError(string.Empty, "No account found with this email address. Please check your email or contact your administrator.");
                    return Page();
                }
                
                // Check if email is confirmed (if required)
                if (!user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    _logger.LogWarning($"Login failed: Email not confirmed for {Input.Email}");
                    ModelState.AddModelError(string.Empty, "Your email address has not been confirmed. Please contact your administrator.");
                    return Page();
                }
                
                string usernameOrEmail = user.UserName ?? Input.Email;
                
                var result = await _signInManager.PasswordSignInAsync(usernameOrEmail, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} logged in successfully.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"User account locked out: {Input.Email}");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    _logger.LogWarning($"Login failed: Invalid password for user {Input.Email}");
                    ModelState.AddModelError(string.Empty, "Incorrect password. Please try again or use 'Forgot your password?' to reset it.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
