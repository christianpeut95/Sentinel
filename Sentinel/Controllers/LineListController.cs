using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("bulk-export")] // Default: strict limit for data exports
public class LineListController : ControllerBase
{
    private readonly ILineListService _lineListService;
    private readonly ILogger<LineListController> _logger;
    
    public LineListController(ILineListService lineListService, ILogger<LineListController> logger)
    {
        _lineListService = lineListService;
        _logger = logger;
    }
    
    [HttpGet("fields/{outbreakId}")]
    [EnableRateLimiting("lookup-api")] // Override: field metadata is lookup data
    public async Task<IActionResult> GetAvailableFields(int outbreakId)
    {
        try
        {
            var fields = await _lineListService.GetAvailableFieldsAsync(outbreakId);
            return Ok(fields);
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
            var data = await _lineListService.GetLineListDataAsync(
                request.OutbreakId, 
                request.FieldPaths, 
                request.SortConfig,
                request.FilterConfig);
            
            // Log for debugging
            _logger.LogInformation("Returning {Count} line list rows with {FieldCount} fields", 
                data.Count, request.FieldPaths.Count);
            
            return Ok(data);
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userConfigs = await _lineListService.GetUserConfigurationsAsync(outbreakId, userId);
            var sharedConfigs = await _lineListService.GetSharedConfigurationsAsync(outbreakId);
            
            return Ok(new
            {
                userConfigurations = userConfigs,
                sharedConfigurations = sharedConfigs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for outbreak {OutbreakId}", outbreakId);
            return StatusCode(500, new { error = "Failed to retrieve configurations" });
        }
    }
    
    [HttpPost("configurations")]
    public async Task<IActionResult> SaveConfiguration([FromBody] OutbreakLineListConfiguration config)
    {
        try
        {
            _logger.LogInformation("Attempting to save configuration: {ConfigName} for outbreak {OutbreakId}", 
                config.Name, config.OutbreakId);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            // If creating new, set user ID and creator
            if (config.Id == 0)
            {
                config.UserId = config.IsShared ? null : userId;
                config.CreatedByUserId = userId;
                _logger.LogInformation("Creating new configuration for user {UserId}", userId);
            }
            // If updating, verify ownership
            else
            {
                var existing = await _lineListService.GetUserConfigurationsAsync(config.OutbreakId, userId);
                if (!existing.Any(c => c.Id == config.Id))
                {
                    _logger.LogWarning("User {UserId} attempted to update configuration {ConfigId} they don't own", 
                        userId, config.Id);
                    return Forbid();
                }
            }
            
            var saved = await _lineListService.SaveConfigurationAsync(config);
            _logger.LogInformation("Configuration saved successfully with ID {ConfigId}", saved.Id);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving line list configuration: {Message}", ex.Message);
            return StatusCode(500, new { error = $"Failed to save configuration: {ex.Message}" });
        }
    }
    
    [HttpDelete("configurations/{id}")]
    public async Task<IActionResult> DeleteConfiguration(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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
    public async Task<IActionResult> ExportToCsv([FromBody] LineListExportRequest request)
    {
        try
        {
            var csvData = await _lineListService.ExportToCsvAsync(
                request.OutbreakId,
                request.FieldPaths,
                request.SortConfig);
            
            var fileName = $"outbreak-linelist-{request.OutbreakId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            
            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting line list for outbreak {OutbreakId}", request.OutbreakId);
            return StatusCode(500, new { error = "Failed to export data" });
        }
    }
}

public class LineListDataRequest
{
    public int OutbreakId { get; set; }
    public List<string> FieldPaths { get; set; } = new();
    public string? SortConfig { get; set; }
    public string? FilterConfig { get; set; }
}

public class LineListExportRequest
{
    public int OutbreakId { get; set; }
    public List<string> FieldPaths { get; set; } = new();
    public string? SortConfig { get; set; }
}
