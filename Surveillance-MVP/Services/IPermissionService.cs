using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, PermissionModule module, PermissionAction action);
        Task<bool> HasPermissionAsync(string userId, string permissionKey);
        Task<List<Permission>> GetUserPermissionsAsync(string userId);
        Task<List<Permission>> GetRolePermissionsAsync(string roleId);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task GrantPermissionToUserAsync(string userId, int permissionId);
        Task RevokePermissionFromUserAsync(string userId, int permissionId);
        Task GrantPermissionToRoleAsync(string roleId, int permissionId);
        Task RevokePermissionFromRoleAsync(string roleId, int permissionId);
        Task<Dictionary<PermissionModule, List<PermissionAction>>> GetUserPermissionMatrixAsync(string userId);
    }
}
