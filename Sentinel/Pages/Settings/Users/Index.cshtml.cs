using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Sentinel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services.Email;
using System.Text.Encodings.Web;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.View")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        public record UserRow(
            string Id, 
            string? Email, 
            string? UserName, 
            string? FirstName, 
            string? LastName, 
            List<string> AssignedRoles, 
            bool EmailConfirmed, 
            DateTimeOffset? LockoutEnd,
            bool IsInterviewWorker);

        public List<UserRow> Users { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? SearchEmail { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        public async Task OnGetAsync()
        {
            Roles = _roleManager.Roles.Select(r => r.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();
            var users = _userManager.Users.ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchEmail))
            {
                users = users.Where(u => u.Email != null && u.Email.Contains(SearchEmail, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                // Apply role filter
                if (!string.IsNullOrEmpty(RoleFilter) && !roles.Contains(RoleFilter))
                {
                    continue;
                }

                Users.Add(new UserRow(
                    u.Id, 
                    u.Email, 
                    u.UserName, 
                    u.FirstName,
                    u.LastName,
                    roles.ToList(), 
                    u.EmailConfirmed, 
                    u.LockoutEnd,
                    u.IsInterviewWorker));
            }
        }

        /// <summary>
        /// Handler for admin-initiated password reset
        /// Generates a reset token and emails it to the user
        /// </summary>
        public async Task<IActionResult> OnPostSendPasswordResetAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                StatusMessage = "Error: User not found or has no email address.";
                return RedirectToPage();
            }

            try
            {
                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Build reset link
                var resetLink = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = token },
                    protocol: Request.Scheme);

                if (string.IsNullOrEmpty(resetLink))
                {
                    _logger.LogError("Failed to generate password reset link for user {UserId}", userId);
                    StatusMessage = "Error: Failed to generate reset link.";
                    return RedirectToPage();
                }

                // Send password reset email
                var userName = !string.IsNullOrEmpty(user.FirstName) 
                    ? $"{user.FirstName} {user.LastName}".Trim() 
                    : user.Email;

                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, userName);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent to user {UserId} ({Email})", userId, user.Email);
                    StatusMessage = $"Password reset email sent successfully to {user.Email}";
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}", user.Email);
                    StatusMessage = $"Warning: Failed to send email to {user.Email}. Please check SMTP settings.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to user {UserId}", userId);
                StatusMessage = "Error: Failed to send password reset email. Please try again.";
            }

            return RedirectToPage();
        }
    }
}
