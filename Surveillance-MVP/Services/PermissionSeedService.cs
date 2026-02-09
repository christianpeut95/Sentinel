using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public static class PermissionSeedService
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Seed all permissions from enums
            await SeedPermissionsAsync(context);

            // 2. Create default roles
            await SeedRolesAsync(roleManager);

            // 3. Assign permissions to roles
            await SeedRolePermissionsAsync(context, roleManager);
        }

        private static async Task SeedPermissionsAsync(ApplicationDbContext context)
        {
            var modules = Enum.GetValues<PermissionModule>();
            var actions = Enum.GetValues<PermissionAction>();

            foreach (var module in modules)
            {
                foreach (var action in actions)
                {
                    var permissionKey = $"{module}.{action}";

                    var exists = await context.Permissions
                        .AnyAsync(p => p.Module == module && p.Action == action);

                    if (!exists)
                    {
                        context.Permissions.Add(new Permission
                        {
                            Module = module,
                            Action = action,
                            Name = permissionKey,
                            Description = $"{action} {module}"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[]
            {
                "Admin",
                "Surveillance Manager",
                "Surveillance Officer",
                "Data Entry",
                "Contact Tracer"
            };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            var permissions = await context.Permissions.ToListAsync();
            var permissionDict = permissions.ToDictionary(p => $"{p.Module}.{p.Action}", p => p.Id);

            // Admin - Full access
            await AssignPermissionsToRole(context, roleManager, "Admin", 
                permissions.Select(p => p.Id).ToList());

            // Surveillance Manager - All except delete audit logs
            await AssignPermissionsToRole(context, roleManager, "Surveillance Manager",
                permissions.Where(p => !(p.Module == PermissionModule.Audit && p.Action == PermissionAction.Delete))
                    .Select(p => p.Id).ToList());

            // Surveillance Officer - View/Create/Edit cases, patients, tasks, surveys
            await AssignPermissionsToRole(context, roleManager, "Surveillance Officer", new[]
            {
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Search),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Search),
                GetPermission(permissionDict, PermissionModule.Task, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Task, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Task, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Outbreak, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Outbreak, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Outbreak, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Location, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Event, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Report, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Report, PermissionAction.Export)
            }.Where(id => id.HasValue).Select(id => id!.Value).ToList());

            // Data Entry - View/Create patients and cases only
            await AssignPermissionsToRole(context, roleManager, "Data Entry", new[]
            {
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Search),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Search)
            }.Where(id => id.HasValue).Select(id => id!.Value).ToList());

            // Contact Tracer - Interview tasks, exposures, locations
            await AssignPermissionsToRole(context, roleManager, "Contact Tracer", new[]
            {
                GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Search),
                GetPermission(permissionDict, PermissionModule.Task, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Task, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Edit),
                GetPermission(permissionDict, PermissionModule.Location, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Location, PermissionAction.Create),
                GetPermission(permissionDict, PermissionModule.Event, PermissionAction.View),
                GetPermission(permissionDict, PermissionModule.Event, PermissionAction.Create)
            }.Where(id => id.HasValue).Select(id => id!.Value).ToList());
        }

        private static int? GetPermission(Dictionary<string, int> permissionDict, PermissionModule module, PermissionAction action)
        {
            var key = $"{module}.{action}";
            return permissionDict.TryGetValue(key, out var id) ? id : null;
        }

        private static async Task AssignPermissionsToRole(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, string roleName, List<int> permissionIds)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null) return;

            foreach (var permissionId in permissionIds)
            {
                var exists = await context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permissionId);

                if (!exists)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        IsGranted = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
