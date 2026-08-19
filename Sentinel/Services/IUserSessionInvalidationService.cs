namespace Sentinel.Services;

/// <summary>
/// Invalidates Identity cookies after an administrator changes account status or access.
/// </summary>
public interface IUserSessionInvalidationService
{
    Task InvalidateUserAsync(string userId);
    Task InvalidateUsersInRoleAsync(string roleId);
}
