using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7.Models;

namespace Sentinel.Services.HL7;

public interface IProviderIdentityResolutionService
{
    Task<ProviderOrganizationResolution> ResolveAsync(
        ProviderIdentity identity,
        ProviderMatchingMode matchingMode,
        bool autoCreate,
        CancellationToken cancellationToken = default);

    Organization CreateOrganization(ProviderIdentity identity, int organizationTypeId);

    bool ApplyMissingIdentity(Organization organization, ProviderIdentity identity);
}

/// <summary>
/// Owns ordering-provider identity policy. Extraction services supply an XCN identity;
/// this service makes one deterministic match/create/review decision for every intake path.
/// </summary>
public sealed class ProviderIdentityResolutionService : IProviderIdentityResolutionService
{
    private static readonly string[] ProviderOrganizationTypeNames =
    {
        "Medical Practitioner",
        "Healthcare Provider",
        "Provider",
        "Medical Clinic",
        "Clinic"
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProviderIdentityResolutionService> _logger;

    public ProviderIdentityResolutionService(
        ApplicationDbContext context,
        ILogger<ProviderIdentityResolutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProviderOrganizationResolution> ResolveAsync(
        ProviderIdentity identity,
        ProviderMatchingMode matchingMode,
        bool autoCreate,
        CancellationToken cancellationToken = default)
    {
        // Fail closed if an invalid numeric enum value reaches the database or model binder.
        if (!Enum.IsDefined(matchingMode))
            matchingMode = ProviderMatchingMode.IdentifierOnly;

        var providerTypes = await _context.OrganizationTypes
            .IgnoreQueryFilters()
            .Where(type => ProviderOrganizationTypeNames.Contains(type.Name))
            .ToListAsync(cancellationToken);

        if (providerTypes.Count == 0)
        {
            return Complete(ProviderOrganizationResolution.NotFound(
                "No supported provider organization type is configured."));
        }

        var providerTypeIds = providerTypes.Select(type => type.Id).ToList();
        var providerOrganizations = _context.Organizations
            .IgnoreQueryFilters()
            .Where(organization =>
                organization.OrganizationTypeId.HasValue &&
                providerTypeIds.Contains(organization.OrganizationTypeId.Value));

        var normalizedIdentifier = ProviderIdentityUtility.NormalizeIdentifier(identity.Identifier);
        var normalizedAuthority = ProviderIdentityUtility.NormalizeAuthority(identity.IdentifierAuthority);

        if (normalizedIdentifier != null)
        {
            var identifierCandidates = await providerOrganizations
                .Where(organization => organization.ProviderIdentifier == normalizedIdentifier)
                .ToListAsync(cancellationToken);

            var identifierResolution = ResolveIdentifierCandidates(
                identifierCandidates,
                normalizedAuthority);
            if (identifierResolution != null)
                return Complete(identifierResolution);
        }

        // Names are deliberately evaluated in memory through the single canonical
        // normalizer. SQL collation cannot reproduce the punctuation/diacritic rules.
        var organizations = await providerOrganizations.ToListAsync(cancellationToken);
        var requireMissingStoredIdentifier = normalizedIdentifier != null;
        var nameMatches = FindNameMatches(
            organizations,
            identity,
            requireMissingStoredIdentifier);

        if (normalizedIdentifier == null)
        {
            if (matchingMode != ProviderMatchingMode.IdentifierOnly)
            {
                if (nameMatches.All.Count == 1)
                {
                    return Complete(ProviderOrganizationResolution.FromMatch(
                        nameMatches.All[0],
                        ProviderMatchBasis.Name,
                        mayEnrichIdentity: false));
                }

                if (nameMatches.All.Count > 1)
                {
                    return Complete(ProviderOrganizationResolution.ManualReview(
                        "The ordering provider name matched multiple organizations."));
                }
            }
            else
            {
                return Complete(ProviderOrganizationResolution.NotFound(
                    "The ordering provider identifier is required by this configuration."));
            }
        }
        else if (nameMatches.All.Count > 0)
        {
            // A new identifier must never claim a name-only master record unless the
            // source has explicitly opted into the trusted enrichment policy.
            if (matchingMode == ProviderMatchingMode.TrustedIdentityEnrichment &&
                nameMatches.All.Count == 1 &&
                nameMatches.StructuredIds.Contains(nameMatches.All[0].Id))
            {
                return Complete(ProviderOrganizationResolution.FromMatch(
                    nameMatches.All[0],
                    ProviderMatchBasis.TrustedNameEnrichment,
                    mayEnrichIdentity: true));
            }

            return Complete(ProviderOrganizationResolution.ManualReview(
                nameMatches.All.Count == 1
                    ? "An ordering provider name matched an organization without an identifier. Review it before assigning the incoming identifier."
                    : "The ordering provider name matched multiple organizations without identifiers."));
        }

        if (!autoCreate)
        {
            return Complete(ProviderOrganizationResolution.NotFound(
                "No matching ordering provider was found."));
        }

        var preferredType = ProviderOrganizationTypeNames
            .Select(typeName => providerTypes.FirstOrDefault(type =>
                type.IsActive && type.Name == typeName))
            .FirstOrDefault(type => type != null);

        return Complete(preferredType == null
            ? ProviderOrganizationResolution.NotFound(
                "No supported provider organization type is configured.")
            : ProviderOrganizationResolution.Create(preferredType.Id, preferredType.Name));
    }

    public Organization CreateOrganization(ProviderIdentity identity, int organizationTypeId) =>
        new()
        {
            Name = identity.DisplayName,
            OrganizationTypeId = organizationTypeId,
            ProviderIdentifier = ProviderIdentityUtility.NormalizeIdentifier(identity.Identifier),
            ProviderIdentifierAuthority = ProviderIdentityUtility.NormalizeAuthority(identity.IdentifierAuthority),
            GivenName = identity.GivenName,
            MiddleName = identity.MiddleName,
            FamilyName = identity.FamilyName,
            NamePrefix = identity.NamePrefix,
            NameSuffix = identity.NameSuffix,
            Credentials = identity.Credentials,
            IsActive = true
        };

    public bool ApplyMissingIdentity(Organization organization, ProviderIdentity identity)
    {
        var changed = false;

        changed |= SetWhenMissing(
            organization.ProviderIdentifier,
            ProviderIdentityUtility.NormalizeIdentifier(identity.Identifier),
            value => organization.ProviderIdentifier = value);
        changed |= SetWhenMissing(
            organization.ProviderIdentifierAuthority,
            ProviderIdentityUtility.NormalizeAuthority(identity.IdentifierAuthority),
            value => organization.ProviderIdentifierAuthority = value);
        changed |= SetWhenMissing(organization.GivenName, identity.GivenName, value => organization.GivenName = value);
        changed |= SetWhenMissing(organization.MiddleName, identity.MiddleName, value => organization.MiddleName = value);
        changed |= SetWhenMissing(organization.FamilyName, identity.FamilyName, value => organization.FamilyName = value);
        changed |= SetWhenMissing(organization.NamePrefix, identity.NamePrefix, value => organization.NamePrefix = value);
        changed |= SetWhenMissing(organization.NameSuffix, identity.NameSuffix, value => organization.NameSuffix = value);
        changed |= SetWhenMissing(organization.Credentials, identity.Credentials, value => organization.Credentials = value);

        if (changed)
            organization.ModifiedAt = DateTime.UtcNow;

        return changed;
    }

    public static bool IsProviderIdentityConflict(DbUpdateException exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(
                    Organization.ProviderIdentityUniqueIndexName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private ProviderOrganizationResolution Complete(ProviderOrganizationResolution resolution)
    {
        _logger.LogInformation(
            "Ordering provider resolution completed with {Outcome} using {MatchBasis} for organization {OrganizationId}",
            resolution.Outcome,
            resolution.MatchBasis,
            resolution.ExistingOrganization?.Id);
        return resolution;
    }

    private static ProviderOrganizationResolution? ResolveIdentifierCandidates(
        IReadOnlyCollection<Organization> candidates,
        string? normalizedAuthority)
    {
        if (normalizedAuthority == null)
        {
            return candidates.Count switch
            {
                0 => null,
                1 => ProviderOrganizationResolution.FromMatch(
                    candidates.Single(),
                    ProviderMatchBasis.Identifier,
                    mayEnrichIdentity: true),
                _ => ProviderOrganizationResolution.ManualReview(
                    "The ordering provider identifier matched multiple authorities.")
            };
        }

        var exactAuthorityMatches = candidates
            .Where(organization =>
                ProviderIdentityUtility.NormalizeAuthority(organization.ProviderIdentifierAuthority) ==
                normalizedAuthority)
            .ToList();

        if (exactAuthorityMatches.Count == 1)
        {
            return ProviderOrganizationResolution.FromMatch(
                exactAuthorityMatches[0],
                ProviderMatchBasis.Identifier,
                mayEnrichIdentity: true);
        }

        if (exactAuthorityMatches.Count > 1)
        {
            return ProviderOrganizationResolution.ManualReview(
                "The ordering provider identifier and authority matched multiple organizations.");
        }

        var candidatesWithoutAuthority = candidates
            .Where(organization =>
                ProviderIdentityUtility.NormalizeAuthority(organization.ProviderIdentifierAuthority) == null)
            .ToList();

        if (candidates.Count == 1 && candidatesWithoutAuthority.Count == 1)
        {
            return ProviderOrganizationResolution.FromMatch(
                candidatesWithoutAuthority[0],
                ProviderMatchBasis.Identifier,
                mayEnrichIdentity: true);
        }

        return candidatesWithoutAuthority.Count > 0
            ? ProviderOrganizationResolution.ManualReview(
                "The ordering provider identifier matched organizations with incomplete authority data.")
            : null;
    }

    private static ProviderNameMatches FindNameMatches(
        IReadOnlyCollection<Organization> organizations,
        ProviderIdentity identity,
        bool requireMissingStoredIdentifier)
    {
        var candidates = requireMissingStoredIdentifier
            ? organizations.Where(organization =>
                ProviderIdentityUtility.NormalizeIdentifier(organization.ProviderIdentifier) == null)
            : organizations;

        var candidateList = candidates.ToList();
        var structuredMatches = new List<Organization>();

        if (identity.HasStructuredName)
        {
            var normalizedGivenName = ProviderIdentityUtility.NormalizeName(identity.GivenName);
            var normalizedFamilyName = ProviderIdentityUtility.NormalizeName(identity.FamilyName);

            structuredMatches = candidateList
                .Where(organization =>
                    ProviderIdentityUtility.NormalizeName(organization.GivenName) == normalizedGivenName &&
                    ProviderIdentityUtility.NormalizeName(organization.FamilyName) == normalizedFamilyName &&
                    ProviderIdentityUtility.MiddleNamesAreCompatible(organization.MiddleName, identity.MiddleName) &&
                    NameComponentsAreCompatible(organization.NameSuffix, identity.NameSuffix))
                .ToList();
        }

        var normalizedDisplayNames = GetNormalizedDisplayNames(identity);
        var legacyMatches = normalizedDisplayNames.Count == 0
            ? new List<Organization>()
            : candidateList
                .Where(organization =>
                    normalizedDisplayNames.Contains(
                        ProviderIdentityUtility.NormalizeName(organization.Name)))
                .ToList();

        var allMatches = structuredMatches
            .Concat(legacyMatches)
            .DistinctBy(organization => organization.Id)
            .ToList();

        return new ProviderNameMatches(
            allMatches,
            structuredMatches.Select(organization => organization.Id).ToHashSet());
    }

    private static HashSet<string> GetNormalizedDisplayNames(ProviderIdentity identity)
    {
        var candidates = new[]
        {
            identity.DisplayName,
            string.Join(" ", new[] { identity.GivenName, identity.MiddleName, identity.FamilyName }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            string.Join(" ", new[] { identity.GivenName, identity.FamilyName }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
        };

        return candidates
            .Select(ProviderIdentityUtility.NormalizeName)
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static bool NameComponentsAreCompatible(string? existingValue, string? incomingValue)
    {
        var normalizedExisting = ProviderIdentityUtility.NormalizeName(existingValue);
        var normalizedIncoming = ProviderIdentityUtility.NormalizeName(incomingValue);

        return normalizedExisting.Length == 0 ||
               normalizedIncoming.Length == 0 ||
               normalizedExisting == normalizedIncoming;
    }

    private static bool SetWhenMissing(
        string? currentValue,
        string? incomingValue,
        Action<string> setter)
    {
        if (!string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(incomingValue))
            return false;

        setter(incomingValue);
        return true;
    }

    private sealed record ProviderNameMatches(
        List<Organization> All,
        HashSet<Guid> StructuredIds);
}

public enum ProviderResolutionOutcome
{
    Matched,
    Create,
    ManualReview,
    NotFound
}

public enum ProviderMatchBasis
{
    None,
    Identifier,
    Name,
    TrustedNameEnrichment
}

public sealed class ProviderOrganizationResolution
{
    public ProviderResolutionOutcome Outcome { get; private init; }
    public ProviderMatchBasis MatchBasis { get; private init; }
    public Organization? ExistingOrganization { get; private init; }
    public int? NewOrganizationTypeId { get; private init; }
    public string? NewOrganizationTypeName { get; private init; }
    public bool MayEnrichIdentity { get; private init; }
    public string? Reason { get; private init; }

    // Retained as a compatibility alias while callers migrate to the explicit outcome.
    public bool IsAmbiguous => Outcome == ProviderResolutionOutcome.ManualReview;

    public static ProviderOrganizationResolution FromMatch(
        Organization organization,
        ProviderMatchBasis matchBasis,
        bool mayEnrichIdentity) =>
        organization.IsActive
            ? new()
            {
                Outcome = ProviderResolutionOutcome.Matched,
                MatchBasis = matchBasis,
                ExistingOrganization = organization,
                MayEnrichIdentity = mayEnrichIdentity
            }
            : ManualReview(
                "The ordering provider matched an inactive organization and requires manual review.");

    public static ProviderOrganizationResolution Create(int typeId, string typeName) =>
        new()
        {
            Outcome = ProviderResolutionOutcome.Create,
            NewOrganizationTypeId = typeId,
            NewOrganizationTypeName = typeName
        };

    public static ProviderOrganizationResolution ManualReview(string reason) =>
        new()
        {
            Outcome = ProviderResolutionOutcome.ManualReview,
            Reason = reason
        };

    public static ProviderOrganizationResolution NotFound(string reason) =>
        new()
        {
            Outcome = ProviderResolutionOutcome.NotFound,
            Reason = reason
        };
}
