using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public static class DemoUserSeedService
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            logger.LogInformation("?? Starting demo user and role seeding...");

            // Create additional demo-specific roles
            await CreateDemoRolesAsync(roleManager, logger);

            // Assign permissions to demo roles
            await AssignDemoRolePermissionsAsync(roleManager, context, logger);

            // Create demo users
            await CreateDemoUsersAsync(userManager, context, logger);

            logger.LogInformation("? Demo user seeding complete");
        }

        private static async Task CreateDemoRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var demoRoles = new[]
            {
                "Contact Tracing Supervisor",
                "STI/BBV Surveillance Officer"
            };

            foreach (var roleName in demoRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation($"Created demo role: {roleName}");
                }
            }
        }

        private static async Task AssignDemoRolePermissionsAsync(
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger logger)
        {
            var permissions = await context.Permissions.ToListAsync();
            var permissionDict = permissions.ToDictionary(p => $"{p.Module}.{p.Action}", p => p.Id);

            // Helper method
            int? GetPermId(PermissionModule module, PermissionAction action)
            {
                var key = $"{module}.{action}";
                return permissionDict.TryGetValue(key, out var id) ? id : null;
            }

            // Contact Tracing Supervisor - Like Contact Tracer + Task management + reassignment
            var supervisorRole = await roleManager.FindByNameAsync("Contact Tracing Supervisor");
            if (supervisorRole != null)
            {
                var supervisorPermissions = new[]
                {
                    GetPermId(PermissionModule.Task, PermissionAction.View),
                    GetPermId(PermissionModule.Task, PermissionAction.Complete),
                    GetPermId(PermissionModule.Task, PermissionAction.Edit),        // Reassign tasks
                    GetPermId(PermissionModule.Task, PermissionAction.Create),
                    GetPermId(PermissionModule.Survey, PermissionAction.View),
                    GetPermId(PermissionModule.Survey, PermissionAction.Complete),
                    GetPermId(PermissionModule.Survey, PermissionAction.Create),
                    GetPermId(PermissionModule.Patient, PermissionAction.View),
                    GetPermId(PermissionModule.Patient, PermissionAction.Edit),
                    GetPermId(PermissionModule.Patient, PermissionAction.Create),
                    GetPermId(PermissionModule.Patient, PermissionAction.Search),
                    GetPermId(PermissionModule.Case, PermissionAction.View),
                    GetPermId(PermissionModule.Case, PermissionAction.Edit),
                    GetPermId(PermissionModule.Case, PermissionAction.Create),
                    GetPermId(PermissionModule.Case, PermissionAction.Search),
                    GetPermId(PermissionModule.Exposure, PermissionAction.View),
                    GetPermId(PermissionModule.Exposure, PermissionAction.Create),
                    GetPermId(PermissionModule.Exposure, PermissionAction.Edit),
                    GetPermId(PermissionModule.Location, PermissionAction.View),
                    GetPermId(PermissionModule.Location, PermissionAction.Create),
                    GetPermId(PermissionModule.Event, PermissionAction.View),
                    GetPermId(PermissionModule.Event, PermissionAction.Create),
                    GetPermId(PermissionModule.Report, PermissionAction.View),
                    GetPermId(PermissionModule.Report, PermissionAction.Export)
                }.Where(id => id.HasValue).Select(id => id!.Value).ToList();

                await AssignPermissionsToRoleAsync(context, supervisorRole.Id, supervisorPermissions, logger);
            }

            // STI/BBV Surveillance Officer - Same as Surveillance Officer (full operational access)
            var stiBbvRole = await roleManager.FindByNameAsync("STI/BBV Surveillance Officer");
            if (stiBbvRole != null)
            {
                // Get all permissions except Settings module
                var stiBbvPermissions = permissions
                    .Where(p => p.Module != PermissionModule.Settings)
                    .Select(p => p.Id)
                    .ToList();

                await AssignPermissionsToRoleAsync(context, stiBbvRole.Id, stiBbvPermissions, logger);
            }
        }

        private static async Task AssignPermissionsToRoleAsync(
            ApplicationDbContext context,
            string roleId,
            List<int> permissionIds,
            ILogger logger)
        {
            var existingPermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var newPermissions = permissionIds
                .Except(existingPermissions)
                .Select(permId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId,
                    IsGranted = true
                })
                .ToList();

            if (newPermissions.Any())
            {
                await context.RolePermissions.AddRangeAsync(newPermissions);
                await context.SaveChangesAsync();
                logger.LogInformation($"? Assigned {newPermissions.Count} permissions to role");
            }
        }

        private static async Task CreateDemoUsersAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger logger)
        {
            var demoUsers = new[]
            {
                new
                {
                    Email = "manager@sentinel-demo.com",
                    Password = "Demo123!@#Manager",
                    FirstName = "Emma",
                    LastName = "Chen",
                    Role = "Surveillance Manager",
                    IsInterviewWorker = false,
                    AvailableForAutoAssignment = false,
                    Description = "Full system access - All modules and settings"
                },
                new
                {
                    Email = "officer@sentinel-demo.com",
                    Password = "Demo123!@#Officer",
                    FirstName = "Isabella",
                    LastName = "Thompson",
                    Role = "Surveillance Officer",
                    IsInterviewWorker = true,
                    AvailableForAutoAssignment = true,
                    Description = "Full operational access (no Settings module)"
                },
                new
                {
                    Email = "tracer@sentinel-demo.com",
                    Password = "Demo123!@#Tracer",
                    FirstName = "Emma",
                    LastName = "Rodriguez",
                    Role = "Contact Tracer",
                    IsInterviewWorker = true,
                    AvailableForAutoAssignment = true,
                    Description = "Interview queue, task completion, survey completion"
                },
                new
                {
                    Email = "supervisor@sentinel-demo.com",
                    Password = "Demo123!@#Supervisor",
                    FirstName = "James",
                    LastName = "Wilson",
                    Role = "Contact Tracing Supervisor",
                    IsInterviewWorker = true,
                    AvailableForAutoAssignment = false,
                    Description = "Contact Tracer + task reassignment + case management"
                },
                new
                {
                    Email = "stiofficer@sentinel-demo.com",
                    Password = "Demo123!@#STI",
                    FirstName = "Megge",
                    LastName = "Patel",
                    Role = "STI/BBV Surveillance Officer",
                    IsInterviewWorker = true,
                    AvailableForAutoAssignment = true,
                    Description = "STI/BBV disease-specific surveillance (will get disease access)"
                }
            };

            foreach (var userInfo in demoUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(userInfo.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userInfo.Email,
                        Email = userInfo.Email,
                        EmailConfirmed = true,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        IsInterviewWorker = userInfo.IsInterviewWorker,
                        AvailableForAutoAssignment = userInfo.AvailableForAutoAssignment,
                        CurrentTaskCapacity = 10
                    };

                    var result = await userManager.CreateAsync(user, userInfo.Password);
                    
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userInfo.Role);
                        logger.LogInformation($"? Created demo user: {userInfo.Email} ({userInfo.FirstName} {userInfo.LastName}) - {userInfo.Role}");
                    }
                    else
                    {
                        logger.LogError($"? Failed to create demo user {userInfo.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    logger.LogInformation($"??  Demo user already exists: {userInfo.Email}");
                }
            }

            // Display demo credentials summary
            logger.LogInformation("");
            logger.LogInformation("========================================");
            logger.LogInformation("?? DEMO USERS AVAILABLE:");
            logger.LogInformation("========================================");
            foreach (var userInfo in demoUsers)
            {
                logger.LogInformation($"?? Email: {userInfo.Email}");
                logger.LogInformation($"   ?? Password: {userInfo.Password}");
                logger.LogInformation($"   ?? Name: {userInfo.FirstName} {userInfo.LastName}");
                logger.LogInformation($"   ?? Role: {userInfo.Role}");
                logger.LogInformation($"   ?? {userInfo.Description}");
                logger.LogInformation("----------------------------------------");
            }
            logger.LogInformation("========================================");
            logger.LogInformation("");
        }
    }
}
