using System.ComponentModel.DataAnnotations;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Models
{
    public class CaseTask : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Case")]
        public Guid CaseId { get; set; }
        public Case? Case { get; set; }

        [Display(Name = "Task Template")]
        public Guid? TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        // Task Details (copied from template or entered manually)
        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Task Type")]
        public Guid TaskTypeId { get; set; }
        public TaskType? TaskType { get; set; }

        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        // Assignment
        [StringLength(450)]
        [Display(Name = "Assigned To User")]
        public string? AssignedToUserId { get; set; }
        public ApplicationUser? AssignedToUser { get; set; }

        [Display(Name = "Assignment Type")]
        public TaskAssignmentType AssignmentType { get; set; }

        // Timing
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Completed At")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Cancelled At")]
        public DateTime? CancelledAt { get; set; }

        // Status Tracking
        [Display(Name = "Status")]
        public CaseTaskStatus Status { get; set; } = CaseTaskStatus.Pending;

        [StringLength(2000)]
        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        public string? CompletionNotes { get; set; }

        [StringLength(450)]
        [Display(Name = "Completed By User")]
        public string? CompletedByUserId { get; set; }
        public ApplicationUser? CompletedByUser { get; set; }

        [StringLength(1000)]
        [Display(Name = "Cancellation Reason")]
        public string? CancellationReason { get; set; }

        // Evidence/Attachments
        [StringLength(2000)]
        [Display(Name = "Evidence File IDs (JSON)")]
        public string? EvidenceFileIds { get; set; }

        // Survey Integration
        [Display(Name = "Survey Response (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? SurveyResponseJson { get; set; }

        // Recurrence Tracking
        [Display(Name = "Parent Task")]
        public Guid? ParentTaskId { get; set; }
        public CaseTask? ParentTask { get; set; }

        [Display(Name = "Recurrence Sequence")]
        public int? RecurrenceSequence { get; set; }

        // Interview/Call Center Features
        [Display(Name = "Is Interview Task")]
        public bool IsInterviewTask { get; set; }

        [Display(Name = "Assignment Method")]
        public TaskAssignmentMethod AssignmentMethod { get; set; } = TaskAssignmentMethod.Manual;

        [Display(Name = "Language Required")]
        [StringLength(50)]
        public string? LanguageRequired { get; set; }

        [Display(Name = "Max Call Attempts")]
        public int MaxCallAttempts { get; set; } = 3;

        [Display(Name = "Current Attempt Count")]
        public int CurrentAttemptCount { get; set; }

        [Display(Name = "Escalation Level")]
        public int EscalationLevel { get; set; }

        [Display(Name = "Last Call Attempt")]
        public DateTime? LastCallAttempt { get; set; }

        [Display(Name = "Auto-Assigned At")]
        public DateTime? AutoAssignedAt { get; set; }

        // Navigation Properties
        public ICollection<CaseTask> RecurrenceInstances { get; set; } = new List<CaseTask>();
        public ICollection<TaskCallAttempt> CallAttempts { get; set; } = new List<TaskCallAttempt>();

        // Audit
        public DateTime? ModifiedAt { get; set; }
    }
}
