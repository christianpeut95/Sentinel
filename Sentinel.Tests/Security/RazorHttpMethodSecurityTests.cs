using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sentinel.Pages.Settings.Mappings;
using Sentinel.Services;

namespace Sentinel.Tests.Security;

/// <summary>
/// Ensures Razor Page GET handlers remain read-only and that the retained
/// legacy mapping deletion URL cannot delete a record by navigation alone.
/// </summary>
public sealed class RazorHttpMethodSecurityTests
{
    private static readonly string[] PersistenceCommitOperations =
    [
        "SaveChanges(",
        "SaveChangesAsync(",
        "ExecuteDelete",
        "ExecuteUpdate",
        "SoftDeleteAsync(",
        "DeleteAsync("
    ];

    [Fact]
    public void RazorPageAndMinimalApiGetHandlers_DoNotContainPersistenceCommitOperations()
    {
        var root = GetRepositoryRoot();
        var razorHandlers = Directory.GetFiles(Path.Combine(root, "Sentinel", "Pages"), "*.cshtml.cs", SearchOption.AllDirectories)
            .SelectMany(GetGetHandlerBodies)
            .ToList();
        var minimalApiSource = string.Join(Environment.NewLine,
            File.ReadAllText(Path.Combine(root, "Sentinel", "Extensions", "SentinelMinimalApiExtensions.cs")),
            File.ReadAllText(Path.Combine(root, "Sentinel", "Extensions", "SentinelStartupExtensions.cs")));
        var minimalApiHandlers = GetMinimalApiGetHandlerBodies(minimalApiSource).ToList();
        var handlerBodies = razorHandlers.Concat(minimalApiHandlers).ToList();

        Assert.True(razorHandlers.Count >= 200, "The Razor GET-handler inventory unexpectedly shrank; review the source-discovery test.");
        Assert.True(minimalApiHandlers.Count >= 10, "The Minimal API GET-handler inventory unexpectedly shrank; review the source-discovery test.");

        var violations = handlerBodies
            .SelectMany(handler => PersistenceCommitOperations
                .Where(operation => handler.Body.Contains(operation, StringComparison.Ordinal))
                .Select(operation => $"{handler.Path}:{handler.LineNumber} {handler.Name} contains {operation}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"Razor GET handlers must not perform direct persistence. Violations:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void MappingDeletion_Get_RedirectsWithoutPerformingTheDeletion()
    {
        var mappingService = new Mock<ISurveyMappingService>();
        var model = new DeleteMappingModel(mappingService.Object);

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Settings/Index", redirect.PageName);
        mappingService.Verify(service => service.DeleteMappingAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void MappingDeletion_DoesNotOptOutOfRazorPagesAntiforgery()
    {
        var root = GetRepositoryRoot();
        var mappingDeletionSource = File.ReadAllText(Path.Combine(root, "Sentinel", "Pages", "Settings", "Mappings", "DeleteMapping.cshtml.cs"));
        var pipelineSource = File.ReadAllText(Path.Combine(root, "Sentinel", "Extensions", "SentinelApplicationPipelineExtensions.cs"));

        Assert.DoesNotContain("IgnoreAntiforgeryToken", mappingDeletionSource, StringComparison.Ordinal);
        Assert.Contains("app.UseAntiforgery();", pipelineSource, StringComparison.Ordinal);
    }

    private static IEnumerable<GetHandler> GetGetHandlerBodies(string path)
    {
        var source = File.ReadAllText(path);
        var matches = Regex.Matches(source, @"\b(?:public|protected|internal|private)\s+(?:async\s+)?(?:Task(?:<[^>]+>)?|IActionResult|JsonResult|void)\s+(OnGet\w*)\s*\(");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var openingBrace = source.IndexOf('{', match.Index + match.Length);
            if (openingBrace < 0)
            {
                continue;
            }

            var depth = 0;
            var closingBrace = -1;
            for (var index = openingBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}' && --depth == 0)
                {
                    closingBrace = index;
                    break;
                }
            }

            if (closingBrace < 0)
            {
                throw new InvalidOperationException($"Could not parse GET handler body in {path} at index {match.Index}.");
            }

            var lineNumber = source[..match.Index].Count(character => character == '\n') + 1;
            yield return new GetHandler(path, lineNumber, match.Groups[1].Value, source[openingBrace..(closingBrace + 1)]);
        }
    }

    private static IEnumerable<GetHandler> GetMinimalApiGetHandlerBodies(string source)
    {
        var matches = Regex.Matches(source, @"app\.MapGet\([\s\S]*?=>\s*\{");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var openingBrace = match.Index + match.Length - 1;
            var depth = 0;
            var closingBrace = -1;
            for (var index = openingBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}' && --depth == 0)
                {
                    closingBrace = index;
                    break;
                }
            }

            if (closingBrace < 0)
            {
                throw new InvalidOperationException($"Could not parse Minimal API GET handler at index {match.Index}.");
            }

            var lineNumber = source[..match.Index].Count(character => character == '\n') + 1;
            yield return new GetHandler("Sentinel/Extensions/SentinelMinimalApiExtensions.cs", lineNumber, "MapGet", source[openingBrace..(closingBrace + 1)]);
        }
    }

    private static string GetRepositoryRoot()
    {
        foreach (var candidate in new[] { new DirectoryInfo(Directory.GetCurrentDirectory()), new DirectoryInfo(AppContext.BaseDirectory) })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root for GET-handler safety checks.");
    }

    private sealed record GetHandler(string Path, int LineNumber, string Name, string Body);
}
