namespace Sentinel.DTOs;

/// <summary>
/// DTO for collection queries (related data queries)
/// Supports both filtering rows and displaying results as columns
/// </summary>
public class CollectionQueryDto
{
    /// <summary>
    /// Collection name (e.g., "LabResults", "ExposureEvents", "Tasks")
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Operation: HasAny, HasAll, Count, Sum, Average, Min, Max
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Field to aggregate for Sum, Average, Min, Max operations
    /// Example: "SpecimenCollectionDate" for min/max date, "QuantitativeResult" for sum/average
    /// </summary>
    public string? AggregateField { get; set; }

    /// <summary>
    /// Display as column instead of filtering rows
    /// </summary>
    public bool DisplayAsColumn { get; set; }

    /// <summary>
    /// Column name (only used when DisplayAsColumn = true)
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Comparator for aggregate operations (only used when DisplayAsColumn = false)
    /// Values: Equals, NotEquals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
    /// </summary>
    public string? Comparator { get; set; }

    /// <summary>
    /// Value to compare against (only used when DisplayAsColumn = false)
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Sub-filters to apply to the collection items
    /// </summary>
    public List<CollectionSubFilter> SubFilters { get; set; } = new();
}
