using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Controllers;

[Authorize]
[Authorize(Policy = "Permission.Task.View")]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("workflow-api")] // 100 per minute - active polling/task management
public class InterviewQueueController : ControllerBase
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<InterviewQueueController> _logger;

    public InterviewQueueController(
        ITaskAssignmentService assignmentService,
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<InterviewQueueController> logger)
    {
        _assignmentService = assignmentService;
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tasks = await _assignmentService.GetAssignedTasksForWorkerAsync(userId);
            return Ok(tasks.Select(ToTaskResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving worker tasks");
            return StatusCode(500, new { error = "Failed to retrieve tasks" });
        }
    }

    [HttpPost("assign-next")]
    [Authorize(Policy = "Permission.Task.Edit")]
    public async Task<IActionResult> AssignNextTask()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var task = await _assignmentService.AssignNextTaskAsync(userId);
            
            if (task == null)
            {
                return Ok(new { message = "No tasks available", task = (CaseTask?)null });
            }

            return Ok(new { message = "Task assigned", task = ToTaskResponse(task) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning task");
            return StatusCode(500, new { error = "Failed to assign task" });
        }
    }

    [HttpPost("log-call-attempt")]
    [Authorize(Policy = "Permission.Task.Edit")]
    public async Task<IActionResult> LogCallAttempt([FromBody] LogCallAttemptRequest request)
    {
        if (!await CanAccessTaskCallAsync(request.TaskId))
        {
            return NotFound();
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var attempt = await _assignmentService.LogCallAttemptAsync(
                request.TaskId,
                userId,
                request.Outcome,
                request.Notes,
                request.DurationSeconds,
                request.NextCallbackScheduled);

            return Ok(ToCallAttemptResponse(attempt));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Rejected invalid call attempt input for task {TaskId}", request.TaskId);
            return BadRequest(new { error = "The call attempt details are not valid." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejected call attempt state for task {TaskId}", request.TaskId);
            return Conflict(new { error = "The task is no longer available for a call attempt." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging call attempt for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "Failed to log call attempt" });
        }
    }

    [HttpGet("call-attempts/{taskId}")]
    public async Task<IActionResult> GetCallAttempts(Guid taskId)
    {
        if (!await CanAccessTaskCallAsync(taskId))
        {
            return NotFound();
        }

        try
        {
            var attempts = await _assignmentService.GetCallAttemptsAsync(taskId);
            return Ok(attempts.Select(ToCallAttemptResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving call attempts for task {TaskId}", taskId);
            return StatusCode(500, new { error = "Failed to retrieve call attempts" });
        }
    }

    [HttpGet("my-stats")]
    public async Task<IActionResult> GetMyStatistics([FromQuery] DateTime? fromDate = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var stats = await _assignmentService.GetWorkerStatisticsAsync(userId, fromDate);
            return Ok(ToWorkerStatisticsResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving worker statistics");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    [HttpPost("set-availability")]
    public async Task<IActionResult> SetAvailability([FromBody] SetAvailabilityRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.SetWorkerAvailabilityAsync(userId, request.Available);
            
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Availability updated", available = request.Available });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting availability");
            return StatusCode(500, new { error = "Failed to set availability" });
        }
    }

    // Supervisor endpoints
    [HttpGet("supervisor/dashboard")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetSupervisorDashboard()
    {
        try
        {
            var data = await _assignmentService.GetSupervisorDashboardAsync();
            return Ok(ToSupervisorDashboardResponse(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supervisor dashboard");
            return StatusCode(500, new { error = "Failed to retrieve dashboard" });
        }
    }

    [HttpGet("supervisor/unassigned-tasks")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUnassignedTasks()
    {
        try
        {
            var tasks = await _assignmentService.GetUnassignedInterviewTasksAsync();
            return Ok(tasks.Select(ToTaskResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unassigned tasks");
            return StatusCode(500, new { error = "Failed to retrieve tasks" });
        }
    }

    [HttpPost("supervisor/assign-task")]
    [Authorize(Roles = "Admin,Supervisor")]
    [Authorize(Policy = "Permission.Task.Edit")]
    public async Task<IActionResult> ManuallyAssignTask([FromBody] ManualAssignRequest request)
    {
        try
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.ManuallyAssignTaskAsync(
                request.TaskId,
                request.UserId,
                supervisorId);

            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Task assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually assigning task {TaskId} to {UserId}", 
                request.TaskId, request.UserId);
            return StatusCode(500, new { error = "Failed to assign task" });
        }
    }

    [HttpPost("supervisor/reassign-task")]
    [Authorize(Roles = "Admin,Supervisor")]
    [Authorize(Policy = "Permission.Task.Edit")]
    public async Task<IActionResult> ReassignTask([FromBody] ReassignTaskRequest request)
    {
        try
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.ReassignTaskAsync(
                request.TaskId,
                request.NewUserId,
                supervisorId,
                request.Reason);

            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Task reassigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "Failed to reassign task" });
        }
    }

    [HttpPost("supervisor/escalate-task")]
    [Authorize(Roles = "Admin,Supervisor")]
    [Authorize(Policy = "Permission.Task.Edit")]
    public async Task<IActionResult> EscalateTask([FromBody] EscalateTaskRequest request)
    {
        try
        {
            var success = await _assignmentService.EscalateTaskAsync(request.TaskId, request.Reason);

            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Task escalated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "Failed to escalate task" });
        }
    }

    [HttpGet("supervisor/available-workers")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAvailableWorkers([FromQuery] string? language = null)
    {
        try
        {
            var workers = await _assignmentService.GetAvailableWorkersAsync(language);
            return Ok(workers.Select(ToWorkerResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available workers");
            return StatusCode(500, new { error = "Failed to retrieve workers" });
        }
    }

    private async Task<bool> CanAccessTaskCallAsync(Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        // CaseTask retains its hierarchy-aware global query filter here. Do not use
        // IgnoreQueryFilters: a guessed task ID must not disclose a restricted case.
        var assignedUserId = await _context.CaseTasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => task.AssignedToUserId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(assignedUserId))
        {
            return false;
        }

        if (assignedUserId == userId)
        {
            return true;
        }

        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            return false;
        }

        return await _permissionService.HasPermissionAsync(
            userId,
            PermissionModule.Task,
            PermissionAction.Edit);
    }

    // API contracts must be explicit. Returning EF/Identity entities from this controller
    // can disclose fields that a caller does not need, including Identity security metadata,
    // task survey content and contact details loaded through navigation properties.
    private static InterviewQueueTaskResponse ToTaskResponse(CaseTask task) => new(
        task.Id,
        task.CaseId,
        task.Title,
        task.Description,
        task.TaskType?.Name,
        task.Priority,
        task.Status,
        task.AssignmentType,
        task.AssignedToUserId,
        task.AssignedToUser is null ? null : BuildDisplayName(task.AssignedToUser),
        task.CreatedAt,
        task.DueDate,
        task.CompletedAt,
        task.IsInterviewTask,
        task.LanguageRequired,
        task.MaxCallAttempts,
        task.CurrentAttemptCount,
        task.EscalationLevel,
        task.LastCallAttempt,
        task.AutoAssignedAt,
        task.Case is null ? null : new InterviewQueueCaseResponse(
            task.Case.Id,
            task.Case.FriendlyId,
            task.Case.DiseaseId,
            task.Case.Disease?.Name));

    private static InterviewQueueCallAttemptResponse ToCallAttemptResponse(TaskCallAttempt attempt) => new(
        attempt.Id,
        attempt.TaskId,
        attempt.AttemptedAt,
        attempt.Outcome,
        attempt.Notes,
        attempt.DurationSeconds,
        attempt.NextCallbackScheduled,
        attempt.AttemptedByUser is null
            ? null
            : new InterviewQueueUserResponse(
                attempt.AttemptedByUser.Id,
                BuildDisplayName(attempt.AttemptedByUser)));

    private static InterviewQueueWorkerResponse ToWorkerResponse(ApplicationUser worker) => new(
        worker.Id,
        BuildDisplayName(worker),
        worker.PrimaryLanguage,
        ParseLanguages(worker.LanguagesSpokenJson),
        worker.AvailableForAutoAssignment,
        worker.CurrentTaskCapacity);

    private static InterviewQueueWorkerStatisticsResponse ToWorkerStatisticsResponse(WorkerStatistics stats) => new(
        stats.UserId,
        stats.WorkerName,
        stats.TasksAssigned,
        stats.TasksCompleted,
        stats.TasksInProgress,
        stats.CallsToday,
        stats.SuccessfulCallsToday,
        stats.CompletionRate,
        stats.AverageDurationSeconds,
        stats.LanguagesSpoken,
        stats.IsAvailable);

    private static InterviewQueueSupervisorDashboardResponse ToSupervisorDashboardResponse(SupervisorDashboardData data) => new(
        data.UnassignedTaskCount,
        data.EscalatedTaskCount,
        data.ActiveWorkerCount,
        data.TotalTasksToday,
        data.CompletedTasksToday,
        data.WorkerStats.Select(ToWorkerStatisticsResponse).ToList(),
        data.EscalatedTasks.Select(ToTaskResponse).ToList(),
        data.UnassignedTasks.Select(ToTaskResponse).ToList(),
        data.LanguageCoverage);

    private static string BuildDisplayName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.UserName ?? "Unknown user" : name;
    }

    private static IReadOnlyList<string> ParseLanguages(string? languagesJson)
    {
        if (string.IsNullOrWhiteSpace(languagesJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(languagesJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}

public sealed record InterviewQueueTaskResponse(
    Guid Id,
    Guid CaseId,
    string Title,
    string? Description,
    string? TaskTypeName,
    TaskPriority Priority,
    CaseTaskStatus Status,
    TaskAssignmentType AssignmentType,
    string? AssignedToUserId,
    string? AssignedToUserName,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? CompletedAt,
    bool IsInterviewTask,
    string? LanguageRequired,
    int MaxCallAttempts,
    int CurrentAttemptCount,
    int EscalationLevel,
    DateTime? LastCallAttempt,
    DateTime? AutoAssignedAt,
    InterviewQueueCaseResponse? Case);

public sealed record InterviewQueueCaseResponse(
    Guid Id,
    string FriendlyId,
    Guid? DiseaseId,
    string? DiseaseName);

public sealed record InterviewQueueCallAttemptResponse(
    Guid Id,
    Guid TaskId,
    DateTime AttemptedAt,
    CallOutcome Outcome,
    string? Notes,
    int? DurationSeconds,
    DateTime? NextCallbackScheduled,
    InterviewQueueUserResponse? AttemptedByUser);

public sealed record InterviewQueueUserResponse(string Id, string DisplayName);

public sealed record InterviewQueueWorkerResponse(
    string Id,
    string DisplayName,
    string? PrimaryLanguage,
    IReadOnlyList<string> LanguagesSpoken,
    bool AvailableForAutoAssignment,
    int CurrentTaskCapacity);

public sealed record InterviewQueueWorkerStatisticsResponse(
    string UserId,
    string WorkerName,
    int TasksAssigned,
    int TasksCompleted,
    int TasksInProgress,
    int CallsToday,
    int SuccessfulCallsToday,
    double CompletionRate,
    double AverageDurationSeconds,
    IReadOnlyList<string> LanguagesSpoken,
    bool IsAvailable);

public sealed record InterviewQueueSupervisorDashboardResponse(
    int UnassignedTaskCount,
    int EscalatedTaskCount,
    int ActiveWorkerCount,
    int TotalTasksToday,
    int CompletedTasksToday,
    IReadOnlyList<InterviewQueueWorkerStatisticsResponse> WorkerStats,
    IReadOnlyList<InterviewQueueTaskResponse> EscalatedTasks,
    IReadOnlyList<InterviewQueueTaskResponse> UnassignedTasks,
    IReadOnlyDictionary<string, int> LanguageCoverage);

public class LogCallAttemptRequest
{
    public Guid TaskId { get; set; }
    public CallOutcome Outcome { get; set; }
    public string? Notes { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? NextCallbackScheduled { get; set; }
}

public class SetAvailabilityRequest
{
    public bool Available { get; set; }
}

public class ManualAssignRequest
{
    public Guid TaskId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class ReassignTaskRequest
{
    public Guid TaskId { get; set; }
    public string? NewUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class EscalateTaskRequest
{
    public Guid TaskId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
