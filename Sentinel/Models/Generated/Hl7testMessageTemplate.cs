using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7testMessageTemplate
{
    public Guid Id { get; set; }

    public string TemplateName { get; set; } = null!;

    public string? Description { get; set; }

    public int LabTemplateType { get; set; }

    public string ConfigurationJson { get; set; } = null!;

    public string? TestComment { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public string? UpdatedByUserId { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual ICollection<Hl7testMessageHistory> Hl7testMessageHistories { get; set; } = new List<Hl7testMessageHistory>();

    public virtual AspNetUser? UpdatedByUser { get; set; }
}
