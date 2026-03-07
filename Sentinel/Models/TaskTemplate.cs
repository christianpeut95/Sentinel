using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models
{
    public class TaskTemplate : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Task Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Task Type")]
        public Guid TaskTypeId { get; set; }
        public TaskType? TaskType { get; set; }

        [Display(Name = "Default Priority")]
        public TaskPriority DefaultPriority { get; set; } = TaskPriority.Medium;

        // Trigger Configuration
        [Display(Name = "Trigger Type")]
        public TaskTrigger TriggerType { get; set; } = TaskTrigger.Manual;

        [Display(Name = "Applicable To")]
        public CaseType? ApplicableToType { get; set; }

        // Timing Configuration
        [Display(Name = "Due Days from Onset")]
        public int? DueDaysFromOnset { get; set; }

        [Display(Name = "Due Days from Notification")]
        public int? DueDaysFromNotification { get; set; }

        [Display(Name = "Due Days from Contact")]
        public int? DueDaysFromContact { get; set; }

        [Display(Name = "Due Date Calculation Method")]
        public TaskDueCalculationMethod DueCalculationMethod { get; set; } = TaskDueCalculationMethod.FromSymptomOnset;

        // Recurrence Configuration
        [Display(Name = "Is Recurring")]
        public bool IsRecurring { get; set; }

        [Display(Name = "Recurrence Pattern")]
        public RecurrencePattern? RecurrencePattern { get; set; }

        [Display(Name = "Recurrence Count")]
        public int? RecurrenceCount { get; set; }

        [Display(Name = "Recurrence Duration (Days)")]
        public int? RecurrenceDurationDays { get; set; }

        // Survey Integration
        [Display(Name = "Survey Template")]
        public Guid? SurveyTemplateId { get; set; }
        public virtual SurveyTemplate? SurveyTemplate { get; set; }

        [Display(Name = "Survey Definition (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? SurveyDefinitionJson { get; set; }

        // Default Survey Mappings (inherited by all diseases unless overridden)
        [Display(Name = "Default Input Mapping (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? DefaultInputMappingJson { get; set; }

        [Display(Name = "Default Output Mapping (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? DefaultOutputMappingJson { get; set; }

        // Task Content
        [StringLength(4000)]
        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string? Instructions { get; set; }

        [StringLength(1000)]
        [Display(Name = "Completion Criteria")]
        public string? CompletionCriteria { get; set; }

        [Display(Name = "Requires Evidence")]
        public bool RequiresEvidence { get; set; }

        // Assignment Configuration
        [Display(Name = "Assignment Type")]
        public TaskAssignmentType AssignmentType { get; set; } = TaskAssignmentType.Investigator;

        // Hierarchy Configuration
        [Display(Name = "Inheritance Behavior")]
        public TaskInheritanceBehavior InheritanceBehavior { get; set; } = TaskInheritanceBehavior.Inherit;

        [StringLength(500)]
        [Display(Name = "Restrict to Sub-Disease IDs (JSON)")]
        public string? RestrictToSubDiseaseIds { get; set; }

        // Status
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Interview Task")]
        public bool IsInterviewTask { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // Navigation Properties
        public ICollection<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; } = new List<DiseaseTaskTemplate>();
        public ICollection<CaseTask> CaseTasks { get; set; } = new List<CaseTask>();
    }
}
