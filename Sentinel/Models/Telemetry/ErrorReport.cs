using System.Text.Json.Serialization;

namespace Sentinel.Models.Telemetry
{
    /// <summary>
    /// Error report DTO matching the Sentinel Feedback API error schema
    /// </summary>
    public class ErrorReport
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("eventId")]
        public string EventId { get; set; } = string.Empty;

        [JsonPropertyName("errorId")]
        public string ErrorId { get; set; } = string.Empty;

        [JsonPropertyName("occurredAtUtc")]
        public DateTime OccurredAtUtc { get; set; }

        [JsonPropertyName("installationId")]
        public string InstallationId { get; set; } = string.Empty;

        [JsonPropertyName("sentinelVersion")]
        public string? SentinelVersion { get; set; }

        [JsonPropertyName("application")]
        public ApplicationContext Application { get; set; } = new();

        [JsonPropertyName("error")]
        public ErrorDetails Error { get; set; } = new();

        [JsonPropertyName("request")]
        public RequestContext Request { get; set; } = new();

        [JsonPropertyName("runtime")]
        public RuntimeContext Runtime { get; set; } = new();

        [JsonPropertyName("breadcrumbs")]
        public List<Breadcrumb> Breadcrumbs { get; set; } = new();

        [JsonPropertyName("redaction")]
        public RedactionInfo Redaction { get; set; } = new();
    }

    /// <summary>
    /// Application context information
    /// </summary>
    public class ApplicationContext
    {
        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("module")]
        public string? Module { get; set; }

        [JsonPropertyName("route")]
        public string? Route { get; set; }
    }

    /// <summary>
    /// Error/exception details
    /// </summary>
    public class ErrorDetails
    {
        [JsonPropertyName("exceptionType")]
        public string ExceptionType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("targetMethod")]
        public string? TargetMethod { get; set; }

        [JsonPropertyName("stackTrace")]
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// HTTP request context
    /// </summary>
    public class RequestContext
    {
        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Runtime environment information
    /// </summary>
    public class RuntimeContext
    {
        [JsonPropertyName("operatingSystem")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("architecture")]
        public string? Architecture { get; set; }

        [JsonPropertyName("dotNetVersion")]
        public string? DotNetVersion { get; set; }

        [JsonPropertyName("processUptimeSeconds")]
        public long? ProcessUptimeSeconds { get; set; }
    }

    /// <summary>
    /// Breadcrumb trail entry for error context
    /// </summary>
    public class Breadcrumb
    {
        [JsonPropertyName("timestampUtc")]
        public DateTime TimestampUtc { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;
    }

    /// <summary>
    /// Redaction metadata
    /// </summary>
    public class RedactionInfo
    {
        [JsonPropertyName("applied")]
        public bool Applied { get; set; } = true;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "sentinel-redactor-v1";
    }
}
