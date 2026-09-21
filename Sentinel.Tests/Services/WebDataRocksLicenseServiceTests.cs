using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class WebDataRocksLicenseServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public WebDataRocksLicenseServiceTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public async Task Status_RequiresOrganisationAndCurrentUserAcceptance()
    {
        await SeedAsync();
        var service = CreateService();

        var beforeAcceptance = await service.GetStatusAsync("report-user");

        Assert.False(beforeAcceptance.OrganizationEnabled);
        Assert.False(beforeAcceptance.CanUse);

        await service.SetOrganizationAcceptanceAsync(enabled: true, acceptedByUserId: "admin-user");

        var afterOrganizationAcceptance = await service.GetStatusAsync("report-user");
        Assert.True(afterOrganizationAcceptance.OrganizationAccepted);
        Assert.False(afterOrganizationAcceptance.UserAccepted);
        Assert.False(afterOrganizationAcceptance.CanUse);

        await service.AcceptForUserAsync("report-user");

        var afterUserAcceptance = await service.GetStatusAsync("report-user");
        Assert.True(afterUserAcceptance.UserAccepted);
        Assert.True(afterUserAcceptance.CanUse);
    }

    [Fact]
    public async Task DisablingFeature_RemovesOrganisationConsentAndPreventsUse()
    {
        await SeedAsync();
        var service = CreateService();

        await service.SetOrganizationAcceptanceAsync(enabled: true, acceptedByUserId: "admin-user");
        await service.AcceptForUserAsync("report-user");
        await service.SetOrganizationAcceptanceAsync(enabled: false, acceptedByUserId: "admin-user");

        var status = await service.GetStatusAsync("report-user");
        var settings = await _context.SystemSettings.SingleAsync();

        Assert.False(status.OrganizationEnabled);
        Assert.False(status.OrganizationAccepted);
        Assert.True(status.UserAccepted);
        Assert.False(status.CanUse);
        Assert.Null(settings.WebDataRocksLicenseVersion);
        Assert.Null(settings.WebDataRocksLicenseAcceptedAt);
        Assert.Null(settings.WebDataRocksLicenseAcceptedByUserId);
    }

    private async Task SeedAsync()
    {
        _context.SystemSettings.Add(new SystemSettings { Id = Guid.NewGuid() });
        _context.Users.AddRange(
            new ApplicationUser { Id = "admin-user", UserName = "admin@example.test", Email = "admin@example.test" },
            new ApplicationUser { Id = "report-user", UserName = "report@example.test", Email = "report@example.test" });
        await _context.SaveChangesAsync();
    }

    private WebDataRocksLicenseService CreateService() => new(
        _context,
        NullLogger<WebDataRocksLicenseService>.Instance);

    public void Dispose() => _context.Dispose();
}
