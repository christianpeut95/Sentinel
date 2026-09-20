using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Controllers.Api;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class SurveyTaskStateTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultHttpContext _requestContext = new();

    public SurveyTaskStateTests()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = _requestContext };
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            httpContextAccessor);
    }

    [Theory]
    [InlineData(CaseTaskStatus.Completed)]
    [InlineData(CaseTaskStatus.Cancelled)]
    public async Task SurveyService_RejectsTerminalTaskBeforeMapping(CaseTaskStatus status)
    {
        var task = await AddTaskAsync(status);
        var mappings = new Mock<ISurveyMappingService>();
        var service = new SurveyService(
            _context,
            NullLogger<SurveyService>.Instance,
            mappings.Object,
            new HttpContextAccessor());

        var exception = await Assert.ThrowsAsync<SurveyTaskStateException>(
            () => service.SaveSurveyResponseAsync(task.Id, new Dictionary<string, object>()));

        Assert.Equal(status, exception.Status);
        mappings.Verify(service => service.GetActiveMappingsAsync(
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Theory]
    [InlineData(CaseTaskStatus.Completed)]
    [InlineData(CaseTaskStatus.Cancelled)]
    public async Task CompletionApi_RejectsTerminalTaskWithoutSavingResponses(CaseTaskStatus status)
    {
        var task = await AddTaskAsync(status);
        var caseAccess = new Mock<ICaseAccessService>();
        caseAccess.Setup(service => service.CanAccessCaseAsync(task.CaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var surveys = new Mock<ISurveyService>();
        var controller = CreateController(caseAccess.Object, surveys.Object);

        var result = await controller.CompleteSurvey(task.Id, new Dictionary<string, object>());

        var conflict = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        surveys.Verify(service => service.SaveSurveyResponseAsync(
            It.IsAny<Guid>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(status, persisted.Status);
    }

    [Fact]
    public async Task CompletionApi_RejectsInaccessibleTaskBeforeSavingResponses()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Pending);
        var caseAccess = new Mock<ICaseAccessService>();
        caseAccess.Setup(service => service.CanAccessCaseAsync(task.CaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var surveys = new Mock<ISurveyService>();
        var controller = CreateController(caseAccess.Object, surveys.Object);

        var result = await controller.CompleteSurvey(task.Id, new Dictionary<string, object>());

        var notFound = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        surveys.Verify(service => service.SaveSurveyResponseAsync(
            It.IsAny<Guid>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task CompletionApi_CompletesAnAccessibleNonTerminalTask()
    {
        var task = await AddTaskAsync(CaseTaskStatus.InProgress);
        var caseAccess = new Mock<ICaseAccessService>();
        caseAccess.Setup(service => service.CanAccessCaseAsync(task.CaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var surveys = new Mock<ISurveyService>();
        var controller = CreateController(caseAccess.Object, surveys.Object);

        var result = await controller.CompleteSurvey(task.Id, new Dictionary<string, object>
        {
            ["onset"] = "2026-09-19"
        });

        var ok = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        surveys.Verify(service => service.SaveSurveyResponseAsync(
            task.Id,
            It.Is<Dictionary<string, object>>(responses => responses.ContainsKey("onset"))), Times.Once);
        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(CaseTaskStatus.Completed, persisted.Status);
        Assert.Equal("test-user", persisted.CompletedByUserId);
        Assert.NotNull(persisted.CompletedAt);
    }

    private async Task<CaseTask> AddTaskAsync(CaseTaskStatus status)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Workflow",
            FamilyName = "Test",
            FriendlyId = "PT-WORKFLOW"
        };
        var caseRecord = new Case
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            Patient = patient,
            DiseaseId = Guid.NewGuid(),
            Disease = new Disease
            {
                Id = Guid.NewGuid(),
                Name = "Workflow test disease",
                Code = "WORKFLOW",
                ExportCode = "WORKFLOW",
                PathIds = "workflow-test"
            },
            FriendlyId = "CASE-WORKFLOW",
            Type = CaseType.Case
        };
        caseRecord.DiseaseId = caseRecord.Disease.Id;
        _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid> { caseRecord.DiseaseId.Value };
        var task = new CaseTask
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            Case = caseRecord,
            TaskTypeId = Guid.NewGuid(),
            Title = "Survey workflow test",
            Status = status
        };
        _context.CaseTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    private SurveyCompletionApiController CreateController(
        ICaseAccessService caseAccess,
        ISurveyService surveys)
    {
        var controller = new SurveyCompletionApiController(
            _context,
            caseAccess,
            surveys,
            NullLogger<SurveyCompletionApiController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "test-user")
                    ], "Test"))
                }
            }
        };
        return controller;
    }

    public void Dispose() => _context.Dispose();
}
