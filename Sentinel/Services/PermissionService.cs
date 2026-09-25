using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public PermissionService(
            ApplicationDbContext context,
            IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _context = context;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<bool> HasPermissionAsync(string userId, PermissionModule module, PermissionAction action)
        {
            var permissionKey = $"{module}.{action}";
            return await HasPermissionAsync(userId, permissionKey);
        }

        public async Task<bool> HasPermissionAsync(string userId, string permissionKey)
        {
            // Permission checks can run concurrently in an interactive Blazor circuit.
            // A scoped DbContext is not thread-safe, so authorization always uses a
            // fresh, short-lived read context rather than the request/circuit context.
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var permissionId = await context.Permissions
                .AsNoTracking()
                .Where(p => p.Name == permissionKey)
                .Select(p => (int?)p.Id)
                .SingleOrDefaultAsync();

            if (permissionId is null) return false;

            // Check user-specific permission (overrides role permissions)
            var userPermission = await context.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId && up.PermissionId == permissionId.Value)
                .Select(up => (bool?)up.IsGranted)
                .SingleOrDefaultAsync();

            if (userPermission.HasValue)
            {
                return userPermission.Value;
            }

            // Check role-based permissions in one no-tracking query.
            return await (
                from userRole in context.UserRoles.AsNoTracking()
                join rolePermission in context.RolePermissions.AsNoTracking()
                    on userRole.RoleId equals rolePermission.RoleId
                where userRole.UserId == userId
                    && rolePermission.PermissionId == permissionId.Value
                    && rolePermission.IsGranted
                select rolePermission.PermissionId)
                .AnyAsync();
        }

        public async Task<List<Permission>> GetUserPermissionsAsync(string userId)
        {
            // Claims transformation can also be invoked while an interactive circuit
            // performs another query. Use an independent context for these reads.
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var directPermissions = await context.UserPermissions
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId)
                .ToListAsync();

            var directByPermissionId = directPermissions
                .GroupBy(up => up.PermissionId)
                .ToDictionary(group => group.Key, group => group.First().IsGranted);

            var rolePermissions = await (
                from userRole in context.UserRoles.AsNoTracking()
                join rolePermission in context.RolePermissions
                    .AsNoTracking()
                    .Include(rp => rp.Permission)
                    on userRole.RoleId equals rolePermission.RoleId
                where userRole.UserId == userId && rolePermission.IsGranted
                select rolePermission)
                .ToListAsync();

            var effectivePermissions = rolePermissions
                .Where(rp => !directByPermissionId.ContainsKey(rp.PermissionId))
                .Select(rp => rp.Permission!)
                .Concat(directPermissions
                    .Where(up => up.IsGranted)
                    .Select(up => up.Permission!))
                .GroupBy(permission => permission.Id)
                .Select(group => group.First())
                .ToList();

            return effectivePermissions;
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
