using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateTaskTemplateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateTaskTemplateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public SelectList TaskTypeSelectList { get; set; } = null!;
        public SelectList SurveyTemplateSelectList { get; set; } = null!;

        public class InputModel
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;

            [StringLength(2000)]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Task Type")]
            public Guid TaskTypeId { get; set; }

            [Display(Name = "Disease")]
            public Guid? DiseaseId { get; set; }

            [Required]
            [Display(Name = "Default Priority")]
            public TaskPriority DefaultPriority { get; set; } = TaskPriority.Medium;

            [Required]
            [Display(Name = "Trigger Type")]
            public TaskTrigger TriggerType { get; set; } = TaskTrigger.Manual;

            [Display(Name = "Applicable To")]
            public CaseType? ApplicableToType { get; set; }

            [Display(Name = "Due Calculation Method")]
            public TaskDueCalculationMethod DueCalculationMethod { get; set; } = TaskDueCalculationMethod.FromSymptomOnset;

            [Display(Name = "Due Days")]
            public int? DueDaysFromOnset { get; set; }

            [Display(Name = "Is Recurring")]
            public bool IsRecurring { get; set; }

            [Display(Name = "Recurrence Pattern")]
            public RecurrencePattern? RecurrencePattern { get; set; }

            [Display(Name = "Recurrence Count")]
            public int? RecurrenceCount { get; set; }

            [Display(Name = "Recurrence Duration (Days)")]
            public int? RecurrenceDurationDays { get; set; }

            [StringLength(4000)]
            [Display(Name = "Instructions")]
            public string? Instructions { get; set; }

            [StringLength(1000)]
            [Display(Name = "Completion Criteria")]
            public string? CompletionCriteria { get; set; }

            [Display(Name = "Requires Evidence")]
            public bool RequiresEvidence { get; set; }

            [Required]
            [Display(Name = "Assignment Type")]
            public TaskAssignmentType AssignmentType { get; set; } = TaskAssignmentType.Investigator;

            [Required]
            [Display(Name = "Inheritance Behavior")]
            public TaskInheritanceBehavior InheritanceBehavior { get; set; } = TaskInheritanceBehavior.Inherit;

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;

            [Display(Name = "Is Interview Task")]
            public bool IsInterviewTask { get; set; }

            [Display(Name = "Survey Template")]
            public Guid? SurveyTemplateId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadTaskTypes();
            await LoadSurveyTemplates();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTaskTypes();
                await LoadSurveyTemplates();
                return Page();
            }

            var taskTemplate = new TaskTemplate
            {
                Id = Guid.NewGuid(),
                Name = Input.Name,
                Description = Input.Description,
                TaskTypeId = Input.TaskTypeId,
                DefaultPriority = Input.DefaultPriority,
                TriggerType = Input.TriggerType,
                ApplicableToType = Input.ApplicableToType,
                DueCalculationMethod = Input.DueCalculationMethod,
                DueDaysFromOnset = Input.DueDaysFromOnset,
                IsRecurring = Input.IsRecurring,
                RecurrencePattern = Input.RecurrencePattern,
                RecurrenceCount = Input.RecurrenceCount,
                RecurrenceDurationDays = Input.RecurrenceDurationDays,
                Instructions = Input.Instructions,
                CompletionCriteria = Input.CompletionCriteria,
                RequiresEvidence = Input.RequiresEvidence,
                AssignmentType = Input.AssignmentType,
                InheritanceBehavior = Input.InheritanceBehavior,
                IsActive = Input.IsActive,
                IsInterviewTask = Input.IsInterviewTask,
                SurveyTemplateId = Input.SurveyTemplateId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskTemplates.Add(taskTemplate);
            await _context.SaveChangesAsync();

            // If disease is specified, create the assignment
            if (Input.DiseaseId.HasValue)
            {
                var assignment = new DiseaseTaskTemplate
                {
                    Id = Guid.NewGuid(),
                    DiseaseId = Input.DiseaseId.Value,
                    TaskTemplateId = taskTemplate.Id,
                    IsInherited = false,
                    ApplyToChildren = true,
                    AllowChildOverride = true,
                    AutoCreateOnCaseCreation = Input.TriggerType == TaskTrigger.OnCaseCreation,
                    AutoCreateOnContactCreation = Input.TriggerType == TaskTrigger.OnContactCreation,
                    AutoCreateOnLabConfirmation = Input.TriggerType == TaskTrigger.OnLabConfirmation,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DiseaseTaskTemplates.Add(assignment);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Task template '{taskTemplate.Name}' has been created successfully.";
            return RedirectToPage("./TaskTemplates");
        }

        private async Task LoadTaskTypes()
        {
            var taskTypes = await _context.TaskTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            TaskTypeSelectList = new SelectList(taskTypes, "Id", "Name");
        }

        private async Task LoadSurveyTemplates()
        {
            var surveyTemplates = await _context.SurveyTemplates
                .Where(st => st.IsActive && st.VersionStatus == SurveyVersionStatus.Active)
                .OrderBy(st => st.Name)
                .ToListAsync();

            SurveyTemplateSelectList = new SelectList(surveyTemplates, "Id", "Name");
        }
    }
}
