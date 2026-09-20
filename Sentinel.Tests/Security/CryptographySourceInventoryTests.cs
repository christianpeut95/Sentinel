namespace Sentinel.Tests.Security;

/// <summary>
/// Guards Sentinel's direct cryptographic API surface against algorithms and
/// modes that do not meet Sentinel's ASVS Level 1 baseline. Framework-managed
/// ASP.NET Core Data Protection remains permitted; it owns algorithm selection
/// and key rotation rather than application code selecting a cipher mode.
/// </summary>
public sealed class CryptographySourceInventoryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ApplicationSource_DoesNotSelectWeakOrInsecureCryptographicPrimitives()
    {
        var prohibitedApiMarkers = new[]
        {
            "CipherMode.ECB",
            "PaddingMode.PKCS1",
            "RSAEncryptionPadding.Pkcs1",
            "TripleDES",
            "Rijndael",
            "RC2",
            "RC4",
            "DES.Create",
            "DES()"
        };

        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "Sentinel"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var findings = sourceFiles
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new SourceLine(path, index + 1, line)))
            .Where(sourceLine => prohibitedApiMarkers.Any(marker =>
                sourceLine.Text.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(findings.Length == 0,
            "Sentinel must not select weak or insecure cryptographic primitives. Findings:" + Environment.NewLine +
            string.Join(Environment.NewLine, findings.Select(finding =>
                $"{finding.Path}:{finding.LineNumber}: {finding.Text.Trim()}")));
    }

    [Fact]
    public void SensitiveConfigurationEncryption_UsesAspNetCoreDataProtection()
    {
        var encryptionService = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Services",
            "EncryptionService.cs"));

        Assert.Contains("Microsoft.AspNetCore.DataProtection", encryptionService, StringComparison.Ordinal);
        Assert.Contains("IDataProtector", encryptionService, StringComparison.Ordinal);
        Assert.Contains("_protector.Protect", encryptionService, StringComparison.Ordinal);
        Assert.Contains("_protector.Unprotect", encryptionService, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", encryptionService, StringComparison.Ordinal);
        Assert.DoesNotContain("computedHash.Equals", encryptionService, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Sentinel.Tests")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }

    private sealed record SourceLine(string Path, int LineNumber, string Text);
}
