using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Middleware
{
    /// <summary>
    /// Middleware that redirects all requests to the setup wizard if initial setup is not completed
    /// Ensures system cannot be used until properly configured
    /// </summary>
    public class SetupRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SetupRedirectMiddleware> _logger;

        public SetupRedirectMiddleware(RequestDelegate next, ILogger<SetupRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // ── Allow these paths without setup check ──────────────────
            // Setup wizard itself
            // NOTE: Access controlled in setup completion check below

            // Health check endpoint
            if (path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            // Blazor framework files
            if (path.StartsWith("/_framework") || path.StartsWith("/_blazor"))
            {
                await _next(context);
                return;
            }

            // Static files (CSS, JS, images for setup wizard)
            if (path.StartsWith("/css") || 
                path.StartsWith("/js") || 
                path.StartsWith("/lib") || 
                path.StartsWith("/images") ||
                path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            // ── Check if setup is completed ────────────────────────────
            try
            {
                var settings = await dbContext.SystemSettings.FirstOrDefaultAsync();
                var isSetupCompleted = settings?.IsSetupCompleted ?? false;

                if (!isSetupCompleted)
                {
                    // Setup not completed - redirect to setup wizard unless already on it
                    if (path.StartsWith("/setup"))
                    {
                        // Already on setup page, allow it
                        await _next(context);
                        return;
                    }

                    // Setup not completed - redirect to setup wizard
                    if (!context.Response.HasStarted)
                    {
                        _logger.LogDebug("Setup not completed, redirecting {Path} to /Setup", path);
                        context.Response.Redirect("/Setup");
                        return;
                    }
                }
                else
                {
                    // Setup IS completed - block access to setup page
                    if (path.StartsWith("/setup"))
                    {
                        _logger.LogWarning("Setup already completed, redirecting from /Setup to /");
                        if (!context.Response.HasStarted)
                        {
                            context.Response.Redirect("/");
                            return;
                        }
                    }
                }

                // Setup completed or allowed path - allow request to proceed
                await _next(context);
            }
            catch (Exception ex)
            {
                // Database might not be initialized yet
                // If SystemSettings table doesn't exist, redirect to setup
                _logger.LogWarning(ex, "Error checking setup status - assuming setup required");

                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect("/Setup");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Extension method to register SetupRedirectMiddleware
    /// </summary>
    public static class SetupRedirectMiddlewareExtensions
    {
        public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SetupRedirectMiddleware>();
        }
    }
}
