using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Text.Json;

namespace Sentinel.Pages.Reports;

[Authorize(Policy = "Permission.Report.Create")]
public class BuilderModel : PageModel
{
    private readonly IReportFieldMetadataService _fieldMetadataService;
    private readonly IReportDataService _reportDataService;
    private readonly IReportDataAccessService _reportDataAccessService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuilderModel> _logger;

    public BuilderModel(
        IReportFieldMetadataService fieldMetadataService,
        IReportDataService reportDataService,
        IReportDataAccessService reportDataAccessService,
        ApplicationDbContext context,
        ILogger<BuilderModel> logger)
    {
        _fieldMetadataService = fieldMetadataService;
        _reportDataService = reportDataService;
        _reportDataAccessService = reportDataAccessService;
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string EntityType { get; set; } = "Case";

    public ReportDefinition? ReportDefinition { get; set; }
    public Dictionary<string, List<ReportFieldMetadata>>? AvailableFields { get; set; }
    public List<Dictionary<string, object?>>? ReportData { get; set; }
    public string? CollectionQueriesJson { get; set; }
    public string? FieldsJson { get; set; }
    public string? FiltersJson { get; set; }
    public string? PivotConfigurationJson { get; set; }
    public string? PreviewConfigurationJson { get; set; }
    public string CreatedByDisplayName { get; set; } = "Not yet saved";
    public string ModifiedByDisplayName { get; set; } = "Not yet saved";

    public async Task<IActionResult> OnGetAsync()
    {
        // Disable browser caching - always fetch fresh data
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
        
        // Load existing report if editing
        if (ReportId.HasValue)
        {
            ReportDefinition = await _context.ReportDefinitions
                .AsNoTracking()
                .Include(rd => rd.Fields)
                .Include(rd => rd.Filters)
                .FirstOrDefaultAsync(rd => rd.Id == ReportId.Value);

            if (ReportDefinition == null)
            {
                return NotFound();
            }

            if (!CanEditReport(ReportDefinition))
            {
                return NotFound();
            }

            // Override entity type with the one from saved report
            EntityType = ReportDefinition.EntityType;

            // Pass collection queries to the page for JavaScript initialization
            CollectionQueriesJson = ReportDefinition.CollectionQueriesJson;

            // Serialize fields to JSON
            var fieldObjects = ReportDefinition.Fields.OrderBy(f => f.DisplayOrder).Select(f => new
            {
                fieldPath = f.FieldPath,
                displayName = f.DisplayName,
                dataType = f.DataType,
                isCustomField = f.IsCustomField,
                customFieldDefinitionId = f.CustomFieldDefinitionId
            });
            FieldsJson = JsonSerializer.Serialize(fieldObjects);

            // Serialize filters to JSON
            var filterObjects = ReportDefinition.Filters.OrderBy(f => f.DisplayOrder).Select(f => new
            {
                fieldPath = f.FieldPath,
                @operator = f.Operator,
                value = f.Value,
                dataType = f.DataType,
                groupId = f.GroupId,
                isDynamicDate = f.IsDynamicDate,
                dynamicDateType = f.DynamicDateType,
                dynamicDateOffset = f.DynamicDateOffset,
                dynamicDateOffsetUnit = f.DynamicDateOffsetUnit
            });
            FiltersJson = JsonSerializer.Serialize(filterObjects);

            // Pass pivot and preview configurations to the page
            PivotConfigurationJson = ReportDefinition.PivotConfiguration;
            PreviewConfigurationJson = ReportDefinition.PreviewConfiguration;

            CreatedByDisplayName = await GetUserDisplayNameAsync(ReportDefinition.CreatedByUserId);
            ModifiedByDisplayName = string.IsNullOrWhiteSpace(ReportDefinition.ModifiedByUserId)
                ? "Not recorded"
                : await GetUserDisplayNameAsync(ReportDefinition.ModifiedByUserId);

            // Load report data
            if (!await _reportDataAccessService.CanReadEntityTypeAsync(ReportDefinition.EntityType))
            {
                return Forbid();
            }

            ReportData = await _reportDataService.GetReportPreviewAsync(ReportDefinition);
        }
        else
        {
            var currentUserName = User.Identity?.Name;
            CreatedByDisplayName = await GetUserDisplayNameAsync(currentUserName);
            ModifiedByDisplayName = CreatedByDisplayName;
        }

        // Load available fields for entity type (after potentially overriding from saved report)
        // Use Report context to include audit fields for technical reporting
        AvailableFields = await _fieldMetadataService.GetFieldsByCategoryAsync(EntityType, FieldUsageContext.Report);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveReportAsync([FromBody] SaveReportRequest request)
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
                    return NotFound();
                }

                if (!CanEditReport(reportDef))
                {
                    return NotFound();
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
            reportDef.ModifiedByUserId = User.Identity?.Name;

            // Save collection queries as JSON
            if (request.CollectionQueries != null && request.CollectionQueries.Any())
            {
                reportDef.CollectionQueriesJson = JsonSerializer.Serialize(request.CollectionQueries);
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
                    GroupLogicOperator = filter.GroupLogicOperator,
                    IsDynamicDate = filter.IsDynamicDate,
                    DynamicDateType = filter.DynamicDateType,
                    DynamicDateOffset = filter.DynamicDateOffset,
                    DynamicDateOffsetUnit = filter.DynamicDateOffsetUnit
                });
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                reportId = reportDef.Id,
                message = "Report saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to save report from the report builder {ReportId}", request.ReportId);
            return new JsonResult(new
            {
                success = false,
                error = "The report could not be saved. Check the report configuration and try again.",
                traceId = HttpContext.TraceIdentifier
            })
            {
                StatusCode = 500
            };
        }
    }

    private bool CanEditReport(ReportDefinition report) =>
        User.IsInRole("Admin") ||
        string.Equals(report.CreatedByUserId, User.Identity?.Name, StringComparison.Ordinal);

    private async Task<string> GetUserDisplayNameAsync(string? userKey)
    {
        if (string.IsNullOrWhiteSpace(userKey))
        {
            return "Unknown";
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userKey || u.UserName == userKey || u.Email == userKey)
            .Select(u => new { u.FirstName, u.LastName, u.UserName, u.Email })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return userKey;
        }

        var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : user.UserName ?? user.Email ?? userKey;
    }

    public async Task<IActionResult> OnPostPreviewReportAsync([FromBody] PreviewReportRequest request)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Request is null"
                });
            }

            if (string.IsNullOrEmpty(request.EntityType))
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Entity type is required"
                });
            }

            if (request.Fields == null || !request.Fields.Any())
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "At least one field is required"
                });
            }

            // Build temporary report definition
            var reportDef = new ReportDefinition
            {
                Name = "Preview",
                EntityType = request.EntityType,
                Fields = request.Fields.Select(f => new ReportField
                {
                    FieldPath = f.FieldPath,
                    DisplayName = f.DisplayName,
                    DataType = f.DataType,
                    IsCustomField = f.IsCustomField,
                    CustomFieldDefinitionId = f.CustomFieldDefinitionId
                }).ToList(),
                Filters = request.Filters?.Select(f => new ReportFilter
                {
                    FieldPath = f.FieldPath,
                    Operator = f.Operator,
                    Value = f.Value,
                    DataType = f.DataType,
                    IsCustomField = f.IsCustomField,
                    CustomFieldDefinitionId = f.CustomFieldDefinitionId,
                    LogicOperator = f.LogicOperator,
                    GroupId = f.GroupId,
                    GroupLogicOperator = f.GroupLogicOperator,
                    IsDynamicDate = f.IsDynamicDate,
                    DynamicDateType = f.DynamicDateType,
                    DynamicDateOffset = f.DynamicDateOffset,
                    DynamicDateOffsetUnit = f.DynamicDateOffsetUnit
                }).ToList() ?? new List<ReportFilter>()
            };

            // Get preview data
            if (!await _reportDataAccessService.CanReadEntityTypeAsync(reportDef.EntityType))
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "You are not permitted to view data for the selected report type."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var data = await _reportDataService.GetReportPreviewAsync(reportDef);

            return new JsonResult(new
            {
                success = true,
                data = data,
                rowCount = data.Count
            });
        }
        catch (ReportDataAccessDeniedException)
        {
            return new JsonResult(new
            {
                success = false,
                error = "You are not permitted to view data for the selected report type."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to generate report preview from the report builder");
            
            return new JsonResult(new
            {
                success = false,
                error = "The report preview could not be generated. Check the selected fields and filters, then try again.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

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

public class PreviewReportRequest
{
    public string EntityType { get; set; } = "Case";
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

    // Dynamic date properties
    public bool IsDynamicDate { get; set; }
    public string? DynamicDateType { get; set; }
    public int? DynamicDateOffset { get; set; }
    public string? DynamicDateOffsetUnit { get; set; }
}
