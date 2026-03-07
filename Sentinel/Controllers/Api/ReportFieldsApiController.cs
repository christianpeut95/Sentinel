using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Controllers.Api;

/// <summary>
/// API endpoints for report field discovery
/// Used by report builder UI to get available fields
/// </summary>
[Authorize]
[ApiController]
[Route("api/reporting/fields")]
[EnableRateLimiting("lookup-api")] // 200 per minute - field metadata
public class ReportFieldsApiController : ControllerBase
{
    private readonly IReportFieldMetadataService _fieldMetadataService;

    public ReportFieldsApiController(IReportFieldMetadataService fieldMetadataService)
    {
        _fieldMetadataService = fieldMetadataService;
    }

    /// <summary>
    /// Get all available fields for an entity type
    /// GET: /api/reporting/fields/{entityType}
    /// </summary>
    [HttpGet("{entityType}")]
    public async Task<ActionResult<List<ReportFieldMetadata>>> GetFields(string entityType)
    {
        try
        {
            var fields = await _fieldMetadataService.GetFieldsForEntityAsync(entityType);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get fields grouped by category
    /// GET: /api/reporting/fields/{entityType}/grouped
    /// </summary>
    [HttpGet("{entityType}/grouped")]
    public async Task<ActionResult<Dictionary<string, List<ReportFieldMetadata>>>> GetFieldsGrouped(string entityType)
    {
        try
        {
            var fields = await _fieldMetadataService.GetFieldsByCategoryAsync(entityType);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get only custom fields for an entity
    /// GET: /api/reporting/fields/{entityType}/custom
    /// </summary>
    [HttpGet("{entityType}/custom")]
    public async Task<ActionResult<List<ReportFieldMetadata>>> GetCustomFields(string entityType)
    {
        try
        {
            var fields = await _fieldMetadataService.GetCustomFieldsForEntityAsync(entityType);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate a field path
    /// POST: /api/reporting/fields/validate
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<bool>> ValidateFieldPath([FromBody] FieldValidationRequest request)
    {
        try
        {
            var isValid = await _fieldMetadataService.ValidateFieldPathAsync(
                request.EntityType, 
                request.FieldPath);
            return Ok(new { isValid, fieldPath = request.FieldPath });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get suggested aggregations for a data type
    /// GET: /api/reporting/fields/aggregations/{dataType}
    /// </summary>
    [HttpGet("aggregations/{dataType}")]
    public ActionResult<List<string>> GetSuggestedAggregations(string dataType)
    {
        try
        {
            var aggregations = _fieldMetadataService.GetSuggestedAggregations(dataType);
            return Ok(aggregations);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for field validation
/// </summary>
public class FieldValidationRequest
{
    public string EntityType { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
}
