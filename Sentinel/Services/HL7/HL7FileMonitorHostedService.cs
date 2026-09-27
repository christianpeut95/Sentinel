using Microsoft.Extensions.Options;

namespace Sentinel.Services.HL7;

/// <summary>
/// Runs the single, bounded HL7 file-drop polling loop for the application.
/// </summary>
public sealed class HL7FileMonitorHostedService : BackgroundService
{
    private readonly IHL7FileMonitorService _fileMonitorService;
    private readonly ILogger<HL7FileMonitorHostedService> _logger;
    private readonly TimeSpan _pollingInterval;

    public HL7FileMonitorHostedService(
        IHL7FileMonitorService fileMonitorService,
        IOptions<HL7FileMonitorOptions> options,
        ILogger<HL7FileMonitorHostedService> logger)
    {
        _fileMonitorService = fileMonitorService;
        _logger = logger;
        _pollingInterval = TimeSpan.FromSeconds(Math.Clamp(options.Value.PollingIntervalSeconds, 1, 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HL7 file-drop polling service is starting with a {PollingInterval} second interval", _pollingInterval.TotalSeconds);

        try
        {
            await _fileMonitorService.StartMonitoringAsync(stoppingToken);

            using var timer = new PeriodicTimer(_pollingInterval);
            do
            {
                try
                {
                    await _fileMonitorService.ScanConfiguredDirectoriesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error during an HL7 file-drop polling cycle");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start the HL7 file-drop polling service");
        }
        finally
        {
            await _fileMonitorService.StopMonitoringAsync();
            _logger.LogInformation("HL7 file-drop polling service stopped");
        }
    }
}
