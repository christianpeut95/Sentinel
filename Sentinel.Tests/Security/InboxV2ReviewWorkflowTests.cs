using Sentinel.Pages.DataInbox;

namespace Sentinel.Tests.Security;

public sealed class InboxV2ReviewWorkflowTests
{
    [Theory]
    [InlineData("CaseChange", "FieldChanged", false)]
    [InlineData("CaseChange", "Updated", false)]
    [InlineData("LabResult", "New", false)]
    [InlineData("Other", "Updated", false)]
    [InlineData("PotentialDuplicate", "Detected", true)]
    public void DismissVisibility_UsesTheSameServerRuleAsTheActionBar(
        string entityType,
        string changeType,
        bool expected)
    {
        Assert.Equal(expected, InboxV2Model.CanDismissReview(entityType, changeType));
    }

    [Fact]
    public void DirectInboxReviewMutations_RequirePendingStateAndConstrainKeepAsNew()
    {
        var source = ReadSentinelFile("Pages/DataInbox/InboxV2.cshtml.cs");

        Assert.Equal(3, Count(source, "r.Id == id && r.ReviewStatus == ReviewStatuses.Pending"));
        Assert.Contains("!CanDismissReview(item.EntityType, item.ChangeType)", source, StringComparison.Ordinal);
        Assert.Contains("item.ChangeType != \"PotentialDuplicate\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InboxActionBar_DelegatesDismissVisibilityToTheServerRule()
    {
        var source = ReadSentinelFile("Pages/DataInbox/InboxV2.cshtml");

        Assert.Contains("InboxV2Model.CanDismissReview(item.EntityType, item.ChangeType)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectReviewResolutionHandlers_RequirePendingStateAndTheirOwnWorkflowType()
    {
        var detailSource = ReadSentinelFile("Pages/DataInbox/Review.cshtml.cs");
        var indexSource = ReadSentinelFile("Pages/DataInbox/Index.cshtml.cs");

        Assert.Contains("r.ReviewStatus == ReviewStatuses.Pending", detailSource, StringComparison.Ordinal);
        Assert.Contains("r.ChangeType == \"PotentialDuplicate\" || r.ChangeType == \"DuplicateDetected\"", detailSource, StringComparison.Ordinal);
        Assert.Contains("r.ChangeType == \"PendingCreation\"", detailSource, StringComparison.Ordinal);
        Assert.Contains("r.ReviewStatus == ReviewStatuses.Pending", indexSource, StringComparison.Ordinal);
        Assert.Contains("r.ChangeType == \"PendingCreation\"", indexSource, StringComparison.Ordinal);
    }

    [Fact]
    public void SurveyMappingRetry_IsServerBoundAndDoesNotExposeStoredResponsesToTheBrowser()
    {
        var detailSource = ReadSentinelFile("Pages/DataInbox/Review.cshtml.cs");
        var viewSource = ReadSentinelFile("Pages/DataInbox/Review.cshtml");

        Assert.Contains("OnPostReprocessSurveyAsync", detailSource, StringComparison.Ordinal);
        Assert.Contains("CanResolveReviewsAsync()", detailSource, StringComparison.Ordinal);
        Assert.Contains("r.Id == id && r.ReviewStatus == ReviewStatuses.Pending", detailSource, StringComparison.Ordinal);
        Assert.Contains("SurveyMappingError", detailSource, StringComparison.Ordinal);
        Assert.Contains("CanAccessCaseAsync(review.CaseId.Value)", detailSource, StringComparison.Ordinal);
        Assert.Contains("task.SurveyResponseJson", detailSource, StringComparison.Ordinal);

        Assert.Contains("asp-page-handler=\"ReprocessSurvey\"", viewSource, StringComparison.Ordinal);
        Assert.Contains("js-reprocess-survey-form", viewSource, StringComparison.Ordinal);
        Assert.Contains("TempData[\"ErrorMessage\"]", viewSource, StringComparison.Ordinal);
        Assert.DoesNotContain("function reprocessSurvey", viewSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/tasks/${taskId}", viewSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/Tasks/CompleteSurvey/${taskId}", viewSource, StringComparison.Ordinal);

        Assert.Contains("SurveyRetryFailed", detailSource, StringComparison.Ordinal);
        Assert.Contains("RedirectToPage(\"./Index\")", detailSource, StringComparison.Ordinal);
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string ReadSentinelFile(string relativePath) =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Sentinel", relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string GetRepositoryRoot()
    {
        foreach (var candidate in new[]
                 {
                     new DirectoryInfo(Directory.GetCurrentDirectory()),
                     new DirectoryInfo(AppContext.BaseDirectory)
                 })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }
}
