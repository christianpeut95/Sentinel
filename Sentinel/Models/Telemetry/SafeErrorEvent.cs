namespace Sentinel.Models.Telemetry;

/// <summary>
/// Privacy-safe error event for logging and reporting.
/// Contains NO patient/lab/survey/custom field data.
/// </summary>
public class SafeErrorEvent : SafeTelemetryData
{
    public SafeErrorEvent()
    {
        EventType = TelemetryEventType.Error;
    }

    public TelemetryErrorSeverity Severity { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? HttpStatusCode { get; set; }
}
