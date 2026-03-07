using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public static class PermissionSeedService
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            logger.LogInformation("Starting permission seeding...");

            // 1. Seed all permissions from enums
            var permissionsAdded = await SeedPermissionsAsync(context, logger);
            logger.LogInformation("Permission seeding complete: {Count} new permissions added", permissionsAdded);

            // 2. Create default roles
            var rolesAdded = await SeedRolesAsync(roleManager, logger);
            logger.LogInformation("Role seeding complete: {Count} new roles added", rolesAdded);

            // 3. Assign permissions to roles
            var assignmentsAdded = await SeedRolePermissionsAsync(context, roleManager, logger);
            logger.LogInformation("Role permission assignment complete: {Count} new assignments added", assignmentsAdded);

            logger.LogInformation("Permission seeding finished successfully");
        }

        private static async Task<int> SeedPermissionsAsync(ApplicationDbContext context, ILogger logger)
        {
            var modules = Enum.GetValues<PermissionModule>();
            var actions = Enum.GetValues<PermissionAction>();
            var addedCount = 0;

            logger.LogInformation("Checking {ModuleCount} modules x {ActionCount} actions = {TotalCount} potential permissions",
                modules.Length, actions.Length, modules.Length * actions.Length);

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
                        addedCount++;
                        logger.LogDebug("Added new permission: {PermissionKey}", permissionKey);
                    }
                }
            }

            if (addedCount > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Saved {Count} new permissions to database", addedCount);
            }
            else
            {
                logger.LogInformation("All permissions already exist - nothing to add");
            }

            return addedCount;
        }

        private static async Task<int> SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var roles = new[]
            {
                "Admin",
                "Surveillance Manager",
                "Surveillance Officer",
                "Data Entry",
                "Contact Tracer"
            };

            var addedCount = 0;

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    addedCount++;
                    logger.LogInformation("Created new role: {RoleName}", roleName);
                }
                else
                {
                    logger.LogDebug("Role already exists: {RoleName}", roleName);
                }
            }

            if (addedCount == 0)
            {
                logger.LogInformation("All roles already exist - nothing to add");
            }

            return addedCount;
        }

        private static async Task<int> SeedRolePermissionsAsync(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var permissions = await context.Permissions.ToListAsync();
            var permissionDict = permissions.ToDictionary(p => $"{p.Module}.{p.Action}", p => p.Id);

            logger.LogInformation("Loaded {Count} permissions for role assignment", permissions.Count);

            var totalAssignments = 0;

            // Admin - Full access
            var adminAssignments = await AssignPermissionsToRole(
                context, roleManager, "Admin",
                permissions.Select(p => p.Id).ToList(),
                logger);
            totalAssignments += adminAssignments;

            // Surveillance Manager - All except delete audit logs
            var managerAssignments = await AssignPermissionsToRole(
                context, roleManager, "Surveillance Manager",
                permissions.Where(p => !(p.Module == PermissionModule.Audit && p.Action == PermissionAction.Delete))
                    .Select(p => p.Id).ToList(),
                logger);
            totalAssignments += managerAssignments;

            // Surveillance Officer - View/Create/Edit cases, patients, tasks, surveys
            var officerAssignments = await AssignPermissionsToRole(
                context, roleManager, "Surveillance Officer", new[]
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
                    GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.Complete),
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
                }.Where(id => id.HasValue).Select(id => id!.Value).ToList(),
                logger);
            totalAssignments += officerAssignments;

            // Data Entry - View/Create patients and cases only
            var dataEntryAssignments = await AssignPermissionsToRole(
                context, roleManager, "Data Entry", new[]
                {
                    GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Create),
                    GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Edit),
                    GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.Search),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Create),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Edit),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Search)
                }.Where(id => id.HasValue).Select(id => id!.Value).ToList(),
                logger);
            totalAssignments += dataEntryAssignments;

            // Contact Tracer - Interview tasks, exposures, locations
            var tracerAssignments = await AssignPermissionsToRole(
                context, roleManager, "Contact Tracer", new[]
                {
                    GetPermission(permissionDict, PermissionModule.Patient, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Case, PermissionAction.Search),
                    GetPermission(permissionDict, PermissionModule.Task, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Task, PermissionAction.Edit),
                    GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.Create),
                    GetPermission(permissionDict, PermissionModule.Survey, PermissionAction.Complete),
                    GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Create),
                    GetPermission(permissionDict, PermissionModule.Exposure, PermissionAction.Edit),
                    GetPermission(permissionDict, PermissionModule.Location, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Location, PermissionAction.Create),
                    GetPermission(permissionDict, PermissionModule.Event, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Event, PermissionAction.Create)
                }.Where(id => id.HasValue).Select(id => id!.Value).ToList(),
                logger);
            totalAssignments += tracerAssignments;

            return totalAssignments;
        }

        private static int? GetPermission(Dictionary<string, int> permissionDict, PermissionModule module, PermissionAction action)
        {
            var key = $"{module}.{action}";
            return permissionDict.TryGetValue(key, out var id) ? id : null;
        }

        private static async Task<int> AssignPermissionsToRole(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            string roleName,
            List<int> permissionIds,
            ILogger logger)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                logger.LogWarning("Role not found: {RoleName} - cannot assign permissions", roleName);
                return 0;
            }

            var addedCount = 0;

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
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Assigned {Count} new permissions to role: {RoleName}", addedCount, roleName);
            }
            else
            {
                logger.LogDebug("Role {RoleName} already has all {Total} assigned permissions", roleName, permissionIds.Count);
            }

            return addedCount;
        }
    }
}
