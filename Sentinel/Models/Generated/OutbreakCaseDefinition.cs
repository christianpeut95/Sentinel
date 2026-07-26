using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakCaseDefinition
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public string DefinitionName { get; set; } = null!;

    public string? DefinitionText { get; set; }

    public int Classification { get; set; }

    public string CriteriaJson { get; set; } = null!;

    public int Version { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Outbreak Outbreak { get; set; } = null!;
}
