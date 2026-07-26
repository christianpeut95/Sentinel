using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReportFolder
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentFolderId { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public int AccessType { get; set; }

    public string? Color { get; set; }

    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<ReportFolder> InverseParentFolder { get; set; } = new List<ReportFolder>();

    public virtual ReportFolder? ParentFolder { get; set; }

    public virtual ICollection<ReportDefinition> ReportDefinitions { get; set; } = new List<ReportDefinition>();

    public virtual ICollection<ReportFolderShare> ReportFolderShares { get; set; } = new List<ReportFolderShare>();
}
