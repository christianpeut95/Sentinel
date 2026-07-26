using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OrganizationType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();
}
