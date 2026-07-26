using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LocationType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsHighRisk { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
}
