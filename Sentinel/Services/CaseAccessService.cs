using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services;

/// <summary>
/// Provides a single resource-level case-access check for APIs that begin at
/// a child entity rather than at <see cref="Sentinel.Models.Case"/>.
/// </summary>
public sealed class CaseAccessService : ICaseAccessService
{
    private readonly ApplicationDbContext _context;

    public CaseAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> CanAccessCaseAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        CanAccessAllCasesAsync(new[] { caseId }, cancellationToken);

    public async Task<bool> CanAccessAllCasesAsync(IEnumerable<Guid> caseIds, CancellationToken cancellationToken = default)
    {
        var requiredCaseIds = caseIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (requiredCaseIds.Length == 0)
        {
            return false;
        }

        // Do not call IgnoreQueryFilters here. The Case query filter is the
        // authoritative disease-visibility boundary for the current request.
        var visibleCaseCount = await _context.Cases
            .AsNoTracking()
            .Where(c => requiredCaseIds.Contains(c.Id))
            .Select(c => c.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return visibleCaseCount == requiredCaseIds.Length;
    }
}
