using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

public class ReportFolderShare
{
    public int Id { get; set; }

    public int ReportFolderId { get; set; }
    public ReportFolder ReportFolder { get; set; } = null!;

    public ShareTargetType TargetType { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public FolderPermissionLevel PermissionLevel { get; set; } = FolderPermissionLevel.View;

    [StringLength(450)]
    public string SharedByUserId { get; set; } = string.Empty;

    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
}

public enum ShareTargetType
{
    User = 0,
    Group = 1
}

public enum FolderPermissionLevel
{
    View = 0,
    Edit = 1,
    Manage = 2
}
