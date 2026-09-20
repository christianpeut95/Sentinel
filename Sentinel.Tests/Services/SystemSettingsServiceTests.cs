using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class SystemSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEncryptionService> _encryptionService = new();

    public SystemSettingsServiceTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public async Task CompleteSetupAsync_ExpiredToken_RejectsWithoutChangingSetupState()
    {
        var settings = new SystemSettings
        {
            Id = Guid.NewGuid(),
            SetupToken = "stored-hash",
            SetupTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            IsSetupCompleted = false
        };
        _context.SystemSettings.Add(settings);
        await _context.SaveChangesAsync();

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CompleteSetupAsync("admin-id", "plain-token"));

        Assert.False(settings.IsSetupCompleted);
        Assert.Equal("stored-hash", settings.SetupToken);
        _encryptionService.Verify(service => service.VerifyHash(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteSetupAsync_ValidToken_CompletesAndConsumesToken()
    {
        var settings = new SystemSettings
        {
            Id = Guid.NewGuid(),
            SetupToken = "stored-hash",
            SetupTokenExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsSetupCompleted = false
        };
        _context.SystemSettings.Add(settings);
        await _context.SaveChangesAsync();
        _encryptionService.Setup(service => service.VerifyHash("plain-token", "stored-hash")).Returns(true);

        var service = CreateService();

        var completed = await service.CompleteSetupAsync("admin-id", "plain-token");

        Assert.True(completed.IsSetupCompleted);
        Assert.Equal("admin-id", completed.SetupCompletedByUserId);
        Assert.Null(completed.SetupToken);
        Assert.NotNull(completed.SetupCompletedAt);
        _encryptionService.Verify(service => service.VerifyHash("plain-token", "stored-hash"), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidUnexpiredToken_ReturnsTrue()
    {
        _context.SystemSettings.Add(new SystemSettings
        {
            Id = Guid.NewGuid(),
            SetupToken = "stored-hash",
            SetupTokenExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await _context.SaveChangesAsync();
        _encryptionService.Setup(service => service.VerifyHash("plain-token", "stored-hash")).Returns(true);

        var result = await CreateService().ValidateTokenAsync("plain-token");

        Assert.True(result);
    }

    private SystemSettingsService CreateService() => new(
        _context,
        _encryptionService.Object,
        NullLogger<SystemSettingsService>.Instance);

    public void Dispose() => _context.Dispose();
}
