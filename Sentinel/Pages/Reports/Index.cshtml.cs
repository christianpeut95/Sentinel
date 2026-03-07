using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Security.Claims;

namespace Sentinel.Pages.Reports;

[Authorize(Policy = "Permission.Report.View")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReportFolderService _folderService;

    public IndexModel(ApplicationDbContext context, IReportFolderService folderService)
    {
        _context = context;
        _folderService = folderService;
    }

    public List<ReportFolder> MyFolders { get; set; } = new();
    public List<ReportFolder> SharedFolders { get; set; } = new();
    public List<ReportDefinition> MyReports { get; set; } = new();
    public List<ReportDefinition> PublicReports { get; set; } = new();
    public List<ReportDefinition> UnfiledReports { get; set; } = new();
    public int? CurrentFolderId { get; set; }
    public ReportFolder? CurrentFolder { get; set; }
    public string? CategoryFilter { get; set; }
    public string? SearchQuery { get; set; }
    public string ViewMode { get; set; } = "cards";

    public async Task OnGetAsync(int? folderId, string? category, string? search, string? view)
    {
        CategoryFilter = category;
        SearchQuery = search;
        CurrentFolderId = folderId;
        ViewMode = view ?? "cards";

        // Get current user - use Name (email) not Id (GUID)
        var currentUserId = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        // Load folders with reports included
        MyFolders = await _folderService.GetUserFoldersAsync(currentUserId);
        SharedFolders = await _folderService.GetSharedFoldersAsync(currentUserId);

        if (CurrentFolderId.HasValue)
        {
            CurrentFolder = await _folderService.GetFolderByIdAsync(CurrentFolderId.Value, currentUserId);
        }

        // Query for reports with proper eager loading
        var query = _context.ReportDefinitions
            .Include(r => r.Fields)
            .Include(r => r.Folder)
            .AsQueryable();

        // If viewing a specific folder, only show reports in that folder
        if (CurrentFolderId.HasValue)
        {
            query = query.Where(r => r.FolderId == CurrentFolderId.Value);
        }
        else
        {
            // If viewing "All Reports", show reports in folders AND unfiled
            // but exclude them from the unfiled section
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(r => r.Category == category);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Name.Contains(search) || 
                                    (r.Description != null && r.Description.Contains(search)));
        }

        // Get all reports (already filtered by folder if CurrentFolderId is set)
        var allReports = await query
            .OrderByDescending(r => r.ModifiedAt ?? r.CreatedAt)
            .ToListAsync();

        // Split into categories based on view context
        if (CurrentFolderId.HasValue)
        {
            // When viewing a specific folder, show only reports in that folder
            MyReports = allReports
                .Where(r => r.CreatedByUserId == currentUserId && !r.IsPublic)
                .ToList();
            
            PublicReports = allReports
                .Where(r => r.IsPublic && r.CreatedByUserId != currentUserId)
                .ToList();
            
            UnfiledReports = new List<ReportDefinition>();
        }
        else
        {
            // When viewing "All Reports", split into filed and unfiled
            MyReports = allReports
                .Where(r => r.CreatedByUserId == currentUserId && !r.IsPublic && r.FolderId != null)
                .ToList();
            
            UnfiledReports = allReports
                .Where(r => r.CreatedByUserId == currentUserId && r.FolderId == null)
                .ToList();
            
            PublicReports = allReports
                .Where(r => r.IsPublic && r.CreatedByUserId != currentUserId)
                .ToList();
        }
    }
}
