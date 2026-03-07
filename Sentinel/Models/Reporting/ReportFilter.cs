using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Reporting;

/// <summary>
/// Represents a filter applied to a report
/// </summary>
public class ReportFilter
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    /// <summary>
    /// Field path to filter on
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Filter operator: Equals, NotEquals, Contains, GreaterThan, LessThan, Between, In
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Operator { get; set; } = "Equals";

    /// <summary>
    /// Filter value (JSON for complex values like date ranges or lists)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Is this a custom field filter?
    /// </summary>
    public bool IsCustomField { get; set; }

    /// <summary>
    /// Custom field definition ID (if IsCustomField = true)
    /// </summary>
    public int? CustomFieldDefinitionId { get; set; }

    /// <summary>
    /// Data type for proper comparison
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Logic operator to combine this filter with the next one: AND or OR
    /// </summary>
    [StringLength(10)]
    public string LogicOperator { get; set; } = "AND";

    /// <summary>
    /// Filter group ID for nested grouping (null = no group)
    /// Filters with same GroupId are grouped together with parentheses
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Logic operator to combine this group with the next one: AND or OR
    /// Only used if this is the last filter in a group
    /// </summary>
    [StringLength(10)]
    public string GroupLogicOperator { get; set; } = "AND";

    /// <summary>
    /// For collection filters: Is this a collection query (e.g., LabResults.Any())?
    /// </summary>
    public bool IsCollectionQuery { get; set; }

    /// <summary>
    /// For collection filters: Sub-filters to apply within the collection
    /// Stored as JSON: [{"field": "TestType", "operator": "Equals", "value": "PCR"}]
    /// </summary>
    public string? CollectionSubFilters { get; set; }

    /// <summary>
    /// Collection operator: HasAny, HasAll, Count, None
    /// </summary>
    [StringLength(20)]
    public string? CollectionOperator { get; set; }

    // Navigation property
    public ReportDefinition ReportDefinition { get; set; } = null!;
}
