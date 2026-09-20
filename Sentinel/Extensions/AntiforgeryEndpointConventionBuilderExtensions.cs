using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sentinel.Extensions;

/// <summary>
/// Makes antiforgery validation a true request gate for minimal API endpoints.
/// <see cref="RequireAntiforgeryTokenAttribute"/> supplies metadata for the
/// middleware, but that middleware deliberately does not short-circuit route
/// execution on validation failure. State-changing cookie-authenticated APIs
/// must therefore validate explicitly before their handler can mutate data.
/// </summary>
public static class AntiforgeryEndpointConventionBuilderExtensions
{
    public static TBuilder RequireValidAntiforgery<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                var antiforgery = context.HttpContext.RequestServices
                    .GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest(new
                {
                    error = "Antiforgery token validation failed."
                });
            }

            return await next(context);
        });

        return builder;
    }
}
