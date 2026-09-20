using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Controllers;

[Authorize(Policy = "Permission.Outbreak.View")]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("bulk-export")] // Default: strict limit for data exports
public class LineListController : ControllerBase
{
    private readonly ILineListService _lineListService;
    private readonly IOutbreakAccessService _outbreakAccessService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LineListController> _logger;
    
    public LineListController(
        ILineListService lineListService,
        IOutbreakAccessService outbreakAccessService,
        ApplicationDbContext context,
        ILogger<LineListController> logger)
    {
        _lineListService = lineListService;
        _outbreakAccessService = outbreakAccessService;
        _context = context;
        _logger = logger;
    }
    
    [HttpGet("fields/{outbreakId}")]
    [EnableRateLimiting("lookup-api")] // Override: field metadata is lookup data
    public async Task<IActionResult> GetAvailableFields(int outbreakId)
    {
        try
        {
            if (!await _outbreakAccessService.CanAccessOutbreakAsync(outbreakId))
            {
                return NotFound();
            }

            var fields = await _lineListService.GetAvailableFieldsAsync(outbreakId);
            return Ok(fields.Select(ToFieldResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available fields for outbreak {OutbreakId}", outbreakId);
            return StatusCode(500, new { error = "Failed to retrieve fields" });
        }
    }
    
    [HttpPost("data")]
    public async Task<IActionResult> GetLineListData([FromBody] LineListDataRequest request)
    {
        try
        {
            if (!await _outbreakAccessService.CanAccessOutbreakAsync(request.OutbreakId))
            {
                return NotFound();
            }

            if (!await ContainsOnlyConfiguredFieldsAsync(request.OutbreakId, request.FieldPaths))
            {
                return BadRequest(new { error = "One or more selected fields are not available for this outbreak." });
            }

            var data = await _lineListService.GetLineListDataAsync(
                request.OutbreakId, 
                request.FieldPaths, 
                request.SortConfig,
                request.FilterConfig);
            
            // Log for debugging
            _logger.LogInformation("Returning {Count} line list rows with {FieldCount} fields", 
                data.Count, request.FieldPaths.Count);
            
            return Ok(data.Select(ToDataResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving line list data for outbreak {OutbreakId}", request.OutbreakId);
            return StatusCode(500, new { error = "Failed to retrieve data" });
        }
    }
    
    [HttpGet("configurations/{outbreakId}")]
    [EnableRateLimiting("lookup-api")] // Override: configuration metadata is lookup data
    public async Task<IActionResult> GetConfigurations(int outbreakId)
    {
        try
        {
            if (!await _outbreakAccessService.CanAccessOutbreakAsync(outbreakId))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userConfigs = await _lineListService.GetUserConfigurationsAsync(outbreakId, userId);
            var sharedConfigs = await _lineListService.GetSharedConfigurationsAsync(outbreakId);
            
            return Ok(new LineListConfigurationsResponse(
                userConfigs.Select(ToConfigurationResponse).ToList(),
                sharedConfigs.Select(ToConfigurationResponse).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for outbreak {OutbreakId}", outbreakId);
            return StatusCode(500, new { error = "Failed to retrieve configurations" });
        }
    }
    
    [HttpPost("configurations")]
    public async Task<IActionResult> SaveConfiguration([FromBody] SaveLineListConfigurationRequest request)
    {
        try
        {
            if (request.OutbreakId <= 0 || !await _outbreakAccessService.CanAccessOutbreakAsync(request.OutbreakId))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            _logger.LogInformation("Saving line-list configuration for outbreak {OutbreakId}", request.OutbreakId);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            OutbreakLineListConfiguration config;
            
            // If creating new, set user ID and creator
            if (request.Id == 0)
            {
                config = new OutbreakLineListConfiguration
                {
                    OutbreakId = request.OutbreakId,
                    Name = request.Name,
                    Description = request.Description,
                    SelectedFields = request.SelectedFields,
                    SortConfiguration = request.SortConfiguration,
                    FilterConfiguration = request.FilterConfiguration,
                    IsShared = request.IsShared,
                    IsDefault = request.IsDefault,
                    UserId = request.IsShared ? null : userId,
                    CreatedByUserId = userId
                };
                _logger.LogInformation("Creating new configuration for user {UserId}", userId);
            }
            // If updating, verify ownership
            else
            {
                var existing = await _context.OutbreakLineListConfigurations
                    .FirstOrDefaultAsync(c => c.Id == request.Id);

                if (existing is null)
                {
                    return NotFound();
                }

                if (existing.OutbreakId != request.OutbreakId)
                {
                    return BadRequest(new { error = "The configuration does not belong to the selected outbreak." });
                }

                if (existing.UserId != userId && existing.CreatedByUserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update configuration {ConfigId} they don't own", 
                        userId, request.Id);
                    return Forbid();
                }

                // Update only the fields the owner is allowed to change. Do
                // not bind ownership, outbreak, or audit fields from the HTTP
                // payload.
                existing.Name = request.Name;
                existing.Description = request.Description;
                existing.SelectedFields = request.SelectedFields;
                existing.SortConfiguration = request.SortConfiguration;
                existing.FilterConfiguration = request.FilterConfiguration;
                existing.IsShared = request.IsShared;
                existing.IsDefault = request.IsDefault;
                config = existing;
            }
            
            var saved = await _lineListService.SaveConfigurationAsync(config);
            _logger.LogInformation("Configuration saved successfully with ID {ConfigId}", saved.Id);
            return Ok(ToConfigurationResponse(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving line list configuration for outbreak {OutbreakId}", request.OutbreakId);
            return StatusCode(500, new
            {
                error = "The line-list configuration could not be saved. Please try again.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    private static LineListConfigurationResponse ToConfigurationResponse(OutbreakLineListConfiguration config) => new(
        config.Id,
        config.Name,
        config.Description,
        config.SelectedFields,
        config.SortConfiguration,
        config.FilterConfiguration,
        config.IsShared,
        config.IsDefault,
        config.CreatedByUser is null ? null : new LineListConfigurationCreatorResponse(config.CreatedByUser.UserName));

    private static LineListFieldResponse ToFieldResponse(LineListField field) => new(
        field.FieldPath,
        field.DisplayName,
        field.Category,
        field.DataType,
        field.IsSortable,
        field.IsFilterable);

    private static LineListDataResponse ToDataResponse(LineListDataRow row) => new(
        row.CaseId,
        row.OutbreakCaseId,
        row.Values);
    
    [HttpDelete("configurations/{id}")]
    public async Task<IActionResult> DeleteConfiguration(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var config = await _context.OutbreakLineListConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (config is null || !await _outbreakAccessService.CanAccessOutbreakAsync(config.OutbreakId))
            {
                return NotFound();
            }

            if (config.UserId != userId && config.CreatedByUserId != userId)
            {
                return Forbid();
            }

            var success = await _lineListService.DeleteConfigurationAsync(id, userId);
            
            if (!success)
            {
                return NotFound();
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {ConfigId}", id);
            return StatusCode(500, new { error = "Failed to delete configuration" });
        }
    }
    
    [HttpPost("configurations/{id}/set-default")]
    public async Task<IActionResult> SetDefaultConfiguration(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var config = await _context.OutbreakLineListConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (config is null || !await _outbreakAccessService.CanAccessOutbreakAsync(config.OutbreakId))
            {
                return NotFound();
            }

            var success = await _lineListService.SetDefaultConfigurationAsync(id, userId);
            
            if (!success)
            {
                return NotFound();
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default configuration {ConfigId}", id);
            return StatusCode(500, new { error = "Failed to set default" });
        }
    }
    
    [HttpPost("export")]
    [Authorize(Policy = "Permission.Outbreak.Export")]
    public async Task<IActionResult> ExportToCsv([FromBody] LineListExportRequest request)
    {
        try
        {
            if (!await _outbreakAccessService.CanAccessOutbreakAsync(request.OutbreakId))
            {
                return NotFound();
            }

            if (!await ContainsOnlyConfiguredFieldsAsync(request.OutbreakId, request.FieldPaths))
            {
                return BadRequest(new { error = "One or more selected fields are not available for this outbreak." });
            }

            var csvData = await _lineListService.ExportToCsvAsync(
                request.OutbreakId,
                request.FieldPaths,
                request.SortConfig);
            
            var fileName = $"outbreak-linelist-{request.OutbreakId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            
            return File(csvData, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting line list for outbreak {OutbreakId}", request.OutbreakId);
            return StatusCode(500, new { error = "Failed to export data" });
        }
    }

    private async Task<bool> ContainsOnlyConfiguredFieldsAsync(int outbreakId, IEnumerable<string>? fieldPaths)
    {
        if (fieldPaths is null)
        {
            return false;
        }

        var allowedFieldPaths = (await _lineListService.GetAvailableFieldsAsync(outbreakId))
            .Select(field => field.FieldPath)
            .ToHashSet(StringComparer.Ordinal);

        return fieldPaths.All(allowedFieldPaths.Contains);
    }
}

public class LineListDataRequest
{
    public int OutbreakId { get; set; }
    public List<string> FieldPaths { get; set; } = new();
    public string? SortConfig { get; set; }
    public string? FilterConfig { get; set; }
}

public sealed class SaveLineListConfigurationRequest
{
    public int Id { get; init; }
    public int OutbreakId { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Description { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    public string SelectedFields { get; init; } = "[]";

    public string SortConfiguration { get; init; } = "[]";
    public string? FilterConfiguration { get; init; }
    public bool IsShared { get; init; }
    public bool IsDefault { get; init; }
}

public sealed record LineListConfigurationResponse(
    int Id,
    string Name,
    string? Description,
    string SelectedFields,
    string SortConfiguration,
    string? FilterConfiguration,
    bool IsShared,
    bool IsDefault,
    LineListConfigurationCreatorResponse? CreatedByUser);

public sealed record LineListConfigurationCreatorResponse(string? UserName);

public sealed record LineListConfigurationsResponse(
    IReadOnlyList<LineListConfigurationResponse> UserConfigurations,
    IReadOnlyList<LineListConfigurationResponse> SharedConfigurations);

public sealed record LineListFieldResponse(
    string FieldPath,
    string DisplayName,
    string Category,
    string DataType,
    bool IsSortable,
    bool IsFilterable);

public sealed record LineListDataResponse(
    Guid CaseId,
    int OutbreakCaseId,
    IReadOnlyDictionary<string, object?> Values);

public class LineListExportRequest
{
    public int OutbreakId { get; set; }
    public List<string> FieldPaths { get; set; } = new();
    public string? SortConfig { get; set; }
}
