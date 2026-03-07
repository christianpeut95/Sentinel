using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Sentinel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.View")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
    }
}
