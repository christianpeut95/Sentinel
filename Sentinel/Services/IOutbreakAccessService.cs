namespace Sentinel.Services;

/// <summary>
/// Applies the current user's disease visibility to an outbreak resource.
/// </summary>
public interface IOutbreakAccessService
{
    Task<bool> CanAccessOutbreakAsync(int outbreakId, CancellationToken cancellationToken = default);
}
