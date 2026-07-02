using System;

namespace Sentinel.Models
{
    /// <summary>
    /// Represents a patient address queued for background geocoding
    /// </summary>
    public class GeocodingQueueItem
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAt { get; set; }
        public bool IsCompleted { get; set; }
        public bool Failed { get; set; }
        public string? ErrorMessage { get; set; }
        public double? ResultLatitude { get; set; }
        public double? ResultLongitude { get; set; }

        // Navigation
        public Patient? Patient { get; set; }
    }
}
