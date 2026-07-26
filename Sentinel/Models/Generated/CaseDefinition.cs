using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid DiseaseId { get; set; }

    public bool ApplyToChildDiseases { get; set; }

    public int ConfirmationStatusId { get; set; }

    public int Status { get; set; }

    public DateTime DateActiveFrom { get; set; }

    public DateTime? DateActiveTo { get; set; }

    public bool AllowAutoClassification { get; set; }

    public bool CreateReviewQueueOnChange { get; set; }

    public bool CreateReviewQueueOnSuggestion { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool EnableAutoEvaluation { get; set; }

    public virtual ICollection<CaseClassificationHistory> CaseClassificationHistories { get; set; } = new List<CaseClassificationHistory>();

    public virtual ICollection<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual CaseStatus ConfirmationStatus { get; set; } = null!;

    public virtual Disease Disease { get; set; } = null!;
}
