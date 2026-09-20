using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class DataReviewWorkflowTests : IDisposable
{
    private readonly DefaultHttpContext _requestContext = new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "reviewer")
        ], "Test"))
    };

    private readonly ApplicationDbContext _context;
    private readonly Mock<ICollectionMappingService> _collectionMappings = new();
    private readonly Mock<ITaskService> _tasks = new();

    public DataReviewWorkflowTests()
    {
        var accessor = new HttpContextAccessor { HttpContext = _requestContext };
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            accessor);
    }

    [Fact]
    public async Task ConfirmReviewAsync_WhenCollectionCreationFails_LeavesReviewPending()
    {
        var review = await AddReviewAsync(ReviewEntityTypes.DuplicateContact, ReviewStatuses.Pending);
        _collectionMappings
            .Setup(service => service.CreateEntitiesFromReviewAsync(review.Id, null))
            .ThrowsAsync(new InvalidOperationException("Simulated collection-mapping failure"));

        Assert.NotNull(await _context.ReviewQueue.SingleOrDefaultAsync(item => item.Id == review.Id));

        var confirmed = await CreateService().ConfirmReviewAsync(review.Id, "approve");

        Assert.False(confirmed);
        var persisted = await _context.ReviewQueue.SingleAsync();
        Assert.Equal(ReviewStatuses.Pending, persisted.ReviewStatus);
        Assert.Null(persisted.ReviewedDate);
        _collectionMappings.Verify(
            service => service.CreateEntitiesFromReviewAsync(review.Id, null),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmReviewAsync_WhenSurveyChangeCannotBeApplied_LeavesReviewPending()
    {
        var review = await AddReviewAsync("SurveyFieldChange", ReviewStatuses.Pending);
        review.ChangeSnapshot = null;
        await _context.SaveChangesAsync();

        var confirmed = await CreateService().ConfirmReviewAsync(review.Id, "approve");

        Assert.False(confirmed);
        var persisted = await _context.ReviewQueue.SingleAsync();
        Assert.Equal(ReviewStatuses.Pending, persisted.ReviewStatus);
        Assert.Null(persisted.ReviewedDate);
    }

    [Fact]
    public async Task CreateTaskForReviewAsync_RejectsNonPendingReviewWithoutCreatingTask()
    {
        var review = await AddReviewAsync(ReviewEntityTypes.LabResult, ReviewStatuses.Reviewed);

        var taskId = await CreateService().CreateTaskForReviewAsync(review.Id, "Follow up");

        Assert.Null(taskId);
        Assert.Empty(await _context.CaseTasks.ToListAsync());
    }

    [Fact]
    public async Task CreateTaskForReviewAsync_RejectsOverlongTitleWithoutCreatingTask()
    {
        var review = await AddReviewAsync(ReviewEntityTypes.LabResult, ReviewStatuses.Pending, withAccessibleCase: true);

        var taskId = await CreateService().CreateTaskForReviewAsync(review.Id, new string('x', 201));

        Assert.Null(taskId);
        Assert.Empty(await _context.CaseTasks.ToListAsync());
        var persisted = await _context.ReviewQueue.SingleAsync();
        Assert.Equal(ReviewStatuses.Pending, persisted.ReviewStatus);
    }

    [Fact]
    public async Task CreateTaskForReviewAsync_CreatesTaskOnlyForAccessiblePendingReview()
    {
        var review = await AddReviewAsync(ReviewEntityTypes.LabResult, ReviewStatuses.Pending, withAccessibleCase: true);

        var taskId = await CreateService().CreateTaskForReviewAsync(review.Id, "Follow up");

        Assert.NotNull(taskId);
        var task = await _context.CaseTasks.SingleAsync();
        Assert.Equal(taskId, task.Id);
        Assert.Equal("Follow up", task.Title);
        var persisted = await _context.ReviewQueue.SingleAsync();
        Assert.Equal(ReviewStatuses.Reviewed, persisted.ReviewStatus);
        Assert.Equal(ReviewActions.TaskCreated, persisted.ReviewAction);
    }

    private async Task<ReviewQueue> AddReviewAsync(string entityType, string status, bool withAccessibleCase = false)
    {
        Guid? caseId = null;
        Guid? diseaseId = null;
        Case? caseRecord = null;
        Disease? disease = null;

        if (withAccessibleCase)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                GivenName = "Review",
                FamilyName = "Workflow",
                FriendlyId = "PT-REVIEW"
            };
            disease = new Disease
            {
                Id = Guid.NewGuid(),
                Name = "Review workflow disease",
                Code = "REVIEW-WORKFLOW",
                ExportCode = "REVIEW-WORKFLOW",
                PathIds = "review-workflow"
            };
            caseRecord = new Case
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Patient = patient,
                DiseaseId = disease.Id,
                Disease = disease,
                FriendlyId = "CASE-REVIEW",
                Type = CaseType.Case
            };
            caseId = caseRecord.Id;
            diseaseId = disease.Id;
            _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid> { disease.Id };
        }

        var review = new ReviewQueue
        {
            EntityType = entityType,
            EntityId = 1,
            CaseId = caseId,
            Case = caseRecord,
            DiseaseId = diseaseId,
            Disease = disease,
            ChangeType = ReviewChangeTypes.New,
            ReviewStatus = status,
            CreatedDate = DateTime.UtcNow
        };
        _context.ReviewQueue.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    private DataReviewService CreateService() => new(
        _context,
        new HttpContextAccessor { HttpContext = _requestContext },
        _collectionMappings.Object,
        _tasks.Object,
        NullLogger<DataReviewService>.Instance);

    public void Dispose() => _context.Dispose();
}
