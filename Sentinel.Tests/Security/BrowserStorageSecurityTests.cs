namespace Sentinel.Tests.Security;

public sealed class BrowserStorageSecurityTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void BrowserStorageInventory_ContainsOnlyDocumentedWorkspaceAndPreferenceKeys()
    {
        var storageCallSites = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "Sentinel"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .Where(path => !NormalizePath(path).Contains("/wwwroot/lib/", StringComparison.OrdinalIgnoreCase) &&
                           !NormalizePath(path).Contains("/bin/", StringComparison.OrdinalIgnoreCase) &&
                           !NormalizePath(path).Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            .Select(path => new { Path = NormalizePath(path), Text = File.ReadAllText(path) })
            .Where(entry => entry.Text.Contains("localStorage", StringComparison.Ordinal) ||
                            entry.Text.Contains("sessionStorage", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(4, storageCallSites.Count);
        Assert.Contains(storageCallSites, entry => entry.Path.EndsWith("_Layout.cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(storageCallSites, entry => entry.Path.EndsWith("report-builder.js", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(storageCallSites, entry => entry.Path.EndsWith("/Settings/Diseases/Edit.cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(storageCallSites, entry => entry.Path.EndsWith("/Account/Login.cshtml", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LogoutAndUnauthenticatedEntry_ClearAuthenticatedBrowserWorkspaceState()
    {
        var layout = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Pages", "Shared", "_Layout.cshtml"));
        var login = File.ReadAllText(Path.Combine(RepositoryRoot, "Sentinel", "Areas", "Identity", "Pages", "Account", "Login.cshtml"));

        Assert.Contains("localStorage.removeItem('sentinel_report_draft')", layout, StringComparison.Ordinal);
        Assert.Contains("sessionStorage.clear()", layout, StringComparison.Ordinal);
        Assert.Contains("localStorage.removeItem('sentinel_report_draft')", login, StringComparison.Ordinal);
        Assert.Contains("sessionStorage.clear()", login, StringComparison.Ordinal);
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

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
