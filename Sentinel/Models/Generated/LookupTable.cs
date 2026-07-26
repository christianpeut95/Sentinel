using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LookupTable
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CustomFieldDefinition> CustomFieldDefinitions { get; set; } = new List<CustomFieldDefinition>();

    public virtual ICollection<LookupValue> LookupValues { get; set; } = new List<LookupValue>();
}
