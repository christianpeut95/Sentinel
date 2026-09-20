using Sentinel.Models.CaseDefinitions;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class CaseDefinitionWorkflowPolicyTests
{
    [Theory]
    [InlineData(CaseDefinitionStatus.Draft, true, true, true)]
    [InlineData(CaseDefinitionStatus.Current, false, false, true)]
    [InlineData(CaseDefinitionStatus.Archived, false, false, false)]
    public void LifecycleActionsAllowOnlyDefinedStateTransitions(
        CaseDefinitionStatus status,
        bool canActivate,
        bool canSaveDraft,
        bool canArchive)
    {
        Assert.Equal(canActivate, CaseDefinitionWorkflowPolicy.CanActivate(status));
        Assert.Equal(canSaveDraft, CaseDefinitionWorkflowPolicy.CanSaveDraft(status));
        Assert.Equal(canArchive, CaseDefinitionWorkflowPolicy.CanArchive(status));
    }

    [Fact]
    public void EveryCaseDefinitionStatusMutationUsesTheSharedPolicy()
    {
        var root = FindRepositoryRoot();
        var paths = new[]
        {
            "Sentinel/Pages/Settings/CaseDefinitions/Review.cshtml.cs",
            "Sentinel/Pages/Settings/CaseDefinitions/Index.cshtml.cs"
        };

        foreach (var path in paths)
        {
            var source = File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("CaseDefinitionWorkflowPolicy", source, StringComparison.Ordinal);
        }
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
