using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Text.Json;

namespace Sentinel.Pages.Tasks
{
    [Authorize(Policy = "Permission.Survey.Complete")]
    public class ViewSurveyResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISurveyService _surveyService;
        private readonly ILogger<ViewSurveyResultModel> _logger;

        public ViewSurveyResultModel(
            ApplicationDbContext context, 
            ISurveyService surveyService,
            ILogger<ViewSurveyResultModel> logger)
        {
            _context = context;
            _surveyService = surveyService;
            _logger = logger;
        }

        public CaseTask Task { get; set; } = null!;
        public string? SurveyDefinitionJson { get; set; }
        public string? SurveyResponseJson { get; set; }
        public bool HasSurveyResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Loading survey result for task {TaskId}", id);

                Task = await _context.CaseTasks
                    .Include(t => t.Case)
                        .ThenInclude(c => c.Patient)
                    .Include(t => t.Case)
                        .ThenInclude(c => c.Disease)
                    .Include(t => t.TaskTemplate)
                    .Include(t => t.TaskType)
                    .Include(t => t.CompletedByUser)
                    .Include(t => t.AssignedToUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (Task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Task found - Status: {Status}, HasSurveyResponse: {HasResponse}", 
                    Task.Status, !string.IsNullOrEmpty(Task.SurveyResponseJson));

                if (Task.Status != CaseTaskStatus.Completed)
                {
                    _logger.LogWarning("Task {TaskId} is not completed (Status: {Status})", id, Task.Status);
                    TempData["ErrorMessage"] = "This task is not completed yet.";
                    return RedirectToPage("/Cases/Details", new { id = Task.CaseId });
                }

                if (string.IsNullOrEmpty(Task.SurveyResponseJson))
                {
                    _logger.LogWarning("Task {TaskId} has no survey response", id);
                    TempData["ErrorMessage"] = "This task does not have a survey response.";
                    return RedirectToPage("/Cases/Details", new { id = Task.CaseId });
                }

                _logger.LogInformation("Fetching survey definition for task {TaskId}", id);
                var surveyData = await _surveyService.GetSurveyForTaskAsync(id);
                
                if (!surveyData.HasSurvey)
                {
                    _logger.LogWarning("No survey definition found for task {TaskId}", id);
                    TempData["ErrorMessage"] = "Survey definition not found for this task.";
                    return RedirectToPage("/Cases/Details", new { id = Task.CaseId });
                }

                _logger.LogInformation("Survey data loaded successfully for task {TaskId}", id);
                
                SurveyDefinitionJson = surveyData.SurveyDefinitionJson;
                SurveyResponseJson = Task.SurveyResponseJson;
                HasSurveyResponse = true;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading survey result for task {TaskId}: {ErrorMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error loading survey result: {ex.Message}";
                return RedirectToPage("/Dashboard/MyTasks");
            }
        }
    }
}
