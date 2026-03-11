using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

/// <summary>
/// Stores saved report configurations
/// </summary>
public class ReportDefinition
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Entity type this report queries: Case, Outbreak, Patient, etc.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Category for organizing reports (e.g., "Epidemiological", "Laboratory", "Outbreak")
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// JSON configuration for WebDataRocks pivot grid
    /// </summary>
    public string? PivotConfiguration { get; set; }

    /// <summary>
    /// JSON configuration for line chart (title, axis labels, grouping, etc.)
    /// </summary>
    public string? ChartConfiguration { get; set; }

    /// <summary>
    /// JSON configuration for collection queries (related data queries)
    /// Stores queries like "Has Positive PCR?", "Exposure Count", etc.
    /// </summary>
    public string? CollectionQueriesJson { get; set; }

    /// <summary>
    /// User who created this report
    /// </summary>
    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Is this report visible to all users?
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Is this a system-provided template?
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Last time this report was executed
    /// </summary>
    public DateTime? LastRunDate { get; set; }

    /// <summary>
    /// How many times this report has been run
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// Folder this report belongs to
    /// </summary>
    public int? FolderId { get; set; }
    public ReportFolder? Folder { get; set; }

    // Navigation properties
    public ICollection<ReportField> Fields { get; set; } = new List<ReportField>();
    public ICollection<ReportFilter> Filters { get; set; } = new List<ReportFilter>();
    public ICollection<CalculatedField> CalculatedFields { get; set; } = new List<CalculatedField>();
}
