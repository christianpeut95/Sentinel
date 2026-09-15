namespace Sentinel.Services.Reporting;

/// <summary>
/// Raised when a caller can use reporting but is not entitled to read the requested entity data.
/// </summary>
public sealed class ReportDataAccessDeniedException : UnauthorizedAccessException
{
    public ReportDataAccessDeniedException(string? entityType)
        : base($"The current user is not permitted to report on entity type '{entityType}'.")
    {
    }
}
