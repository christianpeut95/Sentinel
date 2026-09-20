using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentinel.Services;

/// <summary>
/// Records a handled exception without exposing internal implementation details to the user.
/// </summary>
public static class UserFacingError
{
    public static string Create(HttpContext httpContext, Exception exception)
    {
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Sentinel.UserFacingError");

        return Create(
            logger,
            exception,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User.Identity?.Name ?? "anonymous");
    }

    /// <summary>
    /// Records an exception while allowing a caller to give a safe, contextual
    /// explanation of a validation failure. The exception text itself is never
    /// sent to the client.
    /// </summary>
    public static string Create(HttpContext httpContext, Exception exception, string safeMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessage);

        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Sentinel.UserFacingError");
        var referenceId = Guid.NewGuid().ToString("N");

        logger.LogError(
            exception,
            "Handled request failure. ErrorReference: {ErrorReference}; Operation: {Operation}; Method: {Method}; User: {User}",
            referenceId,
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User.Identity?.Name ?? "anonymous");

        return $"{safeMessage} If the problem continues, contact an administrator and quote reference {referenceId}.";
    }

    public static string Create(ILogger logger, Exception exception, string? operation = null)
    {
        return Create(logger, exception, operation, null, null);
    }

    private static string Create(
        ILogger logger,
        Exception exception,
        string? operation,
        string? method,
        string? user)
    {
        var referenceId = Guid.NewGuid().ToString("N");

        logger.LogError(
            exception,
            "Handled request failure. ErrorReference: {ErrorReference}; Operation: {Operation}; Method: {Method}; User: {User}",
            referenceId,
            operation ?? "background operation",
            method,
            user);

        return $"We could not complete that request. Please try again. If the problem continues, contact an administrator and quote reference {referenceId}.";
    }
}
