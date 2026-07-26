using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class SurveySubmissionLog
{
    public int Id { get; set; }

    public Guid? TaskId { get; set; }

    public Guid? CaseId { get; set; }

    public string? PatientName { get; set; }

    public string? CaseReference { get; set; }

    public string? DiseaseName { get; set; }

    public string? SurveyName { get; set; }

    public string? TaskName { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string? SubmittedByUserId { get; set; }

    public string? SubmittedByName { get; set; }

    public int Outcome { get; set; }

    public int FieldsSavedAutomatically { get; set; }

    public int FieldsSentForReview { get; set; }

    public int FieldsRequiringApproval { get; set; }

    public int FieldsSkipped { get; set; }

    public int FieldsWithErrors { get; set; }

    public int TotalMappingsConfigured { get; set; }

    public string? IssuesSummary { get; set; }

    public string? MappingDetailJson { get; set; }

    public int? ReviewQueueItemId { get; set; }

    public virtual Case? Case { get; set; }

    public virtual ReviewQueue? ReviewQueueItem { get; set; }

    public virtual CaseTask? Task { get; set; }
}
