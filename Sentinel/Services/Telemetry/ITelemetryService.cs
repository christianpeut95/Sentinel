using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry;

/// <summary>
/// Telemetry service interface for logging privacy-safe events.
/// ALL methods accept ONLY whitelisted safe DTOs.
/// NEVER accepts patient, lab result, survey answer, or custom field data.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Log a generic telemetry event (system start, shutdown, login, etc.)
    /// </summary>
    void LogEvent(SafeTelemetryData telemetryData);

    /// <summary>
    /// Log a privacy-safe error event
    /// </summary>
    void LogError(SafeErrorEvent errorEvent);

    /// <summary>
    /// Log a privacy-safe error with exception
    /// </summary>
    void LogError(Exception exception, string? source = null, string? requestPath = null);

    /// <summary>
    /// Log a report generation metric (type, count, duration only - NO actual data)
    /// </summary>
    void LogReportGenerated(SafeReportMetric reportMetric);

    /// <summary>
    /// Log an export metric (type, format, count only - NO actual data)
    /// </summary>
    void LogExport(SafeExportMetric exportMetric);

    /// <summary>
    /// Get the current installation ID
    /// </summary>
    string? GetInstallationId();
}
