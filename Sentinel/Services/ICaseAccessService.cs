namespace Sentinel.Services;

/// <summary>
/// Resolves whether the current request may access case-scoped data.
/// The underlying case query uses Sentinel's global disease access filter.
/// </summary>
public interface ICaseAccessService
{
    Task<bool> CanAccessCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessAllCasesAsync(IEnumerable<Guid> caseIds, CancellationToken cancellationToken = default);
}
