using Sentinel.Models;

namespace Sentinel.Services.HL7;

public interface ICaseDefinitionMatchingService
{
    /// <summary>
    /// Matches a single resolved marker against active case definitions
    /// </summary>
    Task<CaseDefinitionMatchResult?> MatchCaseDefinitionAsync(
        MarkerResolutionResult resolvedMarker,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates all markers together against multi-marker case definitions.
    /// Used when no individual markers match single-marker case definitions.
    /// </summary>
    Task<List<CaseDefinitionMatchResult>> MatchCaseDefinitionsForLabResultAsync(
        LabResult labResult,
        CancellationToken cancellationToken = default);
}
