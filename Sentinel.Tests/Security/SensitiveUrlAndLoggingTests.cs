using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sentinel.Services.Email;

namespace Sentinel.Tests.Security;

/// <summary>
/// Keeps credentials out of browser request URLs and application logs. Password
/// reset links are deliberately carried in a fragment: a browser does not send
/// fragments to the server, access logs or referrers.
/// </summary>
public sealed class SensitiveUrlAndLoggingTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void EveryPasswordResetTokenGenerator_UsesTheFragmentLinkHelper()
    {
        var generatorCallSites = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "Sentinel"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("GeneratePasswordResetTokenAsync", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(3, generatorCallSites.Length);

        var resetLinkGenerators = generatorCallSites
            .Where(path => !path.EndsWith("Settings\\Users\\Edit.cshtml.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(2, resetLinkGenerators.Length);

        foreach (var path in resetLinkGenerators)
        {
            Assert.Contains(
                "PasswordResetTokenEncoding.AddToResetLinkFragment",
                File.ReadAllText(path),
                StringComparison.Ordinal);
        }

        var administratorResetSource = File.ReadAllText(generatorCallSites.Single(path =>
            path.EndsWith("Settings\\Users\\Edit.cshtml.cs", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains("ResetPasswordAsync(user, token, Input.NewPassword)", administratorResetSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Url.Page(", administratorResetSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SendPasswordResetEmailAsync", administratorResetSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevelopmentMockMailer_DoesNotLogBodiesOrPasswordResetLinks()
    {
        var messages = new ConcurrentQueue<string>();
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.SetMinimumLevel(LogLevel.Trace)
                   .AddProvider(new RecordingLoggerProvider(messages)));
        var service = new MockEmailService(loggerFactory.CreateLogger<MockEmailService>());
        const string bodySecret = "body-secret-8a8aa2ea";
        const string resetLink = "https://sentinel.example/Identity/Account/ResetPassword#code=reset-secret-6d4ac1cb";

        await service.SendEmailAsync("user@example.test", "Subject", $"Body contains {bodySecret}");
        await service.SendPasswordResetEmailAsync("user@example.test", resetLink, "Test User");

        var combined = string.Join("\n", messages);
        Assert.DoesNotContain(bodySecret, combined, StringComparison.Ordinal);
        Assert.DoesNotContain(resetLink, combined, StringComparison.Ordinal);
        Assert.DoesNotContain("reset-secret-6d4ac1cb", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportBuilderPreview_LogsOnlyBoundedOperationalMetrics()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Controllers",
            "Api",
            "ReportBuilderApiController.cs"));

        Assert.DoesNotContain("Console.WriteLine", source, StringComparison.Ordinal);
        Assert.Contains("Report preview requested for entity type", source, StringComparison.Ordinal);
        Assert.Contains("Report preview completed for entity type", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DYNAMIC DATE FILTER", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Correcting field path:", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportEngine_DoesNotWriteReportValuesOrFiltersToConsole()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Services",
            "Reporting",
            "ReportDataService.cs"));

        Assert.DoesNotContain("Console.WriteLine", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.Write", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LookupSearchControllers_LogFailuresWithoutWritingExceptionTextToConsole()
    {
        var controllers = new[]
        {
            "CasesController.cs",
            "CountriesController.cs",
            "DiseasesController.cs",
            "EventsController.cs",
            "LocationsController.cs"
        };

        foreach (var controller in controllers)
        {
            var source = File.ReadAllText(Path.Combine(
                RepositoryRoot,
                "Sentinel",
                "Controllers",
                controller));

            Assert.DoesNotContain("Console.WriteLine", source, StringComparison.Ordinal);
            Assert.Contains("_logger.LogError(ex,", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ApplicationSource_DoesNotUseConsoleWriteForOperationalLogging()
    {
        var applicationRoot = Path.Combine(RepositoryRoot, "Sentinel");
        var consoleWriters = Directory
            .EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("Console.Write", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(consoleWriters);
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

    private sealed class RecordingLoggerProvider(ConcurrentQueue<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(messages);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
        }
    }
}
