using System.Diagnostics;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Global exception handler middleware that logs all unhandled exceptions
    /// and provides consistent error responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Generate trace ID for tracking
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            // Log the exception with full details
            _logger.LogError(exception,
                "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}, User: {User}",
                traceId,
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous");

            // Don't modify response if already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, cannot handle exception properly");
                return;
            }

            context.Response.Clear();

            // AJAX Razor Page handlers also expect JSON, despite not living
            // below /api. Returning an error page redirect to them hides the
            // useful message and makes their client-side error handling fail.
            var acceptsJson = context.Request.Headers.Accept.Any(value =>
                value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
            var isAjax = string.Equals(context.Request.Headers["X-Requested-With"],
                "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            // For API and JSON requests, return a safe error with a trace ID.
            if (context.Request.Path.StartsWithSegments("/api") || acceptsJson || isAjax)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    error = "We could not complete that request. Please try again. If the problem continues, contact an administrator and quote the reference ID.",
                    traceId = traceId,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsJsonAsync(response);
            }
            else
            {
                // For page requests, redirect to the safe error page. The
                // detailed exception is only present in the application log.
                context.Response.Redirect($"/Error?requestId={Uri.EscapeDataString(traceId)}");
            }
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
