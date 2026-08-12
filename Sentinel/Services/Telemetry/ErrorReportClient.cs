using System.Net;
using System.Text;
using System.Text.Json;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// HTTP client for submitting error reports to the Sentinel Feedback API
    /// </summary>
    public class ErrorReportClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ErrorReportClient> _logger;
        private const string ErrorsEndpoint = "https://feedback.sentinelsurveillance.app/api/v1/errors";

        public ErrorReportClient(
            HttpClient httpClient,
            ILogger<ErrorReportClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Submit an error report to the Sentinel Feedback API
        /// </summary>
        public async Task<bool> SubmitErrorReportAsync(ErrorReport report)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var json = JsonSerializer.Serialize(report, jsonOptions);
                _logger.LogInformation("Submitting error report {ErrorId} for installation {InstallationId}", 
                    report.ErrorId, report.InstallationId);
                _logger.LogDebug("Error report payload: {Payload}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(ErrorsEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Error report {ErrorId} submitted successfully (Status: {StatusCode})", 
                        report.ErrorId, response.StatusCode);
                    return true;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Error report rate limit reached (429). Report {ErrorId} not submitted", 
                        report.ErrorId);
                    return false;
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error report {ErrorId} rejected with validation error (400): {Error}", 
                        report.ErrorId, errorBody);
                    return false;
                }

                _logger.LogError("Error report {ErrorId} failed with status {StatusCode}", 
                    report.ErrorId, response.StatusCode);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting error report {ErrorId}", report.ErrorId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting error report {ErrorId}", report.ErrorId);
                return false;
            }
        }
    }
}
