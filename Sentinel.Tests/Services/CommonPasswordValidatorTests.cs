using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class CommonPasswordValidatorTests : IDisposable
{
    private readonly string _temporaryRoot = Path.Combine(Path.GetTempPath(), $"sentinel-password-test-{Guid.NewGuid():N}");

    [Fact]
    public async Task ValidateAsync_RejectsACommonPassword()
    {
        var denyList = CreateDenyList("CommonPassword1!");
        var validator = new CommonPasswordValidator(denyList);

        var result = await validator.ValidateAsync(null!, new ApplicationUser(), "commonpassword1!");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "PasswordTooCommon");
    }

    [Fact]
    public async Task ValidateAsync_AllowsAPasswordAbsentFromTheDenyList()
    {
        var denyList = CreateDenyList("CommonPassword1!");
        var validator = new CommonPasswordValidator(denyList);

        var result = await validator.ValidateAsync(null!, new ApplicationUser(), "DifferentPassword2@");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Constructor_RejectsAnIncompleteDenyList()
    {
        WritePasswordList(["only-one-entry"]);
        var environment = CreateEnvironment();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new CommonPasswordDenyList(environment, NullLogger<CommonPasswordDenyList>.Instance));

        Assert.Contains("3,000", exception.Message, StringComparison.Ordinal);
    }

    private ICommonPasswordDenyList CreateDenyList(string commonPassword)
    {
        var entries = Enumerable.Range(0, CommonPasswordDenyList.MinimumRequiredEntries)
            .Select(index => $"test-password-{index}")
            .Append(commonPassword);
        WritePasswordList(entries);
        return new CommonPasswordDenyList(CreateEnvironment(), NullLogger<CommonPasswordDenyList>.Instance);
    }

    private void WritePasswordList(IEnumerable<string> entries)
    {
        var directory = Path.Combine(_temporaryRoot, "Security");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "common-passwords.txt"), entries);
    }

    private IWebHostEnvironment CreateEnvironment() => new TestHostEnvironment
    {
        ContentRootPath = _temporaryRoot
    };

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
