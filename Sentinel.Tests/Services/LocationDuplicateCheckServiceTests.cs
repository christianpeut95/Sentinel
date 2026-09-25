using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class LocationDuplicateCheckServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<LocationDuplicateCheckService>> _logger = new();
    private readonly LocationDuplicateCheckService _service;

    public LocationDuplicateCheckServiceTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        _service = new LocationDuplicateCheckService(_context, _logger.Object);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_FindsARelevantLocationAndExcludesInactiveLocations()
    {
        var expected = CreateLocation("Central Clinic", "1 Health Street");
        var inactive = CreateLocation("Central Clinic", "1 Health Street");
        inactive.IsActive = false;
        _context.Locations.AddRange(expected, inactive, CreateLocation("Unrelated site", "999 Other Road"));
        await _context.SaveChangesAsync();

        var match = Assert.Single(await _service.FindPotentialDuplicatesAsync(CreateLocation("Central Clinic", "1 Health Street")));

        Assert.Equal(expected.Id, match.Location.Id);
        Assert.Contains("Exact Name Match", match.MatchReasons);
        Assert.Contains("Exact Address Match", match.MatchReasons);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_ExcludesTheLocationBeingEdited()
    {
        var location = CreateLocation("Central Clinic", "1 Health Street");
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        var matches = await _service.FindPotentialDuplicatesAsync(location);

        Assert.Empty(matches);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_BoundsCandidateScoringAndWarnsWhenTheLimitIsReached()
    {
        for (var i = 0; i <= 250; i++)
        {
            _context.Locations.Add(CreateLocation("Central Clinic", null));
        }
        await _context.SaveChangesAsync();

        var matches = await _service.FindPotentialDuplicatesAsync(CreateLocation("Central Clinic", null));

        Assert.Equal(250, matches.Count);
        _logger.VerifyLogContains(LogLevel.Warning, "candidate search reached", Times.Once());
    }

    public void Dispose() => _context.Dispose();

    private static Location CreateLocation(string name, string? address) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Address = address,
        IsActive = true
    };
}
