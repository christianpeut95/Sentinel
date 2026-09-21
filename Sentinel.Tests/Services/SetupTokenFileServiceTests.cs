using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class SetupTokenFileServiceTests : IDisposable
{
    private readonly string _temporaryRoot = Path.Combine(Path.GetTempPath(), $"sentinel-setup-token-test-{Guid.NewGuid():N}");

    [Fact]
    public async Task WriteTokenAsync_WritesOutsideWebRoot_ThenDeletesThePlaintextToken()
    {
        var tokenPath = Path.Combine(_temporaryRoot, "protected", "setup-token.txt");
        var service = CreateService(tokenPath);

        await service.WriteTokenAsync("one-time-token");

        Assert.True(service.TokenFileExists);
        Assert.Equal("one-time-token", await File.ReadAllTextAsync(tokenPath));

        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserWrite,
                File.GetUnixFileMode(tokenPath));
            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
                File.GetUnixFileMode(Path.GetDirectoryName(tokenPath)!));
        }

        await service.DeleteTokenAsync();

        Assert.False(service.TokenFileExists);
    }

    [Fact]
    public void Constructor_RejectsAWebRootTokenPath()
    {
        var webRoot = Path.Combine(_temporaryRoot, "wwwroot");
        var tokenPath = Path.Combine(webRoot, "setup-token.txt");

        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(tokenPath));

        Assert.Contains("outside wwwroot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private SetupTokenFileService CreateService(string tokenPath)
    {
        var webRoot = Path.Combine(_temporaryRoot, "wwwroot");
        Directory.CreateDirectory(webRoot);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Setup:TokenFilePath"] = tokenPath
            })
            .Build();

        return new SetupTokenFileService(
            configuration,
            new TestHostEnvironment
            {
                ContentRootPath = _temporaryRoot,
                WebRootPath = webRoot
            },
            NullLogger<SetupTokenFileService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Sentinel.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
