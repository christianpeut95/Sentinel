namespace Sentinel.Services.HL7;

public interface ICaseDefinitionMatchingService
{
    Task<CaseDefinitionMatchResult?> MatchCaseDefinitionAsync(
        MarkerResolutionResult resolvedMarker,
        CancellationToken cancellationToken = default);
}
