using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Security.Claims;

namespace Sentinel.Controllers.Api;

[Authorize]
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
            myFolders,
            sharedFolders
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

        return Ok(folder);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] ReportFolder folder)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _folderService.CreateFolderAsync(folder, userId);
        return CreatedAtAction(nameof(GetFolder), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(int id, [FromBody] ReportFolder folder)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (id != folder.Id)
            return BadRequest();

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
        return Ok(shares);
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
}

public class ShareFolderRequest
{
    public string? UserId { get; set; }
    public int? GroupId { get; set; }
    public FolderPermissionLevel PermissionLevel { get; set; }
}
