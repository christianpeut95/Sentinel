namespace Sentinel.Models.Telemetry;

/// <summary>
/// Privacy-safe export metric.
/// Contains ONLY export metadata - NO actual exported data.
/// </summary>
public class SafeExportMetric : SafeTelemetryData
{
    public SafeExportMetric()
    {
        EventType = TelemetryEventType.ExportPerformed;
    }

    public string? ExportType { get; set; } // e.g., "Cases", "Contacts", "LabResults"
    public ExportFormat ExportFormat { get; set; }
    public int RecordCount { get; set; } // Aggregate count only
    public TimeSpan ExportDuration { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool Success { get; set; }
}
