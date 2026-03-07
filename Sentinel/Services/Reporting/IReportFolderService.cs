using Sentinel.Models.Reporting;

namespace Sentinel.Services.Reporting;

public interface IReportFolderService
{
    Task<List<ReportFolder>> GetUserFoldersAsync(string userId);
    Task<List<ReportFolder>> GetSharedFoldersAsync(string userId);
    Task<ReportFolder?> GetFolderByIdAsync(int folderId, string userId);
    Task<ReportFolder> CreateFolderAsync(ReportFolder folder, string userId);
    Task<bool> UpdateFolderAsync(ReportFolder folder, string userId);
    Task<bool> DeleteFolderAsync(int folderId, string userId);
    Task<bool> CanAccessFolderAsync(int folderId, string userId);
    Task<bool> CanEditFolderAsync(int folderId, string userId);
    Task<bool> ShareFolderAsync(int folderId, string userId, string? targetUserId, int? targetGroupId, FolderPermissionLevel permissionLevel);
    Task<bool> RemoveShareAsync(int shareId, string userId);
    Task<List<ReportFolderShare>> GetFolderSharesAsync(int folderId, string userId);
}
