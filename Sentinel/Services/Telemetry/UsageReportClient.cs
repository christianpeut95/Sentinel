using System.Net;
using System.Text;
using System.Text.Json;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// HTTP client for submitting usage reports to the Sentinel Feedback API
    /// </summary>
    public class UsageReportClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UsageReportClient> _logger;
        private const string UsageEndpoint = "https://feedback.sentinelsurveillance.app/api/v1/usage";

        public UsageReportClient(
            HttpClient httpClient,
            ILogger<UsageReportClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Submit a usage report to the Sentinel Feedback API
        /// </summary>
        public async Task<bool> SubmitUsageReportAsync(UsageReport report)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(report, jsonOptions);
                _logger.LogInformation("Submitting usage report {ReportId} for installation {InstallationId}", 
                    report.ReportId, report.InstallationId);
                _logger.LogInformation("Usage report payload: {Payload}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(UsageEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Usage report {ReportId} submitted successfully (Status: {StatusCode})", 
                        report.ReportId, response.StatusCode);
                    return true;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Usage report rate limit reached (429). Report {ReportId} not submitted", 
                        report.ReportId);
                    return false;
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var contentType = response.Content.Headers.ContentType?.ToString() ?? "none";
                    _logger.LogWarning("Usage report {ReportId} rejected with validation error (400). ContentType: {ContentType}, Error: {Error}", 
                        report.ReportId, contentType, errorBody);
                    _logger.LogWarning("Rejected payload was: {Payload}", json);

                    // Log all response headers for debugging
                    var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"));
                    _logger.LogWarning("Response headers: {Headers}", headers);

                    return false;
                }

                _logger.LogError("Usage report {ReportId} failed with status {StatusCode}", 
                    report.ReportId, response.StatusCode);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting usage report {ReportId}", report.ReportId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting usage report {ReportId}", report.ReportId);
                return false;
            }
        }
    }
}
