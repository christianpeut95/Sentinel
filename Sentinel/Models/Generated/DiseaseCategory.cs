using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string ReportingId { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<Disease> Diseases { get; set; } = new List<Disease>();
}
