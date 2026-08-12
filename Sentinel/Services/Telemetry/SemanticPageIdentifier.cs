using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Sentinel.Services.Telemetry;

/// <summary>
/// Produces privacy-safe, stable page identifiers for usage telemetry.
/// Route values and query strings are intentionally excluded.
/// </summary>
public static class SemanticPageIdentifier
{
    private static readonly IReadOnlyDictionary<string, string> SegmentAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["addlabresult"] = "AddLabResult",
            ["mytasks"] = "MyTasks",
            ["not-found"] = "NotFound",
            ["notfound"] = "NotFound"
        };

    /// <summary>
    /// Gets the semantic identifier for a routed Sentinel page without retaining request values.
    /// </summary>
    public static string FromRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var endpoint = context.GetEndpoint();
        var pageDescriptor = endpoint?.Metadata.GetMetadata<PageActionDescriptor>();
        if (pageDescriptor is not null)
        {
            return FromPageDescriptor(pageDescriptor);
        }

        var controllerDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (controllerDescriptor is not null)
        {
            return FromControllerDescriptor(controllerDescriptor);
        }

        // Interactive Blazor component endpoints do not expose a Razor Page or
        // controller descriptor. Their route template is framework metadata, so it
        // can safely be converted without retaining request path values.
        var routeTemplate = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        if (!string.IsNullOrWhiteSpace(routeTemplate))
        {
            return FromPath(routeTemplate);
        }

        // A non-matched request must not cause arbitrary path content to be sent in telemetry.
        return string.Equals(context.Request.Path.Value, "/not-found", StringComparison.OrdinalIgnoreCase)
            ? "NotFound"
            : "Unknown";
    }

    /// <summary>
    /// Converts a known page route or route template into a dot-separated page identifier.
    /// This method is intended for framework-provided page paths, not untrusted request paths.
    /// </summary>
    public static string FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return "Home";
        }

        var pathWithoutQuery = path.Split('?', 2)[0];
        var segments = pathWithoutQuery
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => !IsRouteParameterOrIdentifier(segment))
            .Select(ToSemanticSegment)
            .Where(segment => !string.IsNullOrEmpty(segment))
            .ToList();

        if (segments.Count == 0)
        {
            return "Home";
        }

        return string.Join('.', segments);
    }

    private static string FromPageDescriptor(PageActionDescriptor descriptor)
    {
        var path = descriptor.ViewEnginePath;
        if (string.IsNullOrWhiteSpace(path) || path == "/Index")
        {
            return string.IsNullOrWhiteSpace(descriptor.AreaName)
                ? "Home"
                : ToSemanticSegment(descriptor.AreaName);
        }

        var pageIdentifier = FromPath(path);
        return string.IsNullOrWhiteSpace(descriptor.AreaName)
            ? pageIdentifier
            : $"{ToSemanticSegment(descriptor.AreaName)}.{pageIdentifier}";
    }

    private static string FromControllerDescriptor(ControllerActionDescriptor descriptor)
    {
        var segments = new List<string>();

        if (descriptor.RouteValues.TryGetValue("area", out var areaName) &&
            !string.IsNullOrWhiteSpace(areaName))
        {
            segments.Add(ToSemanticSegment(areaName));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ControllerName))
        {
            segments.Add(ToSemanticSegment(descriptor.ControllerName));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ActionName) &&
            !string.Equals(descriptor.ActionName, "Index", StringComparison.OrdinalIgnoreCase))
        {
            segments.Add(ToSemanticSegment(descriptor.ActionName));
        }

        return segments.Count == 0 ? "Unknown" : string.Join('.', segments);
    }

    private static bool IsRouteParameterOrIdentifier(string segment)
    {
        return (segment.StartsWith('{') && segment.EndsWith('}')) ||
               Guid.TryParse(segment, out _) ||
               long.TryParse(segment, out _);
    }

    private static string ToSemanticSegment(string segment)
    {
        if (SegmentAliases.TryGetValue(segment, out var alias))
        {
            return alias;
        }

        return string.Concat(segment
            .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}
