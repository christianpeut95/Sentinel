using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7testMessageHistory
{
    public Guid Id { get; set; }

    public Guid? TemplateId { get; set; }

    public string RawHl7message { get; set; } = null!;

    public string? FilePath { get; set; }

    public string? TestComment { get; set; }

    public string? AccessionNumber { get; set; }

    public string? PatientMrn { get; set; }

    public string? ConfigurationSnapshot { get; set; }

    public Guid? Hl7messageId { get; set; }

    public string? ProcessingResultJson { get; set; }

    public int? ProcessingStatus { get; set; }

    public DateTime GeneratedAt { get; set; }

    public string? GeneratedBy { get; set; }

    public string? GeneratedByUserId { get; set; }

    public bool WasAutoProcessed { get; set; }

    public virtual AspNetUser? GeneratedByUser { get; set; }

    public virtual Hl7message? Hl7message { get; set; }

    public virtual Hl7testMessageTemplate? Template { get; set; }
}
