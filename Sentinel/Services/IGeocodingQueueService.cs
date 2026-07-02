using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface IGeocodingQueueService
    {
        /// <summary>
        /// Enqueue a patient address for background geocoding
        /// </summary>
        void Enqueue(Guid patientId, string fullAddress);

        /// <summary>
        /// Dequeue the next item ready for processing
        /// </summary>
        Task<Models.GeocodingQueueItem?> DequeueAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Mark an item as successfully completed
        /// </summary>
        void MarkCompleted(Guid itemId, double? latitude, double? longitude);

        /// <summary>
        /// Mark an item as failed with retry logic
        /// </summary>
        void MarkFailed(Guid itemId, string errorMessage);

        /// <summary>
        /// Get current queue length (pending + in-progress)
        /// </summary>
        int GetQueueLength();

        /// <summary>
        /// Get statistics about queue processing
        /// </summary>
        (int Pending, int InProgress, int CompletedToday, int FailedToday) GetStatistics();
    }
}
