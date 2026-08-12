using Sentinel.Models.Telemetry;
using Sentinel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Sentinel.Services.Telemetry;

/// <summary>
/// Telemetry service implementation with local structured logging.
/// ALL logging is privacy-safe - NO patient/lab/survey/custom field data.
/// Local logs: unrestricted (all safe events).
/// Remote telemetry: deferred for future implementation with explicit whitelist.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly SafeEventBuilder _eventBuilder;
    private readonly SystemInfoProvider _systemInfoProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly IApplicationVersionProvider _applicationVersion;
    private readonly Microsoft.Extensions.Logging.ILogger<TelemetryService> _logger;
    private string? _cachedInstallationId;

    public TelemetryService(
        Microsoft.Extensions.Logging.ILogger<TelemetryService> logger,
        Microsoft.Extensions.Logging.ILogger<SafeEventBuilder> eventBuilderLogger,
        IServiceProvider serviceProvider,
        IApplicationVersionProvider applicationVersion)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _applicationVersion = applicationVersion;
        _eventBuilder = new SafeEventBuilder(eventBuilderLogger);
        _systemInfoProvider = new SystemInfoProvider();
    }

    public void LogEvent(SafeTelemetryData telemetryData)
    {
        EnrichTelemetryData(telemetryData);
        _eventBuilder.LogTelemetryEvent(telemetryData);
    }

    public void LogError(SafeErrorEvent errorEvent)
    {
        EnrichTelemetryData(errorEvent);
        _eventBuilder.LogErrorEvent(errorEvent);
    }

    public void LogError(Exception exception, string? source = null, string? requestPath = null)
    {
        var errorEvent = new SafeErrorEvent
        {
            Severity = TelemetryErrorSeverity.High,
            ErrorCode = exception.GetType().Name,
            ErrorMessage = exception.Message,
            StackTrace = exception.StackTrace,
            Source = source ?? exception.Source,
            RequestPath = requestPath
        };

        LogError(errorEvent);
    }

    public void LogReportGenerated(SafeReportMetric reportMetric)
    {
        EnrichTelemetryData(reportMetric);
        _eventBuilder.LogReportMetric(reportMetric);
    }

    public void LogExport(SafeExportMetric exportMetric)
    {
        EnrichTelemetryData(exportMetric);
        _eventBuilder.LogExportMetric(exportMetric);
    }

    public string? GetInstallationId()
    {
        if (_cachedInstallationId != null)
            return _cachedInstallationId;

        try
        {
            // Create a scope to resolve the scoped service
            using var scope = _serviceProvider.CreateScope();
            var systemSettingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            _cachedInstallationId = systemSettingsService.GetInstallationIdAsync().GetAwaiter().GetResult();
            return _cachedInstallationId;
        }
        catch
        {
            return null;
        }
    }

    private void EnrichTelemetryData(SafeTelemetryData telemetryData)
    {
        telemetryData.InstallationId ??= GetInstallationId();
        telemetryData.ApplicationVersion ??= _applicationVersion.InformationalVersion;
        telemetryData.Environment ??= GetEnvironment();
    }

    private string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }
}
