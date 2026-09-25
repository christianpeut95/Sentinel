using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class PatientDuplicateCheckServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<PatientDuplicateCheckService>> _logger = new();
    private readonly PatientDuplicateCheckService _service;

    public PatientDuplicateCheckServiceTests()
    {
        var requestContext = new DefaultHttpContext();
        requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = requestContext });
        _service = new PatientDuplicateCheckService(_context, _logger.Object);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_FindsAnExactPatientWithoutLoadingUnrelatedRecords()
    {
        var expected = CreatePatient("Alice", "Example", new DateTime(1988, 5, 2));
        _context.Patients.AddRange(
            expected,
            CreatePatient("Unrelated", "Person", new DateTime(1970, 1, 1)));
        await _context.SaveChangesAsync();

        var matches = await _service.FindPotentialDuplicatesAsync(
            CreatePatient("Alice", "Example", new DateTime(1988, 5, 2)));

        var match = Assert.Single(matches);
        Assert.Equal(expected.Id, match.Patient.Id);
        Assert.Contains("Exact name match", match.MatchReasons);
        Assert.Contains("Same date of birth", match.MatchReasons);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_FindsAnExactEmailMatch()
    {
        var expected = CreatePatient("Different", "Name", null);
        expected.EmailAddress = "duplicate@example.test";
        _context.Patients.Add(expected);
        await _context.SaveChangesAsync();

        var request = CreatePatient("Completely", "Different", null);
        request.EmailAddress = "duplicate@example.test";

        var match = Assert.Single(await _service.FindPotentialDuplicatesAsync(request));

        Assert.Equal(expected.Id, match.Patient.Id);
        Assert.Contains("Same email address", match.MatchReasons);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_ChecksBothSubmittedPhoneNumbersAsCandidateAnchors()
    {
        var expected = CreatePatient("Different", "Name", null);
        expected.HomePhone = "0400 000 111";
        _context.Patients.Add(expected);
        await _context.SaveChangesAsync();

        var request = CreatePatient("Completely", "Different", null);
        request.MobilePhone = "0400 000 222";
        request.HomePhone = "0400 000 111";

        var match = Assert.Single(await _service.FindPotentialDuplicatesAsync(request));

        Assert.Equal(expected.Id, match.Patient.Id);
        Assert.Contains("Same telephone number", match.MatchReasons);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_ExcludesThePatientBeingEdited()
    {
        var patient = CreatePatient("Alice", "Example", new DateTime(1988, 5, 2));
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var matches = await _service.FindPotentialDuplicatesAsync(patient);

        Assert.Empty(matches);
    }

    [Fact]
    public async Task FindPotentialDuplicatesAsync_BoundsCandidateScoringAndWarnsWhenTheLimitIsReached()
    {
        for (var i = 0; i <= 250; i++)
        {
            _context.Patients.Add(CreatePatient("Alice", $"Candidate{i:D3}", null));
        }
        await _context.SaveChangesAsync();

        var matches = await _service.FindPotentialDuplicatesAsync(CreatePatient("Alice", "Target", null));

        Assert.Equal(5, matches.Count);
        _logger.VerifyLogContains(LogLevel.Warning, "candidate search reached", Times.Once());
    }

    public void Dispose() => _context.Dispose();

    private static Patient CreatePatient(string givenName, string familyName, DateTime? dateOfBirth) => new()
    {
        Id = Guid.NewGuid(),
        FriendlyId = $"PT-{Guid.NewGuid():N}"[..20],
        GivenName = givenName,
        FamilyName = familyName,
        DateOfBirth = dateOfBirth
    };
}

internal static class DuplicateCheckLoggerVerificationExtensions
{
    public static void VerifyLogContains<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        string text,
        Times times)
    {
        logger.Verify(
            item => item.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
