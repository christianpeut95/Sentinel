using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.Create")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPermissionService _permissionService;

        public CreateModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IPermissionService permissionService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _permissionService = permissionService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<string> AllRoles { get; set; } = new();
        public bool CanManageRoles { get; private set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Username")]
            public string? UserName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Email Confirmed")]
            public bool EmailConfirmed { get; set; }

            public List<string> SelectedRoles { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            CanManageRoles = await UserCanManageRolesAsync();
            if (CanManageRoles)
            {
                AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CanManageRoles = await UserCanManageRolesAsync();
            if (CanManageRoles)
            {
                AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            }

            if (Input.SelectedRoles.Any() && !CanManageRoles)
            {
                return Forbid();
            }

            if (CanManageRoles)
            {
                var allowedRoles = AllRoles.ToHashSet(StringComparer.Ordinal);
                if (Input.SelectedRoles.Any(role => !allowedRoles.Contains(role)))
                {
                    ModelState.AddModelError(nameof(Input.SelectedRoles), "One or more selected roles are invalid.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = string.IsNullOrEmpty(Input.UserName) ? Input.Email : Input.UserName,
                Email = Input.Email,
                EmailConfirmed = Input.EmailConfirmed
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Assign roles
                if (Input.SelectedRoles != null && Input.SelectedRoles.Any())
                {
                    var roleResult = await _userManager.AddToRolesAsync(user, Input.SelectedRoles);
                    if (!roleResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(user);
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }

                TempData["StatusMessage"] = $"User {user.Email} has been created successfully.";
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        private async Task<bool> UserCanManageRolesAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userId) &&
                   await _permissionService.HasPermissionAsync(
                       userId,
                       PermissionModule.User,
                       PermissionAction.ManageRoles);
        }
    }
}
