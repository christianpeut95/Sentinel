using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Data;
using Sentinel.Models;
using Xunit;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Direct HTTP checks for representative state-changing route families.  The
/// inventory tests ensure the application-wide guardrails remain configured;
/// these tests prove that those guardrails stop a request before it can write.
/// </summary>
public sealed class AuthorizationRiskClassBehaviourTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public AuthorizationRiskClassBehaviourTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string> RazorMutationRoutes =>
    [
        "/dashboard?handler=ResetConfig",
        "/Cases/Create",
        "/Settings/Users/Create"
    ];

    public static TheoryData<string> ControllerMutationRoutes =>
    [
        "/api/reports/preview",
        "/api/surveys/complete/00000000-0000-0000-0000-000000000001"
    ];

    [Theory]
    [MemberData(nameof(RazorMutationRoutes))]
    public async Task RazorMutation_AnonymousRequest_IsChallengedBeforeHandlerExecution(string route)
    {
        using var client = CreateClient();

        var response = await client.PostAsync(route, EmptyForm());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ControllerMutationRoutes))]
    public async Task ControllerMutation_AnonymousRequest_IsChallengedBeforeHandlerExecution(string route)
    {
        using var client = CreateClient();

        var response = await client.PostAsync(route, JsonBody("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MinimalApiDelete_WithoutRequiredPermission_IsForbiddenAndDoesNotDelete()
    {
        var labResultId = await SeedLabResultAsync();
        using var client = CreateClient("risk-class-no-delete-permission");

        var response = await client.DeleteAsync($"/api/lab-results/{labResultId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await LabResultExistsAsync(labResultId));
    }

    [Fact]
    public async Task MinimalApiDelete_WithoutAntiforgeryToken_IsRejectedAndDoesNotDelete()
    {
        var labResultId = await SeedLabResultAsync();
        using var client = CreateClient(
            "risk-class-missing-antiforgery",
            "Permission.Laboratory.Delete",
            "Permission.Case.Edit");

        var response = await client.DeleteAsync($"/api/lab-results/{labResultId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await LabResultExistsAsync(labResultId));
    }

    [Fact]
    public async Task ExposureDelete_WithoutRequiredPermission_IsForbiddenAndDoesNotDelete()
    {
        var countBefore = await ExposureCountAsync();
        var exposureId = Guid.NewGuid();
        using var client = CreateClient("risk-class-no-exposure-delete-permission");

        var response = await client.DeleteAsync($"/api/exposures/{exposureId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(countBefore, await ExposureCountAsync());
    }

    [Fact]
    public async Task ExposureDelete_WithoutAntiforgeryToken_IsRejectedAndDoesNotDelete()
    {
        var countBefore = await ExposureCountAsync();
        var exposureId = Guid.NewGuid();
        using var client = CreateClient(
            "risk-class-exposure-missing-antiforgery",
            "Permission.Exposure.Delete",
            "Permission.Case.Edit");

        var response = await client.DeleteAsync($"/api/exposures/{exposureId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(countBefore, await ExposureCountAsync());
    }

    [Fact]
    public async Task RazorSelfServiceMutation_WithValidAntiforgeryToken_OnlyChangesCurrentUsersConfiguration()
    {
        var actorId = $"risk-class-actor-{Guid.NewGuid():N}";
        var otherUserId = $"risk-class-other-{Guid.NewGuid():N}";
        const string otherUserConfiguration = "{\"configVersion\":1,\"layout\":\"list\",\"widgets\":[]}";

        await SeedUserAsync(actorId, dashboardConfigJson: null);
        await SeedUserAsync(otherUserId, otherUserConfiguration);

        using var client = CreateClient(actorId);
        var token = await GetAntiforgeryTokenAsync(client);
        client.DefaultRequestHeaders.Add("RequestVerificationToken", token);

        var response = await client.PostAsync("/dashboard?handler=ResetConfig", EmptyForm());

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actor = await context.Users.SingleAsync(user => user.Id == actorId);
        var otherUser = await context.Users.SingleAsync(user => user.Id == otherUserId);

        Assert.False(string.IsNullOrWhiteSpace(actor.DashboardConfigJson));
        Assert.Equal(otherUserConfiguration, otherUser.DashboardConfigJson);
    }

    private HttpClient CreateClient(string? userId = null, params string[] permissions)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        if (!string.IsNullOrWhiteSpace(userId))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, userId);
            client.DefaultRequestHeaders.Add(
                TestPermissionHandler.PermissionsHeaderName,
                string.Join(',', permissions));
        }

        return client;
    }

    private async Task<Guid> SeedLabResultAsync()
    {
        var id = Guid.NewGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.LabResults.Add(new LabResult
        {
            Id = id,
            FriendlyId = $"RISK-{id:N}"[..20]
        });
        await context.SaveChangesAsync();
        return id;
    }

    private async Task<bool> LabResultExistsAsync(Guid id)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.LabResults
            .IgnoreQueryFilters()
            .AnyAsync(result => result.Id == id);
    }

    private async Task<int> ExposureCountAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.ExposureEvents
            .IgnoreQueryFilters()
            .CountAsync();
    }

    private async Task SeedUserAsync(string id, string? dashboardConfigJson)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var email = $"{id}@example.test";
        context.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            DashboardConfigJson = dashboardConfigJson
        });
        await context.SaveChangesAsync();
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"\\s+type=\"hidden\"\\s+value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(match.Success, "Dashboard response did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static StringContent JsonBody(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static FormUrlEncodedContent EmptyForm() =>
        new(new Dictionary<string, string>());
}
