using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Cases
{
    [Authorize]
    public class AddTaskModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;

        public AddTaskModel(ApplicationDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
        }

        public Case Case { get; set; }
        public string CaseId { get; set; }
        public List<TaskTemplate> AvailableTaskTemplates { get; set; } = new();
        public SelectList TaskTypesList { get; set; }
        public SelectList SurveyTemplatesList { get; set; }

        [BindProperty]
        public Guid? SelectedTaskTemplateId { get; set; }

        [BindProperty]
        public string? ManualTitle { get; set; }

        [BindProperty]
        public string? ManualDescription { get; set; }

        [BindProperty]
        public Guid? ManualTaskTypeId { get; set; }

        [BindProperty]
        public TaskPriority ManualPriority { get; set; } = TaskPriority.Medium;

        [BindProperty]
        public DateTime? ManualDueDate { get; set; }

        [BindProperty]
        public Guid? SurveyTemplateId { get; set; }

        [BindProperty]
        public string? CustomSurveyJson { get; set; }

        [BindProperty]
        public string? AssignedToUserId { get; set; }

        public async Task<IActionResult> OnGetAsync(string caseId)
        {
            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == Guid.Parse(caseId));

            if (Case == null)
            {
                return NotFound();
            }

            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostTemplateAsync(string caseId)
        {
            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == Guid.Parse(caseId));

            if (Case == null || !SelectedTaskTemplateId.HasValue)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Use centralized task creation service (supports exposure dates!)
            var task = await _taskService.CreateTaskFromTemplateForCaseAsync(
                Case.Id,
                SelectedTaskTemplateId.Value,
                !string.IsNullOrEmpty(AssignedToUserId) ? AssignedToUserId : userId
            );

            return Content(
                "<script>window.opener.location.reload(); window.close();</script>",
                "text/html"
            );
        }

        public async Task<IActionResult> OnPostManualAsync(string caseId)
        {
            CaseId = caseId;

            Case = await _context.Cases.FindAsync(Guid.Parse(caseId));
            if (Case == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(ManualTitle) || !ManualTaskTypeId.HasValue)
            {
                await LoadData();
                ModelState.AddModelError("", "Title and Task Type are required.");
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskType = await _context.TaskTypes.FindAsync(ManualTaskTypeId.Value);

            var task = new CaseTask
            {
                CaseId = Case.Id,
                Title = ManualTitle,
                Description = ManualDescription,
                TaskTypeId = ManualTaskTypeId.Value,
                Priority = ManualPriority,
                DueDate = ManualDueDate,
                Status = CaseTaskStatus.Pending,
                AssignedToUserId = !string.IsNullOrEmpty(AssignedToUserId) ? AssignedToUserId : userId,
                IsInterviewTask = taskType?.IsInterviewTask ?? false,
                CreatedAt = DateTime.UtcNow
            };

            // Handle survey if provided
            if (SurveyTemplateId.HasValue)
            {
                var template = await _context.SurveyTemplates.FindAsync(SurveyTemplateId.Value);
                if (template != null)
                {
                    task.TaskTemplate = new TaskTemplate
                    {
                        Name = ManualTitle,
                        Description = ManualDescription,
                        TaskTypeId = ManualTaskTypeId.Value,
                        DefaultPriority = ManualPriority,
                        SurveyTemplateId = SurveyTemplateId.Value,
                        IsActive = false // Manual tasks don't create reusable templates
                    };
                }
            }
            else if (!string.IsNullOrEmpty(CustomSurveyJson))
            {
                task.TaskTemplate = new TaskTemplate
                {
                    Name = ManualTitle,
                    Description = ManualDescription,
                    TaskTypeId = ManualTaskTypeId.Value,
                    DefaultPriority = ManualPriority,
                    SurveyDefinitionJson = CustomSurveyJson,
                    IsActive = false
                };
            }

            _context.CaseTasks.Add(task);
            await _context.SaveChangesAsync();

            return Content(
                "<script>window.opener.location.reload(); window.close();</script>",
                "text/html"
            );
        }

        private async Task LoadData()
        {
            // Load task templates for this disease, filtered to Cases only (ApplicableToType == null || == Case)
            if (Case.DiseaseId.HasValue)
            {
                var templateSources = await _taskService.GetApplicableTaskTemplates(Case.DiseaseId.Value);
                AvailableTaskTemplates = templateSources
                    .Select(ts => ts.Template)
                    .Where(t => t.IsActive && (t.ApplicableToType == null || t.ApplicableToType == Case.Type))
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            TaskTypesList = new SelectList(
                await _context.TaskTypes.OrderBy(t => t.Name).ToListAsync(),
                "Id", "Name");

            SurveyTemplatesList = new SelectList(
                await _context.SurveyTemplates
                    .Where(st => st.IsActive && st.VersionStatus == SurveyVersionStatus.Active)
                    .OrderBy(st => st.Name)
                    .ToListAsync(),
                "Id", "Name");
        }
    }
}
