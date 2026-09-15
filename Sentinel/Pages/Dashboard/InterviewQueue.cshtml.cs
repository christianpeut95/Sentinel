using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Dashboard;

[Authorize(Policy = "Permission.Task.View")]
public class InterviewQueueModel : PageModel
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<InterviewQueueModel> _logger;

    public InterviewQueueModel(
        ITaskAssignmentService assignmentService,
        ApplicationDbContext context,
        IPermissionService permissionService,
        ILogger<InterviewQueueModel> logger)
    {
        _assignmentService = assignmentService;
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    public List<CaseTask> MyTasks { get; set; } = new();
    public CaseTask? CurrentTask { get; set; }
    public List<TaskCallAttempt> CurrentTaskCallAttempts { get; set; } = new();
    public WorkerStatistics? Stats { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        try
        {
            MyTasks = await _assignmentService.GetAssignedTasksForWorkerAsync(userId);
            
            if (MyTasks.Any())
            {
                CurrentTask = MyTasks.First();
                CurrentTaskCallAttempts = await _assignmentService.GetCallAttemptsAsync(CurrentTask.Id);
            }

            Stats = await _assignmentService.GetWorkerStatisticsAsync(userId);
            
            _logger.LogInformation("Interview Queue loaded for user {UserId}. Stats loaded: {StatsLoaded}, IsAvailable: {IsAvailable}", 
                userId, Stats != null, Stats?.IsAvailable ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading interview queue for user {UserId}", userId);
            TempData["ErrorMessage"] = "Error loading dashboard. Please ensure you are configured as an interview worker.";
        }
    }

    public async Task<IActionResult> OnPostGetNextTaskAsync()
    {
        if (!await CanEditTasksAsync())
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var task = await _assignmentService.AssignNextTaskAsync(userId);

        if (task != null)
        {
            TempData["SuccessMessage"] = "New task assigned!";
        }
        else
        {
            TempData["InfoMessage"] = "No tasks available at this time";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLogCallAttemptAsync(Guid taskId, CallOutcome outcome, string? notes)
    {
        if (!await CanEditAssignedTaskAsync(taskId))
        {
            return Forbid();
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _assignmentService.LogCallAttemptAsync(taskId, userId, outcome, notes);

            TempData["SuccessMessage"] = $"Call outcome logged: {outcome}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging call attempt");
            TempData["ErrorMessage"] = "Failed to log call outcome";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetAvailabilityAsync(bool available)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            _logger.LogInformation("User {UserId} attempting to set availability to {Available}", userId, available);
            
            var result = await _assignmentService.SetWorkerAvailabilityAsync(userId, available);

            if (result)
            {
                TempData["SuccessMessage"] = $"Status updated: {(available ? "? Available" : "? Unavailable")} for task assignments";
                _logger.LogInformation("User {UserId} availability successfully set to {Available}", userId, available);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update availability. Please ensure you are configured as an interview worker.";
                _logger.LogWarning("Failed to set availability for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting availability");
            TempData["ErrorMessage"] = "An error occurred while updating your availability status.";
        }
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSkipTaskAsync(Guid taskId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _assignmentService.SkipTaskAsync(taskId, userId);

            TempData["InfoMessage"] = "Task skipped. Getting next task...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error skipping task");
            TempData["ErrorMessage"] = "Failed to skip task";
        }

        return RedirectToPage();
    }

    private async Task<bool> CanEditTasksAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
               await _permissionService.HasPermissionAsync(
                   userId,
                   PermissionModule.Task,
                   PermissionAction.Edit);
    }

    private async Task<bool> CanEditAssignedTaskAsync(Guid taskId)
    {
        if (!await CanEditTasksAsync())
        {
            return false;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
               await _context.CaseTasks
                   .AsNoTracking()
                   .AnyAsync(task => task.Id == taskId && task.AssignedToUserId == userId);
    }
}
