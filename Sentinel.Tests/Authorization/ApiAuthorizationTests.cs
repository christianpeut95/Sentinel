using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Authorization;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Xunit;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Tests authorization on newly secured API controllers
/// Verifies permission policies and rate limiting are properly applied
/// </summary>
public class ApiAuthorizationTests : IClassFixture<SentinelWebApplicationFactory>
{
    private readonly SentinelWebApplicationFactory _factory;

    public ApiAuthorizationTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateSecureClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private HttpClient CreateClientWithAuth(params string[] permissions)
    {
        var client = CreateSecureClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, "test-user");
        client.DefaultRequestHeaders.Add(
            TestPermissionHandler.PermissionsHeaderName,
            string.Join(',', permissions));
        return client;
    }

    #region LocationLookupApiController Tests (CRITICAL - was completely unsecured)

    [Fact]
    public async Task LocationLookup_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        var client = CreateSecureClient();

        // Act
        var response = await client.GetAsync("/api/location-lookup/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LocationLookup_WithReferenceDataViewPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.ReferenceData.View");

        // Act
        var response = await client.GetAsync("/api/location-lookup/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region SurveyMappingApiController Tests

    [Fact]
    public async Task SurveyMapping_WithoutSettingsEditPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Survey.View");

        // Act
        var response = await client.GetAsync("/api/SurveyMappingApi/configuration?surveyTemplateId=00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SurveyMapping_WithSettingsEditPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Settings.Edit");

        // Act
        var response = await client.GetAsync("/api/SurveyMappingApi/configuration?surveyTemplateId=00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region UserLookupController Tests

    [Fact]
    public async Task UserLookup_UnauthenticatedUser_ReturnsUnauthorized()
    {
        var client = CreateSecureClient();

        var response = await client.GetAsync("/api/users/search?term=te");

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected unauthorized but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task UserLookup_AuthenticatedUserWithoutPatientSearchPermission_ReturnsOk()
    {
        var client = CreateClientWithAuth();

        var response = await client.GetAsync("/api/users/search?term=te");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK but received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task UserLookup_ReturnsOnlyTheAssignmentContract()
    {
        var userId = Guid.NewGuid().ToString();
        const string userName = "lookup.assignee@example.test";

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Sentinel.Data.ApplicationDbContext>();
            context.Users.Add(new Sentinel.Models.ApplicationUser
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = userName,
                NormalizedEmail = userName.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            });
            await context.SaveChangesAsync();
        }

        var client = CreateClientWithAuth();
        var response = await client.GetAsync("/api/users/search?term=lookup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(document.RootElement.EnumerateArray());

        Assert.Equal(userId, item.GetProperty("id").GetString());
        Assert.Equal(userName, item.GetProperty("text").GetString());
        Assert.Equal(userName, item.GetProperty("displayName").GetString());
        Assert.False(item.TryGetProperty("email", out _));
        Assert.False(item.TryGetProperty("securityStamp", out _));
        Assert.False(item.TryGetProperty("passwordHash", out _));
    }

    #endregion

    #region Report API Controllers Tests

    [Fact]
    public async Task ReportsApi_Delete_WithoutReportEditPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Report.View");

        // Act
        var response = await client.DeleteAsync("/api/reports/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportsApi_Delete_WithReportEditPermission_ReachesAntiforgeryValidation()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Report.Edit");

        // Act
        var response = await client.DeleteAsync("/api/reports/1");

        // Authorization succeeded. The deliberately token-less unsafe request
        // must then be stopped by ASP.NET Core antiforgery validation.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReportFieldsApi_WithoutReportViewPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Case.View");

        // Act
        var response = await client.GetAsync("/api/reporting/fields/Case");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportFieldsApi_WithReportViewPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Report.View");

        // Act
        var response = await client.GetAsync("/api/reporting/fields/Case");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region HL7DiagnosticsApiController Tests

    [Fact]
    public async Task HL7Diagnostics_WithoutHL7ViewPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Laboratory.View");

        // Act
        var response = await client.GetAsync("/api/hl7/diagnostics/lab-result/00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HL7Diagnostics_WithHL7ViewPermission_ReturnsDiagnosticReport()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.HL7.View");

        // Act
        var response = await client.GetAsync("/api/hl7/diagnostics/lab-result/00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region CaseDefinitionCriteriaController Tests

    [Fact]
    public async Task CaseDefinitionCriteria_WithoutSettingsEditPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Settings.View");
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/case-definitions/1/criteria/laboratory", content);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaseDefinitionCriteria_WithSettingsEditPermission_ReachesAntiforgeryValidation()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Settings.Edit");
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/case-definitions/1/criteria/laboratory", content);

        // Authorization succeeded. The deliberately token-less unsafe request
        // must then be stopped by ASP.NET Core antiforgery validation.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PatientsController Tests

    [Fact]
    public async Task Patients_Search_WithoutPatientSearchPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Patient.View");

        // Act
        var response = await client.GetAsync("/api/Patients/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patients_Search_WithPatientSearchPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Patient.Search");

        // Act
        var response = await client.GetAsync("/api/Patients/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}

/// <summary>
/// Test authentication handler for integration tests
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SentinelTest";
    public const string UserHeaderName = "X-Sentinel-Test-User";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeaderName, out var userId) ||
            string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Test permission handler for integration tests
/// </summary>
public class TestPermissionHandler : IAuthorizationHandler
{
    public const string PermissionsHeaderName = "X-Sentinel-Test-Permissions";

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var permissions = (context.Resource as HttpContext)?
            .Request.Headers[PermissionsHeaderName]
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(permission => permission.StartsWith("Permission.", StringComparison.Ordinal)
                ? permission["Permission.".Length..]
                : permission)
            .ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        foreach (var requirement in context.PendingRequirements.ToList())
        {
            if (requirement is DenyAnonymousAuthorizationRequirement &&
                context.User.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
                continue;
            }

            if (requirement is PermissionRequirement permReq &&
                permissions.Contains(permReq.GetPermissionKey()))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
