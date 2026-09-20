using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

public class ReportFolder
{
    private static readonly HashSet<string> SupportedIcons =
    [
        "bi-folder",
        "bi-folder-fill",
        "bi-archive",
        "bi-bar-chart",
        "bi-graph-up",
        "bi-star",
        "bi-heart"
    ];

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

    /// <summary>
    /// Returns the configured presentation icon only when it is one of Sentinel's
    /// supported Bootstrap icon classes. This also protects views from any legacy
    /// or manually edited database value.
    /// </summary>
    public string SafeIcon => SupportedIcons.Contains(Icon ?? string.Empty) ? Icon! : "bi-folder";

    /// <summary>
    /// Returns a CSS-safe six-digit hexadecimal colour, or <c>null</c> when no
    /// safe colour has been configured.
    /// </summary>
    public string? SafeColor => IsHexColor(Color) ? Color : null;

    private static bool IsHexColor(string? value)
    {
        if (value is not { Length: 7 } || value[0] != '#')
        {
            return false;
        }

        return value[1..].All(c =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F'));
    }

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
