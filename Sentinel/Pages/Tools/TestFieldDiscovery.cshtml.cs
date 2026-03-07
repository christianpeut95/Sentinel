using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Pages.Tools;

public class TestFieldDiscoveryModel : PageModel
{
    private readonly IReportFieldMetadataService _fieldMetadataService;

    public TestFieldDiscoveryModel(IReportFieldMetadataService fieldMetadataService)
    {
        _fieldMetadataService = fieldMetadataService;
    }

    public Dictionary<string, List<ReportFieldMetadata>>? CaseFields { get; set; }
    public Dictionary<string, List<ReportFieldMetadata>>? OutbreakFields { get; set; }
    public List<ReportFieldMetadata>? CaseCustomFields { get; set; }
    public string? SelectedEntityType { get; set; }

    public async Task OnGetAsync(string? entityType = "Case")
    {
        SelectedEntityType = entityType ?? "Case";

        if (SelectedEntityType == "Case")
        {
            CaseFields = await _fieldMetadataService.GetFieldsByCategoryAsync("Case");
            CaseCustomFields = await _fieldMetadataService.GetCustomFieldsForEntityAsync("Case");
        }
        else if (SelectedEntityType == "Outbreak")
        {
            OutbreakFields = await _fieldMetadataService.GetFieldsByCategoryAsync("Outbreak");
        }
    }
}
