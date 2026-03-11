namespace Sentinel.DTOs;

/// <summary>
/// Metadata about a collection (related entity) for reporting
/// </summary>
public class CollectionMetadata
{
    /// <summary>
    /// Display label for the collection
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Allowed operations for this collection
    /// </summary>
    public List<string> AllowedOperations { get; set; } = new();

    /// <summary>
    /// Fields that can be aggregated (Min/Max/Sum/Average)
    /// Key = field name, Value = display label
    /// </summary>
    public Dictionary<string, AggregatableFieldInfo> AggregatableFields { get; set; } = new();

    /// <summary>
    /// Fields available for sub-filtering
    /// </summary>
    public List<CollectionFieldInfo> FilterableFields { get; set; } = new();
}

/// <summary>
/// Information about a field that can be aggregated
/// </summary>
public class AggregatableFieldInfo
{
    /// <summary>
    /// Display label for the field
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Data type (Date, Number)
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Operations allowed for this field (Min/Max for dates, Sum/Average/Min/Max for numbers)
    /// </summary>
    public List<string> AllowedOperations { get; set; } = new();
}

/// <summary>
/// Information about a field available for filtering
/// </summary>
public class CollectionFieldInfo
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Data type
    /// </summary>
    public string DataType { get; set; } = string.Empty;
}
