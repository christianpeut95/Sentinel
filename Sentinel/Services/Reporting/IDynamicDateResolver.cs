namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for resolving dynamic date specifications to actual DateTime values
/// </summary>
public interface IDynamicDateResolver
{
    /// <summary>
    /// Resolves a dynamic date specification to an actual DateTime value
    /// </summary>
    /// <param name="dynamicDateType">Type of dynamic date (Today, Yesterday, Tomorrow, etc.)</param>
    /// <param name="offset">Optional offset value (e.g., -7 for "Past 7 Days")</param>
    /// <param name="offsetUnit">Unit for offset (Days, Weeks, Months, Years)</param>
    /// <param name="referenceDate">Reference date to calculate from (defaults to DateTime.Now)</param>
    /// <returns>Resolved DateTime value</returns>
    DateTime ResolveDate(string dynamicDateType, int? offset = null, string? offsetUnit = null, DateTime? referenceDate = null);

    /// <summary>
    /// Gets a list of supported dynamic date types
    /// </summary>
    IEnumerable<string> GetSupportedDateTypes();

    /// <summary>
    /// Gets a list of supported offset units
    /// </summary>
    IEnumerable<string> GetSupportedOffsetUnits();
}
