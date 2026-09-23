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

        /// <summary>
        /// Submits the minimal, one-time installation report after an
        /// administrator has completed setup and explicitly opted in to remote
        /// usage telemetry. This does not include runtime details, activity,
        /// configuration, organisation, user, or patient information.
        /// </summary>
        public Task SubmitInstallationReportAsync()
        {
            return SubmitUsageReportAsync(isInstallationReport: true);
        }

        private async Task SubmitUsageReportAsync(bool isInstallationReport = false)
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

                if (!settings.IsSetupCompleted || !settings.EnableUsageMonitoring)
                {
                    _logger.LogDebug("Usage monitoring is not enabled for a completed setup, skipping report submission");
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

                _logger.LogInformation(
                    "Building and submitting {ReportKind} usage report",
                    isInstallationReport ? "initial installation" : "hourly");

                var periodEnd = DateTime.UtcNow;
                var periodStart = isInstallationReport
                    ? periodEnd
                    : _activityTracker.GetPeriodStart();
                var activityReport = isInstallationReport
                    ? new ActivityReport()
                    : _activityTracker.GetActivityReportAndReset();

                // The setup report intentionally contains empty aggregate
                // sections required by the existing usage-report schema. It
                // does not query or transmit database counts or runtime data.
                var snapshot = new SnapshotReport();
                UsageRuntime? runtime = null;
                if (!isInstallationReport)
                {
                    var snapshotBuilder = scope.ServiceProvider.GetRequiredService<UsageSnapshotBuilder>();
                    snapshot = await snapshotBuilder.BuildSnapshotAsync();

                    // Add non-identifying runtime information accepted by the usage API.
                    var systemInfoProvider = scope.ServiceProvider.GetRequiredService<SystemInfoProvider>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<Sentinel.Data.ApplicationDbContext>();
                    runtime = await systemInfoProvider.BuildUsageRuntimeAsync(dbContext);
                }

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
                using var timeout = isInstallationReport
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(10))
                    : null;
                var success = await client.SubmitUsageReportAsync(usageReport, timeout?.Token ?? CancellationToken.None);

                if (success)
                {
                    _logger.LogInformation(
                        "{ReportKind} usage report submitted successfully",
                        isInstallationReport ? "Initial installation" : "Hourly");
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to submit {ReportKind} usage report",
                        isInstallationReport ? "initial installation" : "hourly");
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
