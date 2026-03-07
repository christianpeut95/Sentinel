using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using System.Security.Claims;

namespace Sentinel.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/reports")]
[EnableRateLimiting("workflow-api-moderate")] // 60 per minute - report management
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReport(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == userId);

        if (report == null)
            return NotFound();

        _context.ReportDefinitions.Remove(report);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateReport(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var original = await _context.ReportDefinitions
            .Include(r => r.Fields)
            .Include(r => r.Filters)
            .Include(r => r.CalculatedFields)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (original == null)
            return NotFound();

        var duplicate = new ReportDefinition
        {
            Name = $"{original.Name} (Copy)",
            Description = original.Description,
            EntityType = original.EntityType,
            Category = original.Category,
            PivotConfiguration = original.PivotConfiguration,
            CollectionQueriesJson = original.CollectionQueriesJson,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsPublic = false,
            IsTemplate = false,
            FolderId = original.FolderId
        };

        _context.ReportDefinitions.Add(duplicate);
        await _context.SaveChangesAsync();

        // Copy fields
        foreach (var field in original.Fields)
        {
            _context.ReportFields.Add(new ReportField
            {
                ReportDefinitionId = duplicate.Id,
                FieldPath = field.FieldPath,
                DisplayName = field.DisplayName,
                DataType = field.DataType,
                PivotArea = field.PivotArea,
                AggregationType = field.AggregationType,
                DisplayOrder = field.DisplayOrder
            });
        }

        // Copy filters
        foreach (var filter in original.Filters)
        {
            _context.ReportFilters.Add(new ReportFilter
            {
                ReportDefinitionId = duplicate.Id,
                FieldPath = filter.FieldPath,
                Operator = filter.Operator,
                Value = filter.Value,
                DisplayOrder = filter.DisplayOrder
            });
        }

        // Copy calculated fields
        foreach (var calc in original.CalculatedFields)
        {
            _context.CalculatedFields.Add(new CalculatedField
            {
                ReportDefinitionId = duplicate.Id,
                Name = calc.Name,
                Expression = calc.Expression,
                DataType = calc.DataType,
                Description = calc.Description
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { id = duplicate.Id });
    }

    [HttpPut("{id}/folder")]
    public async Task<IActionResult> MoveToFolder(int id, [FromBody] MoveToFolderRequest request)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == userId);

        if (report == null)
            return NotFound();

        report.FolderId = request.FolderId;
        report.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}/folder")]
    public async Task<IActionResult> RemoveFromFolder(int id)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var report = await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == userId);

        if (report == null)
            return NotFound();

        report.FolderId = null;
        report.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchReports([FromQuery] string query)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var reports = await _context.ReportDefinitions
            .Where(r => r.CreatedByUserId == userId || r.IsPublic)
            .Where(r => r.Name.Contains(query) || 
                       (r.Description != null && r.Description.Contains(query)))
            .OrderByDescending(r => r.ModifiedAt ?? r.CreatedAt)
            .Take(10)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Category,
                r.EntityType,
                FolderName = r.Folder != null ? r.Folder.Name : null
            })
            .ToListAsync();

        return Ok(reports);
    }
}

public class MoveToFolderRequest
{
    public int? FolderId { get; set; }
}
