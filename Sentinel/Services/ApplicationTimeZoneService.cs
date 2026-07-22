using Microsoft.Extensions.Configuration;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing application-wide timezone configuration.
    /// Uses the TimeZoneId from Organization settings in appsettings.json.
    /// </summary>
    public class ApplicationTimeZoneService : IApplicationTimeZoneService
    {
        private readonly TimeZoneInfo _appTimeZone;
        private readonly ILogger<ApplicationTimeZoneService> _logger;

        public ApplicationTimeZoneService(IConfiguration configuration, ILogger<ApplicationTimeZoneService> logger)
        {
            _logger = logger;
            var timeZoneId = configuration["Organization:TimeZoneId"] ?? "UTC";

            try
            {
                _appTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                _logger.LogInformation("Application timezone configured: {TimeZoneId} ({DisplayName})", 
                    _appTimeZone.Id, _appTimeZone.DisplayName);
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "Timezone '{TimeZoneId}' not found. Falling back to UTC.", timeZoneId);
                _appTimeZone = TimeZoneInfo.Utc;
            }
        }

        public TimeZoneInfo AppTimeZone => _appTimeZone;

        public DateTime UtcToAppTime(DateTime utcDateTime)
        {
            // Ensure the datetime is treated as UTC
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _appTimeZone);
        }

        public DateTime AppTimeToUtc(DateTime appDateTime)
        {
            // Convert from app timezone to UTC
            return TimeZoneInfo.ConvertTimeToUtc(appDateTime, _appTimeZone);
        }

        public string GetCurrentDateFolder()
        {
            var appTime = UtcToAppTime(DateTime.UtcNow);
            return appTime.ToString("yyyy-MM-dd");
        }
    }
}
