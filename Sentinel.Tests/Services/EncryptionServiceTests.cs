using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class EncryptionServiceTests
{
    [Fact]
    public void HashVerification_AcceptsOnlyTheExactOriginalValue()
    {
        var keyDirectory = Directory.CreateDirectory(Path.Combine(
            Path.GetTempPath(),
            "SentinelTests",
            "Encryption",
            Guid.NewGuid().ToString("N")));
        var provider = DataProtectionProvider.Create(
            keyDirectory,
            configuration => configuration.SetApplicationName("Sentinel.Encryption.Tests"));
        var service = new EncryptionService(
            provider,
            NullLogger<EncryptionService>.Instance);

        var hash = service.Hash("setup-token-value");

        Assert.True(service.VerifyHash("setup-token-value", hash));
        Assert.False(service.VerifyHash("setup-token-value-altered", hash));
        Assert.False(service.VerifyHash("setup-token-value", "not-base64"));
    }
}
