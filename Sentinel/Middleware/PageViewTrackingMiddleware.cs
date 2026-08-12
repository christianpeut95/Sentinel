using Sentinel.Services.Telemetry;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Middleware that tracks page views for usage monitoring
    /// Converts routed pages into privacy-safe semantic identifiers.
    /// </summary>
    public class PageViewTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ActivityTracker _activityTracker;
        private readonly BreadcrumbTracker _breadcrumbTracker;
        private readonly ILogger<PageViewTrackingMiddleware> _logger;

        public PageViewTrackingMiddleware(
            RequestDelegate next,
            ActivityTracker activityTracker,
            BreadcrumbTracker breadcrumbTracker,
            ILogger<PageViewTrackingMiddleware> logger)
        {
            _next = next;
            _activityTracker = activityTracker;
            _breadcrumbTracker = breadcrumbTracker;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            string? pageIdentifier = null;

            try
            {
                // Capture the routed page before executing the request. A 404 can be
                // re-executed as /not-found by StatusCodePages, so resolving it after
                // the pipeline completes would identify the error page rather than the
                // requested page.
                if (ShouldTrackRequest(context, path))
                {
                    pageIdentifier = SemanticPageIdentifier.FromRequest(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving page view for {Path}", context.Request.Path);
            }

            await _next(context);

            try
            {
                // Count only successful HTML page requests. This excludes unmatched
                // paths, failed assets, and the /not-found re-execution for 404s.
                if (string.IsNullOrWhiteSpace(pageIdentifier) ||
                    string.Equals(pageIdentifier, "Unknown", StringComparison.OrdinalIgnoreCase) ||
                    context.Response.StatusCode < StatusCodes.Status200OK ||
                    context.Response.StatusCode >= StatusCodes.Status300MultipleChoices)
                {
                    return;
                }

                _activityTracker.TrackPageView(pageIdentifier);
                _breadcrumbTracker.AddNavigationBreadcrumb(pageIdentifier);
                _logger.LogDebug("Tracked page view: {PageIdentifier}", pageIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking completed page view for {Path}", context.Request.Path);
            }
        }

        private bool ShouldTrackRequest(HttpContext context, string path)
        {
            return HttpMethods.IsGet(context.Request.Method) &&
                AcceptsHtml(context.Request) &&
                context.GetEndpoint() is not null &&
                ShouldTrackPath(path);
        }

        private static bool AcceptsHtml(HttpRequest request)
        {
            return request.Headers.Accept.Any(value =>
                value.Contains("text/html", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determine if the path should be tracked
        /// </summary>
        private bool ShouldTrackPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Don't track API endpoints
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                return false;

            // Don't track static files
            if (path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/_blazor/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
                return false;

            // Don't track health checks
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

    }

    /// <summary>
    /// Extension method for registering page view tracking middleware
    /// </summary>
    public static class PageViewTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UsePageViewTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PageViewTrackingMiddleware>();
        }
    }
}
