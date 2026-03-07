using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Controllers.Api;

/// <summary>
/// API endpoints for report data extraction (testing)
/// </summary>
[Authorize]
[ApiController]
[Route("api/reporting/data")]
[EnableRateLimiting("bulk-export")] // 10 per hour - test data extraction
public class ReportDataApiController : ControllerBase
{
    private readonly IReportDataService _reportDataService;

    public ReportDataApiController(IReportDataService reportDataService)
    {
        _reportDataService = reportDataService;
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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
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
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Validate a report definition
    /// POST: /api/reporting/data/validate
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult> ValidateReport([FromBody] ReportDefinition reportDefinition)
    {
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
    public async Task<ActionResult> GetRowCount([FromBody] ReportDefinition reportDefinition)
    {
        try
        {
            var count = await _reportDataService.GetReportRowCountAsync(reportDefinition);

            return Ok(new
            {
                success = true,
                count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
