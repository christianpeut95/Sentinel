using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services.HL7;
using Sentinel.Services.HL7.Models;

namespace Sentinel.Tests.Services.HL7;

public sealed class ProviderIdentityResolutionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProviderIdentityResolutionService _service;

    public ProviderIdentityResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ProviderIdentity_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(
            options,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        _context.OrganizationTypes.Add(new OrganizationType
        {
            Id = 1,
            Name = "Healthcare Provider",
            IsActive = true
        });
        _context.SaveChanges();

        _service = new ProviderIdentityResolutionService(
            _context,
            NullLogger<ProviderIdentityResolutionService>.Instance);
    }

    [Fact]
    public async Task SafeNameFallback_WithoutIncomingIdentifier_MatchesUniqueStructuredName()
    {
        var provider = AddProvider(givenName: "Anne-Marie", familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity(givenName: "Anne Marie", familyName: "O Connor"),
            ProviderMatchingMode.SafeNameFallback);

        Assert.Equal(ProviderResolutionOutcome.Matched, result.Outcome);
        Assert.Equal(ProviderMatchBasis.Name, result.MatchBasis);
        Assert.Equal(provider.Id, result.ExistingOrganization?.Id);
        Assert.False(result.MayEnrichIdentity);
    }

    [Fact]
    public async Task IdentifierOnly_WithoutIncomingIdentifier_DoesNotUseName()
    {
        AddProvider(givenName: "Anne", familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity(givenName: "Anne", familyName: "O'Connor"),
            ProviderMatchingMode.IdentifierOnly);

        Assert.Equal(ProviderResolutionOutcome.NotFound, result.Outcome);
        Assert.Null(result.ExistingOrganization);
    }

    [Fact]
    public async Task IncomingIdentifier_DifferentStoredIdentifier_NeverMatchesByName()
    {
        var existing = AddProvider(
            identifier: "OLD-123",
            authority: "AHPRA",
            givenName: "Anne",
            familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity("NEW-456", "AHPRA", "Anne", "O'Connor"),
            ProviderMatchingMode.TrustedIdentityEnrichment,
            autoCreate: true);

        Assert.Equal(ProviderResolutionOutcome.Create, result.Outcome);
        Assert.Null(result.ExistingOrganization);
        Assert.NotEqual(existing.Id, result.ExistingOrganization?.Id);
    }

    [Fact]
    public async Task SafeNameFallback_IncomingIdentifierAndNameOnlyRecord_RequiresManualReview()
    {
        AddProvider(givenName: "Anne", familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity("MED-123", "AHPRA", "Anne", "O'Connor"),
            ProviderMatchingMode.SafeNameFallback,
            autoCreate: true);

        Assert.Equal(ProviderResolutionOutcome.ManualReview, result.Outcome);
        Assert.Null(result.ExistingOrganization);
    }

    [Fact]
    public async Task TrustedEnrichment_IncomingIdentifierAndUniqueStructuredName_MayAddIdentity()
    {
        var provider = AddProvider(givenName: "Anne", familyName: "O'Connor");
        var identity = Identity("MED-123", " ahpra ", "Anne", "O'Connor");

        var result = await ResolveAsync(
            identity,
            ProviderMatchingMode.TrustedIdentityEnrichment);

        Assert.Equal(ProviderResolutionOutcome.Matched, result.Outcome);
        Assert.Equal(ProviderMatchBasis.TrustedNameEnrichment, result.MatchBasis);
        Assert.True(result.MayEnrichIdentity);
        Assert.True(_service.ApplyMissingIdentity(provider, identity));
        Assert.Equal("MED123", provider.ProviderIdentifier);
        Assert.Equal("AHPRA", provider.ProviderIdentifierAuthority);
    }

    [Fact]
    public async Task TrustedEnrichment_LegacyDisplayNameOnly_RequiresManualReview()
    {
        AddProvider(name: "Anne O'Connor");

        var result = await ResolveAsync(
            Identity("MED-123", "AHPRA", "Anne", "O'Connor"),
            ProviderMatchingMode.TrustedIdentityEnrichment,
            autoCreate: true);

        Assert.Equal(ProviderResolutionOutcome.ManualReview, result.Outcome);
    }

    [Fact]
    public async Task ExactIdentifierAndAuthority_WinsWhenNamesHaveChanged()
    {
        var provider = AddProvider(
            identifier: "MED-123",
            authority: "AHPRA",
            givenName: "Anne",
            familyName: "Oldname");

        var result = await ResolveAsync(
            Identity("med 123", "ahpra", "Anne", "Newname"),
            ProviderMatchingMode.SafeNameFallback);

        Assert.Equal(ProviderResolutionOutcome.Matched, result.Outcome);
        Assert.Equal(ProviderMatchBasis.Identifier, result.MatchBasis);
        Assert.Equal(provider.Id, result.ExistingOrganization?.Id);
    }

    [Fact]
    public async Task SameIdentifierWithDifferentAuthority_DoesNotMatch()
    {
        AddProvider(
            identifier: "MED-123",
            authority: "LOCAL-A",
            givenName: "Anne",
            familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity("MED-123", "LOCAL-B", "Anne", "O'Connor"),
            ProviderMatchingMode.SafeNameFallback,
            autoCreate: true);

        Assert.Equal(ProviderResolutionOutcome.Create, result.Outcome);
        Assert.Null(result.ExistingOrganization);
    }

    [Fact]
    public async Task IdentifierWithoutAuthority_MatchingMultipleAuthorities_RequiresManualReview()
    {
        AddProvider(identifier: "MED-123", authority: "LOCAL-A", givenName: "Anne", familyName: "O'Connor");
        AddProvider(identifier: "MED-123", authority: "LOCAL-B", givenName: "Anne", familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity("MED-123", givenName: "Anne", familyName: "O'Connor"),
            ProviderMatchingMode.SafeNameFallback);

        Assert.Equal(ProviderResolutionOutcome.ManualReview, result.Outcome);
    }

    [Fact]
    public async Task ExactInactiveProvider_RequiresManualReview()
    {
        AddProvider(
            identifier: "MED-123",
            authority: "AHPRA",
            givenName: "Anne",
            familyName: "O'Connor",
            isActive: false);

        var result = await ResolveAsync(
            Identity("MED-123", "AHPRA", "Anne", "O'Connor"),
            ProviderMatchingMode.SafeNameFallback);

        Assert.Equal(ProviderResolutionOutcome.ManualReview, result.Outcome);
    }

    [Fact]
    public async Task InvalidMatchingMode_FailsClosedToIdentifierOnly()
    {
        AddProvider(givenName: "Anne", familyName: "O'Connor");

        var result = await ResolveAsync(
            Identity(givenName: "Anne", familyName: "O'Connor"),
            (ProviderMatchingMode)999,
            autoCreate: true);

        Assert.Equal(ProviderResolutionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public void Model_EnforcesUniqueAuthorityScopedProviderIdentity()
    {
        var entityType = _context.Model.FindEntityType(typeof(Organization));
        var index = Assert.Single(entityType!.GetIndexes(), candidate =>
            candidate.GetDatabaseName() == Organization.ProviderIdentityUniqueIndexName);

        Assert.True(index.IsUnique);
        Assert.Equal(
            new[] { nameof(Organization.ProviderIdentifier), nameof(Organization.ProviderIdentifierAuthority) },
            index.Properties.Select(property => property.Name));
        Assert.Equal("[ProviderIdentifier] IS NOT NULL", index.GetFilter());
    }

    private Task<ProviderOrganizationResolution> ResolveAsync(
        ProviderIdentity identity,
        ProviderMatchingMode matchingMode,
        bool autoCreate = false) =>
        _service.ResolveAsync(identity, matchingMode, autoCreate);

    private Organization AddProvider(
        string? identifier = null,
        string? authority = null,
        string? givenName = null,
        string? familyName = null,
        string? name = null,
        bool isActive = true)
    {
        var provider = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name ?? string.Join(" ", new[] { givenName, familyName }.Where(value => value != null)),
            OrganizationTypeId = 1,
            ProviderIdentifier = ProviderIdentityUtility.NormalizeIdentifier(identifier),
            ProviderIdentifierAuthority = ProviderIdentityUtility.NormalizeAuthority(authority),
            GivenName = givenName,
            FamilyName = familyName,
            IsActive = isActive
        };

        _context.Organizations.Add(provider);
        _context.SaveChanges();
        return provider;
    }

    private static ProviderIdentity Identity(
        string? identifier = null,
        string? authority = null,
        string? givenName = null,
        string? familyName = null) =>
        new()
        {
            Identifier = identifier,
            IdentifierAuthority = authority,
            GivenName = givenName,
            FamilyName = familyName
        };

    public void Dispose() => _context.Dispose();
}
