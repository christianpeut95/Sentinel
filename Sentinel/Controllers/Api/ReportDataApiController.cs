using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Controllers.Api;

/// <summary>
/// API endpoints for report data extraction (testing)
/// </summary>
[Authorize(Policy = "Permission.Report.View")]
[ApiController]
[Route("api/reporting/data")]
[EnableRateLimiting("bulk-export")] // 10 per hour - test data extraction
public class ReportDataApiController : ControllerBase
{
    private readonly IReportDataService _reportDataService;
    private readonly ILogger<ReportDataApiController> _logger;

    public ReportDataApiController(
        IReportDataService reportDataService,
        ILogger<ReportDataApiController> logger)
    {
        _reportDataService = reportDataService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint: Get preview data for a simple Case report
    /// POST: /api/reporting/data/preview/case
    /// </summary>
    [HttpPost("preview/case")]
    public async Task<ActionResult> GetCasePreview()
    {
        try
        {
            // Create a simple test report definition
            var reportDefinition = new ReportDefinition
            {
                Name = "Test Case Report",
                EntityType = "Case",
                Fields = new List<ReportField>
                {
                    new ReportField { FieldPath = "Id", DisplayName = "Case ID", DataType = "Guid" },
                    new ReportField { FieldPath = "FriendlyId", DisplayName = "Case Number", DataType = "String" },
                    new ReportField { FieldPath = "Type", DisplayName = "Case Type", DataType = "CaseType" },
                    new ReportField { FieldPath = "Patient.GivenName", DisplayName = "Given Name", DataType = "String" },
                    new ReportField { FieldPath = "Patient.FamilyName", DisplayName = "Family Name", DataType = "String" },
                    new ReportField { FieldPath = "Disease.Name", DisplayName = "Disease", DataType = "String" },
                    new ReportField { FieldPath = "Jurisdiction1.Name", DisplayName = "Jurisdiction 1", DataType = "String" }
                },
                Filters = new List<ReportFilter>()
            };

            var data = await _reportDataService.GetReportPreviewAsync(reportDefinition);

            return Ok(new
            {
                success = true,
                rowCount = data.Count,
                data = data
            });
        }
        catch (ReportDataAccessDeniedException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to generate the case report preview");
            return StatusCode(500, new
            {
                success = false,
                error = "The case report preview could not be generated. Check the report configuration and try again.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Test endpoint: Get Outbreak data
    /// POST: /api/reporting/data/preview/outbreak
    /// </summary>
    [HttpPost("preview/outbreak")]
    public async Task<ActionResult> GetOutbreakPreview()
    {
        try
        {
            var reportDefinition = new ReportDefinition
            {
                Name = "Test Outbreak Report",
                EntityType = "Outbreak",
                Fields = new List<ReportField>
                {
                    new ReportField { FieldPath = "Id", DisplayName = "Outbreak ID", DataType = "Int32" },
                    new ReportField { FieldPath = "Name", DisplayName = "Outbreak Name", DataType = "String" },
                    new ReportField { FieldPath = "Status", DisplayName = "Status", DataType = "OutbreakStatus" },
                    new ReportField { FieldPath = "StartDate", DisplayName = "Start Date", DataType = "DateTime" },
                    new ReportField { FieldPath = "ConfirmationStatus.Name", DisplayName = "Confirmation Status", DataType = "String" },
                    new ReportField { FieldPath = "PrimaryDisease.Name", DisplayName = "Disease", DataType = "String" }
                },
                Filters = new List<ReportFilter>()
            };

            var data = await _reportDataService.GetReportPreviewAsync(reportDefinition);

            return Ok(new
            {
                success = true,
                rowCount = data.Count,
                data = data
            });
        }
        catch (ReportDataAccessDeniedException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to generate the outbreak report preview");
            return StatusCode(500, new
            {
                success = false,
                error = "The outbreak report preview could not be generated. Check the report configuration and try again.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Validate a report definition
    /// POST: /api/reporting/data/validate
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult> ValidateReport([FromBody] TransientReportDefinitionRequest request)
    {
        var reportDefinition = request.ToReportDefinition();
        var (isValid, errorMessage) = await _reportDataService.ValidateReportDefinitionAsync(reportDefinition);

        return Ok(new
        {
            isValid,
            errorMessage
        });
    }

    /// <summary>
    /// Get row count for a report
    /// POST: /api/reporting/data/count
    /// </summary>
    [HttpPost("count")]
    public async Task<ActionResult> GetRowCount([FromBody] TransientReportDefinitionRequest request)
    {
        try
        {
            var reportDefinition = request.ToReportDefinition();
            var count = await _reportDataService.GetReportRowCountAsync(reportDefinition);

            return Ok(new
            {
                success = true,
                count
            });
        }
        catch (ReportDataAccessDeniedException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to calculate the report row count");
            return StatusCode(500, new
            {
                success = false,
                error = "The report row count could not be calculated. Check the report configuration and try again.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}

/// <summary>
/// Narrow, non-persistent request shape for validating or counting an ad-hoc
/// report.  These endpoints never need a report ID, ownership, audit fields,
/// folders, saved pivot state or navigation properties, so they are deliberately
/// not model-bound to the EF <see cref="ReportDefinition"/> entity.
/// </summary>
public sealed class TransientReportDefinitionRequest
{
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(100)]
    public List<TransientReportFieldRequest> Fields { get; set; } = new();

    [MaxLength(100)]
    public List<TransientReportFilterRequest> Filters { get; set; } = new();

    public ReportDefinition ToReportDefinition() => new()
    {
        // A constant makes it explicit that no caller-provided display metadata
        // is retained or used for this non-persistent operation.
        Name = "Transient report query",
        EntityType = EntityType,
        Fields = Fields.Select((field, index) => field.ToReportField(index)).ToList(),
        Filters = Filters.Select((filter, index) => filter.ToReportFilter(index)).ToList()
    };
}

public sealed class TransientReportFieldRequest
{
    [Required]
    [StringLength(500)]
    public string FieldPath { get; set; } = string.Empty;

    [StringLength(200)]
    public string? DisplayName { get; set; }

    [StringLength(50)]
    public string? DataType { get; set; }

    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }

    internal ReportField ToReportField(int displayOrder) => new()
    {
        FieldPath = FieldPath,
        DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? FieldPath : DisplayName,
        DataType = string.IsNullOrWhiteSpace(DataType) ? "String" : DataType,
        DisplayOrder = displayOrder,
        IsCustomField = IsCustomField,
        CustomFieldDefinitionId = CustomFieldDefinitionId
    };
}

public sealed class TransientReportFilterRequest
{
    [Required]
    [StringLength(500)]
    public string FieldPath { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Operator { get; set; } = "Equals";

    [StringLength(20_000)]
    public string? Value { get; set; }

    [StringLength(50)]
    public string? DataType { get; set; }

    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }

    [StringLength(10)]
    public string LogicOperator { get; set; } = "AND";

    public int? GroupId { get; set; }

    [StringLength(10)]
    public string GroupLogicOperator { get; set; } = "AND";

    public bool IsCollectionQuery { get; set; }

    [StringLength(20_000)]
    public string? CollectionSubFilters { get; set; }

    [StringLength(20)]
    public string? CollectionOperator { get; set; }

    public bool IsDynamicDate { get; set; }

    [StringLength(50)]
    public string? DynamicDateType { get; set; }

    [Range(-10_000, 10_000)]
    public int? DynamicDateOffset { get; set; }

    [StringLength(20)]
    public string? DynamicDateOffsetUnit { get; set; }

    internal ReportFilter ToReportFilter(int displayOrder) => new()
    {
        FieldPath = FieldPath,
        Operator = Operator,
        Value = Value,
        DataType = string.IsNullOrWhiteSpace(DataType) ? "String" : DataType,
        DisplayOrder = displayOrder,
        IsCustomField = IsCustomField,
        CustomFieldDefinitionId = CustomFieldDefinitionId,
        LogicOperator = LogicOperator,
        GroupId = GroupId,
        GroupLogicOperator = GroupLogicOperator,
        IsCollectionQuery = IsCollectionQuery,
        CollectionSubFilters = CollectionSubFilters,
        CollectionOperator = CollectionOperator,
        IsDynamicDate = IsDynamicDate,
        DynamicDateType = DynamicDateType,
        DynamicDateOffset = DynamicDateOffset,
        DynamicDateOffsetUnit = DynamicDateOffsetUnit
    };
}
