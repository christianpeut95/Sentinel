using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Tests.Services.HL7;

public sealed class HL7FileMonitorPathTests
{
    [Fact]
    public void TryNormalizeFileDropPath_AllowsAChildOfTheConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "sentinel-hl7-root", Guid.NewGuid().ToString("N"));
        var service = CreateService(root);

        var accepted = service.TryNormalizeFileDropPath(
            Path.Combine(root, "laboratory-a", "..", "laboratory-a"),
            out var normalizedPath,
            out var error);

        Assert.True(accepted, error);
        Assert.Equal(Path.Combine(root, "laboratory-a"), normalizedPath);
    }

    [Fact]
    public void TryNormalizeFileDropPath_RejectsAPathOutsideTheConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "sentinel-hl7-root", Guid.NewGuid().ToString("N"));
        var service = CreateService(root);

        var accepted = service.TryNormalizeFileDropPath(
            Path.Combine(root, "..", "not-hl7"),
            out _,
            out var error);

        Assert.False(accepted);
        Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryNormalizeFileDropPath_RejectsRelativePaths()
    {
        var service = CreateService(Path.Combine(Path.GetTempPath(), "sentinel-hl7-root"));

        var accepted = service.TryNormalizeFileDropPath("incoming", out _, out var error);

        Assert.False(accepted);
        Assert.Contains("absolute", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanConfiguredDirectoriesAsync_ProcessesASettledBacklogFileWithoutAFileSystemWatcher()
    {
        var root = Path.Combine(Path.GetTempPath(), "sentinel-hl7-poll", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var sourceFile = Path.Combine(root, "backlog.hl7");
        await File.WriteAllTextAsync(sourceFile, "MSH|^~\\&|TEST");
        File.SetLastWriteTimeUtc(sourceFile, DateTime.UtcNow.AddSeconds(-5));

        var databaseName = $"HL7FileMonitor_{Guid.NewGuid():N}";
        var parser = new Mock<IHL7ParserService>();
        parser.Setup(service => service.ParseMessageAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HL7Message
            {
                Id = Guid.NewGuid(),
                Status = HL7ProcessingStatus.ParsingFailed,
                ErrorMessage = "Synthetic parser failure"
            });

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IHL7ParserService>(_ => parser.Object);
        services.AddScoped<IHL7DataExtractionService>(_ => Mock.Of<IHL7DataExtractionService>());
        using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.HL7Configurations.Add(new HL7Configuration
            {
                Id = Guid.NewGuid(),
                ConfigurationName = "Polling test",
                FileDropPath = root,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        using var service = new HL7FileMonitorService(
            NullLogger<HL7FileMonitorService>.Instance,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new HL7FileMonitorOptions
            {
                AllowedRootPath = root,
                FileSettleDelaySeconds = 1
            }));

        await service.StartMonitoringAsync();
        await service.ScanConfiguredDirectoriesAsync();

        Assert.False(File.Exists(sourceFile));
        Assert.Single(Directory.GetFiles(Path.Combine(root, "Error"), "*backlog.hl7", SearchOption.AllDirectories));
        Assert.Equal(1, service.GetMonitoringStatus().ActivePollingLocations);
        parser.Verify(service => service.ParseMessageAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);

        Directory.Delete(root, recursive: true);
    }

    private static HL7FileMonitorService CreateService(string allowedRootPath)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new HL7FileMonitorService(
            NullLogger<HL7FileMonitorService>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new HL7FileMonitorOptions { AllowedRootPath = allowedRootPath }));
    }
}
