using Microsoft.EntityFrameworkCore;
using Moq;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services.Reporting;

public sealed class ReportDataServiceAccessTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ReportDataServiceAccessTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public async Task GetReportPreviewAsync_WhenUnderlyingEntityAccessIsDenied_ThrowsBeforeExtractingData()
    {
        var reportAccess = new Mock<IReportDataAccessService>();
        reportAccess.Setup(service => service.CanReadEntityTypeAsync("Patient")).ReturnsAsync(false);

        var service = CreateService(reportAccess.Object);
        var report = new ReportDefinition { EntityType = "Patient" };

        await Assert.ThrowsAsync<ReportDataAccessDeniedException>(
            () => service.GetReportPreviewAsync(report));
    }

    [Fact]
    public async Task GetReportRowCountAsync_WhenUnderlyingEntityAccessIsDenied_ThrowsBeforeCountingData()
    {
        var reportAccess = new Mock<IReportDataAccessService>();
        reportAccess.Setup(service => service.CanReadEntityTypeAsync("Case")).ReturnsAsync(false);

        var service = CreateService(reportAccess.Object);
        var report = new ReportDefinition { EntityType = "Case" };

        await Assert.ThrowsAsync<ReportDataAccessDeniedException>(
            () => service.GetReportRowCountAsync(report));
    }

    public void Dispose() => _context.Dispose();

    private ReportDataService CreateService(
        IReportDataAccessService reportAccess,
        ApplicationDbContext? context = null)
    {
        context ??= _context;
        var metadata = new Mock<IReportFieldMetadataService>();
        var collectionFilterBuilder = new CollectionQueryFilterBuilder(
            context,
            new DynamicDateResolver(),
            metadata.Object);

        return new ReportDataService(
            context,
            metadata.Object,
            Mock.Of<IDynamicDateResolver>(),
            collectionFilterBuilder,
            reportAccess,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ReportDataService>>());
    }
}
