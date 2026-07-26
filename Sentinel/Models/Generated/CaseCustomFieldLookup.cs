using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseCustomFieldLookup
{
    public int Id { get; set; }

    public Guid CaseId { get; set; }

    public int FieldDefinitionId { get; set; }

    public int? LookupValueId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Case Case { get; set; } = null!;

    public virtual CustomFieldDefinition FieldDefinition { get; set; } = null!;

    public virtual LookupValue? LookupValue { get; set; }
}
