using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services;

/// <summary>
/// The single object-level visibility check for outbreak resources. It relies
/// on the Disease query filter, which already enforces hierarchy-aware disease
/// access for the current request.
/// </summary>
public sealed class OutbreakAccessService : IOutbreakAccessService
{
    private readonly ApplicationDbContext _context;

    public OutbreakAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessOutbreakAsync(
        int outbreakId,
        CancellationToken cancellationToken = default)
    {
        if (outbreakId <= 0)
        {
            return false;
        }

        var outbreak = await _context.Outbreaks
            .AsNoTracking()
            .Where(o => o.Id == outbreakId && !o.IsDeleted)
            .Select(o => new
            {
                o.PrimaryDiseaseId,
                HasVisiblePrimaryDisease = o.PrimaryDisease != null
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (outbreak is null)
        {
            return false;
        }

        // An outbreak without a primary disease is not disease-scoped. An
        // outbreak with one is visible only when that disease (including its
        // ancestor access rules) is visible through the global query filter.
        return !outbreak.PrimaryDiseaseId.HasValue || outbreak.HasVisiblePrimaryDisease;
    }
}
