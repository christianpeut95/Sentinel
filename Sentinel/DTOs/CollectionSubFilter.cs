namespace Sentinel.DTOs;

/// <summary>
/// Represents a sub-filter condition within a collection query
/// Example: LabResults.Any(TestType == "PCR" AND Result == "Positive")
/// </summary>
public class CollectionSubFilter
{
    /// <summary>
    /// Field name within the collection (e.g., "TestType", "Result")
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Comparison operator (Equals, Contains, GreaterThan, etc.)
    /// </summary>
    public string Operator { get; set; } = "Equals";

    /// <summary>
    /// Value to compare against
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type for proper comparison
    /// </summary>
    public string DataType { get; set; } = "String";
}
