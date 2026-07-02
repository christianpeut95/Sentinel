using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sentinel.Services
{
    /// <summary>
    /// Background service that processes the geocoding queue
    /// </summary>
    public class GeocodingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGeocodingQueueService _queueService;
        private readonly ILogger<GeocodingBackgroundService> _logger;
        private readonly bool _enabled;
        private readonly int _maxConcurrent;
        private readonly int _delayBetweenRequestsMs;
        private readonly int _checkIntervalMs;
        private readonly int _geocodingTimeoutMs;
        private readonly SemaphoreSlim _rateLimiter;

        public GeocodingBackgroundService(
            IServiceProvider serviceProvider,
            IGeocodingQueueService queueService,
            ILogger<GeocodingBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _queueService = queueService;
            _logger = logger;

            // Load configuration
            _enabled = configuration.GetValue("Geocoding:BackgroundProcessing", true);
            _maxConcurrent = configuration.GetValue("Geocoding:MaxConcurrentRequests", 2);
            _delayBetweenRequestsMs = configuration.GetValue("Geocoding:DelayBetweenRequestsMs", 250);
            _checkIntervalMs = configuration.GetValue("Geocoding:CheckIntervalMs", 5000);
            _geocodingTimeoutMs = configuration.GetValue("Geocoding:TimeoutMs", 10000);

            _rateLimiter = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Background geocoding is disabled in configuration");
                return;
            }

            _logger.LogInformation(
                "Geocoding Background Service started (MaxConcurrent={MaxConcurrent}, Delay={DelayMs}ms)",
                _maxConcurrent, _delayBetweenRequestsMs);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var queueLength = _queueService.GetQueueLength();
                    if (queueLength > 0)
                    {
                        _logger.LogDebug("Processing geocoding queue: {Count} items", queueLength);

                        // Process multiple items concurrently (up to maxConcurrent)
                        var tasks = Enumerable.Range(0, _maxConcurrent)
                            .Select(_ => ProcessNextItemAsync(stoppingToken))
                            .ToArray();

                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        // Log stats periodically when idle
                        var stats = _queueService.GetStatistics();
                        if (stats.CompletedToday > 0 || stats.FailedToday > 0)
                        {
                            _logger.LogDebug(
                                "Geocoding stats - Completed today: {Completed}, Failed today: {Failed}",
                                stats.CompletedToday, stats.FailedToday);
                        }
                    }

                    // Wait before checking queue again
                    await Task.Delay(_checkIntervalMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Geocoding Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Geocoding Background Service main loop");
                    await Task.Delay(10000, stoppingToken); // Back off on error
                }
            }

            _logger.LogInformation("Geocoding Background Service stopped");
        }

        private async Task ProcessNextItemAsync(CancellationToken cancellationToken)
        {
            var item = await _queueService.DequeueAsync(cancellationToken);
            if (item == null)
                return;

            // Rate limiting
            await _rateLimiter.WaitAsync(cancellationToken);

            try
            {
                await ProcessGeocodingItemAsync(item, cancellationToken);

                // Delay between requests to respect API limits
                if (_delayBetweenRequestsMs > 0)
                {
                    await Task.Delay(_delayBetweenRequestsMs, cancellationToken);
                }
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private async Task ProcessGeocodingItemAsync(Models.GeocodingQueueItem item, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var geocodingService = scope.ServiceProvider.GetRequiredService<IGeocodingService>();

            try
            {
                _logger.LogInformation(
                    "Starting geocoding for patient {PatientId}: {Address} (attempt {Attempt})",
                    item.PatientId, item.FullAddress, item.AttemptCount + 1);

                // Call geocoding API with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_geocodingTimeoutMs);

                var (latitude, longitude) = await geocodingService.GeocodeAsync(item.FullAddress);

                if (latitude.HasValue && longitude.HasValue)
                {
                    // Update patient coordinates
                    var patient = await context.Patients.FindAsync(new object[] { item.PatientId }, cancellationToken);
                    if (patient != null)
                    {
                        patient.Latitude = latitude;
                        patient.Longitude = longitude;

                        await context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Updated patient {PatientId} coordinates: {Lat}, {Lon}",
                            item.PatientId, latitude, longitude);

                        // Mark as completed
                        _queueService.MarkCompleted(item.Id, latitude, longitude);

                        // Trigger background jurisdiction detection (fire-and-forget)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await DetectJurisdictionsAsync(item.PatientId, latitude.Value, longitude.Value);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error detecting jurisdictions for patient {PatientId}", item.PatientId);
                            }
                        }, CancellationToken.None);
                    }
                    else
                    {
                        _logger.LogWarning("Patient {PatientId} not found in database", item.PatientId);
                        _queueService.MarkFailed(item.Id, "Patient not found");
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Geocoding returned null coordinates for patient {PatientId}: {Address}",
                        item.PatientId, item.FullAddress);
                    _queueService.MarkFailed(item.Id, "No coordinates returned from geocoding service");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Geocoding timeout for patient {PatientId}", item.PatientId);
                _queueService.MarkFailed(item.Id, $"Timeout after {_geocodingTimeoutMs}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding patient {PatientId}: {Address}",
                    item.PatientId, item.FullAddress);
                _queueService.MarkFailed(item.Id, ex.Message);
            }
        }

        private async Task DetectJurisdictionsAsync(Guid patientId, double latitude, double longitude)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GeocodingBackgroundService>>();

            try
            {
                logger.LogInformation(
                    "Starting jurisdiction detection for patient {PatientId} at {Lat}, {Lon}",
                    patientId, latitude, longitude);

                var patient = await context.Patients
                    .Include(p => p.Jurisdiction1)
                    .Include(p => p.Jurisdiction2)
                    .Include(p => p.Jurisdiction3)
                    .Include(p => p.Jurisdiction4)
                    .Include(p => p.Jurisdiction5)
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    logger.LogWarning("Patient {PatientId} not found for jurisdiction detection", patientId);
                    return;
                }

                // TODO: Implement jurisdiction detection using GeoJSON boundaries
                // For now, this is a placeholder that will be implemented when jurisdiction boundary data is configured
                // The jurisdiction detection would check if the lat/lon point falls within any jurisdiction's BoundaryData (GeoJSON polygon)

                logger.LogInformation(
                    "Jurisdiction detection completed for patient {PatientId} (implementation pending)", patientId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in jurisdiction detection for patient {PatientId}", patientId);
            }
        }
    }
}
