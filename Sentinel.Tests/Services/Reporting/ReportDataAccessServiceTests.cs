using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services.Reporting;

public sealed class ReportDataAccessServiceTests
{
    [Fact]
    public async Task CanReadEntityTypeAsync_CaseWithoutCaseView_ReturnsFalse()
    {
        var service = CreateService([]);

        var allowed = await service.CanReadEntityTypeAsync("Case");

        Assert.False(allowed);
    }

    [Fact]
    public async Task CanReadEntityTypeAsync_CaseWithCaseView_ReturnsTrue()
    {
        var service = CreateService(["Permission.Case.View"]);

        var allowed = await service.CanReadEntityTypeAsync("Case");

        Assert.True(allowed);
    }

    [Fact]
    public async Task CanReadEntityTypeAsync_ContactTracingViewRequiresExposurePermission()
    {
        var withoutExposure = CreateService(["Permission.Case.View"]);
        var withAllRequiredPermissions = CreateService(
            ["Permission.Case.View", "Permission.Exposure.View"]);

        Assert.False(await withoutExposure.CanReadEntityTypeAsync("ContactTracingMindMapEdges"));
        Assert.True(await withAllRequiredPermissions.CanReadEntityTypeAsync("ContactTracingMindMapEdges"));
    }

    [Fact]
    public async Task CanReadEntityTypeAsync_UnknownEntityType_ReturnsFalse()
    {
        var service = CreateService(["Permission.Case.View"]);

        var allowed = await service.CanReadEntityTypeAsync("UnknownEntity");

        Assert.False(allowed);
    }

    private static ReportDataAccessService CreateService(IReadOnlyCollection<string> allowedPolicies)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "report-user")],
        authenticationType: "Test"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.SetupGet(accessor => accessor.HttpContext)
            .Returns(new DefaultHttpContext { User = principal });

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync((ClaimsPrincipal _, object? _, string policy) =>
                allowedPolicies.Contains(policy, StringComparer.OrdinalIgnoreCase)
                    ? AuthorizationResult.Success()
                    : AuthorizationResult.Failed());

        return new ReportDataAccessService(httpContextAccessor.Object, authorizationService.Object);
    }
}
