using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services.Reporting;

public sealed class ReportDataServiceAccessTests : IDisposable
{
    private readonly DefaultHttpContext _requestContext = new();
    private readonly ApplicationDbContext _context;

    public ReportDataServiceAccessTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = _requestContext });
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

    [Fact]
    public void AllCollectionItemsMatch_RequiresANonEmptyCollectionAndACompleteMatch()
    {
        Assert.False(ReportDataService.AllCollectionItemsMatch(0, 0));
        Assert.False(ReportDataService.AllCollectionItemsMatch(2, 1));
        Assert.True(ReportDataService.AllCollectionItemsMatch(2, 2));
    }

    [Fact]
    public async Task GetReportPreviewAsync_HasAllIncludesNonEmptyCollectionsWithoutSubFilters()
    {
        var disease = new Disease
        {
            Id = Guid.NewGuid(),
            Name = "Report collection regression disease",
            Code = "REPORT-COLLECTION",
            ExportCode = "REPORT-COLLECTION"
        };
        var matchingCase = new Case { Id = Guid.NewGuid(), DiseaseId = disease.Id, Disease = disease, Type = CaseType.Case };
        var mixedCase = new Case { Id = Guid.NewGuid(), DiseaseId = disease.Id, Disease = disease, Type = CaseType.Case };
        _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid> { disease.Id };
        _context.AddRange(
            disease,
            matchingCase,
            mixedCase,
            new LabResult { Id = Guid.NewGuid(), CaseId = matchingCase.Id, Case = matchingCase, AccessionNumber = "MATCH-001" },
            new LabResult { Id = Guid.NewGuid(), CaseId = matchingCase.Id, Case = matchingCase, AccessionNumber = "MATCH-002" },
            new LabResult { Id = Guid.NewGuid(), CaseId = mixedCase.Id, Case = mixedCase, AccessionNumber = "MATCH-003" },
            new LabResult { Id = Guid.NewGuid(), CaseId = mixedCase.Id, Case = mixedCase, AccessionNumber = "OTHER-004" });
        await _context.SaveChangesAsync();

        var reportAccess = new Mock<IReportDataAccessService>();
        reportAccess.Setup(service => service.CanReadEntityTypeAsync("Case")).ReturnsAsync(true);
        var service = CreateService(reportAccess.Object);

        var rows = await service.GetReportPreviewAsync(
            new ReportDefinition { EntityType = "Case" },
            [
                new CollectionQueryDto
                {
                    CollectionName = "LabResults",
                    Operation = "HasAll",
                    DisplayAsColumn = false
                }
            ]);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => Equals(matchingCase.Id, row["Id"]));
        Assert.Contains(rows, row => Equals(mixedCase.Id, row["Id"]));
    }

    [Fact]
    public async Task GetReportPreviewAsync_HasAllWithSubFiltersExcludesPartiallyMatchingCollections()
    {
        var disease = new Disease
        {
            Id = Guid.NewGuid(),
            Name = "Report HasAll regression disease",
            Code = "REPORT-HAS-ALL",
            ExportCode = "REPORT-HAS-ALL"
        };
        var completeMatch = new Case { Id = Guid.NewGuid(), DiseaseId = disease.Id, Disease = disease, Type = CaseType.Case };
        var partialMatch = new Case { Id = Guid.NewGuid(), DiseaseId = disease.Id, Disease = disease, Type = CaseType.Case };
        _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid> { disease.Id };
        _context.AddRange(
            disease,
            completeMatch,
            partialMatch,
            new CaseTask { Id = Guid.NewGuid(), CaseId = completeMatch.Id, Case = completeMatch, TaskTypeId = Guid.NewGuid(), Title = "Complete match 1", Priority = TaskPriority.High },
            new CaseTask { Id = Guid.NewGuid(), CaseId = completeMatch.Id, Case = completeMatch, TaskTypeId = Guid.NewGuid(), Title = "Complete match 2", Priority = TaskPriority.High },
            new CaseTask { Id = Guid.NewGuid(), CaseId = partialMatch.Id, Case = partialMatch, TaskTypeId = Guid.NewGuid(), Title = "Partial match", Priority = TaskPriority.High },
            new CaseTask { Id = Guid.NewGuid(), CaseId = partialMatch.Id, Case = partialMatch, TaskTypeId = Guid.NewGuid(), Title = "Non-match", Priority = TaskPriority.Medium });
        await _context.SaveChangesAsync();

        var reportAccess = new Mock<IReportDataAccessService>();
        reportAccess.Setup(service => service.CanReadEntityTypeAsync("Case")).ReturnsAsync(true);
        var service = CreateService(reportAccess.Object);

        var rows = await service.GetReportPreviewAsync(
            new ReportDefinition { EntityType = "Case" },
            [
                new CollectionQueryDto
                {
                    CollectionName = "Tasks",
                    Operation = "HasAll",
                    DisplayAsColumn = false,
                    SubFilters =
                    [
                        new CollectionSubFilter
                        {
                            Field = nameof(CaseTask.Priority),
                            Operator = "Equals",
                            Value = ((int)TaskPriority.High).ToString()
                        }
                    ]
                }
            ]);

        var row = Assert.Single(rows);
        Assert.Equal(completeMatch.Id, row["Id"]);
    }

    public void Dispose() => _context.Dispose();

    private ReportDataService CreateService(
        IReportDataAccessService reportAccess,
        ApplicationDbContext? context = null)
    {
        context ??= _context;
        var metadata = new Mock<IReportFieldMetadataService>();
        metadata
            .Setup(service => service.GetFieldsForEntityAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<FieldUsageContext>()))
            .ReturnsAsync([]);
        var collectionFilterBuilder = new CollectionQueryFilterBuilder(
            context,
            new DynamicDateResolver(),
            metadata.Object,
            new CollectionMetadataService());

        return new ReportDataService(
            context,
            metadata.Object,
            Mock.Of<IDynamicDateResolver>(),
            collectionFilterBuilder,
            reportAccess,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ReportDataService>>());
    }
}
