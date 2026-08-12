using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Models.Feedback;
using Sentinel.Services;
using Sentinel.Services.Feedback;

namespace Sentinel.Controllers.Api
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackApiController : ControllerBase
    {
        private readonly FeedbackApiClient _feedbackClient;
        private readonly DiagnosticsBuilder _diagnosticsBuilder;
        private readonly ISystemSettingsService _settingsService;
        private readonly IApplicationVersionProvider _applicationVersion;
        private readonly ILogger<FeedbackApiController> _logger;

        public FeedbackApiController(
            FeedbackApiClient feedbackClient,
            DiagnosticsBuilder diagnosticsBuilder,
            ISystemSettingsService settingsService,
            IApplicationVersionProvider applicationVersion,
            ILogger<FeedbackApiController> logger)
        {
            _feedbackClient = feedbackClient;
            _diagnosticsBuilder = diagnosticsBuilder;
            _settingsService = settingsService;
            _applicationVersion = applicationVersion;
            _logger = logger;
        }

        /// <summary>
        /// Submits feedback to Sentinel Feedback API
        /// POST /api/feedback/submit
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmissionRequest request)
        {
            try
            {
                // Log incoming request for debugging
                _logger.LogInformation("Received feedback submission: Type={Type}, Summary={Summary}, IncludeDiagnostics={IncludeDiagnostics}",
                    request?.Type, request?.Summary, request?.IncludeDiagnostics);

                // Validate request
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("Invalid feedback request: {Errors}", errors);
                    return BadRequest(new { success = false, message = "Invalid feedback data" });
                }

                // Build submission payload
                var submission = new FeedbackSubmission
                {
                    Type = MapFeedbackType(request.Type),
                    Summary = request.Summary,
                    Description = request.Description,
                    ExpectedBehaviour = request.ExpectedBehaviour,
                    ReporterEmail = request.ReporterEmail,
                    Reproducibility = request.Reproducibility,
                    TechnicalInfoIncluded = request.IncludeDiagnostics
                };

                // Enrich with server-side context
                var settings = await _settingsService.GetSettingsAsync();
                submission.InstallationId = settings?.InstallationId;
                submission.SentinelVersion = _applicationVersion.InformationalVersion.Length > 50 
                    ? _applicationVersion.InformationalVersion[..50] 
                    : _applicationVersion.InformationalVersion;
                submission.CommitHash = _applicationVersion.CommitHash;
                submission.PageRoute = SanitizePageRoute(request.PageUrl);
                submission.ClientUserAgent = Request.Headers["User-Agent"].ToString();
                submission.CorrelationId = HttpContext.TraceIdentifier;

                // Build diagnostics if requested
                if (request.IncludeDiagnostics)
                {
                    try
                    {
                        var diagnostics = await _diagnosticsBuilder.BuildDiagnosticsAsync();

                        // Enrich application info with controller-level data
                        if (diagnostics.Application != null)
                        {
                            diagnostics.Application.InstallationId = submission.InstallationId;
                        }

                        // Enrich request info with sanitized route
                        if (diagnostics.Request != null)
                        {
                            diagnostics.Request.RouteTemplate = submission.PageRoute;
                        }

                        // Enrich with client-side browser info if provided
                        if (request.ClientInfo != null)
                        {
                            diagnostics.Client = new BrowserClientInfo
                            {
                                BrowserLanguage = request.ClientInfo.BrowserLanguage,
                                TimeZone = request.ClientInfo.Timezone,
                                ViewportWidth = request.ClientInfo.ViewportWidth,
                                ViewportHeight = request.ClientInfo.ViewportHeight,
                                DevicePixelRatio = request.ClientInfo.DevicePixelRatio
                            };
                        }

                        submission.Diagnostics = diagnostics;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error building diagnostics, submitting without");
                        // Continue without diagnostics rather than failing
                    }
                }

                // Log submission object before sending
                _logger.LogInformation("Submitting feedback with InstallationId={InstallationId}, Version={Version}, Diagnostics={HasDiagnostics}",
                    submission.InstallationId, submission.SentinelVersion, submission.Diagnostics != null);

                // Submit to external API
                var result = await _feedbackClient.SubmitFeedbackAsync(submission);

                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = "Thank you for your feedback!" });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing feedback submission");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred processing your feedback. Please try again later." 
                });
            }
        }

        /// <summary>
        /// Sanitizes a page URL into a route template format
        /// Example: /Cases/12345/Edit -> /Cases/{id}/Edit
        /// </summary>
        private string? SanitizePageRoute(string? pageUrl)
        {
            if (string.IsNullOrWhiteSpace(pageUrl))
                return null;

            try
            {
                // Remove query string
                var path = pageUrl.Split('?')[0];

                // Replace numeric IDs with {id}
                path = System.Text.RegularExpressions.Regex.Replace(path, @"/\d+(?=/|$)", "/{id}");

                // Replace GUIDs with {guid}
                path = System.Text.RegularExpressions.Regex.Replace(
                    path, 
                    @"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(?=/|$)", 
                    "/{guid}"
                );

                return path;
            }
            catch
            {
                return pageUrl; // Return original if sanitization fails
            }
        }

        /// <summary>
        /// Maps string feedback type to API enum value
        /// </summary>
        private int MapFeedbackType(string type)
        {
            return type switch
            {
                "Bug" => 1,
                "FeatureRequest" => 2,
                "Confusing" => 3,
                "General" => 4,
                _ => 4 // Default to General
            };
        }
    }

    /// <summary>
    /// Request payload from client-side feedback widget
    /// </summary>
    public class FeedbackSubmissionRequest
    {
        public string Type { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ExpectedBehaviour { get; set; }
        public string? ReporterEmail { get; set; }
        public string? Reproducibility { get; set; }
        public bool IncludeDiagnostics { get; set; }
        public string? PageUrl { get; set; }
        public ClientInfo? ClientInfo { get; set; }
    }

    /// <summary>
    /// Client-side browser information
    /// </summary>
    public class ClientInfo
    {
        public string? BrowserLanguage { get; set; }
        public string? Timezone { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }
        public double? DevicePixelRatio { get; set; }
    }
}
