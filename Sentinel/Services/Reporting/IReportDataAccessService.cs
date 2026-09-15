namespace Sentinel.Services.Reporting;

/// <summary>
/// Resolves the data permission(s) required to run a report against an entity or report view.
/// Report permissions control access to report definitions and the report builder; they do not
/// grant access to the underlying surveillance data.
/// </summary>
public interface IReportDataAccessService
{
    Task<bool> CanReadEntityTypeAsync(string? entityType);
}
