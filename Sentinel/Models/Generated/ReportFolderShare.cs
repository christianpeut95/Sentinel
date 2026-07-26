using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReportFolderShare
{
    public int Id { get; set; }

    public int ReportFolderId { get; set; }

    public int TargetType { get; set; }

    public string? UserId { get; set; }

    public int? GroupId { get; set; }

    public int PermissionLevel { get; set; }

    public string SharedByUserId { get; set; } = null!;

    public DateTime SharedAt { get; set; }

    public virtual Group? Group { get; set; }

    public virtual ReportFolder ReportFolder { get; set; } = null!;

    public virtual AspNetUser? User { get; set; }
}
