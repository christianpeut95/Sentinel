using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class GeocodingQueueItem
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string FullAddress { get; set; } = null!;

    public DateTime QueuedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public bool IsCompleted { get; set; }

    public bool Failed { get; set; }

    public string? ErrorMessage { get; set; }

    public double? ResultLatitude { get; set; }

    public double? ResultLongitude { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
