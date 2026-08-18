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

            // 1. Synchronise the explicit permission catalogue.
            var (permissionsAdded, permissionsUpdated, permissionsRemoved) = await SynchronizePermissionsAsync(context);
            logger.LogInformation(
                "Permission synchronisation complete: {Added} added, {Updated} updated, {Removed} obsolete permissions removed",
                permissionsAdded,
                permissionsUpdated,
                permissionsRemoved);

            // 2. Create default roles
            var rolesAdded = await SeedRolesAsync(roleManager, logger);
            logger.LogInformation("Role seeding complete: {Count} new roles added", rolesAdded);

            // 3. Assign permissions to roles
            var assignmentsAdded = await SeedRolePermissionsAsync(context, roleManager, logger);
            logger.LogInformation("Role permission assignment complete: {Count} new assignments added", assignmentsAdded);

            logger.LogInformation("Permission seeding finished successfully");
        }

        private static async Task<(int Added, int Updated, int Removed)> SynchronizePermissionsAsync(
            ApplicationDbContext context)
        {
            var definitionsByName = PermissionCatalog.Definitions
                .ToDictionary(definition => $"{definition.Module}.{definition.Action}");
            var definedNames = definitionsByName.Keys
                .ToHashSet();

            await MergeDuplicatePermissionsAsync(context, definedNames);

            var existingPermissions = await context.Permissions.ToListAsync();

            // Module is persisted as an enum integer.  The stable Name key is therefore the
            // source of truth when reconciling an installation created before an enum member was
            // inserted.  Matching Module/Action alone can reinterpret a row as a different
            // permission and silently retain the wrong role assignments.
            var namedPermissions = existingPermissions
                .Where(permission => definedNames.Contains(permission.Name))
                .ToList();

            var permissionsToAdd = PermissionCatalog.Definitions
                .Where(definition => !namedPermissions.Any(permission =>
                    permission.Name == $"{definition.Module}.{definition.Action}"))
                .Select(definition => new Permission
                {
                    Module = definition.Module,
                    Action = definition.Action,
                    Name = $"{definition.Module}.{definition.Action}",
                    Description = definition.Description
                })
                .ToList();

            var obsoletePermissions = existingPermissions
                .Where(permission => !definedNames.Contains(permission.Name))
                .ToList();

            var permissionsToUpdate = namedPermissions
                .Where(permission =>
                {
                    var definition = definitionsByName[permission.Name];
                    return permission.Module != definition.Module ||
                           permission.Action != definition.Action ||
                           permission.Description != definition.Description;
                })
                .ToList();

            if (permissionsToAdd.Count == 0 && permissionsToUpdate.Count == 0 && obsoletePermissions.Count == 0)
            {
                return (0, 0, 0);
            }

            // Free the unique Module/Action index before applying corrected enum values.  This
            // avoids transient collisions where a sequence of shifted enum values rotates through
            // otherwise valid permission pairs (for example, Outbreak.View -> Task.View).
            foreach (var permission in permissionsToUpdate)
            {
                permission.Module = (PermissionModule)(-permission.Id);
            }

            if (permissionsToUpdate.Count > 0)
            {
                await context.SaveChangesAsync();
            }

            if (obsoletePermissions.Count > 0)
            {
                var obsoletePermissionIds = obsoletePermissions.Select(permission => permission.Id).ToList();
                context.RolePermissions.RemoveRange(
                    context.RolePermissions.Where(assignment => obsoletePermissionIds.Contains(assignment.PermissionId)));
                context.UserPermissions.RemoveRange(
                    context.UserPermissions.Where(assignment => obsoletePermissionIds.Contains(assignment.PermissionId)));
                context.Permissions.RemoveRange(obsoletePermissions);
                await context.SaveChangesAsync();
            }

            // Obsolete rows can occupy one of the corrected module/action pairs.  Delete them
            // before assigning the final values, otherwise SQL Server may process an insert or
            // update before its conflicting delete within the same SaveChanges batch.
            foreach (var permission in permissionsToUpdate)
            {
                var definition = definitionsByName[permission.Name];
                permission.Module = definition.Module;
                permission.Action = definition.Action;
                permission.Description = definition.Description;
            }

            if (permissionsToUpdate.Count > 0)
            {
                await context.SaveChangesAsync();
            }

            if (permissionsToAdd.Count > 0)
            {
                context.Permissions.AddRange(permissionsToAdd);
                await context.SaveChangesAsync();
            }

            return (permissionsToAdd.Count, permissionsToUpdate.Count, obsoletePermissions.Count);
        }

        private static async Task MergeDuplicatePermissionsAsync(
            ApplicationDbContext context,
            HashSet<string> definedNames)
        {
            var knownPermissions = await context.Permissions
                .Where(permission => definedNames.Contains(permission.Name))
                .Select(permission => new { permission.Id, permission.Name })
                .ToListAsync();

            var duplicateGroups = knownPermissions
                .GroupBy(permission => permission.Name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => new
                {
                    Name = group.Key,
                    PermissionIds = group.Select(permission => permission.Id).ToList()
                })
                .ToList();

            foreach (var group in duplicateGroups)
            {
                var canonicalPermissionId = group.PermissionIds.Min();
                var duplicatePermissionIds = group.PermissionIds
                    .Where(permissionId => permissionId != canonicalPermissionId)
                    .ToList();

                var roleAssignments = await context.RolePermissions
                    .Where(assignment => group.PermissionIds.Contains(assignment.PermissionId))
                    .ToListAsync();
                var userAssignments = await context.UserPermissions
                    .Where(assignment => group.PermissionIds.Contains(assignment.PermissionId))
                    .ToListAsync();

                foreach (var duplicatePermissionId in duplicatePermissionIds)
                {
                    foreach (var assignment in roleAssignments
                                 .Where(assignment => assignment.PermissionId == duplicatePermissionId)
                                 .ToList())
                    {
                        var canonicalAssignment = roleAssignments.FirstOrDefault(existing =>
                            existing.PermissionId == canonicalPermissionId &&
                            existing.RoleId == assignment.RoleId);

                        if (canonicalAssignment == null)
                        {
                            canonicalAssignment = new RolePermission
                            {
                                RoleId = assignment.RoleId,
                                PermissionId = canonicalPermissionId,
                                IsGranted = assignment.IsGranted
                            };
                            context.RolePermissions.Add(canonicalAssignment);
                            roleAssignments.Add(canonicalAssignment);
                        }
                        else
                        {
                            // Preserve an explicit deny if one exists on either duplicate row.
                            canonicalAssignment.IsGranted &= assignment.IsGranted;
                        }

                        context.RolePermissions.Remove(assignment);
                    }

                    foreach (var assignment in userAssignments
                                 .Where(assignment => assignment.PermissionId == duplicatePermissionId)
                                 .ToList())
                    {
                        var canonicalAssignment = userAssignments.FirstOrDefault(existing =>
                            existing.PermissionId == canonicalPermissionId &&
                            existing.UserId == assignment.UserId);

                        if (canonicalAssignment == null)
                        {
                            canonicalAssignment = new UserPermission
                            {
                                UserId = assignment.UserId,
                                PermissionId = canonicalPermissionId,
                                IsGranted = assignment.IsGranted
                            };
                            context.UserPermissions.Add(canonicalAssignment);
                            userAssignments.Add(canonicalAssignment);
                        }
                        else
                        {
                            // User-specific denials override role grants, so fail closed here too.
                            canonicalAssignment.IsGranted &= assignment.IsGranted;
                        }

                        context.UserPermissions.Remove(assignment);
                    }
                }

                var duplicatePermissions = await context.Permissions
                    .Where(permission => duplicatePermissionIds.Contains(permission.Id))
                    .ToListAsync();
                context.Permissions.RemoveRange(duplicatePermissions);
            }

            if (duplicateGroups.Count > 0)
            {
                await context.SaveChangesAsync();
            }
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
                    GetPermission(permissionDict, PermissionModule.Contact, PermissionAction.Import),
                    GetPermission(permissionDict, PermissionModule.Task, PermissionAction.View),
                    GetPermission(permissionDict, PermissionModule.Task, PermissionAction.Create),
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
