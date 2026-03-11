using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Controllers.Api;

[Authorize(Policy = "Permission.Survey.Complete")]
[ApiController]
[Route("api/surveys")]
public class SurveyCompletionApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISurveyService _surveyService;
    private readonly ILogger<SurveyCompletionApiController> _logger;

    public SurveyCompletionApiController(
        ApplicationDbContext context,
        ISurveyService surveyService,
        ILogger<SurveyCompletionApiController> logger)
    {
        _context = context;
        _surveyService = surveyService;
        _logger = logger;
    }

    [HttpPost("complete/{taskId}")]
    public async Task<IActionResult> CompleteSurvey(Guid taskId, [FromBody] Dictionary<string, object> responses)
    {
        try
        {
            _logger.LogInformation("Starting survey save for task {TaskId}", taskId);

            var task = await _context.CaseTasks
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return NotFound(new { success = false, error = "Task not found" });
            }

            // Check if user is assigned to this task
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.AssignedToUserId != currentUserId)
            {
                _logger.LogWarning("User {UserId} not assigned to task {TaskId}", currentUserId, taskId);
                return Forbid();
            }

            // Save survey response
            _logger.LogInformation("Saving survey response for task {TaskId}", taskId);
            await _surveyService.SaveSurveyResponseAsync(taskId, responses);

            // Mark task as completed
            task.Status = CaseTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = currentUserId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully completed survey for task {TaskId}", taskId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving survey for task {TaskId}: {ErrorMessage}", taskId, ex.Message);

            // Check if this is a "saved but needs review" exception
            if (ex.Message.Contains("review item has been created") ||
                ex.Message.Contains("Survey data was saved"))
            {
                _logger.LogInformation("Survey saved with warnings for task {TaskId}, redirecting to Data Inbox", taskId);

                return Ok(new
                {
                    success = true, // ? Treat as success (JSON was saved)
                    warning = true,
                    message = "? Survey saved! Your responses are secure. However, automatic processing encountered an issue and your submission needs manual review. You'll be redirected to the Data Review Inbox.",
                    redirectUrl = "/DataInbox/Index" // Redirect to review queue
                });
            }

            // For any other exception, return error
            _logger.LogError("Unhandled exception saving survey for task {TaskId}: {Exception}", taskId, ex);

            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
