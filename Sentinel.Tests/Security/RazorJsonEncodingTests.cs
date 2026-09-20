using System.Text.Json;

namespace Sentinel.Tests.Security;

/// <summary>
/// Guards Razor-to-JavaScript data hand-offs. Razor pages use Html.Raw only
/// after JSON serialization so JavaScript receives a value rather than a
/// separately concatenated source fragment. System.Text.Json's default
/// encoder escapes HTML-significant characters, including the script end tag.
/// </summary>
public sealed class RazorJsonEncodingTests
{
    [Fact]
    public void DefaultJsonSerializer_EscapesScriptTerminatorsAndJavaScriptControlCharacters()
    {
        const string hostileValue = "</script><script>alert('x')</script>\u2028\u2029\\\"";

        var serialized = JsonSerializer.Serialize(hostileValue);

        Assert.DoesNotContain("</script>", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C/script\\u003E", serialized, StringComparison.Ordinal);
        Assert.Contains("\\u2028", serialized, StringComparison.Ordinal);
        Assert.Contains("\\u2029", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void RazorRawJsonHandoffs_UseOnlyFrameworkJsonHelpers()
    {
        var pageFiles = Directory.GetFiles(Path.Combine(GetRepositoryRoot(), "Sentinel", "Pages"), "*.cshtml", SearchOption.AllDirectories);
        var rawLines = pageFiles
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new RawLine(path, index + 1, line)))
            .Where(entry => entry.Text.Contains("@Html.Raw", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(rawLines);

        var unsafeLines = rawLines
            .Where(entry => !entry.Text.Contains("JsonSerializer.Serialize", StringComparison.Ordinal)
                         && !entry.Text.Contains("Json.Serialize", StringComparison.Ordinal))
            .ToList();

        Assert.True(unsafeLines.Count == 0,
            $"Every Html.Raw JavaScript hand-off must use a framework JSON helper. Unexpected call sites:{Environment.NewLine}" +
            string.Join(Environment.NewLine, unsafeLines.Select(entry => $"{entry.Path}:{entry.LineNumber}: {entry.Text.Trim()}")));
    }

    [Fact]
    public void SentinelSource_DoesNotOptIntoUnsafeJsonEscaping()
    {
        var sourceFiles = Directory.GetFiles(Path.Combine(GetRepositoryRoot(), "Sentinel"), "*.cs*", SearchOption.AllDirectories);
        var unsafeEscapingReferences = sourceFiles
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new RawLine(path, index + 1, line)))
            .Where(entry => entry.Text.Contains("UnsafeRelaxedJsonEscaping", StringComparison.Ordinal)
                         || entry.Text.Contains("JavaScriptEncoder.UnsafeRelaxedJsonEscaping", StringComparison.Ordinal))
            .ToList();

        Assert.True(unsafeEscapingReferences.Count == 0,
            $"Unsafe JSON escaping is not permitted in Sentinel source:{Environment.NewLine}" +
            string.Join(Environment.NewLine, unsafeEscapingReferences.Select(entry => $"{entry.Path}:{entry.LineNumber}: {entry.Text.Trim()}")));
    }

    [Fact]
    public void DynamicPopupAndRedirectResponses_SerializeServerValuesBeforeWritingJavaScript()
    {
        var root = GetRepositoryRoot();
        var patientCreate = File.ReadAllText(Path.Combine(root, "Sentinel", "Pages", "Patients", "Create.cshtml.cs"));
        var addExposure = File.ReadAllText(Path.Combine(root, "Sentinel", "Pages", "Cases", "AddExposure.cshtml.cs"));

        Assert.Contains("var contactPayload = JsonSerializer.Serialize", patientCreate, StringComparison.Ordinal);
        Assert.Contains("contactCreated(contact.Id, contact.FriendlyId, contact.PatientName)", patientCreate, StringComparison.Ordinal);
        Assert.DoesNotContain("patientName.Replace", patientCreate, StringComparison.Ordinal);
        Assert.Contains("var caseDetailsUrl = JsonSerializer.Serialize", addExposure, StringComparison.Ordinal);
        Assert.Contains("window.location.href = \" + caseDetailsUrl", addExposure, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        var candidates = new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        };

        foreach (var candidate in candidates)
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root for Razor JSON safety checks.");
    }

    private sealed record RawLine(string Path, int LineNumber, string Text);
}
