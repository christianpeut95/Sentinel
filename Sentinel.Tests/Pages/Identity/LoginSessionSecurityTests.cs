using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Models;
using Sentinel.Tests.Authorization;

namespace Sentinel.Tests.Pages.Identity;

/// <summary>
/// Regression coverage for exact password handling and session fixation: both
/// tests use the complete Razor Page, antiforgery and Identity-cookie pipeline.
/// </summary>
public sealed class LoginSessionSecurityTests : IClassFixture<IdentityCookieWebApplicationFactory>
{
    private readonly IdentityCookieWebApplicationFactory _factory;

    public LoginSessionSecurityTests(IdentityCookieWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_UsesThePasswordExactlyAsSubmitted()
    {
        using var client = CreateClient();
        var email = $"exact-password-{Guid.NewGuid():N}@example.test";
        const string password = "exact-Mixed Passphrase-2026";
        await CreateUserAsync(email, password);

        using var caseChanged = await LogInAsync(client, email, "Exact-Mixed Passphrase-2026");
        Assert.Equal(HttpStatusCode.OK, caseChanged.Response.StatusCode);
        var caseChangedBody = await caseChanged.Response.Content.ReadAsStringAsync();
        Assert.Contains("The sign-in attempt was unsuccessful", caseChangedBody, StringComparison.Ordinal);
        Assert.Null(caseChanged.AuthenticationCookie);

        using var whitespaceChanged = await LogInAsync(client, email, password + " ");
        Assert.Equal(HttpStatusCode.OK, whitespaceChanged.Response.StatusCode);
        var whitespaceChangedBody = await whitespaceChanged.Response.Content.ReadAsStringAsync();
        Assert.Contains("The sign-in attempt was unsuccessful", whitespaceChangedBody, StringComparison.Ordinal);
        Assert.Null(whitespaceChanged.AuthenticationCookie);

        using var exact = await LogInAsync(client, email, password);
        Assert.Equal(HttpStatusCode.Redirect, exact.Response.StatusCode);
        Assert.NotNull(exact.AuthenticationCookie);
    }

    [Fact]
    public async Task Login_WithAnEnabledAuthenticator_RedirectsToTheTwoFactorChallenge()
    {
        using var client = CreateClient();
        var email = $"two-factor-{Guid.NewGuid():N}@example.test";
        const string password = "two-factor-test-passphrase-2026";
        await CreateUserAsync(email, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = Assert.IsType<ApplicationUser>(await userManager.FindByEmailAsync(email));
            Assert.True((await userManager.ResetAuthenticatorKeyAsync(user)).Succeeded);
            Assert.True((await userManager.SetTwoFactorEnabledAsync(user, true)).Succeeded);
        }

        using var login = await LogInAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Redirect, login.Response.StatusCode);
        var redirect = Assert.IsType<Uri>(login.Response.Headers.Location);
        Assert.StartsWith(
            "/Identity/Account/LoginWith2fa",
            redirect.IsAbsoluteUri ? redirect.AbsolutePath : redirect.OriginalString,
            StringComparison.Ordinal);
        Assert.Null(login.AuthenticationCookie);
    }

    [Fact]
    public async Task Reauthentication_InSeparateBrowserSessions_InvalidatesOnlyThePriorCookie()
    {
        // Two clients deliberately have independent HTTP state.  This mirrors
        // separate browser profiles rather than two tabs, which share a single
        // cookie jar and therefore both use the replacement cookie.
        using var firstBrowser = CreateClient();
        using var secondBrowser = CreateClient();
        var email = $"reauthentication-{Guid.NewGuid():N}@example.test";
        const string password = "rotation-test-passphrase-2026";
        await CreateUserAsync(email, password);

        using var firstLogin = await LogInAsync(firstBrowser, email, password);
        Assert.Equal(HttpStatusCode.Redirect, firstLogin.Response.StatusCode);
        var firstCookie = Assert.IsType<string>(firstLogin.AuthenticationCookie);

        using var secondLogin = await LogInAsync(secondBrowser, email, password);
        Assert.Equal(HttpStatusCode.Redirect, secondLogin.Response.StatusCode);
        var secondCookie = Assert.IsType<string>(secondLogin.AuthenticationCookie);
        Assert.NotEqual(firstCookie, secondCookie);

        using var firstCookieReplay = await SendAsync(
            firstBrowser,
            "/Identity/Account/Manage/ChangePassword",
            firstCookie);
        Assert.Equal(HttpStatusCode.Redirect, firstCookieReplay.StatusCode);
        var redirectedTo = Assert.IsType<Uri>(firstCookieReplay.Headers.Location);
        Assert.StartsWith(
            "/Identity/Account/Login",
            redirectedTo.IsAbsoluteUri ? redirectedTo.AbsolutePath : redirectedTo.OriginalString,
            StringComparison.Ordinal);

        using var currentCookie = await SendAsync(
            secondBrowser,
            "/Identity/Account/Manage/ChangePassword",
            secondCookie);
        Assert.Equal(HttpStatusCode.OK, currentCookie.StatusCode);
    }

    [Fact]
    public async Task Login_ExternalReturnUrlFallsBackToTheApplicationRoot()
    {
        using var client = CreateClient();
        var email = $"safe-return-url-{Guid.NewGuid():N}@example.test";
        const string password = "safe-return-url-passphrase-2026";
        await CreateUserAsync(email, password);

        using var login = await LogInAsync(client, email, password, "https://example.invalid/external-target");

        Assert.Equal(HttpStatusCode.Redirect, login.Response.StatusCode);
        Assert.NotNull(login.AuthenticationCookie);
        Assert.Equal("/", login.Response.Headers.Location?.OriginalString);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    private async Task CreateUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsEnabled = true,
            LockoutEnabled = true
        }, password);

        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private static async Task<LoginResponse> LogInAsync(HttpClient client, string email, string password, string? returnUrl = null)
    {
        var loginPath = returnUrl is null
            ? "/Identity/Account/Login"
            : $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        using var pageResponse = await client.GetAsync(loginPath);
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var antiforgeryCookie = GetCookiePair(pageResponse, "__Host-Sentinel.AntiForgery");
        var formToken = ExtractAntiForgeryToken(await pageResponse.Content.ReadAsStringAsync());

        var request = new HttpRequestMessage(HttpMethod.Post, loginPath)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["Input.RememberMe"] = "false",
                ["__RequestVerificationToken"] = formToken
            })
        };
        request.Headers.Add("Cookie", antiforgeryCookie);

        var response = await client.SendAsync(request);
        request.Dispose();
        return new LoginResponse(response, TryGetCookiePair(response, "__Host-Sentinel.Auth"));
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path, string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private static string GetCookiePair(HttpResponseMessage response, string name) =>
        Assert.IsType<string>(TryGetCookiePair(response, name));

    private static string? TryGetCookiePair(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        var cookie = values.FirstOrDefault(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        return cookie?.Split(';', 2)[0];
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The page did not render an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private sealed record LoginResponse(HttpResponseMessage Response, string? AuthenticationCookie) : IDisposable
    {
        public void Dispose() => Response.Dispose();
    }
}
