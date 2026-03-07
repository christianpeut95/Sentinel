using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.Create")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<string> AllRoles { get; set; } = new();

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

        public void OnGet()
        {
            AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();

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
                    await _userManager.AddToRolesAsync(user, Input.SelectedRoles);
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
    }
}
