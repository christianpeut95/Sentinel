using System.Text.RegularExpressions;

namespace Sentinel.Tests.Security;

/// <summary>
/// Keeps the completed page/controller/minimal-API authorization inventory from
/// silently regressing when a new web surface is introduced.
/// </summary>
public sealed class AuthorizationSurfaceInventoryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RazorPages_UseGlobalAuthenticationAndOnlyDocumentedGenericOrAnonymousExceptions()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Program.cs"));
        Assert.Contains("options.Conventions.AuthorizeFolder(\"/\")", program, StringComparison.Ordinal);

        var pageModels = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "Sentinel"), "*.cshtml.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains("\\Pages\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("class ", StringComparison.Ordinal))
            .ToList();

        Assert.True(pageModels.Count >= 240, "The Razor Page inventory unexpectedly shrank; review the source-discovery test.");

        var pathsWithNoAuthorizationAttribute = pageModels
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return !source.Contains("[Authorize", StringComparison.Ordinal) &&
                       !source.Contains("[AllowAnonymous", StringComparison.Ordinal);
            })
            .Select(RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Sentinel/Pages/Error.cshtml.cs",
            "Sentinel/Pages/Forbidden.cshtml.cs",
            "Sentinel/Pages/Index.cshtml.cs",
            "Sentinel/Pages/NotFound.cshtml.cs",
            "Sentinel/Pages/Privacy.cshtml.cs",
            "Sentinel/Pages/Settings/About.cshtml.cs"
        ], pathsWithNoAuthorizationAttribute);

        var plainAuthenticatedPages = pageModels
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("[Authorize]", StringComparison.Ordinal) &&
                       !source.Contains("Permission.", StringComparison.Ordinal) &&
                       !source.Contains("[AllowAnonymous", StringComparison.Ordinal);
            })
            .Select(RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Sentinel/Areas/Identity/Pages/Account/Logout.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/GenerateRecoveryCodes.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/ResetAuthenticator.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/ShowRecoveryCodes.cshtml.cs",
            "Sentinel/Areas/Identity/Pages/Account/Manage/TwoFactorAuthentication.cshtml.cs",
            "Sentinel/Pages/Dashboard.cshtml.cs",
            "Sentinel/Pages/Help/CollectionMapping.cshtml.cs",
            // This page only records a signed-in user's acceptance before a
            // separately authorised report page loads WebDataRocks. It does
            // not expose report data or configuration itself.
            "Sentinel/Pages/Reports/WebDataRocksTerms.cshtml.cs"
        ], plainAuthenticatedPages);
    }

    [Fact]
    public void Controllers_AllHaveAuthorizationAndOnlyDocumentedControllersUsePlainAuthentication()
    {
        var controllers = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "Sentinel", "Controllers"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bclass\s+\w+Controller\b"))
            .ToList();

        Assert.True(controllers.Count >= 25, "The controller inventory unexpectedly shrank; review the source-discovery test.");

        var missingAuthorization = controllers
            .Where(path => !File.ReadAllText(path).Contains("[Authorize", StringComparison.Ordinal))
            .Select(RelativePath)
            .ToArray();
        Assert.Empty(missingAuthorization);

        var anonymousControllers = controllers
            .Where(path => File.ReadAllText(path).Contains("[AllowAnonymous", StringComparison.Ordinal))
            .Select(RelativePath)
            .ToArray();
        Assert.Empty(anonymousControllers);

        var plainAuthenticatedControllers = controllers
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("[Authorize]", StringComparison.Ordinal) &&
                       !source.Contains("Permission.", StringComparison.Ordinal);
            })
            .Select(RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Sentinel/Controllers/Api/FeedbackApiController.cs",
            "Sentinel/Controllers/ProtectedAttachmentsController.cs"
        ], plainAuthenticatedControllers);
    }

    [Fact]
    public void MinimalApis_AllApiRoutesRequireAuthorization_AndOnlyHealthIsAnonymous()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Program.cs"));
        var maps = Regex.Matches(program, "app\\.Map(?:Get|Post|Put|Patch|Delete)\\(\\s*\\\"(?<route>[^\\\"]+)\\\"")
            .Cast<Match>()
            .ToList();

        // Case-definition criteria routes moved from Program.cs to an
        // authorized typed MVC controller, leaving these twelve minimal APIs.
        Assert.Equal(12, maps.Count);

        for (var index = 0; index < maps.Count; index++)
        {
            var route = maps[index].Groups["route"].Value;
            var end = index + 1 < maps.Count ? maps[index + 1].Index : program.Length;
            var endpointBlock = program[maps[index].Index..end];

            if (route == "/health")
            {
                Assert.Contains(".AllowAnonymous()", endpointBlock, StringComparison.Ordinal);
                continue;
            }

            Assert.StartsWith("/api/", route, StringComparison.Ordinal);
            Assert.Contains(".RequireAuthorization(", endpointBlock, StringComparison.Ordinal);
            Assert.DoesNotContain(".AllowAnonymous()", endpointBlock, StringComparison.Ordinal);
        }
    }

    private static string RelativePath(string path) =>
        Path.GetRelativePath(RepositoryRoot, path).Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
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
