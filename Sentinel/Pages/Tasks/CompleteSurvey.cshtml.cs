using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Pages.Tasks
{
    [Authorize(Policy = "Permission.Survey.Complete")]
    public class CompleteSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISurveyService _surveyService;
        private readonly ILogger<CompleteSurveyModel> _logger;

        public CompleteSurveyModel(
            ApplicationDbContext context, 
            ISurveyService surveyService,
            ILogger<CompleteSurveyModel> logger)
        {
            _context = context;
            _surveyService = surveyService;
            _logger = logger;
        }

        public CaseTask Task { get; set; } = null!;
        public string? SurveyDefinitionJson { get; set; }
        public string PrePopulatedDataJson { get; set; } = "{}";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.SexAtBirth)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.Gender)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.AtsiStatus)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.LanguageSpokenAtHome)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.Occupation)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Disease)
                .Include(t => t.Case)
                    .ThenInclude(c => c.ConfirmationStatus)
                .Include(t => t.TaskTemplate)
                .Include(t => t.TaskType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Task == null)
                return NotFound();

            // Check if user is assigned to this task
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Task.AssignedToUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "You are not assigned to this task.";
                return RedirectToPage("/Dashboard/MyTasks");
            }

            // Check if task is already completed
            if (Task.Status == CaseTaskStatus.Completed)
            {
                TempData["ErrorMessage"] = "This task is already completed.";
                return RedirectToPage("/Dashboard/MyTasks");
            }

            // Get survey definition and pre-populated data
            var surveyData = await _surveyService.GetSurveyForTaskAsync(id);
            
            if (!surveyData.HasSurvey)
            {
                TempData["ErrorMessage"] = "This task does not have a survey configured.";
                return RedirectToPage("/Dashboard/MyTasks");
            }

            SurveyDefinitionJson = surveyData.SurveyDefinitionJson;
            PrePopulatedDataJson = JsonSerializer.Serialize(surveyData.PrePopulatedData);

            // Automatically set task to InProgress when survey is opened
            if (Task.Status == CaseTaskStatus.Pending || Task.Status == CaseTaskStatus.WaitingForPatient)
            {
                Task.Status = CaseTaskStatus.InProgress;
                Task.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Task {TaskId} automatically set to InProgress when survey opened by user {UserId}", 
                    id, currentUserId);
            }

            return Page();
        }

        // Note: This handler is deprecated in favor of the API endpoint at /api/surveys/complete/{taskId}
        // Keeping it for backward compatibility only
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAsync(Guid id, [FromBody] Dictionary<string, object> responses)
        {
            try
            {
                _logger.LogInformation("Starting survey save for task {TaskId}", id);

                var task = await _context.CaseTasks
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found", id);
                    return NotFound();
                }

                // Check if user is assigned to this task
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (task.AssignedToUserId != currentUserId)
                {
                    _logger.LogWarning("User {UserId} not assigned to task {TaskId}", currentUserId, id);
                    return Forbid();
                }

                // Save survey response
                _logger.LogInformation("Saving survey response for task {TaskId}", id);
                await _surveyService.SaveSurveyResponseAsync(id, responses);

                // Mark task as completed
                task.Status = CaseTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = currentUserId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully completed survey for task {TaskId}", id);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving survey for task {TaskId}: {ErrorMessage}", id, ex.Message);
                
                // Check if this is a "saved but needs review" exception
                if (ex.Message.Contains("review item has been created") || 
                    ex.Message.Contains("Survey data was saved"))
                {
                    _logger.LogInformation("Survey saved with warnings for task {TaskId}, redirecting to Data Inbox", id);
                    
                    return new JsonResult(new 
                    { 
                        success = true, // ? Treat as success (JSON was saved)
                        warning = true,
                        message = "? Survey saved! Your responses are secure. However, automatic processing encountered an issue and your submission needs manual review. You'll be redirected to the Data Review Inbox.",
                        redirectUrl = "/DataInbox/Index" // Redirect to review queue
                    });
                }
                
                // For any other exception, return error
                _logger.LogError("Unhandled exception saving survey for task {TaskId}: {Exception}", id, ex);
                
                return new JsonResult(new 
                { 
                    success = false, 
                    error = ex.Message 
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
