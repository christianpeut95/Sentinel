namespace Sentinel.Tests.Security;

/// <summary>
/// Guards against bypassing ASP.NET Core's typed model-binding boundary with
/// hand-parsed JSON in page routes or minimal APIs. The exceptions below are
/// intentionally narrow: low-level protocol services are not HTTP endpoints.
/// </summary>
public sealed class TrustedInputBoundarySourceTests
{
    [Fact]
    public void HttpEndpointsDoNotHandParseUntypedJsonRequestBodies()
    {
        var root = FindRepositoryRoot();
        var endpointRoots = new[]
        {
            Path.Combine(root, "Sentinel", "Program.cs"),
            Path.Combine(root, "Sentinel", "Controllers"),
            Path.Combine(root, "Sentinel", "Pages")
        };

        var sources = endpointRoots.SelectMany(path =>
            File.Exists(path)
                ? new[] { path }
                : Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories));

        foreach (var sourcePath in sources)
        {
            var source = File.ReadAllText(sourcePath);
            Assert.DoesNotContain("HttpRequest request", source, StringComparison.Ordinal);
            Assert.DoesNotContain("JsonSerializer.Deserialize<JsonElement>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadToEndAsync()", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CollectionConfigurationRequiresAModelBoundPayloadAndServerValidation()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Sentinel",
            "Controllers",
            "SaveCollectionConfigController.cs"));

        Assert.Contains("[FromBody] SaveConfigRequest request", source, StringComparison.Ordinal);
        Assert.Contains("!ModelState.IsValid", source, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(request.ConfigJson)", source, StringComparison.Ordinal);
        Assert.Contains("_validationService.ValidateConfig(config)", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        foreach (var candidate in new[]
                 {
                     new DirectoryInfo(Directory.GetCurrentDirectory()),
                     new DirectoryInfo(AppContext.BaseDirectory)
                 })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Sentinel.Tests")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }
}
