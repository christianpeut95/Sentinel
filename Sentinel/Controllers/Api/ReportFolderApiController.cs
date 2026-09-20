using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sentinel.Controllers.Api;

[Authorize(Policy = "Permission.Report.Edit")]
[ApiController]
[Route("api/reports/folders")]
[EnableRateLimiting("workflow-api-moderate")] // 60 per minute - folder management
public class ReportFolderApiController : ControllerBase
{
    private readonly IReportFolderService _folderService;

    public ReportFolderApiController(IReportFolderService folderService)
    {
        _folderService = folderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFolders()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var myFolders = await _folderService.GetUserFoldersAsync(userId);
        var sharedFolders = await _folderService.GetSharedFoldersAsync(userId);

        return Ok(new
        {
            myFolders = myFolders.Select(ToFolderResponse),
            sharedFolders = sharedFolders.Select(ToFolderResponse)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFolder(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var folder = await _folderService.GetFolderByIdAsync(id, userId);
        if (folder == null)
            return NotFound();

        return Ok(ToFolderResponse(folder));
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] CreateReportFolderRequest request)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var folder = new ReportFolder
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            ParentFolderId = request.ParentFolderId,
            AccessType = request.AccessType,
            Color = request.Color,
            Icon = request.Icon
        };

        var created = await _folderService.CreateFolderAsync(folder, userId);
        if (created == null)
            return Forbid();

        return CreatedAtAction(nameof(GetFolder), new { id = created.Id }, ToFolderResponse(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(int id, [FromBody] UpdateReportFolderRequest request)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var folder = new ReportFolder
        {
            Id = id,
            Name = request.Name.Trim(),
            Description = request.Description,
            Color = request.Color,
            Icon = request.Icon
        };

        var result = await _folderService.UpdateFolderAsync(folder, userId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _folderService.DeleteFolderAsync(id, userId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareFolder(int id, [FromBody] ShareFolderRequest request)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _folderService.ShareFolderAsync(
            id,
            userId,
            request.UserId,
            request.GroupId,
            request.PermissionLevel
        );

        if (!result)
            return BadRequest("Failed to share folder");

        return NoContent();
    }

    [HttpGet("{id}/shares")]
    public async Task<IActionResult> GetFolderShares(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var shares = await _folderService.GetFolderSharesAsync(id, userId);
        return Ok(shares.Select(ToShareResponse));
    }

    [HttpDelete("shares/{shareId}")]
    public async Task<IActionResult> RemoveShare(int shareId)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _folderService.RemoveShareAsync(shareId, userId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    // Folder services materialise navigation collections for page workflows.
    // Do not serialise that EF graph: it can expose complete report definitions,
    // sharing/audit metadata and Identity user fields unrelated to folder display.
    private static ReportFolderResponse ToFolderResponse(ReportFolder folder) => new(
        folder.Id,
        folder.Name,
        folder.Description,
        folder.ParentFolderId,
        folder.CreatedAt,
        folder.ModifiedAt,
        folder.AccessType,
        folder.SafeColor,
        folder.SafeIcon,
        folder.DisplayOrder,
        folder.Reports.Count,
        folder.SubFolders
            .OrderBy(child => child.DisplayOrder)
            .ThenBy(child => child.Name)
            .Select(child => new ReportFolderReferenceResponse(child.Id, child.Name))
            .ToList());

    private static ReportFolderShareResponse ToShareResponse(ReportFolderShare share) => new(
        share.Id,
        share.TargetType,
        share.PermissionLevel,
        share.SharedAt,
        share.User is null
            ? null
            : new ReportFolderUserResponse(
                share.User.Id,
                string.IsNullOrWhiteSpace($"{share.User.FirstName} {share.User.LastName}".Trim())
                    ? share.User.UserName ?? "Unknown user"
                    : $"{share.User.FirstName} {share.User.LastName}".Trim()),
        share.Group is null
            ? null
            : new ReportFolderGroupResponse(share.Group.Id, share.Group.Name));
}

public sealed record ReportFolderResponse(
    int Id,
    string Name,
    string? Description,
    int? ParentFolderId,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    FolderAccessType AccessType,
    string? Color,
    string Icon,
    int DisplayOrder,
    int ReportCount,
    IReadOnlyList<ReportFolderReferenceResponse> SubFolders);

public sealed record ReportFolderReferenceResponse(int Id, string Name);

public sealed record ReportFolderShareResponse(
    int Id,
    ShareTargetType TargetType,
    FolderPermissionLevel PermissionLevel,
    DateTime SharedAt,
    ReportFolderUserResponse? User,
    ReportFolderGroupResponse? Group);

public sealed record ReportFolderUserResponse(string Id, string DisplayName);

public sealed record ReportFolderGroupResponse(int Id, string Name);

public class ShareFolderRequest
{
    public string? UserId { get; set; }
    public int? GroupId { get; set; }
    public FolderPermissionLevel PermissionLevel { get; set; }
}

public sealed class CreateReportFolderRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? ParentFolderId { get; set; }

    [EnumDataType(typeof(FolderAccessType))]
    public FolderAccessType AccessType { get; set; } = FolderAccessType.Private;

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Folder colour must be a six-digit hex value.")]
    public string? Color { get; set; }

    [RegularExpression("^(bi-folder|bi-folder-fill|bi-archive|bi-bar-chart|bi-graph-up|bi-star|bi-heart)$", ErrorMessage = "Folder icon is not supported.")]
    public string? Icon { get; set; }
}

public sealed class UpdateReportFolderRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Folder colour must be a six-digit hex value.")]
    public string? Color { get; set; }

    [RegularExpression("^(bi-folder|bi-folder-fill|bi-archive|bi-bar-chart|bi-graph-up|bi-star|bi-heart)$", ErrorMessage = "Folder icon is not supported.")]
    public string? Icon { get; set; }
}
