using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sentinel.Areas.Identity.Pages.Account;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class PasswordResetExpiryTests
{
    private const string OriginalPassword = "originalmeadowpassphrase";
    private const string ReplacementPassword = "replacementorchardpassphrase";

    [Fact]
    public async Task FreshToken_ChangesPassword_AndCannotBeReused()
    {
        using var fixture = new IdentityFixture(TimeSpan.FromMinutes(1));
        var token = await fixture.Manager.GeneratePasswordResetTokenAsync(fixture.User);

        var result = await fixture.Manager.ResetPasswordAsync(fixture.User, token, ReplacementPassword);

        Assert.True(result.Succeeded);
        Assert.True(await fixture.Manager.CheckPasswordAsync(fixture.User, ReplacementPassword));
        Assert.False(await fixture.Manager.CheckPasswordAsync(fixture.User, OriginalPassword));

        var reused = await fixture.Manager.ResetPasswordAsync(fixture.User, token, OriginalPassword);
        Assert.Contains(reused.Errors, error => error.Code == "InvalidToken");
        Assert.True(await fixture.Manager.CheckPasswordAsync(fixture.User, ReplacementPassword));
    }

    [Fact]
    public async Task ExpiredToken_ResetPageRejectsIt_WithoutChangingPasswordOrSecurityStamp()
    {
        var lifetime = TimeSpan.FromSeconds(2);
        using var fixture = new IdentityFixture(lifetime);
        var token = await fixture.Manager.GeneratePasswordResetTokenAsync(fixture.User);
        Assert.True(await fixture.Manager.VerifyUserTokenAsync(fixture.User,
            TokenOptions.DefaultProvider, "ResetPassword", token));
        var originalHash = fixture.User.PasswordHash;
        var originalStamp = fixture.User.SecurityStamp;

        // Exercise real elapsed-time expiry in the standard Identity provider.
        // This override exists only in this fixture, with ephemeral keys and a mock store.
        await Task.Delay(lifetime + TimeSpan.FromSeconds(1));

        var page = new ResetPasswordModel(fixture.Manager, NullLogger<ResetPasswordModel>.Instance);
        page.Input = new ResetPasswordModel.InputModel
        {
            UserId = fixture.User.Id,
            Code = PasswordResetTokenEncoding.Encode(token),
            Password = ReplacementPassword,
            ConfirmPassword = ReplacementPassword
        };

        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.False(page.ResetSuccessful);
        Assert.Contains(page.ModelState.Values.SelectMany(value => value.Errors),
            error => error.ErrorMessage == "The password reset link is invalid or has expired. Please request a new one.");
        Assert.Equal(originalHash, fixture.User.PasswordHash);
        Assert.Equal(originalStamp, fixture.User.SecurityStamp);
        Assert.True(await fixture.Manager.CheckPasswordAsync(fixture.User, OriginalPassword));
        Assert.False(await fixture.Manager.CheckPasswordAsync(fixture.User, ReplacementPassword));
        fixture.Store.Verify(store => store.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void IdentityDefaultTokenLifetime_IsOneDay() =>
        Assert.Equal(TimeSpan.FromDays(1), new DataProtectionTokenProviderOptions().TokenLifespan);

    private sealed class IdentityFixture : IDisposable
    {
        public ApplicationUser User { get; } = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "reset-expiry@sentinel.invalid",
            SecurityStamp = Guid.NewGuid().ToString(),
            IsEnabled = true
        };

        public Mock<IUserPasswordStore<ApplicationUser>> Store { get; } = new();
        public UserManager<ApplicationUser> Manager { get; }

        public IdentityFixture(TimeSpan lifetime)
        {
            Store.Setup(s => s.GetUserIdAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(() => User.Id);
            Store.Setup(s => s.FindByIdAsync(User.Id, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            Store.Setup(s => s.GetUserNameAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(() => User.UserName);
            Store.Setup(s => s.GetPasswordHashAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(() => User.PasswordHash);
            Store.Setup(s => s.SetPasswordHashAsync(User, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<ApplicationUser, string, CancellationToken>((user, hash, _) => user.PasswordHash = hash)
                .Returns(Task.CompletedTask);
            Store.Setup(s => s.UpdateAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);
            var stamps = Store.As<IUserSecurityStampStore<ApplicationUser>>();
            stamps.Setup(s => s.GetSecurityStampAsync(User, It.IsAny<CancellationToken>())).ReturnsAsync(() => User.SecurityStamp);
            stamps.Setup(s => s.SetSecurityStampAsync(User, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<ApplicationUser, string, CancellationToken>((user, stamp, _) => user.SecurityStamp = stamp)
                .Returns(Task.CompletedTask);

            var options = new IdentityOptions();
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            var hasher = new PasswordHasher<ApplicationUser>();
            User.PasswordHash = hasher.HashPassword(User, OriginalPassword);
            Manager = new UserManager<ApplicationUser>(Store.Object, Options.Create(options), hasher,
                [], [new PasswordValidator<ApplicationUser>()], new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), NullLogger<UserManager<ApplicationUser>>.Instance);
            Manager.RegisterTokenProvider(TokenOptions.DefaultProvider,
                new DataProtectorTokenProvider<ApplicationUser>(new EphemeralDataProtectionProvider(),
                    Options.Create(new DataProtectionTokenProviderOptions { TokenLifespan = lifetime }),
                    NullLogger<DataProtectorTokenProvider<ApplicationUser>>.Instance));
        }

        public void Dispose() => Manager.Dispose();
    }
}
