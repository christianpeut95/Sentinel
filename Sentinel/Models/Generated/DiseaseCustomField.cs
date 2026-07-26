using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseCustomField
{
    public int Id { get; set; }

    public Guid DiseaseId { get; set; }

    public int CustomFieldDefinitionId { get; set; }

    public bool InheritToChildDiseases { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;

    public virtual Disease Disease { get; set; } = null!;
}
