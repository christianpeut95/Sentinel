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
        private readonly ICaseAccessService _caseAccessService;
        private readonly ISurveyService _surveyService;
        private readonly ILogger<CompleteSurveyModel> _logger;

        public CompleteSurveyModel(
            ApplicationDbContext context, 
            ICaseAccessService caseAccessService,
            ISurveyService surveyService,
            ILogger<CompleteSurveyModel> logger)
        {
            _context = context;
            _caseAccessService = caseAccessService;
            _surveyService = surveyService;
            _logger = logger;
        }

        public CaseTask Task { get; set; } = null!;
        public string? SurveyDefinitionJson { get; set; }
        public string PrePopulatedDataJson { get; set; } = "{}";
        public string? SurveyName { get; set; }
        public string? SurveyVersionNumber { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            // Authorise using only task metadata before loading the linked case,
            // patient, and survey data.
            var taskSummary = await _context.CaseTasks
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new { t.CaseId, t.Status })
                .FirstOrDefaultAsync();

            if (taskSummary == null || !await _caseAccessService.CanAccessCaseAsync(taskSummary.CaseId))
                return NotFound();

            if (TaskWorkflowPolicy.IsTerminal(taskSummary.Status))
            {
                TempData["ErrorMessage"] = TaskStateMessage(taskSummary.Status);
                return RedirectToPage("/Dashboard/MyTasks");
            }

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

            // Get survey definition and pre-populated data
            var surveyData = await _surveyService.GetSurveyForTaskAsync(id);
            
            if (!surveyData.HasSurvey)
            {
                TempData["ErrorMessage"] = "This task does not have a survey configured.";
                return RedirectToPage("/Dashboard/MyTasks");
            }

            SurveyDefinitionJson = surveyData.SurveyDefinitionJson;
            PrePopulatedDataJson = JsonSerializer.Serialize(surveyData.PrePopulatedData);
            SurveyName = surveyData.SurveyName;
            SurveyVersionNumber = surveyData.SurveyVersionNumber;

            return Page();
        }

        /// <summary>
        /// Records that a survey has been opened. This is intentionally a POST
        /// handler: GET requests must not change task state.
        /// </summary>
        public async Task<IActionResult> OnPostStartAsync(Guid id)
        {
            var taskSummary = await _context.CaseTasks
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new { t.CaseId })
                .FirstOrDefaultAsync();

            if (taskSummary == null || !await _caseAccessService.CanAccessCaseAsync(taskSummary.CaseId))
            {
                return NotFound();
            }

            var task = await _context.CaseTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            if (TaskWorkflowPolicy.IsTerminal(task.Status))
            {
                return StatusCode(StatusCodes.Status409Conflict, new { error = TaskStateMessage(task.Status) });
            }

            if (task.Status == CaseTaskStatus.Pending || task.Status == CaseTaskStatus.WaitingForPatient)
            {
                task.Status = CaseTaskStatus.InProgress;
                task.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Task {TaskId} set to InProgress after survey opened by user {UserId}",
                    id,
                    User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            return new JsonResult(new { success = true });
        }

        // Note: This handler is deprecated in favor of the API endpoint at /api/surveys/complete/{taskId}
        // Keeping it for backward compatibility only
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

                if (!await _caseAccessService.CanAccessCaseAsync(task.CaseId))
                {
                    _logger.LogWarning("Survey completion denied for inaccessible task {TaskId}", id);
                    return NotFound();
                }

                if (TaskWorkflowPolicy.IsTerminal(task.Status))
                {
                    _logger.LogWarning("Survey completion rejected because task {TaskId} is in terminal state {TaskStatus}", id, task.Status);
                    return new JsonResult(new { success = false, error = TaskStateMessage(task.Status) })
                    {
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
            catch (SurveyTaskStateException ex)
            {
                _logger.LogInformation(ex, "Survey completion rejected after service-level task state validation for task {TaskId}", id);
                return new JsonResult(new { success = false, error = TaskStateMessage(ex.Status) })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving survey for task {TaskId}", id);
                
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
                _logger.LogError(ex, "Unhandled exception saving survey for task {TaskId}", id);
                
                return new JsonResult(new 
                { 
                    success = false, 
                    error = "The survey could not be saved. Your responses have not been submitted; please try again.",
                    traceId = HttpContext.TraceIdentifier
                })
                {
                    StatusCode = 500
                };
            }
        }

        private static string TaskStateMessage(CaseTaskStatus status) =>
            status == CaseTaskStatus.Completed
                ? "This task is already completed."
                : "This task has been cancelled.";
    }
}
