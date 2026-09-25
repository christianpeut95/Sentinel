using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public class PermissionServiceConcurrencyTests
{
    [Fact]
    public async Task HasPermissionAsync_uses_independent_contexts_for_concurrent_checks()
    {
        var (service, _) = await CreateServiceAsync(
            new Permission { Id = 1, Module = PermissionModule.Case, Action = PermissionAction.View, Name = "Case.View" },
            new Permission { Id = 2, Module = PermissionModule.Patient, Action = PermissionAction.View, Name = "Patient.View" },
            rolePermissions:
            [
                new RolePermission { RoleId = "investigator", PermissionId = 1, IsGranted = true },
                new RolePermission { RoleId = "investigator", PermissionId = 2, IsGranted = true }
            ]);

        var checks = await Task.WhenAll(
            service.HasPermissionAsync("user-1", "Case.View"),
            service.HasPermissionAsync("user-1", "Patient.View"));

        Assert.All(checks, Assert.True);
    }

    [Fact]
    public async Task HasPermissionAsync_direct_denial_overrides_a_role_grant()
    {
        var (service, _) = await CreateServiceAsync(
            new Permission { Id = 1, Module = PermissionModule.Case, Action = PermissionAction.View, Name = "Case.View" },
            rolePermissions:
            [new RolePermission { RoleId = "investigator", PermissionId = 1, IsGranted = true }],
            userPermissions:
            [new UserPermission { UserId = "user-1", PermissionId = 1, IsGranted = false }]);

        var result = await service.HasPermissionAsync("user-1", "Case.View");

        Assert.False(result);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_applies_direct_overrides_to_role_permissions()
    {
        var (service, _) = await CreateServiceAsync(
            new Permission { Id = 1, Module = PermissionModule.Case, Action = PermissionAction.View, Name = "Case.View" },
            new Permission { Id = 2, Module = PermissionModule.Patient, Action = PermissionAction.View, Name = "Patient.View" },
            new Permission { Id = 3, Module = PermissionModule.Task, Action = PermissionAction.View, Name = "Task.View" },
            rolePermissions:
            [
                new RolePermission { RoleId = "investigator", PermissionId = 1, IsGranted = true },
                new RolePermission { RoleId = "investigator", PermissionId = 2, IsGranted = true }
            ],
            userPermissions:
            [
                new UserPermission { UserId = "user-1", PermissionId = 1, IsGranted = false },
                new UserPermission { UserId = "user-1", PermissionId = 3, IsGranted = true }
            ]);

        var permissions = await service.GetUserPermissionsAsync("user-1");

        Assert.Equal(["Patient.View", "Task.View"], permissions.Select(permission => permission.Name).OrderBy(name => name));
    }

    private static async Task<(PermissionService Service, PooledDbContextFactory<ApplicationDbContext> Factory)> CreateServiceAsync(
        Permission firstPermission,
        Permission? secondPermission = null,
        Permission? thirdPermission = null,
        IReadOnlyCollection<RolePermission>? rolePermissions = null,
        IReadOnlyCollection<UserPermission>? userPermissions = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setupContext = new ApplicationDbContext(options))
        {
            setupContext.Permissions.Add(firstPermission);
            if (secondPermission is not null) setupContext.Permissions.Add(secondPermission);
            if (thirdPermission is not null) setupContext.Permissions.Add(thirdPermission);

            setupContext.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = "user-1",
                RoleId = "investigator"
            });

            if (rolePermissions is not null) setupContext.RolePermissions.AddRange(rolePermissions);
            if (userPermissions is not null) setupContext.UserPermissions.AddRange(userPermissions);
            await setupContext.SaveChangesAsync();
        }

        var factory = new PooledDbContextFactory<ApplicationDbContext>(options);
        var scopedContext = new ApplicationDbContext(options);
        return (new PermissionService(scopedContext, factory), factory);
    }
}
