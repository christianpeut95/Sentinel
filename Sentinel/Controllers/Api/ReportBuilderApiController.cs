using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using Sentinel.DTOs;

namespace Sentinel.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/reports")]
[EnableRateLimiting("bulk-export-moderate")] // 20 per hour - report building/testing
public class ReportBuilderApiController : ControllerBase
{
    private readonly IReportFieldMetadataService _fieldMetadataService;
    private readonly IReportDataService _reportDataService;
    private readonly ICollectionMetadataService _collectionMetadataService;
    private readonly ApplicationDbContext _context;

    public ReportBuilderApiController(
        IReportFieldMetadataService fieldMetadataService,
        IReportDataService reportDataService,
        ICollectionMetadataService collectionMetadataService,
        ApplicationDbContext context)
    {
        _fieldMetadataService = fieldMetadataService;
        _reportDataService = reportDataService;
        _collectionMetadataService = collectionMetadataService;
        _context = context;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewReport([FromBody] PreviewReportRequest request)
    {
        try
        {
            // Debug logging - log the incoming entity type
            Console.WriteLine($"============ PREVIEW REQUEST ============");
            Console.WriteLine($"Entity Type: {request?.EntityType}");
            Console.WriteLine($"Fields Count: {request?.Fields?.Count}");
            Console.WriteLine($"Collection Queries Count: {request?.CollectionQueries?.Count}");
            Console.WriteLine($"==========================================");

            // Validate request
            if (request == null)
            {
                return BadRequest(new { success = false, error = "Request is null" });
            }

            if (string.IsNullOrEmpty(request.EntityType))
            {
                return BadRequest(new { success = false, error = "Entity type is required" });
            }

            if (request.Fields == null || !request.Fields.Any())
            {
                return BadRequest(new { success = false, error = "At least one field is required" });
            }

            // Validate and fix field paths
            var correctedFields = await ValidateAndCorrectFieldPaths(request.EntityType, request.Fields);

            // Build temporary report definition
            var reportDef = new ReportDefinition
            {
                Name = "Preview",
                EntityType = request.EntityType,
                Fields = correctedFields.Select((f, index) => new ReportField
                {
                    FieldPath = f.FieldPath,
                    DisplayName = f.DisplayName,
                    DataType = f.DataType,
                    DisplayOrder = index,
                    IsCustomField = f.IsCustomField,
                    CustomFieldDefinitionId = f.CustomFieldDefinitionId
                }).ToList(),
                Filters = request.Filters?.Select((f, index) => new ReportFilter
                {
                    FieldPath = f.FieldPath,
                    Operator = f.Operator,
                    Value = f.Value,
                    DataType = f.DataType,
                    DisplayOrder = index,
                    IsCustomField = f.IsCustomField,
                    CustomFieldDefinitionId = f.CustomFieldDefinitionId,
                    LogicOperator = f.LogicOperator,
                    GroupId = f.GroupId,
                    GroupLogicOperator = f.GroupLogicOperator
                }).ToList() ?? new List<ReportFilter>()
            };

            Console.WriteLine($"Report Definition Entity Type: {reportDef.EntityType}");

            // Get preview data with collection queries
            var data = await _reportDataService.GetReportPreviewAsync(reportDef, request.CollectionQueries ?? new List<CollectionQueryDto>());

            // Debug logging
            Console.WriteLine($"Preview returned {data.Count} rows for Entity Type: {reportDef.EntityType}");
            Console.WriteLine($"Fields requested: {reportDef.Fields.Count}");
            Console.WriteLine($"Filters applied: {reportDef.Filters.Count}");
            Console.WriteLine($"Collection queries processed: {request.CollectionQueries?.Count ?? 0}");

            return Ok(new
            {
                success = true,
                data = data,
                rowCount = data.Count,
                debug = new {
                    entityType = reportDef.EntityType,
                    fieldsCount = reportDef.Fields.Count
                }
            });
        }
        catch (Exception ex)
        {
            // Log the full exception
            Console.WriteLine($"Preview error: {ex}");

            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    private async Task<List<ReportFieldDto>> ValidateAndCorrectFieldPaths(string entityType, List<ReportFieldDto> fields)
    {
        var correctedFields = new List<ReportFieldDto>();

        // Get valid field metadata for this entity type
        var validFields = await _fieldMetadataService.GetFieldsForEntityAsync(entityType);
        var validFieldPaths = validFields.ToDictionary(f => f.FieldPath, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            var correctedField = new ReportFieldDto
            {
                FieldPath = field.FieldPath,
                DisplayName = field.DisplayName,
                DataType = field.DataType,
                DisplayOrder = field.DisplayOrder,
                IsCustomField = field.IsCustomField,
                CustomFieldDefinitionId = field.CustomFieldDefinitionId,
                PivotArea = field.PivotArea,
                AggregationType = field.AggregationType
            };

            // Check if field path is valid
            if (!validFieldPaths.ContainsKey(field.FieldPath) && !field.IsCustomField)
            {
                // Try to find a corrected path
                var possibleCorrections = validFieldPaths.Keys
                    .Where(vf => vf.EndsWith("." + field.FieldPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (possibleCorrections.Any())
                {
                    var correction = possibleCorrections.First();
                    Console.WriteLine($"?? Correcting field path: '{field.FieldPath}' ? '{correction}'");
                    correctedField.FieldPath = correction;
                }
                else
                {
                    Console.WriteLine($"?? Warning: Field path '{field.FieldPath}' not found for entity '{entityType}'");
                }
            }

            correctedFields.Add(correctedField);
        }

        return correctedFields;
    }

    [HttpPost("save")]
    public async Task<IActionResult> SaveReport([FromBody] SaveReportRequest request)
    {
        try
        {
            ReportDefinition reportDef;

            if (request.ReportId.HasValue)
            {
                // Update existing
                reportDef = await _context.ReportDefinitions
                    .Include(rd => rd.Fields)
                    .Include(rd => rd.Filters)
                    .FirstOrDefaultAsync(rd => rd.Id == request.ReportId.Value);

                if (reportDef == null)
                {
                    return NotFound(new { success = false, error = "Report not found" });
                }

                // Clear existing fields and filters
                _context.ReportFields.RemoveRange(reportDef.Fields);
                _context.ReportFilters.RemoveRange(reportDef.Filters);
            }
            else
            {
                // Create new
                reportDef = new ReportDefinition
                {
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = User.Identity?.Name
                };
                _context.ReportDefinitions.Add(reportDef);
            }

            // Update properties
            reportDef.Name = request.Name;
            reportDef.Description = request.Description;
            reportDef.EntityType = request.EntityType;
            reportDef.Category = request.Category;
            reportDef.IsPublic = request.IsPublic;
            reportDef.PivotConfiguration = request.PivotConfiguration;
            reportDef.ModifiedAt = DateTime.UtcNow;

            // Save collection queries as JSON
            if (request.CollectionQueries != null && request.CollectionQueries.Any())
            {
                reportDef.CollectionQueriesJson = System.Text.Json.JsonSerializer.Serialize(request.CollectionQueries);
            }
            else
            {
                reportDef.CollectionQueriesJson = null;
            }

            // Add fields
            foreach (var field in request.Fields)
            {
                reportDef.Fields.Add(new ReportField
                {
                    FieldPath = field.FieldPath,
                    DisplayName = field.DisplayName,
                    DataType = field.DataType,
                    PivotArea = field.PivotArea,
                    AggregationType = field.AggregationType,
                    DisplayOrder = field.DisplayOrder,
                    IsCustomField = field.IsCustomField,
                    CustomFieldDefinitionId = field.CustomFieldDefinitionId
                });
            }

            // Add filters
            foreach (var filter in request.Filters)
            {
                reportDef.Filters.Add(new ReportFilter
                {
                    FieldPath = filter.FieldPath,
                    Operator = filter.Operator,
                    Value = filter.Value,
                    DataType = filter.DataType,
                    DisplayOrder = filter.DisplayOrder,
                    IsCustomField = filter.IsCustomField,
                    CustomFieldDefinitionId = filter.CustomFieldDefinitionId,
                    LogicOperator = filter.LogicOperator,
                    GroupId = filter.GroupId,
                    GroupLogicOperator = filter.GroupLogicOperator
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                reportId = reportDef.Id,
                message = "Report saved successfully"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save error: {ex}");

            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReport(int id)
    {
        try
        {
            var report = await _context.ReportDefinitions
                .Include(rd => rd.Fields)
                .Include(rd => rd.Filters)
                .FirstOrDefaultAsync(rd => rd.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            // Check permissions (only owner can delete)
            if (report.CreatedByUserId != User.Identity?.Name && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.ReportFields.RemoveRange(report.Fields);
            _context.ReportFilters.RemoveRange(report.Filters);
            _context.ReportDefinitions.Remove(report);

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("collection-metadata/{entityType}")]
    [Authorize(Policy = "Permission.Reports.View")]
    public IActionResult GetCollectionMetadata(string entityType)
    {
        try
        {
            var metadata = _collectionMetadataService.GetCollectionMetadata(entityType);
            return Ok(new { success = true, collections = metadata });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

public class PreviewReportRequest
{
    public string EntityType { get; set; } = "Case";
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportFilterDto> Filters { get; set; } = new();
    public List<CollectionQueryDto> CollectionQueries { get; set; } = new();
}

public class SaveReportRequest
{
    public int? ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "Case";
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public string? PivotConfiguration { get; set; }
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportFilterDto> Filters { get; set; } = new();
    public List<CollectionQueryDto> CollectionQueries { get; set; } = new();
}

public class ReportFieldDto
{
    public string FieldPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? PivotArea { get; set; }
    public string? AggregationType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }
}

public class ReportFilterDto
{
    public string FieldPath { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public string? Value { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }
    public string LogicOperator { get; set; } = "AND";
    public int? GroupId { get; set; }
    public string GroupLogicOperator { get; set; } = "AND";
}
