using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public int ApplicableTo { get; set; }

    public virtual ICollection<CaseClassificationHistory> CaseClassificationHistoryFromConfirmationStatuses { get; set; } = new List<CaseClassificationHistory>();

    public virtual ICollection<CaseClassificationHistory> CaseClassificationHistoryToConfirmationStatuses { get; set; } = new List<CaseClassificationHistory>();

    public virtual ICollection<CaseDefinition> CaseDefinitions { get; set; } = new List<CaseDefinition>();

    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    public virtual ICollection<DiseaseHl7matchingConfig> DiseaseHl7matchingConfigs { get; set; } = new List<DiseaseHl7matchingConfig>();

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();
}
