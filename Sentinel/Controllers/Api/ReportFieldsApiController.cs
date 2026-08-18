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
[Authorize(Policy = "Permission.Report.View")]
[ApiController]
[Route("api/reporting/fields")]
[EnableRateLimiting("lookup-api")] // 200 per minute - field metadata
public class ReportFieldsApiController : ControllerBase
{
    private readonly IReportFieldMetadataService _fieldMetadataService;
    private readonly ILogger<ReportFieldsApiController> _logger;

    public ReportFieldsApiController(
        IReportFieldMetadataService fieldMetadataService,
        ILogger<ReportFieldsApiController> logger)
    {
        _fieldMetadataService = fieldMetadataService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available fields for an entity type
    /// GET: /api/reporting/fields/{entityType}?context=Report
    /// </summary>
    [HttpGet("{entityType}")]
    public async Task<ActionResult<List<ReportFieldMetadata>>> GetFields(
        string entityType,
        [FromQuery] FieldUsageContext context = FieldUsageContext.Report)
    {
        try
        {
            var fields = await _fieldMetadataService.GetFieldsForEntityAsync(entityType, excludeNavigationFields: false, context: context);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return MetadataError(ex, "available fields", entityType);
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
            return MetadataError(ex, "grouped fields", entityType);
        }
    }

    /// <summary>
    /// Get recommended (core) fields for an entity type
    /// GET: /api/reporting/fields/{entityType}/recommended
    /// </summary>
    [HttpGet("{entityType}/recommended")]
    public async Task<ActionResult<List<ReportFieldMetadata>>> GetRecommendedFields(string entityType)
    {
        try
        {
            var fields = await _fieldMetadataService.GetRecommendedFieldsAsync(entityType);
            return Ok(fields);
        }
        catch (Exception ex)
        {
            return MetadataError(ex, "recommended fields", entityType);
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
            return MetadataError(ex, "custom fields", entityType);
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
            return MetadataError(ex, "field validation", request.EntityType);
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
            return MetadataError(ex, "suggested aggregations", dataType);
        }
    }

    private ObjectResult MetadataError(Exception exception, string operation, string subject)
    {
        _logger.LogError(exception, "Unable to load report {Operation} for {Subject}", operation, subject);
        return StatusCode(500, new
        {
            error = "Report field metadata could not be loaded. Please try again.",
            traceId = HttpContext.TraceIdentifier
        });
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
