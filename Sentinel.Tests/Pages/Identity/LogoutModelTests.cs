using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sentinel.Areas.Identity.Pages.Account;
using Sentinel.Models;

namespace Sentinel.Tests.Pages.Identity;

public sealed class LogoutModelTests
{
    [Fact]
    public async Task OnPostAsync_AuthenticatedUser_InvalidatesSecurityStampAndSignsOut()
    {
        var user = new ApplicationUser { Id = "mock-account", IsEnabled = true };
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(manager => manager.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.SignOutAsync()).Returns(Task.CompletedTask);

        var model = CreateModel(userManager.Object, signInManager.Object);

        var result = await model.OnPostAsync("/Dashboard");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Dashboard", redirect.Url);
        userManager.Verify(manager => manager.UpdateSecurityStampAsync(user), Times.Once);
        signInManager.Verify(manager => manager.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ExternalReturnUrl_IsNotUsed()
    {
        var userManager = CreateUserManager();
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        var signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.SignOutAsync()).Returns(Task.CompletedTask);

        var model = CreateModel(userManager.Object, signInManager.Object);

        var result = await model.OnPostAsync("https://example.invalid/redirect");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
        signInManager.Verify(manager => manager.SignOutAsync(), Times.Once);
    }

    private static LogoutModel CreateModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "mock-account")], "Test"))
        };

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(helper => helper.IsLocalUrl(It.IsAny<string?>()))
            .Returns((string? url) => !string.IsNullOrWhiteSpace(url)
                && url.StartsWith('/')
                && !url.StartsWith("//", StringComparison.Ordinal)
                && !url.StartsWith("/\\", StringComparison.Ordinal));

        return new LogoutModel(userManager, signInManager, NullLogger<LogoutModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            Url = urlHelper.Object
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
