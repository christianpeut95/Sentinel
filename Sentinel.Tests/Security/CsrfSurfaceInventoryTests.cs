using System.Text.RegularExpressions;

namespace Sentinel.Tests.Security;

/// <summary>
/// Protects the three ASP.NET Core mutation surfaces against accidental
/// antiforgery opt-outs: Razor Pages, MVC controllers and Minimal APIs.
/// </summary>
public sealed class CsrfSurfaceInventoryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RazorPagesAndControllers_UseGlobalAntiforgeryWithOnlyTheErrorPageOptingOut()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Program.cs"));
        Assert.Contains("AutoValidateAntiforgeryTokenAttribute", program, StringComparison.Ordinal);
        Assert.Contains("app.UseAntiforgery();", program, StringComparison.Ordinal);

        var optOuts = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "Sentinel"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("IgnoreAntiforgeryToken", StringComparison.Ordinal))
            .Select(RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Sentinel/Pages/Error.cshtml.cs"], optOuts);
    }

    [Fact]
    public void EveryUnsafeMinimalApiRoute_RequiresAnAntiforgeryToken()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Program.cs"));
        var maps = Regex.Matches(program, "app\\.Map(?<method>Post|Put|Patch|Delete)\\(\\s*\\\"(?<route>[^\\\"]+)\\\"")
            .Cast<Match>()
            .ToList();

        // The criteria builder now uses its typed MVC controller rather than
        // seven hand-parsed minimal API mutations in Program.cs.
        Assert.Equal(2, maps.Count);

        for (var index = 0; index < maps.Count; index++)
        {
            var end = index + 1 < maps.Count ? maps[index + 1].Index : program.Length;
            var endpointBlock = program[maps[index].Index..end];
            Assert.Contains("WithMetadata(new RequireAntiforgeryTokenAttribute(true))", endpointBlock, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReportBuilder_EmitsAndSendsAntiforgeryTokensForEveryUnsafeRequest()
    {
        var page = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Pages", "Reports", "Builder.cshtml"));
        var script = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "wwwroot", "js", "report-builder-actions.js"));

        Assert.Contains("id=\"reportBuilderAntiforgeryToken\"", page, StringComparison.Ordinal);
        Assert.Contains("@Html.AntiForgeryToken()", page, StringComparison.Ordinal);
        Assert.Contains("#reportBuilderAntiforgeryToken input[name=\"__RequestVerificationToken\"]", script, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Matches(script, "\\.\\.\\.this\\.getAntiforgeryHeaders\\(\\)").Count);
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
