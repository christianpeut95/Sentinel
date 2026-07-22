namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing application-wide timezone configuration
    /// </summary>
    public interface IApplicationTimeZoneService
    {
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

        /// <summary>
        /// Gets the current date in the application timezone (formatted as yyyy-MM-dd)
        /// </summary>
        string GetCurrentDateFolder();
    }
}
