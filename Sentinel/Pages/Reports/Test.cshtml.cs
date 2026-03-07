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

    public TestModel(IReportDataService reportDataService)
    {
        _reportDataService = reportDataService;
    }

    public List<Dictionary<string, object?>>? ReportData { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
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

            ReportData = await _reportDataService.GetReportPreviewAsync(reportDef);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}\n\nStack: {ex.StackTrace}";
        }
    }
}
