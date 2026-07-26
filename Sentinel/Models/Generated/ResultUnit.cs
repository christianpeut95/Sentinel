using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ResultUnit
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Abbreviation { get; set; }

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
}
