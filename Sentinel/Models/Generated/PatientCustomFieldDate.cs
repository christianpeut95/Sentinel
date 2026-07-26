using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class PatientCustomFieldDate
{
    public int Id { get; set; }

    public Guid PatientId { get; set; }

    public int FieldDefinitionId { get; set; }

    public DateTime? Value { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CustomFieldDefinition FieldDefinition { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
