using Sentinel.Models.Telemetry;
using Sentinel.Services;
using Sentinel.Services.Telemetry;
using System.Security.Cryptography;
using System.Text;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Middleware that captures unhandled exceptions and reports them to the Sentinel Feedback API
    /// </summary>
    public class ErrorReportingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationVersionProvider _applicationVersion;
        private readonly ILogger<ErrorReportingMiddleware> _logger;

        public ErrorReportingMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider,
            IApplicationVersionProvider applicationVersion,
            ILogger<ErrorReportingMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _applicationVersion = applicationVersion;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Report the error in the background (don't block the exception handling)
                _ = Task.Run(async () => await ReportErrorAsync(context, ex));

                // Rethrow to preserve normal error handling pipeline
                throw;
            }
        }

        private async Task ReportErrorAsync(HttpContext context, Exception exception)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                // Check if error reporting is enabled
                var systemSettingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var settings = await systemSettingsService.GetSettingsAsync();

                if (settings == null || !settings.EnableUsageMonitoring)
                {
                    _logger.LogDebug("Error reporting is disabled, skipping submission");
                    return;
                }

                // Build error report
                var errorReport = BuildErrorReport(context, exception, settings);

                // Submit error report
                var errorClient = scope.ServiceProvider.GetRequiredService<ErrorReportClient>();
                var success = await errorClient.SubmitErrorReportAsync(errorReport);

                if (success)
                {
                    _logger.LogInformation("Error report {ErrorId} submitted successfully for exception {ExceptionType}",
                        errorReport.ErrorId, exception.GetType().Name);
                }
                else
                {
                    _logger.LogWarning("Failed to submit error report {ErrorId}", errorReport.ErrorId);
                }
            }
            catch (Exception ex)
            {
                // Don't let error reporting itself crash the application
                _logger.LogError(ex, "Failed to report error to Sentinel Feedback API");
            }
        }

        private ErrorReport BuildErrorReport(HttpContext context, Exception exception, Models.SystemSettings settings)
        {
            var eventId = Guid.NewGuid().ToString();
            var errorId = $"ERR-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            // Use framework route metadata rather than the incoming URL. This
            // deliberately excludes identifiers, query strings and arbitrary
            // user-controlled path segments from remote error reporting.
            var pageIdentifier = SemanticPageIdentifier.FromRequest(context);
            var module = ExtractModuleFromPageIdentifier(pageIdentifier);

            // Get environment name
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            // Build request context
            var requestContext = new RequestContext
            {
                Method = context.Request.Method,
                // The remote report is created before the global handler writes
                // the response, so report the actual unhandled-error status.
                StatusCode = StatusCodes.Status500InternalServerError
            };

            // Do not transmit exception messages, stack traces, source details,
            // request headers or trace IDs. Any of them can contain PHI, server
            // topology, paths or user-provided values. The fingerprint groups
            // recurring failures without exposing their content.
            var errorDetails = new ErrorDetails
            {
                ExceptionType = exception.GetType().Name,
                Fingerprint = CreateFingerprint(exception.GetType().Name, pageIdentifier, context.Request.Method)
            };

            // Build application context
            var applicationContext = new ApplicationContext
            {
                Environment = environment,
                Module = module,
                Route = pageIdentifier
            };

            return new ErrorReport
            {
                SchemaVersion = 1,
                EventId = eventId,
                ErrorId = errorId,
                OccurredAtUtc = DateTime.UtcNow,
                InstallationId = settings.InstallationId ?? "unknown",
                SentinelVersion = _applicationVersion.InformationalVersion.Length > 50 
                    ? _applicationVersion.InformationalVersion[..50] 
                    : _applicationVersion.InformationalVersion,
                Application = applicationContext,
                Error = errorDetails,
                Request = requestContext,
                Redaction = new RedactionInfo
                {
                    Applied = true,
                    Version = "sentinel-error-minimal-v2"
                }
            };
        }

        private static string ExtractModuleFromPageIdentifier(string pageIdentifier)
        {
            return pageIdentifier.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown";
        }

        private static string CreateFingerprint(string exceptionType, string pageIdentifier, string method)
        {
            var input = $"{exceptionType}|{pageIdentifier}|{method}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }
}
