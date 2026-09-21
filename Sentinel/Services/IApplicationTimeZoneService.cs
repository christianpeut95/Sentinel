namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing application-wide timezone configuration
    /// </summary>
    public interface IApplicationTimeZoneService
    {
        /// <summary>Portable IANA identifier for the organisation time zone where available.</summary>
        string TimeZoneId { get; }

        /// <summary>Validated organisation culture for dates, times and numbers.</summary>
        System.Globalization.CultureInfo AppCulture { get; }

        /// <summary>
        /// Gets the configured application timezone
        /// </summary>
        TimeZoneInfo AppTimeZone { get; }

        /// <summary>
        /// Converts a UTC DateTime to the application's timezone
        /// </summary>
        DateTime UtcToAppTime(DateTime utcDateTime);

        /// <summary>
        /// Converts an application timezone DateTime to UTC
        /// </summary>
        DateTime AppTimeToUtc(DateTime appDateTime);

        /// <summary>Converts an organisation-local value to UTC, rejecting invalid or ambiguous DST times.</summary>
        bool TryAppTimeToUtc(DateTime appDateTime, out DateTime utcDateTime);

        /// <summary>Formats a UTC timestamp in the organisation time zone and culture.</summary>
        string Format(DateTime? utcDateTime, string format = "g");

        /// <summary>Formats a UTC value for an HTML datetime-local input.</summary>
        string ToDateTimeLocalValue(DateTime utcDateTime);

        /// <summary>Gets the current date and time in the organisation's configured zone.</summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the current date in the application timezone (formatted as yyyy-MM-dd)
        /// </summary>
        string GetCurrentDateFolder();
    }
}
