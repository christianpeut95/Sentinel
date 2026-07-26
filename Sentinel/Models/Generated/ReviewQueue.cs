using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReviewQueue
{
    public int Id { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public Guid? CaseId { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? DiseaseId { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? TriggerField { get; set; }

    public string? ChangeSnapshot { get; set; }

    public int Priority { get; set; }

    public string ReviewStatus { get; set; } = null!;

    public string? ReviewAction { get; set; }

    public string? GroupKey { get; set; }

    public int GroupCount { get; set; }

    public string? PotentialMatchesJson { get; set; }

    public string? ProposedEntityDataJson { get; set; }

    public string? CollectionSourceDataJson { get; set; }

    public Guid? SelectedExistingEntityId { get; set; }

    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedDate { get; set; }

    public string? ReviewNotes { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? TaskId { get; set; }

    public virtual Case? Case { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual Disease? Disease { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual AspNetUser? ReviewedByUser { get; set; }

    public virtual ICollection<SurveySubmissionLog> SurveySubmissionLogs { get; set; } = new List<SurveySubmissionLog>();

    public virtual CaseTask? Task { get; set; }
}
