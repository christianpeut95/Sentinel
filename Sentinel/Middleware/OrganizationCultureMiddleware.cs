using System.Globalization;
using Sentinel.Services;

namespace Sentinel.Middleware;

/// <summary>
/// Applies Sentinel's validated organisation culture for each request. Culture
/// is intentionally not taken from an arbitrary request header.
/// </summary>
public sealed class OrganizationCultureMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationCultureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IApplicationTimeZoneService regionalSettings)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = regionalSettings.AppCulture;
            CultureInfo.CurrentUICulture = regionalSettings.AppCulture;
            await _next(context);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
