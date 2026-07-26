using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7messageSegment
{
    public Guid Id { get; set; }

    public Guid Hl7messageId { get; set; }

    public string SegmentType { get; set; } = null!;

    public int SequenceNumber { get; set; }

    public int? SetId { get; set; }

    public string RawSegment { get; set; } = null!;

    public bool IsParsed { get; set; }

    public string? ParsedData { get; set; }

    public int? FieldCount { get; set; }

    public string? ErrorDetails { get; set; }

    public bool HasIssues { get; set; }

    public DateTime? ParsedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Hl7message Hl7message { get; set; } = null!;

    public virtual ICollection<Hl7parsingIssue> Hl7parsingIssues { get; set; } = new List<Hl7parsingIssue>();
}
