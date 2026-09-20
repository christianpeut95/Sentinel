using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sentinel.Areas.Identity.Pages.Account.Manage;
using Sentinel.Models;

namespace Sentinel.Tests.Pages.Identity;

public sealed class ChangePasswordModelTests
{
    [Fact]
    public async Task OnPostAsync_ValidMockAccountPasswordChange_RefreshesCurrentSession()
    {
        var user = new ApplicationUser { Id = "mock-account", IsEnabled = true };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(manager => manager.ChangePasswordAsync(user, "CurrentPassword1!", "NewPassword2!"))
            .ReturnsAsync(IdentityResult.Success);

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var model = CreateModel(userManager.Object, signInManager.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "CurrentPassword1!",
            NewPassword = "NewPassword2!",
            ConfirmPassword = "NewPassword2!"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Your password has been changed.", model.StatusMessage);
        userManager.Verify(manager => manager.ChangePasswordAsync(user, "CurrentPassword1!", "NewPassword2!"), Times.Once);
        signInManager.Verify(manager => manager.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_IncorrectCurrentPassword_DoesNotRefreshSession()
    {
        var user = new ApplicationUser { Id = "mock-account", IsEnabled = true };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(manager => manager.ChangePasswordAsync(user, "WrongPassword1!", "NewPassword2!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" }));

        var signInManager = CreateSignInManager(userManager.Object);
        var model = CreateModel(userManager.Object, signInManager.Object);
        model.Input = new ChangePasswordModel.InputModel
        {
            CurrentPassword = "WrongPassword1!",
            NewPassword = "NewPassword2!",
            ConfirmPassword = "NewPassword2!"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "The current password is incorrect.");
        signInManager.Verify(manager => manager.RefreshSignInAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static ChangePasswordModel CreateModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "mock-account")], "Test"))
        };

        return new ChangePasswordModel(userManager, signInManager, NullLogger<ChangePasswordModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() => new(
        new Mock<IUserStore<ApplicationUser>>().Object,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!);

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(UserManager<ApplicationUser> userManager) => new(
        userManager,
        new HttpContextAccessor(),
        new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<ApplicationUser>>.Instance,
        new Mock<IAuthenticationSchemeProvider>().Object,
        new Mock<IUserConfirmation<ApplicationUser>>().Object);
}
