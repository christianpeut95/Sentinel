using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

/// <summary>
/// Represents a field selected for inclusion in a report
/// </summary>
public class ReportField
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    /// <summary>
    /// Path to the field (e.g., "Patient.Age", "Jurisdiction1.Name", "CustomField_RiskLevel")
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field in the report
    /// </summary>
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Data type: String, Number, Date, Boolean
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Where this field appears in the pivot: Row, Column, Value, Filter
    /// </summary>
    [StringLength(20)]
    public string? PivotArea { get; set; }

    /// <summary>
    /// Aggregation type: Sum, Count, Average, Min, Max, DistinctCount
    /// </summary>
    [StringLength(50)]
    public string? AggregationType { get; set; }

    /// <summary>
    /// Display order in the report
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Is this a custom field (EAV pattern)?
    /// </summary>
    public bool IsCustomField { get; set; }

    /// <summary>
    /// If custom field, stores the CustomFieldDefinition ID
    /// </summary>
    public int? CustomFieldDefinitionId { get; set; }

    // Navigation property
    public ReportDefinition ReportDefinition { get; set; } = null!;
}
