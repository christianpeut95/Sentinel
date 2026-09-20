namespace Sentinel.Tests.Security;

/// <summary>
/// Guards endpoints that used to serialise EF entities directly.  These checks
/// deliberately fail if ownership, audit, or navigation properties are added
/// back to the line-list configuration HTTP contract without review.
/// </summary>
public sealed class ApiResponseContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void LineListConfigurationEndpoint_UsesExplicitInputAndOutputContracts()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Controllers",
            "LineListController.cs"));

        Assert.DoesNotContain("[FromBody] OutbreakLineListConfiguration", source, StringComparison.Ordinal);
        Assert.DoesNotContain("return Ok(saved)", source, StringComparison.Ordinal);
        Assert.Contains("[FromBody] SaveLineListConfigurationRequest request", source, StringComparison.Ordinal);
        Assert.Contains("return Ok(fields.Select(ToFieldResponse))", source, StringComparison.Ordinal);
        Assert.Contains("return Ok(data.Select(ToDataResponse))", source, StringComparison.Ordinal);
        Assert.Contains("new LineListConfigurationsResponse(", source, StringComparison.Ordinal);
        Assert.Contains("userConfigs.Select(ToConfigurationResponse).ToList()", source, StringComparison.Ordinal);
        Assert.Contains("sharedConfigs.Select(ToConfigurationResponse).ToList()", source, StringComparison.Ordinal);
        Assert.Contains("return Ok(ToConfigurationResponse(saved))", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record LineListConfigurationResponse(", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record LineListFieldResponse(", source, StringComparison.Ordinal);
        Assert.Contains("public sealed record LineListDataResponse(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string? UserId,", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string? CreatedByUserId,", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LineListDataAndExport_AllowOnlyConfiguredFields()
    {
        var controller = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Controllers",
            "LineListController.cs"));
        var service = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Services",
            "LineListService.cs"));

        Assert.Equal(2, CountOccurrences(
            controller,
            "ContainsOnlyConfiguredFieldsAsync(request.OutbreakId, request.FieldPaths)"));
        Assert.Contains("One or more selected fields are not available for this outbreak.", controller, StringComparison.Ordinal);
        Assert.Contains("var allowedFieldPaths = (await GetAvailableFieldsAsync(outbreakId))", service, StringComparison.Ordinal);
        Assert.Contains(".Where(allowedFieldPaths.Contains)", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WriteLine", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WriteLine", service, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var startIndex = 0;
        while ((startIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
    }

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
