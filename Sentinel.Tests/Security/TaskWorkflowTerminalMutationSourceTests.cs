using System.Text.RegularExpressions;

namespace Sentinel.Tests.Security;

/// <summary>
/// Keeps direct terminal task mutations visible. The normal task-edit route
/// must never become another way to complete or cancel a task without the
/// dedicated workflow's actor, timestamp and audit information.
/// </summary>
public sealed class TaskWorkflowTerminalMutationSourceTests
{
    [Fact]
    public void DirectTerminalTaskAssignments_AreLimitedToReviewedWorkflowPaths()
    {
        var root = FindRepositoryRoot();
        var applicationRoot = Path.Combine(root, "Sentinel");
        var assignments = Directory
            .EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => Regex.Matches(
                    File.ReadAllText(path),
                    @"\.Status\s*=\s*CaseTaskStatus\.(Completed|Cancelled)")
                .Cast<Match>()
                .Select(match => $"{RelativePath(root, path)}:{match.Groups[1].Value}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Sentinel/Controllers/Api/SurveyCompletionApiController.cs:Completed",
            "Sentinel/Pages/Cases/Details.cshtml.cs:Cancelled",
            "Sentinel/Pages/Cases/Details.cshtml.cs:Completed",
            "Sentinel/Pages/Tasks/CompleteSurvey.cshtml.cs:Completed",
            "Sentinel/Services/DataReviewService.cs:Completed",
            "Sentinel/Services/TaskAssignmentService.cs:Completed",
            "Sentinel/Services/TaskService.cs:Cancelled",
            "Sentinel/Services/TaskService.cs:Completed"
        ], assignments);
    }

    [Fact]
    public void ReviewedTerminalMutationPaths_UseTheSharedTerminalPolicy()
    {
        var root = FindRepositoryRoot();
        var relativePaths = new[]
        {
            "Sentinel/Controllers/Api/SurveyCompletionApiController.cs",
            "Sentinel/Pages/Cases/Details.cshtml.cs",
            "Sentinel/Pages/Tasks/CompleteSurvey.cshtml.cs",
            "Sentinel/Services/DataReviewService.cs",
            "Sentinel/Services/TaskAssignmentService.cs",
            "Sentinel/Services/TaskService.cs"
        };

        foreach (var relativePath in relativePaths)
        {
            var source = File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("TaskWorkflowPolicy", source, StringComparison.Ordinal);
        }

        var editTask = File.ReadAllText(Path.Combine(root, "Sentinel", "Pages", "Cases", "EditTask.cshtml.cs"));
        Assert.Contains("!TaskWorkflowPolicy.CanChangeStatus(taskToUpdate.Status, Task.Status)", editTask, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"Completed\"", editTask, StringComparison.Ordinal);
        Assert.DoesNotContain("Text = \"Cancelled\"", editTask, StringComparison.Ordinal);
    }

    private static string RelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Sentinel")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Sentinel.Tests")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }
}
