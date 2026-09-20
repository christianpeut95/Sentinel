using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Pages.Settings.Users;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Tests.Pages.Settings.Users;

public sealed class UserPermissionAdministrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public UserPermissionAdministrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PermissionsPost_InvalidPermissionId_ReturnsPageWithoutChangingPermissions()
    {
        var targetUser = new ApplicationUser { Id = "target-user", Email = "target@example.test" };
        var validPermission = new Permission
        {
            Id = 101,
            Module = PermissionModule.Patient,
            Action = PermissionAction.View,
            Name = "Patient.View",
            Description = "View patients"
        };

        _context.Permissions.Add(validPermission);
        _context.UserPermissions.Add(new UserPermission
        {
            UserId = targetUser.Id,
            PermissionId = validPermission.Id,
            IsGranted = true
        });
        await _context.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
        userManager.Setup(manager => manager.GetRolesAsync(targetUser)).ReturnsAsync([]);

        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(service => service.GetAllPermissionsAsync())
            .ReturnsAsync([validPermission]);

        var model = new PermissionsModel(_context, userManager.Object, permissionService.Object)
        {
            SelectedPermissions = [999999]
        };

        var result = await model.OnPostAsync(targetUser.Id);

        Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.ErrorCount > 0);
        var persistedPermissionIds = await _context.UserPermissions
            .Where(permission => permission.UserId == targetUser.Id)
            .Select(permission => permission.PermissionId)
            .ToListAsync();
        Assert.Equal([validPermission.Id], persistedPermissionIds);
    }

    [Fact]
    public async Task EditUserPost_WithoutManageRoles_RejectsForgedRoleField()
    {
        var actingUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "acting-user")], "Test"));
        var targetUser = new ApplicationUser { Id = "target-user", Email = "target@example.test" };

        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.FindByIdAsync(targetUser.Id)).ReturnsAsync(targetUser);
        userManager.Setup(manager => manager.GetRolesAsync(targetUser)).ReturnsAsync(["Data Entry"]);

        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(service => service.HasPermissionAsync(
                "acting-user",
                PermissionModule.User,
                PermissionAction.ManageRoles))
            .ReturnsAsync(false);

        var httpContext = new DefaultHttpContext
        {
            User = actingUser
        };
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["Input.SelectedRoles"] = "Admin"
        });

        var roleManager = new Mock<RoleManager<IdentityRole>>(
            new Mock<IRoleStore<IdentityRole>>().Object,
            null!,
            null!,
            null!,
            null!);
        var model = new EditModel(userManager.Object, roleManager.Object, permissionService.Object)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            Input = new EditModel.InputModel
            {
                Id = targetUser.Id,
                Email = targetUser.Email,
                SelectedRoles = ["Admin"]
            }
        };

        var result = await model.OnPostAsync("save");

        Assert.IsType<ForbidResult>(result);
        userManager.Verify(manager => manager.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        userManager.Verify(manager => manager.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    public void Dispose() => _context.Dispose();

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        return new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
