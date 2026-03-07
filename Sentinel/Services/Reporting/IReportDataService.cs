using Sentinel.Models.Reporting;
using Sentinel.DTOs;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for extracting data from database for reports
/// Handles regular fields, custom fields (EAV), navigation properties, and filters
/// </summary>
public interface IReportDataService
{
    /// <summary>
    /// Extracts data for a report definition
    /// Returns data in format ready for WebDataRocks pivot grid
    /// </summary>
    /// <param name="reportDefinition">Report configuration</param>
    /// <returns>List of dictionaries where keys are field paths and values are field values</returns>
    Task<List<Dictionary<string, object?>>> GetReportDataAsync(ReportDefinition reportDefinition);

    /// <summary>
    /// Gets a preview of report data (first 100 rows)
    /// </summary>
    Task<List<Dictionary<string, object?>>> GetReportPreviewAsync(ReportDefinition reportDefinition);

    /// <summary>
    /// Gets a preview of report data with collection queries (first 100 rows)
    /// </summary>
    Task<List<Dictionary<string, object?>>> GetReportPreviewAsync(
        ReportDefinition reportDefinition, 
        List<CollectionQueryDto> collectionQueries);

    /// <summary>
    /// Gets aggregated data for year-to-date comparisons
    /// </summary>
    Task<List<Dictionary<string, object?>>> GetAggregatedDataAsync(
        ReportDefinition reportDefinition, 
        DateTime startDate, 
        DateTime endDate,
        string groupByField);

    /// <summary>
    /// Validates that a report definition can be executed
    /// </summary>
    Task<(bool isValid, string? errorMessage)> ValidateReportDefinitionAsync(ReportDefinition reportDefinition);

    /// <summary>
    /// Gets row count without loading all data (for pagination)
    /// </summary>
    Task<int> GetReportRowCountAsync(ReportDefinition reportDefinition);
}
