using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Services.HL7.Models;
using System.Text.Json;

namespace Sentinel.Services.HL7;

/// <summary>
/// Calculates specificity scores for case definition matches by re-evaluating
/// the criteria tree to determine which specific criteria contributed to the match.
/// </summary>
public static class CaseDefinitionSpecificityCalculator
{
    /// <summary>
    /// Scores a case definition match by counting how many LEAF laboratory criteria
    /// were satisfied along the successful evaluation path.
    /// This accurately reflects specificity: (B AND C) = 2 criteria, C = 1 criterion.
    /// </summary>
    public static async Task<int> CalculateSpecificityScoreAsync(
        CaseDefinitionMatchResult match,
        List<StagedMarker> labMarkers,
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (match.CaseDefinition == null)
            return 0;

        var caseDefId = match.CaseDefinition.Id;

        // Load ALL criteria for this case definition (to build the tree)
        var allCriteria = await context.CaseDefinitionCriteria
            .Where(c => c.CaseDefinitionId == caseDefId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (!allCriteria.Any())
            return 0;

        // Get only laboratory criteria (leaf nodes that check actual lab data)
        var laboratoryCriteria = allCriteria
            .Where(c => c.CriterionType == CriterionType.Laboratory)
            .ToList();

        if (!laboratoryCriteria.Any())
            return 0;

        // Evaluate each lab criterion and count satisfied ones
        var satisfiedCriteria = new HashSet<int>();

        foreach (var criterion in laboratoryCriteria)
        {
            if (await IsLabCriterionSatisfiedAsync(criterion, labMarkers, context, cancellationToken))
            {
                satisfiedCriteria.Add(criterion.Id);
            }
        }

        var score = satisfiedCriteria.Count;

        logger.LogDebug(
            "[SPECIFICITY CALC] Definition '{Name}': {Score} lab criteria satisfied (out of {Total})",
            match.CaseDefinition.Name,
            score,
            laboratoryCriteria.Count);

        return score;
    }

    /// <summary>
    /// Scores a case definition match using persisted LabResult markers.
    /// Counts how many laboratory (leaf) criteria are satisfied by the markers.
    /// </summary>
    public static async Task<int> CalculateSpecificityScoreFromLabResultAsync(
        CaseDefinitionMatchResult match,
        List<LabResultMarker> labMarkers,
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (match.CaseDefinition == null)
            return 0;

        var caseDefId = match.CaseDefinition.Id;

        // Load ALL criteria for this case definition
        var allCriteria = await context.CaseDefinitionCriteria
            .Where(c => c.CaseDefinitionId == caseDefId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (!allCriteria.Any())
            return 0;

        // Get only laboratory criteria (leaf nodes that check actual lab data)
        var laboratoryCriteria = allCriteria
            .Where(c => c.CriterionType == CriterionType.Laboratory)
            .ToList();

        if (!laboratoryCriteria.Any())
            return 0;

        // Evaluate each lab criterion and count satisfied ones
        var satisfiedCriteria = new HashSet<int>();

        foreach (var criterion in laboratoryCriteria)
        {
            if (IsLabCriterionSatisfiedByPersistedMarker(criterion, labMarkers))
            {
                satisfiedCriteria.Add(criterion.Id);
            }
        }

        var score = satisfiedCriteria.Count;

        logger.LogWarning(
            "🔥 [SPECIFICITY CALC] Definition '{Name}': {Score} lab criteria satisfied (out of {Total})",
            match.CaseDefinition.Name,
            score,
            laboratoryCriteria.Count);

        return score;
    }

    /// <summary>
    /// Overload: Checks if a single laboratory criterion is satisfied by ANY staged marker.
    /// </summary>
    private static async Task<bool> IsLabCriterionSatisfiedAsync(
        CaseDefinitionCriteria criterion,
        List<StagedMarker> labMarkers,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        // Parse acceptable pathogens
        List<Guid>? acceptablePathogens = null;
        if (!string.IsNullOrEmpty(criterion.AcceptablePathogensJson))
        {
            try
            {
                acceptablePathogens = JsonSerializer.Deserialize<List<Guid>>(criterion.AcceptablePathogensJson);
            }
            catch { return false; }
        }

        if (acceptablePathogens == null || !acceptablePathogens.Any())
            return false;

        // Parse other constraints
        List<int>? acceptableTestMethods = ParseJsonIntArray(criterion.AcceptableTestMethodsJson);
        List<int>? acceptableSpecimenTypes = ParseJsonIntArray(criterion.AcceptableSpecimenTypesJson);
        List<int>? acceptableResults = ParseJsonIntArray(criterion.AcceptableResultsJson);

        // Check if any lab marker satisfies this criterion
        foreach (var marker in labMarkers)
        {
            if (!marker.ResolvedPathogenId.HasValue)
                continue;

            var pathogenId = marker.ResolvedPathogenId.Value;

            // Check pathogen
            if (!acceptablePathogens.Contains(pathogenId))
                continue;

            // Check test method (if specified)
            if (acceptableTestMethods?.Any() == true)
            {
                if (!marker.ResolvedTestMethodId.HasValue ||
                    !acceptableTestMethods.Contains(marker.ResolvedTestMethodId.Value))
                    continue;
            }

            // Check specimen type (if specified)
            if (acceptableSpecimenTypes?.Any() == true)
            {
                if (!marker.ResolvedSpecimenTypeId.HasValue ||
                    !acceptableSpecimenTypes.Contains(marker.ResolvedSpecimenTypeId.Value))
                    continue;
            }

            // Check result (if specified)
            if (acceptableResults?.Any() == true)
            {
                if (!marker.ResolvedTestResultId.HasValue ||
                    !acceptableResults.Contains(marker.ResolvedTestResultId.Value))
                    continue;
            }

            // This marker satisfies all constraints
            return true;
        }

        return false;
    }

    /// <summary>
    /// Overload: Checks if a single laboratory criterion is satisfied by ANY persisted lab result marker.
    /// </summary>
    private static bool IsLabCriterionSatisfiedByPersistedMarker(
        CaseDefinitionCriteria criterion,
        List<LabResultMarker> labMarkers)
    {
        // Parse acceptable pathogens
        List<Guid>? acceptablePathogens = null;
        if (!string.IsNullOrEmpty(criterion.AcceptablePathogensJson))
        {
            try
            {
                acceptablePathogens = JsonSerializer.Deserialize<List<Guid>>(criterion.AcceptablePathogensJson);
            }
            catch { return false; }
        }

        if (acceptablePathogens == null || !acceptablePathogens.Any())
            return false;

        // Parse other constraints
        List<int>? acceptableTestMethods = ParseJsonIntArray(criterion.AcceptableTestMethodsJson);
        List<int>? acceptableSpecimenTypes = ParseJsonIntArray(criterion.AcceptableSpecimenTypesJson);
        List<int>? acceptableResults = ParseJsonIntArray(criterion.AcceptableResultsJson);

        // Check if any lab marker satisfies this criterion
        foreach (var marker in labMarkers)
        {
            if (!marker.PathogenId.HasValue)
                continue;

            var pathogenId = marker.PathogenId.Value;

            // Check pathogen
            if (!acceptablePathogens.Contains(pathogenId))
                continue;

            // Check test method (if specified)
            if (acceptableTestMethods?.Any() == true)
            {
                if (!marker.TestMethodId.HasValue ||
                    !acceptableTestMethods.Contains(marker.TestMethodId.Value))
                    continue;
            }

            // Check specimen type (if specified)
            if (acceptableSpecimenTypes?.Any() == true)
            {
                if (!marker.SpecimenTypeId.HasValue ||
                    !acceptableSpecimenTypes.Contains(marker.SpecimenTypeId.Value))
                    continue;
            }

            // Check result (if specified)
            if (acceptableResults?.Any() == true)
            {
                if (!marker.TestResultId.HasValue ||
                    !acceptableResults.Contains(marker.TestResultId.Value))
                    continue;
            }

            // This marker satisfies all constraints
            return true;
        }

        return false;
    }

    private static List<int>? ParseJsonIntArray(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json);
        }
        catch
        {
            return null;
        }
    }
}
