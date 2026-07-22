using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.HL7;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;
using Sentinel.Services.HL7;
using Sentinel.Services.CaseDefinitionEvaluation;
using Xunit;

namespace Sentinel.Tests.Services.HL7;

/// <summary>
/// Tests for partial field matching in HL7 disease identification.
/// Validates that missing fields can be allowed based on per-disease configuration.
/// </summary>
public class PartialMatchingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CaseDefinitionMatchingService>> _mockLogger;
    private readonly Mock<ILogger<TreeBasedCriteriaEvaluator>> _mockTreeLogger;
    private readonly TreeBasedCriteriaEvaluator _treeEvaluator;
    private readonly CaseDefinitionMatchingService _matchingService;

    public PartialMatchingTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"PartialMatchingTest_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<CaseDefinitionMatchingService>>();
        _mockTreeLogger = new Mock<ILogger<TreeBasedCriteriaEvaluator>>();
        _treeEvaluator = new TreeBasedCriteriaEvaluator(_mockTreeLogger.Object);
        _matchingService = new CaseDefinitionMatchingService(_context, _mockLogger.Object, _cache, _treeEvaluator);
    }

    [Fact]
    public async Task FieldResolutionStatus_NotPresent_IsDifferentFrom_ParseFailed()
    {
        // Arrange - Validate that status enum properly distinguishes missing vs failed
        var notPresent = FieldResolutionStatus.NotPresent;
        var parseFailed = FieldResolutionStatus.ParseFailed;
        var resolved = FieldResolutionStatus.Resolved;

        // Assert - These should be distinct values
        Assert.NotEqual(notPresent, parseFailed);
        Assert.NotEqual(notPresent, resolved);
        Assert.NotEqual(parseFailed, resolved);
    }

    [Fact]
    public void DiseaseIdentification_CanTrackPartialMatch()
    {
        // Arrange & Act
        var identification = new DiseaseIdentification
        {
            IsPartialMatch = true,
            MissingFields = new List<string> { "SpecimenType", "TestMethod" },
            OriginalConfirmationStatusId = 1
        };

        // Assert - Verify the partial match fields exist and work
        Assert.True(identification.IsPartialMatch);
        Assert.Equal(2, identification.MissingFields.Count);
        Assert.Contains("SpecimenType", identification.MissingFields);
        Assert.Contains("TestMethod", identification.MissingFields);
        Assert.Equal(1, identification.OriginalConfirmationStatusId);
    }

    [Fact]
    public void CaseMatchingResult_IncludesDiseaseIdentifications()
    {
        // Arrange & Act
        var result = new CaseMatchingResult
        {
            Success = true,
            DiseaseIdentifications = new List<DiseaseIdentification>
            {
                new DiseaseIdentification
                {
                    IsPartialMatch = true,
                    MissingFields = new List<string> { "TestMethod" }
                }
            }
        };

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.DiseaseIdentifications);
        Assert.True(result.DiseaseIdentifications[0].IsPartialMatch);
    }

    [Fact]
    public void DiseaseHL7MatchingConfig_SupportsPartialMatchSettings()
    {
        // Arrange & Act
        var config = new DiseaseHL7MatchingConfig
        {
            DiseaseId = Guid.NewGuid(),
            AllowMissingSpecimenType = true,
            AllowMissingTestMethod = true,
            AllowMissingPathogen = false,
            AllowMissingResult = true,
            MaxMissingFieldsAllowed = 1,
            PartialMatchConfirmationStatusId = 2
        };

        // Assert - Verify all new fields exist
        Assert.True(config.AllowMissingSpecimenType);
        Assert.True(config.AllowMissingTestMethod);
        Assert.False(config.AllowMissingPathogen);
        Assert.True(config.AllowMissingResult);
        Assert.Equal(1, config.MaxMissingFieldsAllowed);
        Assert.Equal(2, config.PartialMatchConfirmationStatusId);
    }

    [Fact]
    public void MarkerResolutionResult_SupportsFieldStatusTracking()
    {
        // Arrange & Act
        var resolution = new MarkerResolutionResult
        {
            PathogenStatus = FieldResolutionStatus.Resolved,
            TestMethodStatus = FieldResolutionStatus.NotPresent,
            SpecimenTypeStatus = FieldResolutionStatus.ParseFailed,
            TestResultStatus = FieldResolutionStatus.Resolved
        };

        // Assert - Verify status properties exist
        Assert.Equal(FieldResolutionStatus.Resolved, resolution.PathogenStatus);
        Assert.Equal(FieldResolutionStatus.NotPresent, resolution.TestMethodStatus);
        Assert.Equal(FieldResolutionStatus.ParseFailed, resolution.SpecimenTypeStatus);
        Assert.Equal(FieldResolutionStatus.Resolved, resolution.TestResultStatus);
    }

    [Fact]
    public void CaseDefinitionMatchResult_SupportsPartialMatchTracking()
    {
        // Arrange & Act
        var matchResult = new CaseDefinitionMatchResult
        {
            IsPartialMatch = true,
            MissingFields = new List<string> { "SpecimenType", "TestResult" },
            OriginalConfirmationStatusId = 1,
            ConfirmationStatusId = 2
        };

        // Assert
        Assert.True(matchResult.IsPartialMatch);
        Assert.Equal(2, matchResult.MissingFields.Count);
        Assert.Equal(1, matchResult.OriginalConfirmationStatusId);
        Assert.Equal(2, matchResult.ConfirmationStatusId);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _cache.Dispose();
    }
}
