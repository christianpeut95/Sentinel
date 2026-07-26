using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class PatientCustomFieldLookup
{
    public int Id { get; set; }

    public Guid PatientId { get; set; }

    public int FieldDefinitionId { get; set; }

    public int? LookupValueId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CustomFieldDefinition FieldDefinition { get; set; } = null!;

    public virtual LookupValue? LookupValue { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
