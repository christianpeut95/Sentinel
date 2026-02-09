using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Surveillance_MVP.Pages.Tasks
{
    [Authorize(Policy = "Permission.Task.Edit")]
    public class CompleteSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISurveyService _surveyService;

        public CompleteSurveyModel(ApplicationDbContext context, ISurveyService surveyService)
        {
            _context = context;
            _surveyService = surveyService;
        }

        public CaseTask Task { get; set; } = null!;
        public string? SurveyDefinitionJson { get; set; }
        public string PrePopulatedDataJson { get; set; } = "{}";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Disease)
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, [FromBody] Dictionary<string, object> responses)
        {
            var task = await _context.CaseTasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // Check if user is assigned to this task
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.AssignedToUserId != currentUserId)
            {
                return Forbid();
            }

            // Save survey response
            await _surveyService.SaveSurveyResponseAsync(id, responses);

            // Mark task as completed
            task.Status = CaseTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = currentUserId;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}
