using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Reporting;

namespace Sentinel.Services.Reporting;

public class ReportFolderService : IReportFolderService
{
    private readonly ApplicationDbContext _context;

    public ReportFolderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportFolder>> GetUserFoldersAsync(string userId)
    {
        return await _context.ReportFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Reports)
            .Include(f => f.FolderShares)
            .Where(f => f.CreatedByUserId == userId)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<List<ReportFolder>> GetSharedFoldersAsync(string userId)
    {
        var userGroupIds = await _context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return await _context.ReportFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Reports)
            .Include(f => f.FolderShares)
            .Where(f => f.AccessType == FolderAccessType.Public ||
                       f.FolderShares.Any(fs => fs.UserId == userId) ||
                       f.FolderShares.Any(fs => fs.GroupId.HasValue && userGroupIds.Contains(fs.GroupId.Value)))
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<ReportFolder?> GetFolderByIdAsync(int folderId, string userId)
    {
        var folder = await _context.ReportFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Reports)
            .Include(f => f.FolderShares)
                .ThenInclude(fs => fs.User)
            .Include(f => f.FolderShares)
                .ThenInclude(fs => fs.Group)
            .FirstOrDefaultAsync(f => f.Id == folderId);

        if (folder == null)
            return null;

        if (!await CanAccessFolderAsync(folderId, userId))
            return null;

        return folder;
    }

    public async Task<ReportFolder> CreateFolderAsync(ReportFolder folder, string userId)
    {
        folder.CreatedByUserId = userId;
        folder.CreatedAt = DateTime.UtcNow;

        _context.ReportFolders.Add(folder);
        await _context.SaveChangesAsync();

        return folder;
    }

    public async Task<bool> UpdateFolderAsync(ReportFolder folder, string userId)
    {
        var existing = await _context.ReportFolders.FindAsync(folder.Id);
        if (existing == null)
            return false;

        if (!await CanEditFolderAsync(folder.Id, userId))
            return false;

        existing.Name = folder.Name;
        existing.Description = folder.Description;
        existing.AccessType = folder.AccessType;
        existing.Color = folder.Color;
        existing.Icon = folder.Icon;
        existing.DisplayOrder = folder.DisplayOrder;
        existing.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteFolderAsync(int folderId, string userId)
    {
        var folder = await _context.ReportFolders
            .Include(f => f.Reports)
            .Include(f => f.SubFolders)
            .FirstOrDefaultAsync(f => f.Id == folderId);

        if (folder == null)
            return false;

        if (folder.CreatedByUserId != userId)
            return false;

        if (folder.Reports.Any())
        {
            foreach (var report in folder.Reports)
            {
                report.FolderId = null;
            }
        }

        if (folder.SubFolders.Any())
        {
            foreach (var subFolder in folder.SubFolders)
            {
                subFolder.ParentFolderId = folder.ParentFolderId;
            }
        }

        _context.ReportFolders.Remove(folder);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanAccessFolderAsync(int folderId, string userId)
    {
        var folder = await _context.ReportFolders
            .Include(f => f.FolderShares)
            .FirstOrDefaultAsync(f => f.Id == folderId);

        if (folder == null)
            return false;

        if (folder.CreatedByUserId == userId)
            return true;

        if (folder.AccessType == FolderAccessType.Public)
            return true;

        var userGroupIds = await _context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return folder.FolderShares.Any(fs =>
            fs.UserId == userId ||
            (fs.GroupId.HasValue && userGroupIds.Contains(fs.GroupId.Value)));
    }

    public async Task<bool> CanEditFolderAsync(int folderId, string userId)
    {
        var folder = await _context.ReportFolders
            .Include(f => f.FolderShares)
            .FirstOrDefaultAsync(f => f.Id == folderId);

        if (folder == null)
            return false;

        if (folder.CreatedByUserId == userId)
            return true;

        var userGroupIds = await _context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return folder.FolderShares.Any(fs =>
            (fs.UserId == userId || (fs.GroupId.HasValue && userGroupIds.Contains(fs.GroupId.Value))) &&
            (fs.PermissionLevel == FolderPermissionLevel.Edit || fs.PermissionLevel == FolderPermissionLevel.Manage));
    }

    public async Task<bool> ShareFolderAsync(int folderId, string userId, string? targetUserId, int? targetGroupId, FolderPermissionLevel permissionLevel)
    {
        var folder = await _context.ReportFolders.FindAsync(folderId);
        if (folder == null || folder.CreatedByUserId != userId)
            return false;

        if (string.IsNullOrEmpty(targetUserId) && !targetGroupId.HasValue)
            return false;

        var existingShare = await _context.ReportFolderShares
            .FirstOrDefaultAsync(fs => fs.ReportFolderId == folderId &&
                                      fs.UserId == targetUserId &&
                                      fs.GroupId == targetGroupId);

        if (existingShare != null)
        {
            existingShare.PermissionLevel = permissionLevel;
        }
        else
        {
            var share = new ReportFolderShare
            {
                ReportFolderId = folderId,
                TargetType = targetUserId != null ? ShareTargetType.User : ShareTargetType.Group,
                UserId = targetUserId,
                GroupId = targetGroupId,
                PermissionLevel = permissionLevel,
                SharedByUserId = userId,
                SharedAt = DateTime.UtcNow
            };

            _context.ReportFolderShares.Add(share);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveShareAsync(int shareId, string userId)
    {
        var share = await _context.ReportFolderShares
            .Include(fs => fs.ReportFolder)
            .FirstOrDefaultAsync(fs => fs.Id == shareId);

        if (share == null || share.ReportFolder.CreatedByUserId != userId)
            return false;

        _context.ReportFolderShares.Remove(share);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ReportFolderShare>> GetFolderSharesAsync(int folderId, string userId)
    {
        var folder = await _context.ReportFolders.FindAsync(folderId);
        if (folder == null || folder.CreatedByUserId != userId)
            return new List<ReportFolderShare>();

        return await _context.ReportFolderShares
            .Include(fs => fs.User)
            .Include(fs => fs.Group)
            .Where(fs => fs.ReportFolderId == folderId)
            .ToListAsync();
    }
}
