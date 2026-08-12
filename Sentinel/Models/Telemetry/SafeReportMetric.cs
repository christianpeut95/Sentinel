namespace Sentinel.Models.Telemetry;

/// <summary>
/// Privacy-safe report generation metric.
/// Contains ONLY report type and generic metadata - NO actual report data.
/// </summary>
public class SafeReportMetric : SafeTelemetryData
{
    public SafeReportMetric()
    {
        EventType = TelemetryEventType.ReportGenerated;
    }

    public ReportType ReportType { get; set; }
    public int RecordCount { get; set; } // Aggregate count only
    public TimeSpan GenerationDuration { get; set; }
    public ExportFormat? ExportFormat { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool Success { get; set; }
}
