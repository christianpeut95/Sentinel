using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Sentinel.Models;

namespace Sentinel.Middleware;

/// <summary>
/// Rejects an application cookie whose security stamp no longer represents the
/// current user record. ASP.NET Identity's security-stamp validator provides
/// the same protection; retaining this small middleware makes the immediate
/// session-replacement requirement explicit in Sentinel's request pipeline.
/// </summary>
public sealed class UserSessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserSessionValidationMiddleware> _logger;

    public UserSessionValidationMiddleware(
        RequestDelegate next,
        ILogger<UserSessionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> identityOptions)
    {
        // Authorization integration tests intentionally replace Sentinel's
        // application cookie with a synthetic authentication scheme. Session
        // stamps belong only to the real Identity application cookie, so do
        // not impose this cookie-specific check on another scheme.
        var applicationIdentity = context.User.Identities.FirstOrDefault(identity =>
            identity.IsAuthenticated &&
            string.Equals(
                identity.AuthenticationType,
                IdentityConstants.ApplicationScheme,
                StringComparison.Ordinal));

        if (applicationIdentity is not null)
        {
            var user = await userManager.GetUserAsync(context.User);
            var cookieSecurityStamp = applicationIdentity.FindFirst(
                identityOptions.Value.ClaimsIdentity.SecurityStampClaimType)?.Value;

            var isValid = user is not null &&
                          user.IsEnabled &&
                          !string.IsNullOrWhiteSpace(cookieSecurityStamp) &&
                          string.Equals(
                              cookieSecurityStamp,
                              await userManager.GetSecurityStampAsync(user),
                              StringComparison.Ordinal);

            if (!isValid)
            {
                _logger.LogInformation(
                    "Rejected an outdated or disabled user session for user {UserId}",
                    user?.Id ?? "unknown");

                await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                var loginUrl = $"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(loginUrl);
                return;
            }
        }

        await _next(context);
    }
}
