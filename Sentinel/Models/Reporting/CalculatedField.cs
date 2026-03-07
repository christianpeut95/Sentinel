using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

/// <summary>
/// Represents a calculated field created by the user using expressions
/// </summary>
public class CalculatedField
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    /// <summary>
    /// Name of the calculated field
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// NCalc expression (e.g., "[Age] >= 18 ? 'Adult' : 'Child'")
    /// </summary>
    [Required]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Return data type: String, Number, Date, Boolean
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Description of what this field calculates
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation property
    public ReportDefinition ReportDefinition { get; set; } = null!;
}
