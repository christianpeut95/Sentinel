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
    /// Comparison operator (Equals, Contains, GreaterThan, After, Before, InLast, etc.)
    /// </summary>
    public string Operator { get; set; } = "Equals";

    /// <summary>
    /// Value to compare against
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type for proper comparison (String, DateTime, Int32, Boolean, etc.)
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Is this a dynamic date filter? (e.g., "In Last 30 Days")
    /// </summary>
    public bool IsDynamicDate { get; set; }

    /// <summary>
    /// Dynamic date type (e.g., "Past30Days", "Next7Days", "PastWeeks")
    /// </summary>
    public string? DynamicDateType { get; set; }

    /// <summary>
    /// Offset value for dynamic dates (e.g., 30 for "Past 30 Days")
    /// </summary>
    public int? DynamicDateOffset { get; set; }

    /// <summary>
    /// Offset unit for dynamic dates (Days, Weeks, Months)
    /// </summary>
    public string? DynamicDateOffsetUnit { get; set; }
}
