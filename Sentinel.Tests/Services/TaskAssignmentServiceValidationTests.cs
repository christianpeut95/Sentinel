using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class TaskAssignmentServiceValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public TaskAssignmentServiceValidationTests()
    {
        var requestContext = new DefaultHttpContext();
        requestContext.Items["CaseScopedPatientAccess"] = false;

        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = requestContext });
    }

    [Fact]
    public async Task LogCallAttemptAsync_RejectsTerminalTaskWithoutPersistingAttempt()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Completed);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().LogCallAttemptAsync(task.Id, "worker", CallOutcome.NoAnswer));

        Assert.Empty(await _context.TaskCallAttempts.IgnoreQueryFilters().ToListAsync());
        Assert.Equal(CaseTaskStatus.Completed, (await _context.CaseTasks.SingleAsync()).Status);
    }

    [Fact]
    public async Task LogCallAttemptAsync_RejectsInvalidInputBeforePersistingAttempt()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Pending);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateService().LogCallAttemptAsync(task.Id, "worker", CallOutcome.NoAnswer, durationSeconds: -1));

        Assert.Empty(await _context.TaskCallAttempts.IgnoreQueryFilters().ToListAsync());
        Assert.Equal(0, (await _context.CaseTasks.SingleAsync()).CurrentAttemptCount);
    }

    [Fact]
    public async Task LogCallAttemptAsync_RecordsValidAttemptAndUpdatesPendingTask()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Pending);

        var attempt = await CreateService().LogCallAttemptAsync(
            task.Id,
            "worker",
            CallOutcome.NoAnswer,
            notes: "No response from the patient.",
            durationSeconds: 90);

        Assert.Equal(task.Id, attempt.TaskId);
        Assert.Single(await _context.TaskCallAttempts.IgnoreQueryFilters().ToListAsync());
        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(1, persisted.CurrentAttemptCount);
        Assert.Equal(CaseTaskStatus.InProgress, persisted.Status);
    }

    [Fact]
    public async Task AssignmentOperations_RejectTerminalTaskWithoutChangingItsAssignment()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Completed, assignedToUserId: "worker");

        Assert.False(await CreateService().AutoAssignTaskAsync(task.Id));
        Assert.False(await CreateService().ReassignTaskAsync(task.Id, null, "supervisor", "Test"));
        Assert.False(await CreateService().EscalateTaskAsync(task.Id, "Test"));
        Assert.False(await CreateService().SkipTaskAsync(task.Id, "worker"));

        var persisted = await _context.CaseTasks.SingleAsync();
        Assert.Equal(CaseTaskStatus.Completed, persisted.Status);
        Assert.Equal("worker", persisted.AssignedToUserId);
        Assert.Equal(0, persisted.EscalationLevel);
    }

    [Fact]
    public async Task ReassignTaskAsync_RejectsDisabledAssigneeWithoutChangingTask()
    {
        var task = await AddTaskAsync(CaseTaskStatus.Pending);
        _context.Users.Add(new ApplicationUser
        {
            Id = "disabled-worker",
            UserName = "disabled-worker",
            Email = "disabled-worker@example.test",
            IsEnabled = false
        });
        await _context.SaveChangesAsync();

        var reassigned = await CreateService().ReassignTaskAsync(
            task.Id,
            "disabled-worker",
            "supervisor",
            "Test");

        Assert.False(reassigned);
        Assert.Null((await _context.CaseTasks.SingleAsync()).AssignedToUserId);
    }

    private async Task<CaseTask> AddTaskAsync(CaseTaskStatus status, string? assignedToUserId = null)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Test",
            FamilyName = "Patient",
            FriendlyId = "PT-TASK-TEST"
        };
        var caseRecord = new Case
        {
            Id = Guid.NewGuid(),
            FriendlyId = "CA-TASK-TEST",
            PatientId = patient.Id,
            Patient = patient
        };
        var task = new CaseTask
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            Case = caseRecord,
            TaskTypeId = Guid.NewGuid(),
            Title = "Call patient",
            Status = status,
            AssignedToUserId = assignedToUserId,
            MaxCallAttempts = 3
        };

        _context.CaseTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    private TaskAssignmentService CreateService() => new(
        _context,
        NullLogger<TaskAssignmentService>.Instance);

    public void Dispose() => _context.Dispose();
}
