using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseSymptom
{
    public int Id { get; set; }

    public Guid CaseId { get; set; }

    public int SymptomId { get; set; }

    public DateTime? OnsetDate { get; set; }

    public string? Severity { get; set; }

    public string? Notes { get; set; }

    public string? OtherSymptomText { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public virtual Case Case { get; set; } = null!;

    public virtual Symptom Symptom { get; set; } = null!;
}
