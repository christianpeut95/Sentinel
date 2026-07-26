using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TestResult
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? SnomedCode { get; set; }

    public string? SnomedDisplay { get; set; }

    public string? Hl7Code { get; set; }

    public string? ExportCode { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int? TestTypeId { get; set; }

    public virtual ICollection<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual TestType? TestType { get; set; }
}
