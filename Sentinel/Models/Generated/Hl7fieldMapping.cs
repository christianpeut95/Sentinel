using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7fieldMapping
{
    public Guid Id { get; set; }

    public Guid ConfigurationId { get; set; }

    public string SegmentType { get; set; } = null!;

    public string FieldPath { get; set; } = null!;

    public string? FieldName { get; set; }

    public string TargetEntity { get; set; } = null!;

    public string TargetProperty { get; set; } = null!;

    public int MappingType { get; set; }

    public string? TransformationRule { get; set; }

    public string? LookupTable { get; set; }

    public string? CodeMappingJson { get; set; }

    public string? DefaultValue { get; set; }

    public bool IsRequired { get; set; }

    public string? ValidationRegex { get; set; }

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public string? Notes { get; set; }

    public string? ExampleHl7value { get; set; }

    public string? ExampleMappedValue { get; set; }

    public int TimesUsed { get; set; }

    public int TimesFailed { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public Guid? CreatedFromIssueId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public Guid? DiseaseId { get; set; }

    public string? SampleMessage { get; set; }

    public virtual Hl7configuration Configuration { get; set; } = null!;

    public virtual Hl7parsingIssue? CreatedFromIssue { get; set; }

    public virtual Disease? Disease { get; set; }

    public virtual ICollection<Hl7parsingIssue> Hl7parsingIssues { get; set; } = new List<Hl7parsingIssue>();
}
