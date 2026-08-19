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
        private const string GenericSignInFailureMessage =
            "The sign-in attempt was unsuccessful. Please check your credentials and try again.";

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly Sentinel.Services.Telemetry.ActivityTracker _activityTracker;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager, 
            ILogger<LoginModel> logger, 
            IConfiguration configuration,
            Sentinel.Services.Telemetry.ActivityTracker activityTracker)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _activityTracker = activityTracker;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }
        
        public bool IsDemoMode => _configuration.GetValue<bool>("Demo:EnableDemoUsers");

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

        public async Task OnGetAsync(string returnUrl = 
            null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, GenericSignInFailureMessage);
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
                    _logger.LogWarning("Login attempt failed because no matching account was found");
                    return ReturnGenericSignInFailure();
                }

                if (!user.IsEnabled)
                {
                    _logger.LogWarning("Login attempt rejected because account {UserId} is disabled", user.Id);
                    return ReturnGenericSignInFailure();
                }

                if (!await EnsureLockoutEnabledAsync(user))
                {
                    return ReturnGenericSignInFailure();
                }
                
                // Check if email is confirmed (if required)
                if (!user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    _logger.LogWarning("Login attempt rejected because confirmation is required for user {UserId}", user.Id);
                    return ReturnGenericSignInFailure();
                }
                
                string usernameOrEmail = user.UserName ?? Input.Email;
                
                var result = await _signInManager.PasswordSignInAsync(
                    usernameOrEmail,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} logged in successfully", user.Id);
                    _activityTracker.TrackLogin(success: true, userId: user.Id);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Login attempt rejected because user {UserId} is locked out", user.Id);
                    return ReturnGenericSignInFailure();
                }

                _logger.LogWarning("Login attempt rejected for user {UserId}", user.Id);
                return ReturnGenericSignInFailure();
            }

            return Page();
        }
        
        public async Task<IActionResult> OnPostDemoLoginAsync(string email, string password, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!IsDemoMode)
            {
                _logger.LogWarning("Demo login attempted while demo mode is disabled");
                return ReturnGenericSignInFailure();
            }
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Demo login attempt failed because no matching account was found");
                return ReturnGenericSignInFailure();
            }

            if (!user.IsEnabled)
            {
                _logger.LogWarning("Demo login attempt rejected because account {UserId} is disabled", user.Id);
                return ReturnGenericSignInFailure();
            }

            if (!await EnsureLockoutEnabledAsync(user))
            {
                return ReturnGenericSignInFailure();
            }
            
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                password,
                isPersistent: false,
                lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Demo user {UserId} logged in successfully", user.Id);
                _activityTracker.TrackLogin(success: true, userId: user.Id);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Demo login attempt rejected because user {UserId} is locked out", user.Id);
            }

            return ReturnGenericSignInFailure();
        }

        private async Task<bool> EnsureLockoutEnabledAsync(ApplicationUser user)
        {
            if (await _userManager.GetLockoutEnabledAsync(user))
            {
                return true;
            }

            // Options.Lockout.AllowedForNewUsers covers newly-created accounts. Enable
            // legacy accounts on their next sign-in attempt as well, so the password
            // failure policy is enforced consistently.
            var result = await _userManager.SetLockoutEnabledAsync(user, enabled: true);
            if (!result.Succeeded)
            {
                _logger.LogError("Unable to enable failed-password lockout for user {UserId}", user.Id);
            }

            return result.Succeeded;
        }

        private PageResult ReturnGenericSignInFailure()
        {
            _activityTracker.TrackLogin(success: false);
            ModelState.AddModelError(string.Empty, GenericSignInFailureMessage);
            return Page();
        }
    }
}
