using Sentinel.Middleware;

namespace Sentinel.Extensions;

/// <summary>
/// Configures the Sentinel request pipeline. The order here is intentional:
/// proxy handling, security headers, routing, session, setup, identity,
/// authorization, and endpoint mapping each depend on the steps before them.
/// </summary>
public static class SentinelApplicationPipelineExtensions
{
    public static WebApplication UseSentinelApplicationPipeline(this WebApplication app, bool useForwardedHeaders)
    {
        if (useForwardedHeaders)
        {
            // Must run before HSTS and HTTPS redirection so the request scheme from
            // Caddy is used rather than the private HTTP hop to Kestrel.
            app.UseForwardedHeaders();
        }

        // This must be the outermost application handler. It logs every exception that
        // escapes an endpoint and replaces the response with a safe, traceable error.
        app.UseGlobalExceptionHandler();
        app.UseMiddleware<Utf8ContentTypeMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseHsts();
        }

        // API failures retain their status/body. Browser page requests can use the
        // friendly not-found page without converting API validation into HTML.
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseStatusCodePagesWithReExecute("/not-found"));

        app.UseHttpsRedirection();

        // Baseline browser hardening. SAMEORIGIN permits Sentinel's own embedded case
        // subforms while preventing other sites from framing authenticated pages.
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
                context.Response.Headers["Content-Security-Policy"] =
                    "base-uri 'self'; object-src 'none'; frame-ancestors 'self'; form-action 'self'";
                return Task.CompletedTask;
            });

            await next();
        });

        // Sensitive data used to be written under wwwroot/uploads and wwwroot/data.
        // Deny those legacy paths before static-file middleware so stale files cannot
        // be retrieved by a guessed URL during or after migration.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/uploads") ||
                context.Request.Path.StartsWithSegments("/data/timeline-entries") ||
                context.Request.Path.StartsWithSegments("/data/timeline-backups"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();

        // Apply the validated organisation culture before model binding and Razor
        // rendering. It never trusts an Accept-Language request header for this.
        app.UseMiddleware<OrganizationCultureMiddleware>();
        app.UseSession();

        // Must remain after routing but before authentication. The test host has no
        // deployment setup state, so it deliberately bypasses this redirect.
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseSetupRedirect();
        }

        app.UseRateLimiter();
        app.UseMiddleware<PageViewTrackingMiddleware>();
        app.UseMiddleware<ErrorReportingMiddleware>();
        app.UseAuthentication();

        // A later successful sign-in rotates the security stamp, rejecting an older
        // browser session before it reaches an endpoint.
        app.UseMiddleware<UserSessionValidationMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<DiseaseAccessMiddleware>();
        app.UseAntiforgery();

        app.MapRazorPages();
        app.MapControllers();
        app.MapRazorComponents<Sentinel.Components.App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
