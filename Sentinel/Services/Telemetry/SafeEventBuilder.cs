using Microsoft.Extensions.Logging;

namespace Sentinel.Services.Telemetry;

/// <summary>
/// Builds structured log events from safe telemetry DTOs.
/// Ensures only privacy-safe data is logged - NO patient/lab/survey/custom field data.
/// </summary>
public class SafeEventBuilder
{
    private readonly Microsoft.Extensions.Logging.ILogger<SafeEventBuilder> _logger;

    public SafeEventBuilder(Microsoft.Extensions.Logging.ILogger<SafeEventBuilder> logger)
    {
        _logger = logger;
    }

    public void LogTelemetryEvent(Models.Telemetry.SafeTelemetryData telemetryData)
    {
        var logLevel = DetermineLogLevel(telemetryData.EventType);

        _logger.Log(logLevel, 
            "Telemetry: {EventType} {@TelemetryData}",
            telemetryData.EventType,
            new
            {
                telemetryData.EventId,
                telemetryData.Timestamp,
                telemetryData.EventType,
                telemetryData.InstallationId,
                telemetryData.UserId,
                telemetryData.UserRole,
                telemetryData.ApplicationVersion,
                telemetryData.Environment,
                telemetryData.Metadata
            });
    }

    public void LogErrorEvent(Models.Telemetry.SafeErrorEvent errorEvent)
    {
        var logLevel = DetermineLogLevel(errorEvent.Severity);

        _logger.Log(logLevel,
            errorEvent.StackTrace != null 
                ? "Error: {ErrorCode} - {ErrorMessage} | {Source} | {RequestPath} {@ErrorDetails}" 
                : "Error: {ErrorCode} - {ErrorMessage} | {Source} | {RequestPath}",
            errorEvent.ErrorCode,
            errorEvent.ErrorMessage,
            errorEvent.Source,
            errorEvent.RequestPath,
            new
            {
                errorEvent.EventId,
                errorEvent.Timestamp,
                errorEvent.Severity,
                errorEvent.ErrorCode,
                errorEvent.ErrorMessage,
                errorEvent.StackTrace,
                errorEvent.Source,
                errorEvent.RequestPath,
                errorEvent.HttpMethod,
                errorEvent.HttpStatusCode,
                errorEvent.InstallationId,
                errorEvent.UserId,
                errorEvent.UserRole,
                errorEvent.ApplicationVersion,
                errorEvent.Environment
            });
    }

    public void LogReportMetric(Models.Telemetry.SafeReportMetric reportMetric)
    {
        _logger.LogInformation(
            "Report Generated: {ReportType} | Records: {RecordCount} | Duration: {Duration}ms | Success: {Success} {@ReportMetric}",
            reportMetric.ReportType,
            reportMetric.RecordCount,
            reportMetric.GenerationDuration.TotalMilliseconds,
            reportMetric.Success,
            new
            {
                reportMetric.EventId,
                reportMetric.Timestamp,
                reportMetric.ReportType,
                reportMetric.RecordCount,
                GenerationDurationMs = reportMetric.GenerationDuration.TotalMilliseconds,
                reportMetric.ExportFormat,
                reportMetric.FileSizeBytes,
                reportMetric.Success,
                reportMetric.InstallationId,
                reportMetric.UserId,
                reportMetric.UserRole
            });
    }

    public void LogExportMetric(Models.Telemetry.SafeExportMetric exportMetric)
    {
        _logger.LogInformation(
            "Export Performed: {ExportType} | Format: {ExportFormat} | Records: {RecordCount} | Duration: {Duration}ms | Success: {Success} {@ExportMetric}",
            exportMetric.ExportType,
            exportMetric.ExportFormat,
            exportMetric.RecordCount,
            exportMetric.ExportDuration.TotalMilliseconds,
            exportMetric.Success,
            new
            {
                exportMetric.EventId,
                exportMetric.Timestamp,
                exportMetric.ExportType,
                exportMetric.ExportFormat,
                exportMetric.RecordCount,
                ExportDurationMs = exportMetric.ExportDuration.TotalMilliseconds,
                exportMetric.FileSizeBytes,
                exportMetric.Success,
                exportMetric.InstallationId,
                exportMetric.UserId,
                exportMetric.UserRole
            });
    }

    private static LogLevel DetermineLogLevel(Models.Telemetry.TelemetryEventType eventType)
    {
        return eventType switch
        {
            Models.Telemetry.TelemetryEventType.Error => LogLevel.Error,
            Models.Telemetry.TelemetryEventType.Warning => LogLevel.Warning,
            Models.Telemetry.TelemetryEventType.BackupFailed => LogLevel.Error,
            _ => LogLevel.Information
        };
    }

    private static LogLevel DetermineLogLevel(Models.Telemetry.TelemetryErrorSeverity severity)
    {
        return severity switch
        {
            Models.Telemetry.TelemetryErrorSeverity.Critical => LogLevel.Critical,
            Models.Telemetry.TelemetryErrorSeverity.High => LogLevel.Error,
            Models.Telemetry.TelemetryErrorSeverity.Medium => LogLevel.Warning,
            Models.Telemetry.TelemetryErrorSeverity.Low => LogLevel.Information,
            _ => LogLevel.Error
        };
    }
}
