namespace Sentinel.Tests.Security;

/// <summary>
/// Guards the deliberately ordered composition root after startup code was
/// separated into focused extension classes. These source-level checks make a
/// future middleware or initialization-order regression explicit in review.
/// </summary>
public sealed class StartupCompositionTests
{
    [Fact]
    public void Program_ComposesConfigurationServicesPipelineEndpointsAndInitializationInOrder()
    {
        var program = ReadSource("Sentinel", "Program.cs");

        AssertAppearsInOrder(program,
            "builder.ConfigureSentinelDeployment()",
            "builder.Host.UseSerilog()",
            "builder.Services.AddSentinelDomainServices",
            "var app = builder.Build();",
            "app.UseSentinelApplicationPipeline(useForwardedHeaders);",
            "app.MapSentinelMinimalApis();",
            "app.MapSentinelHealthCheck();",
            "await app.InitializeSentinelAsync();",
            "app.Run();");
    }

    [Fact]
    public void RequestPipeline_PreservesSecurityCriticalOrdering()
    {
        var pipeline = ReadSource("Sentinel", "Extensions", "SentinelApplicationPipelineExtensions.cs");

        AssertAppearsInOrder(pipeline,
            "app.UseForwardedHeaders();",
            "app.UseGlobalExceptionHandler();",
            "app.UseStaticFiles();",
            "app.UseRouting();",
            "app.UseSession();",
            "app.UseSetupRedirect();",
            "app.UseRateLimiter();",
            "app.UseAuthentication();",
            "app.UseMiddleware<UserSessionValidationMiddleware>();",
            "app.UseAuthorization();",
            "app.UseMiddleware<DiseaseAccessMiddleware>();",
            "app.UseAntiforgery();",
            "app.MapRazorPages();",
            "app.MapControllers();");
    }

    [Fact]
    public void StartupInitializer_MigratesAndSeedsBeforeTheEvaluationQueueAndSetupState()
    {
        var initializer = ReadSource("Sentinel", "Extensions", "SentinelStartupExtensions.cs");

        AssertAppearsInOrder(initializer,
            "await ApplyMigrationsAndSeedAsync(app.Services);",
            "dbContext.SetEvaluationQueue(queue);",
            "await InitializeSetupStateAsync(app.Services);");
    }

    private static void AssertAppearsInOrder(string source, params string[] fragments)
    {
        var position = -1;
        foreach (var fragment in fragments)
        {
            var nextPosition = source.IndexOf(fragment, position + 1, StringComparison.Ordinal);
            Assert.True(nextPosition >= 0, $"Expected startup fragment was not found: {fragment}");
            Assert.True(nextPosition > position, $"Startup fragment appeared out of order: {fragment}");
            position = nextPosition;
        }
    }

    private static string ReadSource(params string[] pathSegments)
        => File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathSegments]));

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
