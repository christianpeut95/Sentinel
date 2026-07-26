using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class JurisdictionType
{
    public int Id { get; set; }

    public int FieldNumber { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<Jurisdiction> Jurisdictions { get; set; } = new List<Jurisdiction>();
}
