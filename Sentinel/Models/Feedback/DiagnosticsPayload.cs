using System.Text.Json.Serialization;

namespace Sentinel.Models.Feedback
{
    /// <summary>
    /// Privacy-safe diagnostic payload for feedback submissions.
    /// Must include redactionApplied: true to confirm no patient data is included.
    /// Maximum size: 256 KiB when serialized.
    /// </summary>
    public class DiagnosticsPayload
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("capturedAtUtc")]
        public DateTime? CapturedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// REQUIRED: Confirms that redaction has been applied and no patient data is included
        /// </summary>
        [JsonPropertyName("redactionApplied")]
        public bool RedactionApplied { get; set; } = true;

        /// <summary>
        /// Redaction mechanism identifier
        /// </summary>
        [JsonPropertyName("redactionVersion")]
        public string? RedactionVersion { get; set; } = "sentinel-redactor-v1";

        /// <summary>
        /// List of categories that have been redacted
        /// </summary>
        [JsonPropertyName("redactedFields")]
        public List<string>? RedactedFields { get; set; }

        [JsonPropertyName("application")]
        public ApplicationInfo? Application { get; set; }

        [JsonPropertyName("request")]
        public RequestInfo? Request { get; set; }

        [JsonPropertyName("exception")]
        public ExceptionInfo? Exception { get; set; }

        [JsonPropertyName("runtime")]
        public RuntimeInfo? Runtime { get; set; }

        [JsonPropertyName("database")]
        public DatabaseInfo? Database { get; set; }

        [JsonPropertyName("authentication")]
        public AuthenticationInfo? Authentication { get; set; }

        [JsonPropertyName("client")]
        public BrowserClientInfo? Client { get; set; }

        [JsonPropertyName("performance")]
        public PerformanceInfo? Performance { get; set; }

        [JsonPropertyName("configuration")]
        public Dictionary<string, string?>? Configuration { get; set; }

        [JsonPropertyName("featureFlags")]
        public Dictionary<string, string?>? FeatureFlags { get; set; }

        [JsonPropertyName("breadcrumbs")]
        public List<BreadcrumbInfo>? Breadcrumbs { get; set; }

        [JsonPropertyName("recentLogs")]
        public List<LogInfo>? RecentLogs { get; set; }
    }

    public class ApplicationInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("commitHash")]
        public string? CommitHash { get; set; }

        [JsonPropertyName("installationId")]
        public string? InstallationId { get; set; }
    }

    public class RequestInfo
    {
        [JsonPropertyName("routeTemplate")]
        public string? RouteTemplate { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("userAgent")]
        public string? UserAgent { get; set; }
    }

    public class ExceptionInfo
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("stackTrace")]
        public string? StackTrace { get; set; }
    }

    public class RuntimeInfo
    {
        [JsonPropertyName("dotNetVersion")]
        public string? DotNetVersion { get; set; }

        [JsonPropertyName("operatingSystem")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("osArchitecture")]
        public string? OsArchitecture { get; set; }

        [JsonPropertyName("runtimeIdentifier")]
        public string? RuntimeIdentifier { get; set; }

        [JsonPropertyName("processorCount")]
        public int? ProcessorCount { get; set; }

        [JsonPropertyName("workingSetMB")]
        public long? WorkingSetMB { get; set; }
    }

    public class DatabaseInfo
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("serverVersion")]
        public string? ServerVersion { get; set; }
    }

    public class AuthenticationInfo
    {
        [JsonPropertyName("isAuthenticated")]
        public bool? IsAuthenticated { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }

    public class BrowserClientInfo
    {
        [JsonPropertyName("browserLanguage")]
        public string? BrowserLanguage { get; set; }

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }

        [JsonPropertyName("viewportWidth")]
        public int? ViewportWidth { get; set; }

        [JsonPropertyName("viewportHeight")]
        public int? ViewportHeight { get; set; }

        [JsonPropertyName("devicePixelRatio")]
        public double? DevicePixelRatio { get; set; }
    }

    public class PerformanceInfo
    {
        [JsonPropertyName("recentErrors")]
        public ErrorCounts? RecentErrors { get; set; }
    }

    public class ConfigurationInfo
    {
        [JsonPropertyName("environment")]
        public string? Environment { get; set; }
    }

    public class BreadcrumbInfo
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }
    }

    public class LogInfo
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string? Level { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Recent error counts (no error messages or details)
    /// </summary>
    public class ErrorCounts
    {
        [JsonPropertyName("last1Hour")]
        public int Last1Hour { get; set; }

        [JsonPropertyName("last24Hours")]
        public int Last24Hours { get; set; }
    }
}
