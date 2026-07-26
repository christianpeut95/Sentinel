using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7message
{
    public Guid Id { get; set; }

    public string MessageControlId { get; set; } = null!;

    public string MessageType { get; set; } = null!;

    public DateTime MessageDateTime { get; set; }

    public string? SendingFacility { get; set; }

    public string? SendingApplication { get; set; }

    public string? ReceivingFacility { get; set; }

    public string? ReceivingApplication { get; set; }

    public string? Hl7version { get; set; }

    public string RawMessage { get; set; } = null!;

    public string? FilePath { get; set; }

    public string? FileName { get; set; }

    public long? FileSizeBytes { get; set; }

    public int Status { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ProcessingNotes { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? ParsedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ProcessedByUserId { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? CaseId { get; set; }

    public Guid? LabResultId { get; set; }

    public Guid? LaboratoryOrganizationId { get; set; }

    public Guid? OrderingProviderOrganizationId { get; set; }

    public Guid? ConfigurationId { get; set; }

    public bool IsDuplicate { get; set; }

    public Guid? DuplicateOfMessageId { get; set; }

    public string? DuplicateDetectionMethod { get; set; }

    public bool RequiresManualReview { get; set; }

    public bool ManualReviewCompleted { get; set; }

    public string? ManualReviewByUserId { get; set; }

    public DateTime? ManualReviewDate { get; set; }

    public string? ManualReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public string? PartialMatchDetailsJson { get; set; }

    public int ReviewOutcome { get; set; }

    public bool NoSurveillanceItem { get; set; }

    public virtual Case? Case { get; set; }

    public virtual Hl7configuration? Configuration { get; set; }

    public virtual Hl7message? DuplicateOfMessage { get; set; }

    public virtual ICollection<Hl7messageSegment> Hl7messageSegments { get; set; } = new List<Hl7messageSegment>();

    public virtual ICollection<Hl7parsingIssue> Hl7parsingIssues { get; set; } = new List<Hl7parsingIssue>();

    public virtual ICollection<Hl7testMessageHistory> Hl7testMessageHistories { get; set; } = new List<Hl7testMessageHistory>();

    public virtual ICollection<Hl7message> InverseDuplicateOfMessage { get; set; } = new List<Hl7message>();

    public virtual LabResult? LabResult { get; set; }

    public virtual ICollection<LabResultMarkerHistory> LabResultMarkerHistories { get; set; } = new List<LabResultMarkerHistory>();

    public virtual Organization? LaboratoryOrganization { get; set; }

    public virtual AspNetUser? ManualReviewByUser { get; set; }

    public virtual Organization? OrderingProviderOrganization { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual AspNetUser? ProcessedByUser { get; set; }
}
