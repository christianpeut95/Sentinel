namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for resolving dynamic date specifications to actual DateTime values
/// </summary>
public class DynamicDateResolver : IDynamicDateResolver
{
    private static readonly string[] SupportedDateTypes = new[]
    {
        "Today",
        "Yesterday",
        "Tomorrow",
        "StartOfWeek",
        "EndOfWeek",
        "StartOfMonth",
        "EndOfMonth",
        "StartOfYear",
        "EndOfYear",
        "PastDays",      // Requires offset
        "NextDays",      // Requires offset
        "PastWeeks",     // Requires offset
        "NextWeeks",     // Requires offset
        "PastMonths",    // Requires offset
        "NextMonths"     // Requires offset
    };

    private static readonly string[] SupportedOffsetUnits = new[]
    {
        "Days",
        "Weeks",
        "Months",
        "Years"
    };

    /// <inheritdoc />
    public DateTime ResolveDate(string dynamicDateType, int? offset = null, string? offsetUnit = null, DateTime? referenceDate = null)
    {
        var baseDate = referenceDate ?? DateTime.Now;
        var date = baseDate.Date; // Start with date component only (midnight)

        switch (dynamicDateType)
        {
            case "Today":
                return date;

            case "Yesterday":
                return date.AddDays(-1);

            case "Tomorrow":
                return date.AddDays(1);

            case "StartOfWeek":
                // Start of week (Monday)
                var dayOfWeek = (int)date.DayOfWeek;
                var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Handle Sunday
                return date.AddDays(-daysToSubtract);

            case "EndOfWeek":
                // End of week (Sunday)
                var daysToAdd = date.DayOfWeek == 0 ? 0 : 7 - (int)date.DayOfWeek;
                return date.AddDays(daysToAdd);

            case "StartOfMonth":
                return new DateTime(date.Year, date.Month, 1);

            case "EndOfMonth":
                return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

            case "StartOfYear":
                return new DateTime(date.Year, 1, 1);

            case "EndOfYear":
                return new DateTime(date.Year, 12, 31);

            case "PastDays":
                if (!offset.HasValue)
                    throw new ArgumentException("PastDays requires an offset value");
                return date.AddDays(-Math.Abs(offset.Value));

            case "NextDays":
                if (!offset.HasValue)
                    throw new ArgumentException("NextDays requires an offset value");
                return date.AddDays(Math.Abs(offset.Value));

            case "PastWeeks":
                if (!offset.HasValue)
                    throw new ArgumentException("PastWeeks requires an offset value");
                return date.AddDays(-7 * Math.Abs(offset.Value));

            case "NextWeeks":
                if (!offset.HasValue)
                    throw new ArgumentException("NextWeeks requires an offset value");
                return date.AddDays(7 * Math.Abs(offset.Value));

            case "PastMonths":
                if (!offset.HasValue)
                    throw new ArgumentException("PastMonths requires an offset value");
                return date.AddMonths(-Math.Abs(offset.Value));

            case "NextMonths":
                if (!offset.HasValue)
                    throw new ArgumentException("NextMonths requires an offset value");
                return date.AddMonths(Math.Abs(offset.Value));

            default:
                // If no specific type, apply offset to base date
                if (offset.HasValue && !string.IsNullOrEmpty(offsetUnit))
                {
                    return ApplyOffset(date, offset.Value, offsetUnit);
                }
                throw new ArgumentException($"Unsupported dynamic date type: {dynamicDateType}");
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedDateTypes()
    {
        return SupportedDateTypes;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedOffsetUnits()
    {
        return SupportedOffsetUnits;
    }

    private DateTime ApplyOffset(DateTime date, int offset, string unit)
    {
        return unit switch
        {
            "Days" => date.AddDays(offset),
            "Weeks" => date.AddDays(offset * 7),
            "Months" => date.AddMonths(offset),
            "Years" => date.AddYears(offset),
            _ => throw new ArgumentException($"Unsupported offset unit: {unit}")
        };
    }
}
