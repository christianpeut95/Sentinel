using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Microsoft.AspNetCore.Identity;

namespace Surveillance_MVP.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<bool> HasPermissionAsync(string userId, PermissionModule module, PermissionAction action)
        {
            var permissionKey = $"{module}.{action}";
            return await HasPermissionAsync(userId, permissionKey);
        }

        public async Task<bool> HasPermissionAsync(string userId, string permissionKey)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionKey);

            if (permission == null) return false;

            // Check user-specific permission (overrides role permissions)
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permission.Id);

            if (userPermission != null)
            {
                return userPermission.IsGranted;
            }

            // Check role-based permissions
            var userRoles = await _userManager.GetRolesAsync(user);
            
            foreach (var roleName in userRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null) continue;

                var rolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                if (rolePermission != null && rolePermission.IsGranted)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<List<Permission>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<Permission>();

            var permissions = new List<Permission>();

            // Get direct user permissions
            var userPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && up.IsGranted)
                .Select(up => up.Permission!)
                .ToListAsync();

            permissions.AddRange(userPermissions);

            // Get role-based permissions
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in userRoles)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null) continue;

                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == role.Id && rp.IsGranted)
                    .Select(rp => rp.Permission!)
                    .ToListAsync();

                permissions.AddRange(rolePermissions);
            }

            return permissions.Distinct().ToList();
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(string roleId)
        {
            return await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId && rp.IsGranted)
                .Select(rp => rp.Permission!)
                .ToListAsync();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync();
        }

        public async Task GrantPermissionToUserAsync(string userId, int permissionId)
        {
            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            if (existing != null)
            {
                existing.IsGranted = true;
            }
            else
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = permissionId,
                    IsGranted = true
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokePermissionFromUserAsync(string userId, int permissionId)
        {
            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

            if (existing != null)
            {
                _context.UserPermissions.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task GrantPermissionToRoleAsync(string roleId, int permissionId)
        {
            var existing = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existing != null)
            {
                existing.IsGranted = true;
            }
            else
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    IsGranted = true
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokePermissionFromRoleAsync(string roleId, int permissionId)
        {
            var existing = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existing != null)
            {
                _context.RolePermissions.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<PermissionModule, List<PermissionAction>>> GetUserPermissionMatrixAsync(string userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            
            var matrix = new Dictionary<PermissionModule, List<PermissionAction>>();
            
            foreach (var permission in permissions)
            {
                if (!matrix.ContainsKey(permission.Module))
                {
                    matrix[permission.Module] = new List<PermissionAction>();
                }
                matrix[permission.Module].Add(permission.Action);
            }

            return matrix;
        }
    }
}
