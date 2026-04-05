using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentinel.Models;

/// <summary>
/// Records each survey submission with a plain-language summary of what happened.
/// Designed for epidemiologists and public health staff — not IT personnel.
/// </summary>
public class SurveySubmissionLog
{
    [Key]
    public int Id { get; set; }

    // ── Links to live records (nullable so the log survives record deletion) ──

    public Guid? TaskId { get; set; }
    [ForeignKey(nameof(TaskId))]
    public CaseTask? Task { get; set; }

    public Guid? CaseId { get; set; }
    [ForeignKey(nameof(CaseId))]
    public Case? Case { get; set; }

    public int? ReviewQueueItemId { get; set; }
    [ForeignKey(nameof(ReviewQueueItemId))]
    public ReviewQueue? ReviewQueueItem { get; set; }

    // ── Snapshot data preserved at submission time ──

    [MaxLength(200)]
    public string? PatientName { get; set; }

    [MaxLength(50)]
    public string? CaseReference { get; set; }

    [MaxLength(200)]
    public string? DiseaseName { get; set; }

    [MaxLength(200)]
    public string? SurveyName { get; set; }

    [MaxLength(200)]
    public string? TaskName { get; set; }

    // ── Who / when ──

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? SubmittedByUserId { get; set; }

    [MaxLength(200)]
    public string? SubmittedByName { get; set; }

    // ── Outcome ──

    public SurveySubmissionOutcome Outcome { get; set; }

    // ── Field-level counts ──

    /// <summary>Fields that were saved automatically to the case record.</summary>
    public int FieldsSavedAutomatically { get; set; }

    /// <summary>Fields that were sent to the review queue for staff approval.</summary>
    public int FieldsSentForReview { get; set; }

    /// <summary>Fields that require explicit approval before being applied.</summary>
    public int FieldsRequiringApproval { get; set; }

    /// <summary>Fields that were intentionally skipped (e.g. business rule said "don't overwrite").</summary>
    public int FieldsSkipped { get; set; }

    /// <summary>Fields where an error prevented processing.</summary>
    public int FieldsWithErrors { get; set; }

    /// <summary>Total number of mappings that were configured for this survey.</summary>
    public int TotalMappingsConfigured { get; set; }

    // ── Plain-language issue description ──

    /// <summary>
    /// A short, plain-English description of any issues — suitable for display to
    /// epidemiologists without technical background.
    /// </summary>
    [MaxLength(2000)]
    public string? IssuesSummary { get; set; }

    /// <summary>
    /// Full JSON detail of each mapping step — available for deeper inspection if needed.
    /// </summary>
    public string? MappingDetailJson { get; set; }
}

/// <summary>
/// Overall result of a survey submission, shown in plain language in the activity log.
/// </summary>
public enum SurveySubmissionOutcome
{
    /// <summary>All fields were saved automatically with no issues.</summary>
    Completed = 0,

    /// <summary>Fields were submitted but some are waiting for staff review before being applied.</summary>
    SentForReview = 1,

    /// <summary>Some fields were saved but others had problems — check the details.</summary>
    PartiallyCompleted = 2,

    /// <summary>The submission encountered a serious problem — the survey data was saved but no fields were updated.</summary>
    ProblemOccurred = 3,

    /// <summary>No field mappings are set up yet for this survey — the data was saved but nothing was automatically applied.</summary>
    NotConfigured = 4,
}
