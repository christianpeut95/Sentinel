using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakSearchQuery
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public string QueryName { get; set; } = null!;

    public string QueryJson { get; set; } = null!;

    public bool IsAutoLink { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? LastRunDate { get; set; }

    public int? LastRunMatchCount { get; set; }

    public bool IsActive { get; set; }

    public virtual Outbreak Outbreak { get; set; } = null!;

    public virtual ICollection<OutbreakCase> OutbreakCases { get; set; } = new List<OutbreakCase>();
}
