using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Sentinel.Pages.Settings;
using Sentinel.Services;

namespace Sentinel.Tests.Pages.Settings;

public sealed class InteractiveReportsModelTests
{
    [Fact]
    public async Task Enable_WithVendorAcceptance_PersistsTheSubmittedEnabledState()
    {
        var licenseService = new Mock<IWebDataRocksLicenseService>();
        licenseService
            .Setup(service => service.GetStatusAsync("admin-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebDataRocksLicenseStatus(false, false, false));

        var model = CreateModel(licenseService.Object);
        model.EnableWebDataRocks = true;
        model.AcceptLicenseTerms = true;

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        licenseService.Verify(
            service => service.SetOrganizationAcceptanceAsync(true, "admin-user", It.IsAny<CancellationToken>()),
            Times.Once);
        licenseService.Verify(
            service => service.AcceptForUserAsync("admin-user", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Enable_WithoutVendorAcceptance_ReturnsThePageWithoutChangingSettings()
    {
        var licenseService = new Mock<IWebDataRocksLicenseService>();
        licenseService
            .Setup(service => service.GetStatusAsync("admin-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebDataRocksLicenseStatus(false, false, false));

        var model = CreateModel(licenseService.Object);
        model.EnableWebDataRocks = true;
        model.AcceptLicenseTerms = false;

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        licenseService.Verify(
            service => service.SetOrganizationAcceptanceAsync(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static InteractiveReportsModel CreateModel(IWebDataRocksLicenseService licenseService)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-user")
        ], "test"));

        var httpContext = new DefaultHttpContext { User = user };
        var model = new InteractiveReportsModel(licenseService)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            }
        };

        model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return model;
    }
}
