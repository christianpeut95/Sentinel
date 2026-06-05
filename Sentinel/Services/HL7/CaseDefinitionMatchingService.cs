using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using System.Text.Json;

namespace Sentinel.Services.HL7;

/// <summary>
/// Service for matching resolved marker fields against case definitions
/// to determine disease and confirmation status
/// </summary>
public class CaseDefinitionMatchingService : ICaseDefinitionMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CaseDefinitionMatchingService> _logger;

    public CaseDefinitionMatchingService(
        ApplicationDbContext context,
        ILogger<CaseDefinitionMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Matches resolved marker fields against active case definitions
    /// Returns the first matching case definition with disease and confirmation status
    /// </summary>
    public async Task<CaseDefinitionMatchResult?> MatchCaseDefinitionAsync(
        MarkerResolutionResult resolvedMarker,
        CancellationToken cancellationToken = default)
    {
        // Get all active case definitions with laboratory criteria
        var caseDefinitions = await _context.CaseDefinitions
            .IgnoreQueryFilters()
            .Include(cd => cd.Disease)
            .Include(cd => cd.ConfirmationStatus)
            .Include(cd => cd.Criteria)
            .Where(cd =>
                cd.Status == CaseDefinitionStatus.Current &&
                cd.EnableAutoEvaluation &&
                cd.Criteria.Any(c => c.CriterionType == CriterionType.Laboratory))
            .ToListAsync(cancellationToken);

        if (!caseDefinitions.Any())
        {
            _logger.LogDebug("[CASE DEFINITION] No active case definitions with laboratory criteria found");
            return null;
        }

        _logger.LogDebug("[CASE DEFINITION] Evaluating {Count} case definitions", caseDefinitions.Count);

        foreach (var caseDefinition in caseDefinitions)
        {
            var matchResult = await EvaluateCaseDefinitionAsync(caseDefinition, resolvedMarker, cancellationToken);

            if (matchResult != null)
            {
                _logger.LogInformation(
                    "[CASE DEFINITION] Matched! Disease={Disease}, ConfirmationStatus={Status}",
                    matchResult.Disease?.Name ?? "NULL",
                    matchResult.ConfirmationStatus?.Name ?? "NULL");

                return matchResult;
            }
        }

        _logger.LogDebug("[CASE DEFINITION] No matching case definition found for resolved marker");
        return null;
    }

    private async Task<CaseDefinitionMatchResult?> EvaluateCaseDefinitionAsync(
        CaseDefinition caseDefinition,
        MarkerResolutionResult resolvedMarker,
        CancellationToken cancellationToken)
    {
        var laboratoryCriteria = caseDefinition.Criteria
            .Where(c => c.CriterionType == CriterionType.Laboratory)
            .ToList();

        if (!laboratoryCriteria.Any())
            return null;

        // Group criteria by group number and logical operator
        var groupedCriteria = laboratoryCriteria
            .GroupBy(c => c.GroupNumber)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groupedCriteria)
        {
            var groupResults = new List<bool>();

            foreach (var criterion in group)
            {
                var criterionMatch = await EvaluateLaboratoryCriterion(criterion, resolvedMarker, cancellationToken);
                groupResults.Add(criterionMatch);

                _logger.LogDebug(
                    "[CASE DEFINITION] CaseDefinition={CaseDefId}, Criterion={CritId}, Group={Group}, Operator={Op}, Match={Match}",
                    caseDefinition.Id,
                    criterion.Id,
                    criterion.GroupNumber,
                    criterion.LogicalOperator,
                    criterionMatch);
            }

            // Evaluate group result based on logical operators
            var groupMatch = EvaluateGroupLogic(group.ToList(), groupResults);

            if (!groupMatch)
            {
                // If any required group fails, this case definition doesn't match
                return null;
            }
        }

        // All groups passed - this case definition matches!
        return new CaseDefinitionMatchResult
        {
            CaseDefinition = caseDefinition,
            Disease = caseDefinition.Disease,
            ConfirmationStatus = caseDefinition.ConfirmationStatus,
            DiseaseId = caseDefinition.DiseaseId,
            ConfirmationStatusId = caseDefinition.ConfirmationStatusId
        };
    }

    private async Task<bool> EvaluateLaboratoryCriterion(
        CaseDefinitionCriteria criterion,
        MarkerResolutionResult resolvedMarker,
        CancellationToken cancellationToken)
    {
        var matches = new List<bool>();

        // Evaluate Specimen Type
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableSpecimenTypesJson))
        {
            var acceptableSpecimenTypes = DeserializeIntArray(criterion.AcceptableSpecimenTypesJson);
            var specimenMatch = resolvedMarker.SpecimenTypeId.HasValue &&
                                acceptableSpecimenTypes.Contains(resolvedMarker.SpecimenTypeId.Value);
            matches.Add(specimenMatch);

            _logger.LogDebug(
                "[CRITERION] Specimen: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                resolvedMarker.SpecimenTypeId?.ToString() ?? "NULL",
                string.Join(",", acceptableSpecimenTypes),
                specimenMatch);
        }

        // Evaluate Pathogen/Biomarker
        if (!string.IsNullOrWhiteSpace(criterion.AcceptablePathogensJson))
        {
            var acceptablePathogens = await ResolveAcceptablePathogenIdsAsync(
                criterion.AcceptablePathogensJson,
                cancellationToken);

            var pathogenMatch = resolvedMarker.PathogenId.HasValue &&
                                acceptablePathogens.Contains(resolvedMarker.PathogenId.Value);
            matches.Add(pathogenMatch);

            _logger.LogDebug(
                "[CRITERION] Pathogen: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                resolvedMarker.PathogenId?.ToString() ?? "NULL",
                string.Join(",", acceptablePathogens),
                pathogenMatch);
        }

        // Evaluate Test Method
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableTestMethodsJson))
        {
            var acceptableTestMethods = DeserializeIntArray(criterion.AcceptableTestMethodsJson);
            var testMethodMatch = resolvedMarker.TestMethodId.HasValue &&
                                  acceptableTestMethods.Contains(resolvedMarker.TestMethodId.Value);
            matches.Add(testMethodMatch);

            _logger.LogDebug(
                "[CRITERION] TestMethod: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                resolvedMarker.TestMethodId?.ToString() ?? "NULL",
                string.Join(",", acceptableTestMethods),
                testMethodMatch);
        }

        // Evaluate Test Result
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableResultsJson))
        {
            var acceptableResults = DeserializeIntArray(criterion.AcceptableResultsJson);
            var resultMatch = resolvedMarker.TestResultId.HasValue &&
                              acceptableResults.Contains(resolvedMarker.TestResultId.Value);
            matches.Add(resultMatch);

            _logger.LogDebug(
                "[CRITERION] Result: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                resolvedMarker.TestResultId?.ToString() ?? "NULL",
                string.Join(",", acceptableResults),
                resultMatch);
        }

        // If no matches were evaluated (no criteria fields set), treat as no match
        if (!matches.Any())
        {
            _logger.LogDebug("[CRITERION] No laboratory criteria fields set - treating as no match");
            return false;
        }

        // BUSINESS RULE: ALL criteria elements must match for a case definition to match
        // There is no "at least one" logic - if pathogen, specimen, test method, and result are specified,
        // ALL must match for this criterion to be satisfied
        var criterionResult = matches.All(m => m);

        _logger.LogDebug(
            "[CRITERION] All criteria elements must match. Matches={Matches}, Result={Result}",
            string.Join(",", matches.Select(m => m ? "T" : "F")),
            criterionResult);

        return criterionResult;
    }

    private bool EvaluateGroupLogic(List<CaseDefinitionCriteria> group, List<bool> results)
    {
        if (!results.Any())
            return false;

        // Determine the dominant operator for the group
        var dominantOperator = group.First().LogicalOperator;

        return dominantOperator switch
        {
            LogicalOperator.AND => results.All(r => r),
            LogicalOperator.OR => results.Any(r => r),
            LogicalOperator.NOT => !results.Any(r => r),
            _ => false
        };
    }

    /// <summary>
    /// Resolves acceptable pathogen IDs from JSON that may contain either GUID array or string array (pathogen names)
    /// </summary>
    private async Task<List<Guid>> ResolveAcceptablePathogenIdsAsync(
        string json,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<Guid>();

        try
        {
            // First, try to deserialize as GUID array (new format)
            var guidArray = DeserializeGuidArray(json);
            if (guidArray.Any())
            {
                _logger.LogDebug("[PATHOGEN RESOLUTION] Deserialized {Count} pathogen GUIDs from case definition", guidArray.Count);
                return guidArray;
            }

            // If that didn't work, try to deserialize as string array (legacy format - pathogen names)
            var nameArray = JsonSerializer.Deserialize<List<string>>(json);
            if (nameArray == null || !nameArray.Any())
            {
                _logger.LogWarning("[PATHOGEN RESOLUTION] Failed to deserialize pathogen JSON as GUID or string array: {Json}", json);
                return new List<Guid>();
            }

            _logger.LogDebug("[PATHOGEN RESOLUTION] Deserialized {Count} pathogen names, resolving to GUIDs: {Names}",
                nameArray.Count,
                string.Join(", ", nameArray));

            // Look up pathogen IDs by name
            var pathogens = await _context.Pathogens
                .Where(p => nameArray.Contains(p.Name) && p.IsActive)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);

            if (pathogens.Count != nameArray.Count)
            {
                var foundNames = pathogens.Select(p => p.Name).ToList();
                var missingNames = nameArray.Except(foundNames).ToList();
                _logger.LogWarning(
                    "[PATHOGEN RESOLUTION] Could not resolve all pathogen names. Found: {Found}, Missing: {Missing}",
                    string.Join(", ", foundNames),
                    string.Join(", ", missingNames));
            }

            var resolvedIds = pathogens.Select(p => p.Id).ToList();
            _logger.LogDebug("[PATHOGEN RESOLUTION] Resolved {Count} pathogen names to GUIDs: {Ids}",
                resolvedIds.Count,
                string.Join(", ", resolvedIds));

            return resolvedIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PATHOGEN RESOLUTION] Error resolving pathogen IDs from JSON: {Json}", json);
            return new List<Guid>();
        }
    }

    private List<int> DeserializeIntArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize int array from JSON: {Json}", json);
            return new List<int>();
        }
    }

    private List<Guid> DeserializeGuidArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Guid array from JSON: {Json}", json);
            return new List<Guid>();
        }
    }
}

#region Result Classes

public class CaseDefinitionMatchResult
{
    public CaseDefinition? CaseDefinition { get; set; }
    public Disease? Disease { get; set; }
    public CaseStatus? ConfirmationStatus { get; set; }
    public Guid? DiseaseId { get; set; }
    public int? ConfirmationStatusId { get; set; }
}

#endregion
