using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseClassificationHistory
{
    public int Id { get; set; }

    public Guid CaseId { get; set; }

    public int? FromConfirmationStatusId { get; set; }

    public int ToConfirmationStatusId { get; set; }

    public int? CaseDefinitionId { get; set; }

    public DateTime? ClassifiedDate { get; set; }

    public string? ClassifiedByUserId { get; set; }

    public bool IsAutoClassified { get; set; }

    public string? Rationale { get; set; }

    public string? CriteriaResultJson { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime EvaluationDate { get; set; }

    public bool IsMatch { get; set; }

    public int RecommendedAction { get; set; }

    public bool WasApplied { get; set; }

    public virtual Case Case { get; set; } = null!;

    public virtual CaseDefinition? CaseDefinition { get; set; }

    public virtual CaseStatus? FromConfirmationStatus { get; set; }

    public virtual CaseStatus ToConfirmationStatus { get; set; } = null!;
}
