using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TestMethod
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ExportCode { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public string? LoincMethodCode { get; set; }

    public string? SnomedCode { get; set; }

    public string? SnomedDisplay { get; set; }

    public virtual ICollection<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();
}
