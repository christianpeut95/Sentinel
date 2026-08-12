using Microsoft.Extensions.DependencyInjection;
using Sentinel.Models.Telemetry;
using Sentinel.Services;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// Background service that submits hourly usage reports to the Sentinel Feedback API
    /// </summary>
    public class UsageMonitoringHostedService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ActivityTracker _activityTracker;
        private readonly IApplicationVersionProvider _applicationVersion;
        private readonly ILogger<UsageMonitoringHostedService> _logger;
        private Timer? _timer;
        private const int HourlyIntervalMs = 3600000; // 1 hour

        public UsageMonitoringHostedService(
            IServiceProvider serviceProvider,
            ActivityTracker activityTracker,
            IApplicationVersionProvider applicationVersion,
            ILogger<UsageMonitoringHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _activityTracker = activityTracker;
            _applicationVersion = applicationVersion;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Usage Monitoring Hosted Service starting");

            // Start the timer to run immediately on startup, then hourly
            _timer = new Timer(
                callback: async _ => await SubmitUsageReportAsync(),
                state: null,
                dueTime: TimeSpan.Zero, // Submit immediately on startup
                period: TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private async Task SubmitUsageReportAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                // Check if usage monitoring is enabled
                var systemSettingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var settings = await systemSettingsService.GetSettingsAsync();

                if (settings == null)
                {
                    _logger.LogWarning("System settings are unavailable; skipping usage report submission");
                    return;
                }

                if (!settings.EnableUsageMonitoring)
                {
                    _logger.LogDebug("Usage monitoring is disabled, skipping report submission");
                    return;
                }

                // Use the central accessor so a missing ID is repaired before any
                // telemetry is sent. Do not submit an unassociated "unknown" report.
                var installationId = await systemSettingsService.GetInstallationIdAsync();
                if (string.IsNullOrWhiteSpace(installationId))
                {
                    _logger.LogWarning("Installation ID is unavailable; skipping usage report submission");
                    return;
                }

                _logger.LogInformation("Building and submitting hourly usage report");

                // Get activity report and reset counters
                var periodEnd = DateTime.UtcNow;
                var periodStart = _activityTracker.GetPeriodStart();
                var activityReport = _activityTracker.GetActivityReportAndReset();

                // Build snapshot of current system state
                var snapshotBuilder = scope.ServiceProvider.GetRequiredService<UsageSnapshotBuilder>();
                var snapshot = await snapshotBuilder.BuildSnapshotAsync();

                // Add non-identifying runtime information accepted by the usage API.
                var systemInfoProvider = scope.ServiceProvider.GetRequiredService<SystemInfoProvider>();
                var dbContext = scope.ServiceProvider.GetRequiredService<Sentinel.Data.ApplicationDbContext>();
                var runtime = await systemInfoProvider.BuildUsageRuntimeAsync(dbContext);

                // Build complete usage report
                var usageReport = new UsageReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    InstallationId = installationId,
                    SentinelVersion = _applicationVersion.InformationalVersion.Length > 50 
                        ? _applicationVersion.InformationalVersion[..50] 
                        : _applicationVersion.InformationalVersion,
                    GeneratedAtUtc = DateTime.UtcNow,
                    Period = new ReportPeriod
                    {
                        StartUtc = periodStart,
                        EndUtc = periodEnd
                    },
                    Activity = activityReport,
                    Snapshot = snapshot,
                    Runtime = runtime
                };

                // Submit report to API
                var client = scope.ServiceProvider.GetRequiredService<UsageReportClient>();
                var success = await client.SubmitUsageReportAsync(usageReport);

                if (success)
                {
                    _logger.LogInformation("Hourly usage report submitted successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to submit hourly usage report");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting hourly usage report");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Usage Monitoring Hosted Service stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
