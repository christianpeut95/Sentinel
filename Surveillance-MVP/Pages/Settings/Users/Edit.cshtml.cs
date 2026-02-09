using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Surveillance_MVP.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.Edit")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<string> AllRoles { get; set; } = new();
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        public class InputModel
        {
            public string Id { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            public string? UserName { get; set; }

            [Display(Name = "First Name")]
            [StringLength(100)]
            public string? FirstName { get; set; }

            [Display(Name = "Last Name")]
            [StringLength(100)]
            public string? LastName { get; set; }

            [Display(Name = "Email Confirmed")]
            public bool EmailConfirmed { get; set; }

            public List<string> SelectedRoles { get; set; } = new();

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }

            // Interview Worker Fields
            [Display(Name = "Interview Worker")]
            public bool IsInterviewWorker { get; set; }

            [Display(Name = "Primary Language")]
            [StringLength(50)]
            public string? PrimaryLanguage { get; set; }

            [Display(Name = "Additional Languages (comma-separated)")]
            public string? AdditionalLanguages { get; set; }

            [Display(Name = "Available for Auto-Assignment")]
            public bool AvailableForAutoAssignment { get; set; }

            [Display(Name = "Task Capacity")]
            [Range(1, 100)]
            public int CurrentTaskCapacity { get; set; } = 10;
        }

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            // Parse languages from JSON
            List<string> additionalLanguages = new();
            if (!string.IsNullOrEmpty(user.LanguagesSpokenJson))
            {
                try
                {
                    additionalLanguages = JsonSerializer.Deserialize<List<string>>(user.LanguagesSpokenJson) ?? new();
                }
                catch { }
            }

            Input = new InputModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed,
                SelectedRoles = userRoles.ToList(),
                IsInterviewWorker = user.IsInterviewWorker,
                PrimaryLanguage = user.PrimaryLanguage,
                AdditionalLanguages = string.Join(", ", additionalLanguages),
                AvailableForAutoAssignment = user.AvailableForAutoAssignment,
                CurrentTaskCapacity = user.CurrentTaskCapacity
            };

            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            LockoutEnd = user.LockoutEnd;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
                return NotFound();

            AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            LockoutEnd = user.LockoutEnd;

            if (action == "lock")
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["StatusMessage"] = "User account has been locked.";
                return RedirectToPage(new { id = Input.Id });
            }

            if (action == "unlock")
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["StatusMessage"] = "User account has been unlocked.";
                return RedirectToPage(new { id = Input.Id });
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update basic info
            if (user.UserName != Input.UserName)
            {
                user.UserName = Input.UserName;
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;

            // Update email confirmed
            if (user.EmailConfirmed != Input.EmailConfirmed)
            {
                user.EmailConfirmed = Input.EmailConfirmed;
            }

            // Update Interview Worker fields
            user.IsInterviewWorker = Input.IsInterviewWorker;
            user.PrimaryLanguage = Input.PrimaryLanguage;
            user.AvailableForAutoAssignment = Input.AvailableForAutoAssignment;
            user.CurrentTaskCapacity = Input.CurrentTaskCapacity;

            // Parse and save additional languages
            if (!string.IsNullOrWhiteSpace(Input.AdditionalLanguages))
            {
                var languages = Input.AdditionalLanguages
                    .Split(',')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct()
                    .ToList();

                user.LanguagesSpokenJson = JsonSerializer.Serialize(languages);
            }
            else
            {
                user.LanguagesSpokenJson = null;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = Input.SelectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(Input.SelectedRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
                
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            TempData["StatusMessage"] = "User has been updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}
