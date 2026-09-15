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

            await LoadPermissionsAsync(user);

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

            // Treat IDs from the request as untrusted.  Do this before changing
            // anything so an invalid or stale form cannot partially update access.
            var selectedPermissionIds = SelectedPermissions.Distinct().ToHashSet();
            var validPermissionIds = await _context.Permissions
                .Select(permission => permission.Id)
                .ToListAsync();

            if (!selectedPermissionIds.IsSubsetOf(validPermissionIds))
            {
                ModelState.AddModelError(string.Empty, "The submitted permission selection is no longer valid. Refresh the page and try again.");
                await LoadPermissionsAsync(user);
                return Page();
            }

            var currentUserPermissions = await _context.UserPermissions
                .Where(up => up.UserId == id)
                .ToListAsync();

            var currentPermissionIds = currentUserPermissions
                .Select(permission => permission.PermissionId)
                .ToHashSet();

            var permissionsToRevoke = currentUserPermissions
                .Where(permission => !selectedPermissionIds.Contains(permission.PermissionId))
                .ToList();

            var permissionsToGrant = selectedPermissionIds
                .Except(currentPermissionIds)
                .Select(permissionId => new UserPermission
                {
                    UserId = id,
                    PermissionId = permissionId,
                    IsGranted = true
                })
                .ToList();

            if (permissionsToRevoke.Count > 0 || permissionsToGrant.Count > 0)
            {
                _context.UserPermissions.RemoveRange(permissionsToRevoke);
                _context.UserPermissions.AddRange(permissionsToGrant);

                // Save the complete diff and session invalidation together. EF Core
                // wraps a single SaveChanges call in a transaction where required;
                // explicitly starting one here conflicts with SQL Server's retrying
                // execution strategy.
                user.SecurityStamp = Guid.NewGuid().ToString();
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Permissions updated for user '{user.Email}'.";
            return RedirectToPage("./Index");
        }

        private async Task LoadPermissionsAsync(ApplicationUser user)
        {
            User = user;
            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();
            AllPermissions = await _permissionService.GetAllPermissionsAsync();

            GrantedPermissionIds = (await _context.UserPermissions
                    .Where(up => up.UserId == user.Id && up.IsGranted)
                    .Select(up => up.PermissionId)
                    .ToListAsync())
                .ToHashSet();

            var rolePermissions = new HashSet<int>();
            foreach (var roleName in UserRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var permissions = await _permissionService.GetRolePermissionsAsync(role.Id);
                    foreach (var permission in permissions)
                    {
                        rolePermissions.Add(permission.Id);
                    }
                }
            }

            RolePermissionIds = rolePermissions;
            PermissionsByModule = AllPermissions
                .GroupBy(permission => permission.Module)
                .ToDictionary(group => group.Key, group => group.ToList());
        }
    }
}
