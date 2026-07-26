using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7parsingIssue
{
    public Guid Id { get; set; }

    public Guid Hl7messageId { get; set; }

    public Guid? MessageSegmentId { get; set; }

    public string SegmentType { get; set; } = null!;

    public string? FieldPath { get; set; }

    public string? FieldName { get; set; }

    public int IssueType { get; set; }

    public int Severity { get; set; }

    public string Description { get; set; } = null!;

    public string? RawValue { get; set; }

    public string? ExpectedFormat { get; set; }

    public string? SuggestedMapping { get; set; }

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? ResolvedByUserId { get; set; }

    public string? ResolutionNotes { get; set; }

    public Guid? FieldMappingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IgnoreFutureOccurrences { get; set; }

    public virtual Hl7fieldMapping? FieldMapping { get; set; }

    public virtual ICollection<Hl7fieldMapping> Hl7fieldMappings { get; set; } = new List<Hl7fieldMapping>();

    public virtual Hl7message Hl7message { get; set; } = null!;

    public virtual Hl7messageSegment? MessageSegment { get; set; }

    public virtual AspNetUser? ResolvedByUser { get; set; }
}
