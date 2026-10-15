using System.Globalization;
using System.Text;

namespace Sentinel.Services.HL7.Models;

/// <summary>
/// Structured identity extracted from an HL7 XCN provider field.
/// This is an ingestion value object, not a persisted entity.
/// </summary>
public sealed class ProviderIdentity
{
    public string? Identifier { get; init; }
    public string? IdentifierAuthority { get; init; }
    public string? GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? FamilyName { get; init; }
    public string? NamePrefix { get; init; }
    public string? NameSuffix { get; init; }
    public string? Credentials { get; init; }

    public bool HasIdentifier => !string.IsNullOrWhiteSpace(Identifier);
    public bool HasStructuredName =>
        !string.IsNullOrWhiteSpace(GivenName) &&
        !string.IsNullOrWhiteSpace(FamilyName);

    public string DisplayName
    {
        get
        {
            var parts = new[] { NamePrefix, GivenName, MiddleName, FamilyName, NameSuffix }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim());

            var name = string.Join(" ", parts);
            return !string.IsNullOrWhiteSpace(name) ? name : Identifier ?? string.Empty;
        }
    }
}

/// <summary>
/// Central parser and normalizer for provider identities. Keeping this logic in one
/// place prevents preview, staging, reprocessing, and CRUD validation from drifting.
/// </summary>
public static class ProviderIdentityUtility
{
    public static ProviderIdentity? ParseXcn(string? fieldValue)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
            return null;

        // XCN may repeat. Sentinel currently stores one ordering provider, so use the
        // first repetition containing either an identifier or a usable person name.
        foreach (var repetition in fieldValue.Split('~', StringSplitOptions.RemoveEmptyEntries))
        {
            var identity = ParseSingleXcn(repetition);
            if (identity is { HasIdentifier: true } || identity is { HasStructuredName: true })
                return identity;
        }

        return null;
    }

    public static string? NormalizeIdentifier(string? value)
    {
        var cleaned = Clean(value, 100);
        if (cleaned == null)
            return null;

        var normalized = new string(cleaned
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        return normalized.Length == 0 ? null : normalized;
    }

    public static string? NormalizeAuthority(string? value)
    {
        var cleaned = Clean(value, 100);
        return cleaned?.ToUpperInvariant();
    }

    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = true;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    public static bool MiddleNamesAreCompatible(string? left, string? right)
    {
        var normalizedLeft = NormalizeName(left);
        var normalizedRight = NormalizeName(right);

        if (normalizedLeft.Length == 0 || normalizedRight.Length == 0)
            return true;

        if (normalizedLeft == normalizedRight)
            return true;

        return (normalizedLeft.Length == 1 || normalizedRight.Length == 1) &&
               normalizedLeft[0] == normalizedRight[0];
    }

    private static ProviderIdentity? ParseSingleXcn(string repetition)
    {
        var components = repetition.Split('^');
        if (components.Length == 0)
            return null;

        var component0 = Component(components, 0, 100);
        var component1 = Component(components, 1, 100);
        var component2 = Component(components, 2, 100);
        var component8 = Component(components, 8, 100);

        // Backward compatibility for Sentinel's original generator and common
        // non-standard Last^First^^^^^^^Identifier payloads.
        var legacyLastFirst = component0 != null &&
                              component1 != null &&
                              component2 == null &&
                              component8 != null &&
                              component8.Any(char.IsDigit) &&
                              !component0.Any(char.IsDigit) &&
                              !component1.Any(char.IsDigit);

        if (legacyLastFirst)
        {
            return new ProviderIdentity
            {
                Identifier = NormalizeIdentifier(component8),
                FamilyName = component0,
                GivenName = component1,
                MiddleName = Component(components, 2, 100),
                NameSuffix = Component(components, 4, 50),
                NamePrefix = Component(components, 5, 50),
                Credentials = Component(components, 6, 100)
            };
        }

        // Standard XCN: ID^Family^Given^FurtherGiven^Suffix^Prefix^Degree^Source^Authority
        return new ProviderIdentity
        {
            Identifier = NormalizeIdentifier(component0),
            FamilyName = FirstSubcomponent(component1, 100),
            GivenName = component2,
            MiddleName = Component(components, 3, 100),
            NameSuffix = Component(components, 4, 50),
            NamePrefix = Component(components, 5, 50),
            Credentials = Component(components, 6, 100),
            IdentifierAuthority = NormalizeAuthority(FirstSubcomponent(component8, 100))
        };
    }

    private static string? Component(string[] components, int index, int maxLength) =>
        index < components.Length ? Clean(components[index], maxLength) : null;

    private static string? FirstSubcomponent(string? value, int maxLength) =>
        Clean(value?.Split('&')[0], maxLength);

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sanitized = new string(value
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();

        if (sanitized.Length == 0)
            return null;

        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }
}
