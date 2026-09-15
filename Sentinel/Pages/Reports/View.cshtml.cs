using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Pages.Reports;

[Authorize(Policy = "Permission.Report.View")]
public class ViewModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReportDataService _reportDataService;
    private readonly IReportDataAccessService _reportDataAccessService;

    public ViewModel(
        ApplicationDbContext context,
        IReportDataService reportDataService,
        IReportDataAccessService reportDataAccessService)
    {
        _context = context;
        _reportDataService = reportDataService;
        _reportDataAccessService = reportDataAccessService;
    }

    public ReportDefinition? ReportDefinition { get; set; }
    public List<Dictionary<string, object?>>? ReportData { get; set; }
    public int TotalRows { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Disable browser caching for reports - always fetch fresh data
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
        
        // Load report definition
        ReportDefinition = await _context.ReportDefinitions
            .AsNoTracking()
            .Include(rd => rd.Fields.OrderBy(f => f.DisplayOrder))
            .Include(rd => rd.Filters.OrderBy(f => f.DisplayOrder))
            .FirstOrDefaultAsync(rd => rd.Id == id);

        if (ReportDefinition == null)
        {
            return NotFound();
        }

        if (!CanViewReport(ReportDefinition))
        {
            return NotFound();
        }

        if (!await _reportDataAccessService.CanReadEntityTypeAsync(ReportDefinition.EntityType))
        {
            return Forbid();
        }

        try
        {
            // Load report data (always fresh from DB)
            ReportData = await _reportDataService.GetReportDataAsync(ReportDefinition);
            TotalRows = ReportData.Count;
        }
        catch (ReportDataAccessDeniedException)
        {
            return Forbid();
        }
        catch (Exception)
        {
            ErrorMessage = "The report data could not be loaded. Please try again.";
        }

        return Page();
    }

    private bool CanViewReport(ReportDefinition report) =>
        report.IsPublic ||
        User.IsInRole("Admin") ||
        string.Equals(report.CreatedByUserId, User.Identity?.Name, StringComparison.Ordinal);
}
