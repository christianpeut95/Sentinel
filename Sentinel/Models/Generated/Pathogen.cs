using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Pathogen
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? ShortName { get; set; }

    public string? Loinccode { get; set; }

    public string? LoincdisplayName { get; set; }

    public string? Description { get; set; }

    public Guid? DiseaseId { get; set; }

    public int Category { get; set; }

    public int ResultType { get; set; }

    public string? DefaultUnit { get; set; }

    public decimal? DefaultReferenceRangeLow { get; set; }

    public decimal? DefaultReferenceRangeHigh { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual Disease? Disease { get; set; }

    public virtual ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();
}
