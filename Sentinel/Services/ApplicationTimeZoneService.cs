using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing application-wide timezone configuration.
    /// Uses the validated time-zone and locale identifiers from Organization
    /// settings in appsettings.json.
    /// </summary>
    public class ApplicationTimeZoneService : IApplicationTimeZoneService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicationTimeZoneService> _logger;
        private readonly IDisposable _configurationReloadRegistration;
        private TimeZoneInfo _appTimeZone = TimeZoneInfo.Utc;
        private CultureInfo _appCulture = CultureInfo.GetCultureInfo(OrganizationRegionalSettings.DefaultLocale);
        private string _timeZoneId = OrganizationRegionalSettings.DefaultTimeZoneId;

        public ApplicationTimeZoneService(IConfiguration configuration, ILogger<ApplicationTimeZoneService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            ReloadTimeZone();
            _configurationReloadRegistration = ChangeToken.OnChange(
                _configuration.GetReloadToken,
                ReloadTimeZone);
        }

        public TimeZoneInfo AppTimeZone => Volatile.Read(ref _appTimeZone);

        public string TimeZoneId => Volatile.Read(ref _timeZoneId);

        public CultureInfo AppCulture => Volatile.Read(ref _appCulture);

        public DateTime Now => UtcToAppTime(DateTime.UtcNow);

        public void Dispose() => _configurationReloadRegistration.Dispose();

        private void ReloadTimeZone()
        {
            var configuredTimeZoneId = _configuration["Organization:TimeZoneId"];
            if (OrganizationRegionalSettings.TryResolveTimeZone(configuredTimeZoneId, out var timeZone))
            {
                Volatile.Write(ref _appTimeZone, timeZone);
                Volatile.Write(ref _timeZoneId, OrganizationRegionalSettings.GetCanonicalTimeZoneId(timeZone));
                _logger.LogInformation("Application timezone configured: {TimeZoneId} ({DisplayName})",
                    TimeZoneId,
                    timeZone.DisplayName);
            }
            else
            {
                _logger.LogWarning("Timezone '{TimeZoneId}' is invalid. Falling back to UTC.", configuredTimeZoneId);
                Volatile.Write(ref _appTimeZone, TimeZoneInfo.Utc);
                Volatile.Write(ref _timeZoneId, OrganizationRegionalSettings.GetCanonicalTimeZoneId(TimeZoneInfo.Utc));
            }

            var configuredLocale = _configuration["Organization:Locale"];
            if (OrganizationRegionalSettings.TryGetCulture(configuredLocale, out var culture))
            {
                Volatile.Write(ref _appCulture, culture);
            }
            else
            {
                _logger.LogWarning("Locale '{Locale}' is invalid. Falling back to {DefaultLocale}.",
                    configuredLocale,
                    OrganizationRegionalSettings.DefaultLocale);
                Volatile.Write(ref _appCulture, CultureInfo.GetCultureInfo(OrganizationRegionalSettings.DefaultLocale));
            }
        }

        public DateTime UtcToAppTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Local)
            {
                utcDateTime = utcDateTime.ToUniversalTime();
            }
            else if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                // SQL datetime does not retain DateTime.Kind. Sentinel's stored
                // timestamp values are UTC, so unspecified persisted values are
                // explicitly treated as UTC here.
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, AppTimeZone);
        }

        public DateTime AppTimeToUtc(DateTime appDateTime)
        {
            if (!TryAppTimeToUtc(appDateTime, out var utcDateTime))
            {
                throw new ArgumentException(
                    "The supplied local time is invalid or ambiguous in the configured application timezone.",
                    nameof(appDateTime));
            }

            return utcDateTime;
        }

        public bool TryAppTimeToUtc(DateTime appDateTime, out DateTime utcDateTime)
        {
            // API clients may legitimately send an explicitly offset/UTC ISO
            // value. A browser datetime-local control, by contrast, is parsed as
            // Unspecified and handled as organisation-local below.
            if (appDateTime.Kind == DateTimeKind.Utc)
            {
                utcDateTime = appDateTime;
                return true;
            }

            if (appDateTime.Kind == DateTimeKind.Local)
            {
                utcDateTime = appDateTime.ToUniversalTime();
                return true;
            }

            var localTime = DateTime.SpecifyKind(appDateTime, DateTimeKind.Unspecified);
            // A datetime-local control does not transmit an offset. Reject both
            // the DST gap and the repeated hour rather than persisting an instant
            // the user did not unambiguously choose.
            if (AppTimeZone.IsInvalidTime(localTime) || AppTimeZone.IsAmbiguousTime(localTime))
            {
                utcDateTime = default;
                return false;
            }

            utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localTime, AppTimeZone);
            return true;
        }

        public string Format(DateTime? utcDateTime, string format = "g") =>
            utcDateTime.HasValue
                ? UtcToAppTime(utcDateTime.Value).ToString(format, AppCulture)
                : string.Empty;

        public string ToDateTimeLocalValue(DateTime utcDateTime) =>
            UtcToAppTime(utcDateTime).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        public string GetCurrentDateFolder()
        {
            var appTime = UtcToAppTime(DateTime.UtcNow);
            return appTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
