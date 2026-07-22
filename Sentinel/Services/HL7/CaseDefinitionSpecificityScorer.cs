using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Services.CaseDefinitionEvaluation;
using Sentinel.Services.HL7.Models;
using System.Text.Json;

namespace Sentinel.Services.HL7;

/// <summary>
/// Scores case definition matches by specificity to determine which match is most specific
/// when multiple definitions match the same disease.
/// </summary>
public class CaseDefinitionSpecificityScorer
{
    private readonly ApplicationDbContext _context;
    private readonly TreeBasedCriteriaEvaluator _treeEvaluator;
    private readonly ILogger<CaseDefinitionSpecificityScorer> _logger;

    public CaseDefinitionSpecificityScorer(
        ApplicationDbContext context,
        TreeBasedCriteriaEvaluator treeEvaluator,
        ILogger<CaseDefinitionSpecificityScorer> logger)
    {
        _context = context;
        _treeEvaluator = treeEvaluator;
        _logger = logger;
    }

    /// <summary>
    /// Scores a case definition match by counting how many unique pathogens
    /// were actually matched by the lab markers.
    /// This represents specificity: a definition that matched 2 pathogens is more specific
    /// than one that matched only 1 pathogen.
    /// </summary>
    public async Task<int> ScoreMatchAsync(
        CaseDefinitionMatchResult match,
        List<StagedMarker> labMarkers,
        CancellationToken cancellationToken = default)
    {
        if (match.CaseDefinition == null)
            return 0;

        var caseDefId = match.CaseDefinition.Id;

        // Get all laboratory criteria for this definition
        var laboratoryCriteria = await _context.CaseDefinitionCriteria
            .Where(c => c.CaseDefinitionId == caseDefId && c.CriterionType == CriterionType.Laboratory)
            .ToListAsync(cancellationToken);

        if (!laboratoryCriteria.Any())
        {
            _logger.LogDebug(
                "[SPECIFICITY] Case definition {Name} has no laboratory criteria",
                match.CaseDefinition.Name);
            return 0;
        }

        // Count how many unique pathogens from lab markers matched ANY criterion
        var matchedPathogens = new HashSet<Guid>();

        foreach (var marker in labMarkers.Where(m => m.ResolvedPathogenId.HasValue))
        {
            var pathogenId = marker.ResolvedPathogenId!.Value;

            // Check if this pathogen matches any criterion in the definition
            foreach (var criterion in laboratoryCriteria)
            {
                if (await PathogenMatchesCriterionAsync(pathogenId, criterion, cancellationToken) &&
                    MarkerSatisfiesCriterion(marker, criterion))
                {
                    matchedPathogens.Add(pathogenId);
                    break; // This pathogen matched, move to next marker
                }
            }
        }

        var score = matchedPathogens.Count;

        _logger.LogDebug(
            "[SPECIFICITY] Case definition {Name}: {Score} unique pathogens matched",
            match.CaseDefinition.Name,
            score);

        return score;
    }

    /// <summary>
    /// Checks if a pathogen ID is acceptable for a given criterion
    /// </summary>
    private async Task<bool> PathogenMatchesCriterionAsync(
        Guid pathogenId,
        CaseDefinitionCriteria criterion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(criterion.AcceptablePathogensJson))
            return false;

        try
        {
            var acceptableIds = JsonSerializer.Deserialize<List<Guid>>(criterion.AcceptablePathogensJson);
            return acceptableIds?.Contains(pathogenId) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a lab marker satisfies a criterion's constraints
    /// (test method, specimen type, result value).
    /// </summary>
    private bool MarkerSatisfiesCriterion(StagedMarker marker, CaseDefinitionCriteria criterion)
    {
        // Check test method
        if (!string.IsNullOrEmpty(criterion.AcceptableTestMethodsJson))
        {
            try
            {
                var acceptableTestMethods = JsonSerializer.Deserialize<List<int>>(criterion.AcceptableTestMethodsJson);
                if (acceptableTestMethods != null && acceptableTestMethods.Any())
                {
                    if (!marker.ResolvedTestMethodId.HasValue ||
                        !acceptableTestMethods.Contains(marker.ResolvedTestMethodId.Value))
                        return false;
                }
            }
            catch { /* Ignore JSON errors */ }
        }

        // Check specimen type
        if (!string.IsNullOrEmpty(criterion.AcceptableSpecimenTypesJson))
        {
            try
            {
                var acceptableSpecimenTypes = JsonSerializer.Deserialize<List<int>>(criterion.AcceptableSpecimenTypesJson);
                if (acceptableSpecimenTypes != null && acceptableSpecimenTypes.Any())
                {
                    if (!marker.ResolvedSpecimenTypeId.HasValue ||
                        !acceptableSpecimenTypes.Contains(marker.ResolvedSpecimenTypeId.Value))
                        return false;
                }
            }
            catch { /* Ignore JSON errors */ }
        }

        // Check result value
        if (!string.IsNullOrEmpty(criterion.AcceptableResultsJson))
        {
            try
            {
                var acceptableResults = JsonSerializer.Deserialize<List<int>>(criterion.AcceptableResultsJson);
                if (acceptableResults != null && acceptableResults.Any())
                {
                    if (!marker.ResolvedTestResultId.HasValue ||
                        !acceptableResults.Contains(marker.ResolvedTestResultId.Value))
                        return false;
                }
            }
            catch { /* Ignore JSON errors */ }
        }

        return true;
    }
}
