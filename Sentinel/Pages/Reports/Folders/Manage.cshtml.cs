using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Security.Claims;

namespace Sentinel.Pages.Reports.Folders;

[Authorize(Policy = "Permission.Report.Edit")]
public class ManageModel : PageModel
{
    private readonly IReportFolderService _folderService;
    private readonly ApplicationDbContext _context;

    public ManageModel(IReportFolderService folderService, ApplicationDbContext context)
    {
        _folderService = folderService;
        _context = context;
    }

    public ReportFolder Folder { get; set; } = null!;
    public List<ReportFolderShare> Shares { get; set; } = new();
    public List<Group> AvailableGroups { get; set; } = new();
    public List<ApplicationUser> AvailableUsers { get; set; } = new();

    [BindProperty]
    public int FolderId { get; set; }

    [BindProperty]
    public string? ShareWithUserId { get; set; }

    [BindProperty]
    public int? ShareWithGroupId { get; set; }

    [BindProperty]
    public FolderPermissionLevel PermissionLevel { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        Folder = await _folderService.GetFolderByIdAsync(id, userId);
        if (Folder == null || Folder.CreatedByUserId != userId)
            return NotFound();

        Shares = await _folderService.GetFolderSharesAsync(id, userId);
        AvailableGroups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
        AvailableUsers = await _context.Users
            .Where(u => u.Email != userId)
            .OrderBy(u => u.Email)
            .ToListAsync();

        FolderId = id;

        return Page();
    }

    public async Task<IActionResult> OnPostAddShareAsync()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrEmpty(ShareWithUserId) && !ShareWithGroupId.HasValue)
        {
            ModelState.AddModelError("", "Please select a user or group to share with");
            return await OnGetAsync(FolderId);
        }

        var result = await _folderService.ShareFolderAsync(
            FolderId,
            userId,
            ShareWithUserId,
            ShareWithGroupId,
            PermissionLevel
        );

        if (!result)
        {
            ModelState.AddModelError("", "Failed to share folder");
            return await OnGetAsync(FolderId);
        }

        return RedirectToPage(new { id = FolderId });
    }

    public async Task<IActionResult> OnPostRemoveShareAsync(int shareId)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _folderService.RemoveShareAsync(shareId, userId);
        if (!result)
        {
            ModelState.AddModelError("", "Failed to remove share");
        }

        return RedirectToPage(new { id = FolderId });
    }
}
