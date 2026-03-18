using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditTaskTemplateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditTaskTemplateModel> _logger;

        public EditTaskTemplateModel(ApplicationDbContext context, ILogger<EditTaskTemplateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TaskTemplate TaskTemplate { get; set; } = default!;

        public SelectList TaskTypes { get; set; } = default!;
        public SelectList SurveyTemplates { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskTemplate = await _context.TaskTemplates
                .Include(t => t.TaskType)
                .Include(t => t.SurveyTemplate)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskTemplate == null)
            {
                return NotFound();
            }

            TaskTemplate = taskTemplate;
            await LoadTaskTypes();
            await LoadSurveyTemplates();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTaskTypes();
                await LoadSurveyTemplates();
                return Page();
            }

            var taskTemplateToUpdate = await _context.TaskTemplates.FindAsync(TaskTemplate.Id);

            if (taskTemplateToUpdate == null)
            {
                return NotFound();
            }

            // Update all properties
            taskTemplateToUpdate.Name = TaskTemplate.Name;
            taskTemplateToUpdate.Description = TaskTemplate.Description;
            taskTemplateToUpdate.TaskTypeId = TaskTemplate.TaskTypeId;
            taskTemplateToUpdate.DefaultPriority = TaskTemplate.DefaultPriority;
            taskTemplateToUpdate.TriggerType = TaskTemplate.TriggerType;
            taskTemplateToUpdate.AssignmentType = TaskTemplate.AssignmentType;
            taskTemplateToUpdate.DueCalculationMethod = TaskTemplate.DueCalculationMethod;
            taskTemplateToUpdate.DueDaysFromOnset = TaskTemplate.DueDaysFromOnset;
            taskTemplateToUpdate.Instructions = TaskTemplate.Instructions;
            taskTemplateToUpdate.CompletionCriteria = TaskTemplate.CompletionCriteria;
            taskTemplateToUpdate.IsActive = TaskTemplate.IsActive;
            taskTemplateToUpdate.RequiresEvidence = TaskTemplate.RequiresEvidence;
            taskTemplateToUpdate.IsInterviewTask = TaskTemplate.IsInterviewTask;
            taskTemplateToUpdate.ApplicableToType = TaskTemplate.ApplicableToType;
            taskTemplateToUpdate.SurveyTemplateId = TaskTemplate.SurveyTemplateId;
            taskTemplateToUpdate.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Task template updated successfully.";
            _logger.LogInformation("Task template {Id} updated by {User}", TaskTemplate.Id, User.Identity?.Name);

            return RedirectToPage("./EditTaskTemplate", new { id = TaskTemplate.Id });
        }

        private async Task LoadTaskTypes()
        {
            var taskTypes = await _context.TaskTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            TaskTypes = new SelectList(taskTypes, "Id", "Name");
        }

        private async Task LoadSurveyTemplates()
        {
            // Show active versions in dropdown but store the family root ID
            // so the link survives when new versions are published
            var activeSurveys = await _context.SurveyTemplates
                .Where(st => st.IsActive && st.VersionStatus == SurveyVersionStatus.Active)
                .OrderBy(st => st.Name)
                .Select(st => new { Id = st.ParentSurveyTemplateId ?? st.Id, st.Name, st.VersionNumber })
                .ToListAsync();

            SurveyTemplates = new SelectList(
                activeSurveys.Select(s => new { s.Id, DisplayName = $"{s.Name} (v{s.VersionNumber})" }),
                "Id",
                "DisplayName");
        }

        private bool TaskTemplateExists(Guid id)
        {
            return _context.TaskTemplates.Any(e => e.Id == id);
        }
    }
}

