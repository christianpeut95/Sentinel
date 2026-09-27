namespace Sentinel.Services.HL7;

/// <summary>
/// Bounded, deployment-controlled settings for HL7 file-drop polling.
/// </summary>
public sealed class HL7FileMonitorOptions
{
    public const string SectionName = "HL7:FileMonitor";

    /// <summary>How often configured inbound folders are scanned.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// A file must not have changed for this duration before it can be processed.
    /// </summary>
    public int FileSettleDelaySeconds { get; set; } = 2;

    /// <summary>Maximum accepted inbound HL7 file size, before it is read.</summary>
    public long MaxInboundFileBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Optional canonical root for configured file-drop locations. Docker deployments
    /// should set this to their dedicated mounted HL7 directory (for example /data/hl7).
    /// </summary>
    public string? AllowedRootPath { get; set; }
}
