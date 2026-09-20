using Sentinel.Models;
using Sentinel.Services;
using Sentinel.Controllers;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Tests.Services;

public sealed class SurveyVersionWorkflowPolicyTests
{
    [Theory]
    [InlineData(SurveyVersionStatus.Draft, true)]
    [InlineData(SurveyVersionStatus.Active, false)]
    [InlineData(SurveyVersionStatus.Archived, false)]
    public void OnlyDraftSurveyVersionsCanBePublishedOrArchived(
        SurveyVersionStatus status,
        bool allowed)
    {
        Assert.Equal(allowed, SurveyVersionWorkflowPolicy.CanPublish(status));
        Assert.Equal(allowed, SurveyVersionWorkflowPolicy.CanArchive(status));
    }

    [Fact]
    public void AllSurveyVersionMutationPathsUseTheSharedPolicy()
    {
        var root = FindRepositoryRoot();
        var relativePaths = new[]
        {
            "Sentinel/Controllers/SurveyVersionController.cs",
            "Sentinel/Pages/Settings/Surveys/EditSurveyTemplate.cshtml.cs",
            "Sentinel/Pages/Settings/Surveys/SurveyTemplateDetails.cshtml.cs"
        };

        foreach (var relativePath in relativePaths)
        {
            var source = File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("SurveyVersionWorkflowPolicy", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SaveAsVersionRequest_RejectsAnOversizedVersionNumberAtTheRequestBoundary()
    {
        var request = new SaveAsVersionRequest
        {
            ParentSurveyId = Guid.NewGuid(),
            VersionNumber = new string('9', 21),
            SurveyDefinitionJson = "{}"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(SaveAsVersionRequest.VersionNumber)));
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
