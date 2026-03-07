using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

public class ReportFolder
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? ParentFolderId { get; set; }
    public ReportFolder? ParentFolder { get; set; }
    public ICollection<ReportFolder> SubFolders { get; set; } = new List<ReportFolder>();

    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public FolderAccessType AccessType { get; set; } = FolderAccessType.Private;

    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<ReportDefinition> Reports { get; set; } = new List<ReportDefinition>();
    public ICollection<ReportFolderShare> FolderShares { get; set; } = new List<ReportFolderShare>();
}

public enum FolderAccessType
{
    Private = 0,
    SharedWithGroups = 1,
    SharedWithUsers = 2,
    Public = 3
}
