using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Dashboard;

[Authorize]
public class InterviewQueueModel : PageModel
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ILogger<InterviewQueueModel> _logger;

    public InterviewQueueModel(ITaskAssignmentService assignmentService, ILogger<InterviewQueueModel> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    public List<CaseTask> MyTasks { get; set; } = new();
    public CaseTask? CurrentTask { get; set; }
    public List<TaskCallAttempt> CurrentTaskCallAttempts { get; set; } = new();
    public WorkerStatistics? Stats { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        MyTasks = await _assignmentService.GetAssignedTasksForWorkerAsync(userId);
        
        if (MyTasks.Any())
        {
            CurrentTask = MyTasks.First();
            CurrentTaskCallAttempts = await _assignmentService.GetCallAttemptsAsync(CurrentTask.Id);
        }

        Stats = await _assignmentService.GetWorkerStatisticsAsync(userId);
    }

    public async Task<IActionResult> OnPostGetNextTaskAsync()
    {
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _assignmentService.SetWorkerAvailabilityAsync(userId, available);

        TempData["SuccessMessage"] = $"Status updated: {(available ? "Available" : "Unavailable")}";
        return RedirectToPage();
    }
}
