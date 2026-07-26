using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReportDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string EntityType { get; set; } = null!;

    public string? Category { get; set; }

    public string? PivotConfiguration { get; set; }

    public string? CollectionQueriesJson { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsPublic { get; set; }

    public bool IsTemplate { get; set; }

    public DateTime? LastRunDate { get; set; }

    public int RunCount { get; set; }

    public int? FolderId { get; set; }

    public virtual ICollection<CalculatedField> CalculatedFields { get; set; } = new List<CalculatedField>();

    public virtual ReportFolder? Folder { get; set; }

    public virtual ICollection<ReportField> ReportFields { get; set; } = new List<ReportField>();

    public virtual ICollection<ReportFilter> ReportFilters { get; set; } = new List<ReportFilter>();
}
