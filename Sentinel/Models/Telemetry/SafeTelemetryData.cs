namespace Sentinel.Models.Telemetry;

/// <summary>
/// Base telemetry data object with only whitelisted safe fields.
/// NO patient, lab result, survey answer, or custom field data.
/// </summary>
public class SafeTelemetryData
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TelemetryEventType EventType { get; set; }
    public string? InstallationId { get; set; }
    public string? UserId { get; set; } // Anonymized/hashed if needed
    public string? UserRole { get; set; }
    public string? ApplicationVersion { get; set; }
    public string? Environment { get; set; } // Production, Staging, etc.
    public Dictionary<string, object>? Metadata { get; set; }
}
