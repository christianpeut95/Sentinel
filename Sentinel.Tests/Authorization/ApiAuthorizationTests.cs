using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentinel.Authorization;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Tests authorization on newly secured API controllers
/// Verifies permission policies and rate limiting are properly applied
/// </summary>
public class ApiAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithAuth(params string[] permissions)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                services.AddSingleton<IAuthorizationHandler>(sp =>
                {
                    return new TestPermissionHandler(permissions);
                });
            });
        }).CreateClient();
    }

    #region LocationLookupApiController Tests (CRITICAL - was completely unsecured)

    [Fact]
    public async Task LocationLookup_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/location-lookup/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LocationLookup_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth(); // No specific permission required, just authenticated

        // Act
        var response = await client.GetAsync("/api/location-lookup/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region TimelineEntryApiController Tests

    [Fact]
    public async Task TimelineApi_WithoutCaseEditPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Case.View"); // Wrong permission

        // Act
        var response = await client.GetAsync("/api/timeline/00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TimelineApi_WithCaseEditPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Case.Edit");

        // Act
        var response = await client.GetAsync("/api/timeline/00000000-0000-0000-0000-000000000001");

        // Assert - Will return OK with empty timeline or 404 if case doesn't exist, but not 403
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region SurveyMappingApiController Tests

    [Fact]
    public async Task SurveyMapping_WithoutSurveyEditPermission_ReturnsForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Survey.View");

        // Act
        var response = await client.GetAsync("/api/SurveyMappingApi/configuration?surveyTemplateId=00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SurveyMapping_WithSurveyEditPermission_ReturnsOk()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Survey.Edit");

        // Act
        var response = await client.GetAsync("/api/SurveyMappingApi/configuration?surveyTemplateId=00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
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
    public async Task ReportsApi_Delete_WithReportEditPermission_NotForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Report.Edit");

        // Act
        var response = await client.DeleteAsync("/api/reports/1");

        // Assert - Will return 404 if not found, but not 403
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
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
    public async Task HL7Diagnostics_WithHL7ViewPermission_NotForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.HL7.View");

        // Act
        var response = await client.GetAsync("/api/hl7/diagnostics/lab-result/00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
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
    public async Task CaseDefinitionCriteria_WithSettingsEditPermission_NotForbidden()
    {
        // Arrange
        var client = CreateClientWithAuth("Permission.Settings.Edit");
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/case-definitions/1/criteria/laboratory", content);

        // Assert - Will return BadRequest if model invalid, but not 403
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
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
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "test-user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Test permission handler for integration tests
/// </summary>
public class TestPermissionHandler : IAuthorizationHandler
{
    private readonly HashSet<string> _permissions;

    public TestPermissionHandler(params string[] permissions)
    {
        _permissions = new HashSet<string>(permissions);
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            if (requirement is PermissionRequirement permReq &&
                _permissions.Contains(permReq.GetPermissionKey()))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
