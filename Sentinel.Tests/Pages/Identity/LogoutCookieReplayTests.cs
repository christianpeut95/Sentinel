using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Models;
using Sentinel.Tests.Authorization;

namespace Sentinel.Tests.Pages.Identity;

/// <summary>
/// Exercises the actual Identity cookie, rather than a mocked SignInManager:
/// after logout, an intercepted pre-logout cookie must no longer authenticate.
/// </summary>
public sealed class LogoutCookieReplayTests : IClassFixture<IdentityCookieWebApplicationFactory>
{
    private const string TestPassword = "correct-horse-battery-staple-2026";
    private readonly IdentityCookieWebApplicationFactory _factory;

    public LogoutCookieReplayTests(IdentityCookieWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Logout_InvalidatesACopiedPreLogoutCookie()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var email = $"logout-replay-{Guid.NewGuid():N}@example.test";
        await CreateUserAsync(email);

        var loginPage = await client.GetAsync("/Identity/Account/Login");
        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);

        var antiForgeryCookie = GetCookiePair(loginPage, "__Host-Sentinel.AntiForgery");
        var loginToken = ExtractAntiForgeryToken(await loginPage.Content.ReadAsStringAsync());

        var loginResponse = await SendFormAsync(
            client,
            "/Identity/Account/Login",
            new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = TestPassword,
                ["Input.RememberMe"] = "false",
                ["__RequestVerificationToken"] = loginToken
            },
            antiForgeryCookie);

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        var copiedAuthenticationCookie = GetCookiePair(loginResponse, "__Host-Sentinel.Auth");

        var protectedBeforeLogout = await SendAsync(
            client,
            HttpMethod.Get,
            "/Identity/Account/Manage/ChangePassword",
            copiedAuthenticationCookie);
        Assert.Equal(HttpStatusCode.OK, protectedBeforeLogout.StatusCode);

        // The protected form renders a new antiforgery cookie when the browser
        // did not bring one with it. Carry that cookie and its matching form
        // token into the logout POST exactly as a browser would.
        var protectedPageAntiForgeryCookie = GetCookiePair(
            protectedBeforeLogout,
            "__Host-Sentinel.AntiForgery");
        var logoutToken = ExtractAntiForgeryToken(await protectedBeforeLogout.Content.ReadAsStringAsync());
        var logoutResponse = await SendFormAsync(
            client,
            "/Identity/Account/Logout",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken,
                ["returnUrl"] = "/"
            },
            JoinCookies(copiedAuthenticationCookie, protectedPageAntiForgeryCookie));

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);

        // Replay only the cookie captured before logout. Security-stamp
        // validation must reject it and redirect an unauthenticated browser.
        var replayResponse = await SendAsync(
            client,
            HttpMethod.Get,
            "/Identity/Account/Manage/ChangePassword",
            copiedAuthenticationCookie);

        Assert.Equal(HttpStatusCode.Redirect, replayResponse.StatusCode);
        var redirectedTo = Assert.IsType<Uri>(replayResponse.Headers.Location);
        Assert.StartsWith(
            "/Identity/Account/Login",
            redirectedTo.IsAbsoluteUri ? redirectedTo.AbsolutePath : redirectedTo.OriginalString,
            StringComparison.Ordinal);
    }

    private async Task CreateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsEnabled = true,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private static async Task<HttpResponseMessage> SendFormAsync(
        HttpClient client,
        string path,
        IReadOnlyDictionary<string, string> values,
        string cookieHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Add("Cookie", cookieHeader);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string cookieHeader)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Cookie", cookieHeader);
        return await client.SendAsync(request);
    }

    private static string GetCookiePair(HttpResponseMessage response, string name)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(value => value.StartsWith($"{name}=", StringComparison.Ordinal));

        Assert.False(string.IsNullOrWhiteSpace(cookie), $"Expected a {name} cookie.");
        return cookie!.Split(';', 2)[0];
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

    private static string JoinCookies(params string[] cookies) => string.Join("; ", cookies);
}
