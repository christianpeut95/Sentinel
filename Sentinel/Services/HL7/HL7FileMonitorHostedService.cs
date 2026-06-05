namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Background service that keeps the HL7 file monitor running
    /// </summary>
    public class HL7FileMonitorHostedService : IHostedService
    {
        private readonly IHL7FileMonitorService _fileMonitorService;
        private readonly ILogger<HL7FileMonitorHostedService> _logger;

        public HL7FileMonitorHostedService(
            IHL7FileMonitorService fileMonitorService,
            ILogger<HL7FileMonitorHostedService> logger)
        {
            _fileMonitorService = fileMonitorService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("HL7 File Monitor Hosted Service is starting");

            try
            {
                // Start monitoring
                await _fileMonitorService.StartMonitoringAsync(cancellationToken);

                _logger.LogInformation("HL7 File Monitor Hosted Service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start HL7 File Monitor Hosted Service");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("HL7 File Monitor Hosted Service is stopping");

            try
            {
                await _fileMonitorService.StopMonitoringAsync();

                _logger.LogInformation("HL7 File Monitor Hosted Service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping HL7 File Monitor Hosted Service");
            }
        }
    }
}
