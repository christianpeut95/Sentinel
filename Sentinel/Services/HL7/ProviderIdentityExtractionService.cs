using Sentinel.Models;
using Sentinel.Services.HL7.Models;

namespace Sentinel.Services.HL7;

public interface IProviderIdentityExtractionService
{
    ProviderIdentity? Extract(
        HL7Message message,
        HL7Configuration? configuration = null);
}

/// <summary>
/// Extracts one structured ordering-provider identity from configured and
/// standards-based HL7 locations. Matching and persistence policy deliberately
/// remain in <see cref="IProviderIdentityResolutionService"/>.
/// </summary>
public sealed class ProviderIdentityExtractionService : IProviderIdentityExtractionService
{
    private readonly ILogger<ProviderIdentityExtractionService> _logger;

    public ProviderIdentityExtractionService(
        ILogger<ProviderIdentityExtractionService> logger)
    {
        _logger = logger;
    }

    public ProviderIdentity? Extract(
        HL7Message message,
        HL7Configuration? configuration = null)
    {
        if (message.Segments == null || message.Segments.Count == 0)
        {
            _logger.LogWarning(
                "Ordering provider extraction skipped because message {MessageControlId} has no parsed segments",
                message.MessageControlId);
            return null;
        }

        // A configured full-field mapping takes priority. Only metadata is logged;
        // provider names, identifiers, and raw HL7 segments are intentionally omitted.
        var providerMapping = configuration?.FieldMappings?
            .Where(mapping => mapping.IsActive)
            .OrderByDescending(mapping => mapping.Priority)
            .FirstOrDefault(mapping =>
                mapping.TargetProperty == "OrderingProvider" ||
                mapping.TargetProperty == "OrderingProviderName");

        if (providerMapping != null &&
            TryParseFieldPath(
                providerMapping.FieldPath,
                out var mappedSegmentType,
                out var mappedFieldNumber))
        {
            var configuredIdentity = ExtractFromSegments(
                message,
                mappedSegmentType,
                new[] { mappedFieldNumber });

            if (configuredIdentity != null)
            {
                _logger.LogDebug(
                    "Ordering provider identity extracted using configured field {FieldPath}",
                    providerMapping.FieldPath);
                return configuredIdentity;
            }
        }

        // Standard locations first, followed by known sender-specific fallbacks.
        var identity = ExtractFromSegments(message, "ORC", new[] { 12 })
            ?? ExtractFromSegments(message, "ORC", new[] { 13, 14 }, requireComposite: true)
            ?? ExtractFromSegments(message, "OBR", new[] { 16 })
            ?? ExtractFromSegments(message, "OBR", new[] { 28, 32 }, requireComposite: true);

        if (identity == null)
        {
            _logger.LogDebug(
                "No ordering provider identity found in configured, ORC, or OBR fields for message {MessageControlId}",
                message.MessageControlId);
        }

        return identity;
    }

    private static bool TryParseFieldPath(
        string? fieldPath,
        out string segmentType,
        out int fieldNumber)
    {
        segmentType = string.Empty;
        fieldNumber = 0;

        if (string.IsNullOrWhiteSpace(fieldPath))
            return false;

        var pathParts = fieldPath.Split('-', 2);
        if (pathParts.Length != 2)
            return false;

        var fieldPart = pathParts[1].Split('.', 2)[0];
        if (!int.TryParse(fieldPart, out fieldNumber) || fieldNumber < 1)
            return false;

        segmentType = pathParts[0].Trim().ToUpperInvariant();
        return segmentType.Length is > 0 and <= 10 &&
               segmentType.All(character => char.IsAsciiLetterOrDigit(character));
    }

    private static ProviderIdentity? ExtractFromSegments(
        HL7Message message,
        string segmentType,
        IReadOnlyCollection<int> fieldNumbers,
        bool requireComposite = false)
    {
        foreach (var segment in message.Segments.Where(segment =>
                     string.Equals(segment.SegmentType, segmentType, StringComparison.Ordinal)))
        {
            var fields = segment.RawSegment.Split('|');
            foreach (var fieldNumber in fieldNumbers)
            {
                if (fieldNumber >= fields.Length)
                    continue;

                if (requireComposite && !fields[fieldNumber].Contains('^'))
                    continue;

                var identity = ProviderIdentityUtility.ParseXcn(fields[fieldNumber]);
                if (identity != null)
                    return identity;
            }
        }

        return null;
    }
}
