using Microsoft.Extensions.Logging;
using Sentinel.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sentinel.Services
{
    /// <summary>
    /// In-memory queue service for background geocoding of patient addresses
    /// </summary>
    public class GeocodingQueueService : IGeocodingQueueService
    {
        private readonly ConcurrentQueue<GeocodingQueueItem> _queue = new();
        private readonly ConcurrentDictionary<Guid, GeocodingQueueItem> _inProgress = new();
        private readonly ConcurrentBag<GeocodingQueueItem> _completedToday = new();
        private readonly ConcurrentBag<GeocodingQueueItem> _failedToday = new();
        private readonly ILogger<GeocodingQueueService> _logger;
        private DateTime _lastDailyReset = DateTime.UtcNow.Date;

        public GeocodingQueueService(ILogger<GeocodingQueueService> logger)
        {
            _logger = logger;
        }

        public void Enqueue(Guid patientId, string fullAddress)
        {
            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                _logger.LogWarning("Skipping geocoding queue for patient {PatientId} - address is empty", patientId);
                return;
            }

            var item = new GeocodingQueueItem
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                FullAddress = fullAddress,
                QueuedAt = DateTime.UtcNow,
                NextAttemptAt = DateTime.UtcNow,
                AttemptCount = 0
            };

            _queue.Enqueue(item);
            _logger.LogInformation("Queued patient {PatientId} for background geocoding: {Address}", 
                patientId, fullAddress);
        }

        public Task<GeocodingQueueItem?> DequeueAsync(CancellationToken cancellationToken)
        {
            ResetDailyStatsIfNeeded();

            // Try to find an item ready for processing
            while (_queue.TryDequeue(out var item))
            {
                // Check if it's time to retry this item
                if (item.NextAttemptAt > DateTime.UtcNow)
                {
                    // Not ready yet, re-queue it
                    _queue.Enqueue(item);
                    continue;
                }

                // Track in-progress
                _inProgress.TryAdd(item.Id, item);
                _logger.LogDebug("Dequeued geocoding item {ItemId} for patient {PatientId}", 
                    item.Id, item.PatientId);
                return Task.FromResult<GeocodingQueueItem?>(item);
            }

            return Task.FromResult<GeocodingQueueItem?>(null);
        }

        public void MarkCompleted(Guid itemId, double? latitude, double? longitude)
        {
            if (_inProgress.TryRemove(itemId, out var item))
            {
                item.IsCompleted = true;
                item.ProcessedAt = DateTime.UtcNow;
                item.ResultLatitude = latitude;
                item.ResultLongitude = longitude;

                _completedToday.Add(item);

                _logger.LogInformation(
                    "Geocoding completed for patient {PatientId}: {Lat}, {Lon} (attempt {Attempt})",
                    item.PatientId, latitude, longitude, item.AttemptCount + 1);
            }
        }

        public void MarkFailed(Guid itemId, string errorMessage)
        {
            if (_inProgress.TryRemove(itemId, out var item))
            {
                item.AttemptCount++;

                // Max 3 retry attempts
                if (item.AttemptCount >= 3)
                {
                    item.Failed = true;
                    item.ErrorMessage = errorMessage;
                    item.ProcessedAt = DateTime.UtcNow;

                    _failedToday.Add(item);

                    _logger.LogError(
                        "Geocoding failed permanently for patient {PatientId} after {Attempts} attempts: {Error}",
                        item.PatientId, item.AttemptCount, errorMessage);
                }
                else
                {
                    // Exponential backoff: 1min, 5min, 15min
                    var delayMinutes = item.AttemptCount switch
                    {
                        1 => 1,
                        2 => 5,
                        _ => 15
                    };

                    item.NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                    _queue.Enqueue(item); // Re-queue for retry

                    _logger.LogWarning(
                        "Geocoding failed for patient {PatientId}, will retry in {Minutes} minutes (attempt {Attempt}/3): {Error}",
                        item.PatientId, delayMinutes, item.AttemptCount, errorMessage);
                }
            }
        }

        public int GetQueueLength()
        {
            return _queue.Count + _inProgress.Count;
        }

        public (int Pending, int InProgress, int CompletedToday, int FailedToday) GetStatistics()
        {
            ResetDailyStatsIfNeeded();

            return (
                Pending: _queue.Count,
                InProgress: _inProgress.Count,
                CompletedToday: _completedToday.Count,
                FailedToday: _failedToday.Count
            );
        }

        private void ResetDailyStatsIfNeeded()
        {
            var today = DateTime.UtcNow.Date;
            if (_lastDailyReset < today)
            {
                _completedToday.Clear();
                _failedToday.Clear();
                _lastDailyReset = today;
                _logger.LogInformation("Daily geocoding statistics reset");
            }
        }
    }
}
