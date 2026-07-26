using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class SpecimenType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ExportCode { get; set; }

    public bool IsInvasive { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsSterileSite { get; set; }

    public string? BodySite { get; set; }

    public string? CollectionMethod { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Hl7Code { get; set; }

    public string? LoincSystemCode { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? SnomedCode { get; set; }

    public string? SnomedDisplay { get; set; }

    public virtual ICollection<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
}
