using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models;

/// <summary>
/// Stores user-defined line list configurations for outbreak investigations
/// </summary>
public class OutbreakLineListConfiguration
{
    public int Id { get; set; }
    
    [Required]
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Configuration Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// JSON array of field paths (e.g., ["Patient.GivenName", "Patient.FamilyName", "Case.DateOfOnset"])
    /// </summary>
    [Required]
    public string SelectedFields { get; set; } = "[]";
    
    /// <summary>
    /// JSON array of sort configurations (e.g., [{"field": "Case.DateOfOnset", "direction": "asc"}])
    /// </summary>
    public string SortConfiguration { get; set; } = "[]";
    
    /// <summary>
    /// JSON object with filter settings (optional)
    /// </summary>
    public string? FilterConfiguration { get; set; }
    
    /// <summary>
    /// User ID if personal configuration, null if shared
    /// </summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// If true, this is shared across all team members
    /// </summary>
    public bool IsShared { get; set; } = false;
    
    /// <summary>
    /// If true, this is the user's default view
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

/// <summary>
/// Available field definitions for line list
/// </summary>
public class LineListField
{
    public string FieldPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty; // string, date, number, boolean
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; } = true;
}

/// <summary>
/// Line list data row (flattened data from multiple tables)
/// </summary>
public class LineListDataRow
{
    public Guid CaseId { get; set; }
    public int OutbreakCaseId { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
}
