using Sentinel.Models.Telemetry;
using Sentinel.Services;
using Sentinel.Services.Telemetry;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Middleware that captures unhandled exceptions and reports them to the Sentinel Feedback API
    /// </summary>
    public class ErrorReportingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly BreadcrumbTracker _breadcrumbTracker;
        private readonly IApplicationVersionProvider _applicationVersion;
        private readonly ILogger<ErrorReportingMiddleware> _logger;
        private readonly DateTime _processStartTime;

        // Regex patterns for sanitizing routes (same as PageViewTrackingMiddleware)
        private static readonly Regex GuidPattern = new Regex(
            @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex NumericIdPattern = new Regex(
            @"/\d+(/|$)",
            RegexOptions.Compiled);

        public ErrorReportingMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider,
            BreadcrumbTracker breadcrumbTracker,
            IApplicationVersionProvider applicationVersion,
            ILogger<ErrorReportingMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _breadcrumbTracker = breadcrumbTracker;
            _applicationVersion = applicationVersion;
            _logger = logger;
            _processStartTime = DateTime.UtcNow;
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
            var sanitizedRoute = SanitizeRoute(context.Request.Path.Value ?? "/");
            var module = ExtractModuleFromRoute(sanitizedRoute);

            // Get environment name
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            // Build runtime context
            var uptime = (long)(DateTime.UtcNow - _processStartTime).TotalSeconds;
            var runtime = new RuntimeContext
            {
                OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                ProcessUptimeSeconds = uptime
            };

            // Build request context
            var requestContext = new RequestContext
            {
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode > 0 ? context.Response.StatusCode : 500,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            };

            // Build error details with privacy-safe stack trace
            var stackTrace = SanitizeStackTrace(exception.StackTrace);
            var errorDetails = new ErrorDetails
            {
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                Source = exception.Source,
                TargetMethod = exception.TargetSite?.Name,
                StackTrace = stackTrace
            };

            // Build application context
            var applicationContext = new ApplicationContext
            {
                Environment = environment,
                Module = module,
                Route = sanitizedRoute
            };

            // Get recent breadcrumbs
            var breadcrumbs = _breadcrumbTracker.GetRecentBreadcrumbs();

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
                Runtime = runtime,
                Breadcrumbs = breadcrumbs,
                Redaction = new RedactionInfo
                {
                    Applied = true,
                    Version = "sentinel-redactor-v1"
                }
            };
        }

        private string SanitizeRoute(string path)
        {
            // Remove query string
            var questionMarkIndex = path.IndexOf('?');
            if (questionMarkIndex > 0)
            {
                path = path.Substring(0, questionMarkIndex);
            }

            // Replace GUIDs with placeholder
            path = GuidPattern.Replace(path, "{id}");

            // Replace numeric IDs with placeholder
            path = NumericIdPattern.Replace(path, "/{id}$1");

            return path.ToLowerInvariant();
        }

        private string? ExtractModuleFromRoute(string route)
        {
            // Extract the first segment as the module (e.g., /Cases/Create -> Cases)
            var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 ? segments[0] : null;
        }

        private string? SanitizeStackTrace(string? stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;

            // Filter to only Sentinel namespace lines and remove file paths
            var lines = stackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var filtered = lines
                .Where(line => line.Contains("Sentinel.") || line.Contains("   at "))
                .Select(line =>
                {
                    // Remove file paths (everything after " in ")
                    var inIndex = line.IndexOf(" in ");
                    if (inIndex > 0)
                    {
                        line = line.Substring(0, inIndex);
                    }
                    return line;
                })
                .Take(10); // Limit to first 10 lines

            return string.Join("\n", filtered);
        }
    }
}
