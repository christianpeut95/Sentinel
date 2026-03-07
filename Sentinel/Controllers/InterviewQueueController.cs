using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("workflow-api")] // 100 per minute - active polling/task management
public class InterviewQueueController : ControllerBase
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ILogger<InterviewQueueController> _logger;

    public InterviewQueueController(
        ITaskAssignmentService assignmentService,
        ILogger<InterviewQueueController> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tasks = await _assignmentService.GetAssignedTasksForWorkerAsync(userId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving worker tasks");
            return StatusCode(500, new { error = "Failed to retrieve tasks" });
        }
    }

    [HttpPost("assign-next")]
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

            return Ok(new { message = "Task assigned", task });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning task");
            return StatusCode(500, new { error = "Failed to assign task" });
        }
    }

    [HttpPost("log-call-attempt")]
    public async Task<IActionResult> LogCallAttempt([FromBody] LogCallAttemptRequest request)
    {
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

            return Ok(attempt);
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
        try
        {
            var attempts = await _assignmentService.GetCallAttemptsAsync(taskId);
            return Ok(attempts);
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
            return Ok(stats);
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
            return Ok(data);
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
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unassigned tasks");
            return StatusCode(500, new { error = "Failed to retrieve tasks" });
        }
    }

    [HttpPost("supervisor/assign-task")]
    [Authorize(Roles = "Admin,Supervisor")]
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
            return Ok(workers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available workers");
            return StatusCode(500, new { error = "Failed to retrieve workers" });
        }
    }
}

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
