namespace Sentinel.Models.Telemetry;

public enum TelemetryEventType
{
    SystemStart,
    SystemShutdown,
    UserLogin,
    UserLogout,
    ReportGenerated,
    ExportPerformed,
    BackupCompleted,
    BackupFailed,
    DatabaseMigration,
    Error,
    Warning
}

public enum TelemetryErrorSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ExportFormat
{
    Csv,
    Excel,
    Pdf,
    Json
}

public enum ReportType
{
    CaseSummary,
    ContactTracing,
    EpidemiologicalCurve,
    LabResultsSummary,
    CustomReport
}
