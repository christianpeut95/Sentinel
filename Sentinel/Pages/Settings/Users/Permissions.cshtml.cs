using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.ManagePermissions")]
    public class PermissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionService _permissionService;

        public PermissionsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPermissionService permissionService)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
        }

        public ApplicationUser User { get; set; } = default!;
        public List<Permission> AllPermissions { get; set; } = new();
        public HashSet<int> GrantedPermissionIds { get; set; } = new();
        public HashSet<int> RolePermissionIds { get; set; } = new();
        public Dictionary<PermissionModule, List<Permission>> PermissionsByModule { get; set; } = new();
        public List<string> UserRoles { get; set; } = new();

        [BindProperty]
        public List<int> SelectedPermissions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            User = user;
            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();

            AllPermissions = await _permissionService.GetAllPermissionsAsync();
            
            // Get user-specific permissions
            var userPermissions = await _context.UserPermissions
                .Where(up => up.UserId == id && up.IsGranted)
                .Select(up => up.PermissionId)
                .ToListAsync();
            GrantedPermissionIds = userPermissions.ToHashSet();

            // Get role-based permissions (for display only)
            var rolePermissions = new HashSet<int>();
            foreach (var roleName in UserRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var permissions = await _permissionService.GetRolePermissionsAsync(role.Id);
                    foreach (var p in permissions)
                    {
                        rolePermissions.Add(p.Id);
                    }
                }
            }
            RolePermissionIds = rolePermissions;

            PermissionsByModule = AllPermissions
                .GroupBy(p => p.Module)
                .ToDictionary(g => g.Key, g => g.ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserPermissions = await _context.UserPermissions
                .Where(up => up.UserId == id)
                .Select(up => up.PermissionId)
                .ToListAsync();

            // Remove permissions that were unchecked
            foreach (var permissionId in currentUserPermissions)
            {
                if (!SelectedPermissions.Contains(permissionId))
                {
                    await _permissionService.RevokePermissionFromUserAsync(id, permissionId);
                }
            }

            // Add permissions that were checked
            foreach (var permissionId in SelectedPermissions)
            {
                if (!currentUserPermissions.Contains(permissionId))
                {
                    await _permissionService.GrantPermissionToUserAsync(id, permissionId);
                }
            }

            TempData["SuccessMessage"] = $"Permissions updated for user '{user.Email}'.";
            return RedirectToPage("./Index");
        }
    }
}
