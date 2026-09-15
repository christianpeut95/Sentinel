using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Pages.Reports;

[Authorize(Policy = "Permission.Report.View")]
public class TestModel : PageModel
{
    private readonly IReportDataService _reportDataService;
    private readonly IReportDataAccessService _reportDataAccessService;
    private readonly ILogger<TestModel> _logger;

    public TestModel(
        IReportDataService reportDataService,
        IReportDataAccessService reportDataAccessService,
        ILogger<TestModel> logger)
    {
        _reportDataService = reportDataService;
        _reportDataAccessService = reportDataAccessService;
        _logger = logger;
    }

    public List<Dictionary<string, object?>>? ReportData { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Create simple test report
            var reportDef = new ReportDefinition
            {
                Name = "Test",
                EntityType = "Case",
                Fields = new List<ReportField>
                {
                    new ReportField { FieldPath = "FriendlyId", DisplayName = "Case Number", DataType = "String" },
                    new ReportField { FieldPath = "Type", DisplayName = "Type", DataType = "Int32" }
                },
                Filters = new List<ReportFilter>()
            };

            if (!await _reportDataAccessService.CanReadEntityTypeAsync(reportDef.EntityType))
            {
                return Forbid();
            }

            ReportData = await _reportDataService.GetReportPreviewAsync(reportDef);
        }
        catch (ReportDataAccessDeniedException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load report test data");
            ErrorMessage = $"The report test data could not be loaded. Please try again. Reference: {HttpContext.TraceIdentifier}";
        }

        return Page();
    }
}
