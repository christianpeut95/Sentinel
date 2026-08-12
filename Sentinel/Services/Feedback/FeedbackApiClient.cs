using System.Net;
using System.Text;
using System.Text.Json;
using Sentinel.Models.Feedback;

namespace Sentinel.Services.Feedback
{
    /// <summary>
    /// Client for submitting feedback to Sentinel Feedback API
    /// </summary>
    public class FeedbackApiClient
    {
        private const string FeedbackApiEndpoint = "https://feedback.sentinelsurveillance.app/api/v1/feedback";
        private const int MaxPayloadSizeBytes = 512 * 1024; // 512 KiB

        private readonly HttpClient _httpClient;
        private readonly ILogger<FeedbackApiClient> _logger;

        public FeedbackApiClient(HttpClient httpClient, ILogger<FeedbackApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Submits feedback to the Sentinel Feedback API
        /// </summary>
        /// <param name="feedback">Feedback submission payload</param>
        /// <returns>Result indicating success or failure with error message</returns>
        public async Task<FeedbackSubmissionResult> SubmitFeedbackAsync(FeedbackSubmission feedback)
        {
            try
            {
                // Serialize payload
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false // Ensure compact JSON
                };

                var json = JsonSerializer.Serialize(feedback, jsonOptions);
                var payloadSize = Encoding.UTF8.GetByteCount(json);

                // Log the actual payload for debugging
                _logger.LogInformation("Feedback payload being sent: {Json}", json);

                // Check payload size limit
                if (payloadSize > MaxPayloadSizeBytes)
                {
                    _logger.LogWarning("Feedback payload exceeds maximum size: {Size} bytes", payloadSize);
                    return FeedbackSubmissionResult.Failure(
                        $"Feedback payload too large ({payloadSize / 1024} KB). Maximum allowed is 512 KB. Please reduce the amount of diagnostic information."
                    );
                }

                // Create request
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, FeedbackApiEndpoint)
                {
                    Content = content
                };

                _logger.LogInformation("Submitting feedback to API: Type={Type}, Summary={Summary}", 
                    feedback.Type, feedback.Summary);

                // Test connectivity (diagnostic)
                try
                {
                    var testResponse = await _httpClient.GetAsync(FeedbackApiEndpoint.Replace("/api/v1/feedback", "/health"));
                    _logger.LogInformation("Health check result: {StatusCode}", testResponse.StatusCode);
                }
                catch (Exception healthEx)
                {
                    _logger.LogWarning(healthEx, "Health check failed - API may be unreachable");
                }

                // Send request
                var response = await _httpClient.SendAsync(request);

                // Log full response details for debugging
                _logger.LogInformation("Response Status: {StatusCode}, ReasonPhrase: {ReasonPhrase}, HasContent: {HasContent}", 
                    response.StatusCode, response.ReasonPhrase, response.Content.Headers.ContentLength > 0);

                // Handle response
                if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation("Feedback submitted successfully");
                    return FeedbackSubmissionResult.Success();
                }
                else if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
                {
                    _logger.LogWarning("Feedback submission rate limited");
                    return FeedbackSubmissionResult.Failure(
                        "You've submitted feedback too frequently. Please wait a few minutes and try again. (Limit: ~20 submissions per 10 minutes)"
                    );
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Feedback submission rejected: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);

                    // Log the full error for debugging
                    _logger.LogError("Bad Request Error Details: {ErrorContent}", errorContent);

                    // If error is empty, the API may not be available
                    if (string.IsNullOrWhiteSpace(errorContent))
                    {
                        _logger.LogError("Empty error response from API - the endpoint may not be deployed or available yet");
                        return FeedbackSubmissionResult.Failure(
                            "The feedback service is currently unavailable. The API endpoint may not be deployed yet."
                        );
                    }

                    return FeedbackSubmissionResult.Failure(
                        "Your feedback could not be submitted. Please check that all fields are filled correctly."
                    );
                }
                else
                {
                    _logger.LogError("Feedback submission failed: {StatusCode}", response.StatusCode);
                    return FeedbackSubmissionResult.Failure(
                        $"Feedback submission failed with status {(int)response.StatusCode}. Please try again later."
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting feedback");
                return FeedbackSubmissionResult.Failure(
                    "Could not connect to feedback service. Please check your internet connection and try again."
                );
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Feedback submission timeout");
                return FeedbackSubmissionResult.Failure(
                    "Feedback submission timed out. Please try again."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting feedback");
                return FeedbackSubmissionResult.Failure(
                    "An unexpected error occurred. Please try again later."
                );
            }
        }
    }

    /// <summary>
    /// Result of a feedback submission attempt
    /// </summary>
    public class FeedbackSubmissionResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static FeedbackSubmissionResult Success() => new() { IsSuccess = true };

        public static FeedbackSubmissionResult Failure(string errorMessage) => new() 
        { 
            IsSuccess = false, 
            ErrorMessage = errorMessage 
        };
    }
}
