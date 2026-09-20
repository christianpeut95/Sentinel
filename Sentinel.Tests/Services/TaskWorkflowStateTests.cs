using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

/// <summary>
/// Covers the shared task-service boundary, rather than relying on the UI to
/// hide buttons for completed or cancelled tasks.
/// </summary>
public sealed class TaskWorkflowStateTests : IDisposable
{
    private readonly DefaultHttpContext _requestContext = new();
    private readonly ApplicationDbContext _context;

    public TaskWorkflowStateTests()
    {
        _requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = _requestContext });
    }

    [Theory]
    [InlineData(CaseTaskStatus.Completed)]
    [InlineData(CaseTaskStatus.Cancelled)]
    public async Task CompleteAndCancel_RejectTerminalTasksWithoutChangingThem(CaseTaskStatus status)
    {
        var task = await AddTaskAsync(status);
        var service = new TaskService(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteTask(task.Id, "forged second completion", "workflow-user"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelTask(task.Id, "forged second cancellation", "workflow-user"));

        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(status, persisted.Status);
        Assert.NotEqual("forged second completion", persisted.CompletionNotes);
    }

    [Fact]
    public async Task StartTask_TransitionsAnActiveTaskButCannotReopenATerminalTask()
    {
        var pending = await AddTaskAsync(CaseTaskStatus.Pending);
        var completed = await AddTaskAsync(CaseTaskStatus.Completed);
        var service = new TaskService(_context);

        await service.StartTask(pending.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartTask(completed.Id));

        var persistedPending = await _context.CaseTasks.SingleAsync(task => task.Id == pending.Id);
        var persistedCompleted = await _context.CaseTasks.SingleAsync(task => task.Id == completed.Id);
        Assert.Equal(CaseTaskStatus.InProgress, persistedPending.Status);
        Assert.Equal(CaseTaskStatus.Completed, persistedCompleted.Status);
    }

    [Fact]
    public async Task UpdateTask_RejectsChangesToATerminalTask()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Cancelled);
        var service = new TaskService(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateTask(task.Id, new CaseTask
        {
            Title = "Forged update",
            Priority = TaskPriority.Urgent
        }));

        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(CaseTaskStatus.Cancelled, persisted.Status);
        Assert.NotEqual("Forged update", persisted.Title);
    }

    [Theory]
    [InlineData(CaseTaskStatus.Pending, CaseTaskStatus.Completed)]
    [InlineData(CaseTaskStatus.Pending, CaseTaskStatus.Cancelled)]
    [InlineData(CaseTaskStatus.InProgress, CaseTaskStatus.Completed)]
    [InlineData(CaseTaskStatus.InProgress, CaseTaskStatus.Cancelled)]
    public void NormalStatusEdit_RejectsTerminalTransitions(
        CaseTaskStatus current,
        CaseTaskStatus requested)
    {
        // A forged normal-edit POST must not be able to bypass the dedicated
        // completion/cancellation workflows and their audit metadata.
        Assert.False(TaskWorkflowPolicy.CanChangeStatus(current, requested));
    }

    [Theory]
    [InlineData(CaseTaskStatus.Pending, CaseTaskStatus.InProgress)]
    [InlineData(CaseTaskStatus.InProgress, CaseTaskStatus.WaitingForPatient)]
    public void NormalStatusEdit_AllowsNonTerminalTransitions(
        CaseTaskStatus current,
        CaseTaskStatus requested)
    {
        Assert.True(TaskWorkflowPolicy.CanChangeStatus(current, requested));
    }

    private async Task<CaseTask> AddTaskAsync(CaseTaskStatus status)
    {
        var disease = new Disease
        {
            Id = Guid.NewGuid(),
            Name = "Task workflow test disease",
            Code = $"TASK-{Guid.NewGuid():N}"[..18],
            ExportCode = $"EXPORT-{Guid.NewGuid():N}"[..20],
            PathIds = "task-workflow"
        };
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Task",
            FamilyName = "Workflow",
            FriendlyId = $"PT-{Guid.NewGuid():N}"[..18]
        };
        var caseRecord = new Case
        {
            Id = Guid.NewGuid(),
            DiseaseId = disease.Id,
            Disease = disease,
            PatientId = patient.Id,
            Patient = patient,
            FriendlyId = $"CASE-{Guid.NewGuid():N}"[..20],
            Type = CaseType.Case
        };
        var task = new CaseTask
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            Case = caseRecord,
            Title = "Task workflow regression",
            Status = status,
            Priority = TaskPriority.Medium
        };

        _context.CaseTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _context.Dispose();
}
