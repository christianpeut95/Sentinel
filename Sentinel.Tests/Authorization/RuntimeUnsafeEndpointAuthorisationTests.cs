using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sentinel.Tests.Authorization;

/// <summary>
/// Executes an unauthenticated request against every registered business
/// mutation route. This is deliberately runtime discovery rather than a
/// handwritten route list: adding a protected Razor Page, controller action or
/// minimal API brings it into the test automatically.
/// </summary>
public sealed class RuntimeUnsafeEndpointAuthorisationTests : IClassFixture<SentinelWebApplicationFactory>
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static readonly HashSet<string> AnonymousMutationRoutes =
    [
        "/Identity/Account/Login",
        "/Identity/Account/ForgotPassword",
        "/Identity/Account/ResetPassword"
    ];

    private readonly SentinelWebApplicationFactory _factory;

    public RuntimeUnsafeEndpointAuthorisationTests(SentinelWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EveryRegisteredProtectedBusinessMutation_AnonymousRequestIsChallenged()
    {
        var routes = DiscoverUnsafeBusinessRoutes()
            .Where(route => !route.AllowsAnonymous)
            .OrderBy(route => route.Path, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ToArray();

        // The source inventory presently contains 293 Razor handlers, 60 MVC
        // actions and two minimal API mutations. Multiple handlers share one
        // Razor endpoint, so the runtime count is lower but must still cover
        // the broad protected mutation surface.
        Assert.True(routes.Length >= 200,
            $"Expected at least 200 registered protected mutation endpoints; discovered {routes.Length}.");
        Assert.Contains(routes, route => route.Kind == "Razor Page");
        Assert.Contains(routes, route => route.Kind == "MVC controller");
        Assert.Contains(routes, route => route.Kind == "Minimal API");

        Console.WriteLine(
            $"Verified anonymous challenges for {routes.Length} registered unsafe endpoints " +
            $"({routes.Count(route => route.Kind == "Razor Page")} Razor Page, " +
            $"{routes.Count(route => route.Kind == "MVC controller")} MVC controller, " +
            $"{routes.Count(route => route.Kind == "Minimal API")} Minimal API).");

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var failures = new List<string>();
        foreach (var route in routes)
        {
            using var request = new HttpRequestMessage(new HttpMethod(route.Method), route.Path);
            var response = await client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                failures.Add($"{route.Method} {route.Path} ({route.DisplayName}) returned {(int)response.StatusCode} {response.StatusCode}");
            }
        }

        Assert.True(
            failures.Count == 0,
            "Anonymous mutation routes must challenge before their handlers execute:" +
            Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void OnlyDocumentedAccountRecoveryEndpointsAllowAnonymousUnsafeRequests()
    {
        var anonymousRoutes = DiscoverUnsafeBusinessRoutes()
            .Where(route => route.AllowsAnonymous)
            .Select(route => route.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(route => route, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(
            AnonymousMutationRoutes.OrderBy(route => route, StringComparer.OrdinalIgnoreCase),
            anonymousRoutes);
    }

    private IReadOnlyList<UnsafeRoute> DiscoverUnsafeBusinessRoutes()
    {
        return _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(CreateUnsafeRoutes)
            .ToArray();
    }

    private static IEnumerable<UnsafeRoute> CreateUnsafeRoutes(RouteEndpoint endpoint)
    {
        var rawPath = endpoint.RoutePattern.RawText;
        if (string.IsNullOrWhiteSpace(rawPath) || !IsBusinessEndpoint(endpoint, rawPath))
        {
            yield break;
        }

        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
            .Where(IsUnsafeMethod)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Razor Page route endpoints inherit their POST handler constraints at
        // invocation time. Treat each page that declares an unsafe handler as
        // a POST route so the global authorisation convention is tested.
        var pageDescriptor = endpoint.Metadata.GetMetadata<PageActionDescriptor>();
        var controllerDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if ((methods == null || methods.Length == 0) &&
            pageDescriptor is not null &&
            PageDeclaresUnsafeHandler(pageDescriptor))
        {
            methods = [HttpMethod.Post.Method];
        }

        if (methods == null || methods.Length == 0)
        {
            yield break;
        }

        var path = BuildConcretePath(rawPath);
        var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        foreach (var method in methods)
        {
            yield return new UnsafeRoute(
                method,
                path,
                endpoint.DisplayName ?? rawPath,
                allowsAnonymous,
                pageDescriptor is not null
                    ? "Razor Page"
                    : controllerDescriptor is not null
                        ? "MVC controller"
                        : "Minimal API");
        }
    }

    private static bool IsBusinessEndpoint(RouteEndpoint endpoint, string rawPath) =>
        endpoint.Metadata.GetMetadata<PageActionDescriptor>() is not null ||
        endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null ||
        rawPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

    private static bool PageDeclaresUnsafeHandler(PageActionDescriptor pageDescriptor)
    {
        var relativePath = pageDescriptor.RelativePath?.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var pageModelPath = Path.Combine(RepositoryRoot, "Sentinel", relativePath + ".cs");
        return File.Exists(pageModelPath) &&
               Regex.IsMatch(File.ReadAllText(pageModelPath), @"\bOnPost\w*\s*\(");
    }

    private static bool IsUnsafeMethod(string method) =>
        method.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethod.Put.Method, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethod.Patch.Method, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethod.Delete.Method, StringComparison.OrdinalIgnoreCase);

    private static string BuildConcretePath(string rawPath)
    {
        var path = Regex.Replace(rawPath, @"\{(?<parameter>[^}:?=]+)(?:=[^}:?]+)?(?::[^}?]+)?\??\}", match =>
        {
            var parameter = match.Groups["parameter"].Value.Trim('*').ToLowerInvariant();
            if (parameter.Contains("id", StringComparison.Ordinal) || parameter.Contains("guid", StringComparison.Ordinal))
            {
                return "00000000-0000-0000-0000-000000000001";
            }

            if (parameter.Contains("date", StringComparison.Ordinal))
            {
                return "2026-01-01";
            }

            if (parameter.Contains("page", StringComparison.Ordinal) || parameter.Contains("index", StringComparison.Ordinal))
            {
                return "1";
            }

            return "test";
        });

        return path.StartsWith('/') ? path : "/" + path;
    }

    private sealed record UnsafeRoute(
        string Method,
        string Path,
        string DisplayName,
        bool AllowsAnonymous,
        string Kind);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Sentinel")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Sentinel.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }
}
