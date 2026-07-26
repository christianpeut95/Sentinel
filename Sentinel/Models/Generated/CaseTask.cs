using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseTask
{
    public Guid Id { get; set; }

    public Guid CaseId { get; set; }

    public Guid? TaskTemplateId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid TaskTypeId { get; set; }

    public int Priority { get; set; }

    public string? AssignedToUserId { get; set; }

    public int AssignmentType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int Status { get; set; }

    public string? CompletionNotes { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? CancellationReason { get; set; }

    public string? EvidenceFileIds { get; set; }

    public string? SurveyResponseJson { get; set; }

    public Guid? ParentTaskId { get; set; }

    public int? RecurrenceSequence { get; set; }

    public bool IsInterviewTask { get; set; }

    public int AssignmentMethod { get; set; }

    public string? LanguageRequired { get; set; }

    public int MaxCallAttempts { get; set; }

    public int CurrentAttemptCount { get; set; }

    public int EscalationLevel { get; set; }

    public DateTime? LastCallAttempt { get; set; }

    public DateTime? AutoAssignedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public Guid? CaseId1 { get; set; }

    public virtual AspNetUser? AssignedToUser { get; set; }

    public virtual Case Case { get; set; } = null!;

    public virtual Case? CaseId1Navigation { get; set; }

    public virtual AspNetUser? CompletedByUser { get; set; }

    public virtual ICollection<CaseTask> InverseParentTask { get; set; } = new List<CaseTask>();

    public virtual CaseTask? ParentTask { get; set; }

    public virtual ICollection<ReviewQueue> ReviewQueues { get; set; } = new List<ReviewQueue>();

    public virtual ICollection<SurveySubmissionLog> SurveySubmissionLogs { get; set; } = new List<SurveySubmissionLog>();

    public virtual ICollection<TaskCallAttempt> TaskCallAttempts { get; set; } = new List<TaskCallAttempt>();

    public virtual TaskTemplate? TaskTemplate { get; set; }

    public virtual TaskType TaskType { get; set; } = null!;
}
