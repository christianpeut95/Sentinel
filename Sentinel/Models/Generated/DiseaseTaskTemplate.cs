using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseTaskTemplate
{
    public Guid Id { get; set; }

    public Guid DiseaseId { get; set; }

    public Guid TaskTemplateId { get; set; }

    public int? ApplicableTo { get; set; }

    public bool IsInherited { get; set; }

    public Guid? InheritedFromDiseaseId { get; set; }

    public bool ApplyToChildren { get; set; }

    public bool AllowChildOverride { get; set; }

    public bool? OverrideAutoCreate { get; set; }

    public int? OverridePriority { get; set; }

    public int? OverrideDueDays { get; set; }

    public string? OverrideInstructions { get; set; }

    public bool AutoCreateOnCaseCreation { get; set; }

    public bool AutoCreateOnContactCreation { get; set; }

    public bool AutoCreateOnLabConfirmation { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public string? InputMappingJson { get; set; }

    public string? OutputMappingJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual TaskTemplate TaskTemplate { get; set; } = null!;
}
