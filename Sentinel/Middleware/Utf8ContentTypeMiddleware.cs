namespace Sentinel.Middleware;

/// <summary>
/// Ensures textual HTTP responses declare their UTF-8 encoding. ASP.NET Core
/// sets this for Razor and most JSON results, but static text assets and custom
/// response writers can otherwise omit the charset parameter.
/// </summary>
public sealed class Utf8ContentTypeMiddleware
{
    private readonly RequestDelegate _next;

    public Utf8ContentTypeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Register before downstream components write the response. OnStarting
        // runs immediately before headers are sent, including for static files.
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            var response = httpContext.Response;

            if (!HttpMethods.IsHead(httpContext.Request.Method) &&
                response.StatusCode is not (>= 100 and < 200 or 204 or 304) &&
                RequiresUtf8Charset(response.ContentType))
            {
                response.ContentType = $"{response.ContentType}; charset=utf-8";
            }

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }

    internal static bool RequiresUtf8Charset(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType) ||
            contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();

        return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("application/x-javascript", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase);
    }
}
