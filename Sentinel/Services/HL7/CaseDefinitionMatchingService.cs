using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Models.HL7;
using Sentinel.Services.CaseDefinitionEvaluation;
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
    private readonly IMemoryCache _cache;
    private readonly TreeBasedCriteriaEvaluator _treeEvaluator;

    public CaseDefinitionMatchingService(
        ApplicationDbContext context,
        ILogger<CaseDefinitionMatchingService> logger,
        IMemoryCache cache,
        TreeBasedCriteriaEvaluator treeEvaluator)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _treeEvaluator = treeEvaluator;
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

        // Load disease-specific HL7 matching configuration
        var diseaseConfig = await GetDiseaseConfigAsync(caseDefinition.DiseaseId, cancellationToken);

        // Track missing fields for audit trail
        var missingFields = new List<string>();

        _logger.LogInformation(
            "[CASE DEFINITION] Evaluating case definition {CaseDefId} '{Name}' (Disease: {Disease}) using tree-based evaluation for SINGLE marker",
            caseDefinition.Id,
            caseDefinition.Name,
            caseDefinition.Disease?.Name ?? "NULL");

        _logger.LogInformation(
            "[CASE DEFINITION] Single-marker input: PathogenId={PathogenId}, TestMethodId={MethodId}, SpecimenId={SpecimenId}, ResultId={ResultId}",
            resolvedMarker.PathogenId?.ToString() ?? "NULL",
            resolvedMarker.TestMethodId?.ToString() ?? "NULL",
            resolvedMarker.SpecimenTypeId?.ToString() ?? "NULL",
            resolvedMarker.TestResultId?.ToString() ?? "NULL");

        // Use tree-based evaluator instead of flattening by GroupNumber
        var matches = await _treeEvaluator.EvaluateAsync(
            laboratoryCriteria,
            async (criterion) =>
            {
                return await EvaluateLaboratoryCriterion(
                    criterion,
                    resolvedMarker,
                    diseaseConfig,
                    missingFields,
                    cancellationToken);
            },
            $"CaseDef={caseDefinition.Id}");

        if (!matches)
        {
            _logger.LogWarning("[CASE DEFINITION] ❌ Case definition {CaseDefId} '{Name}' did NOT match single marker", caseDefinition.Id, caseDefinition.Name);
            return null;
        }

        // All groups passed - this case definition matches!
        var isPartialMatch = missingFields.Any();
        var confirmationStatusId = caseDefinition.ConfirmationStatusId;
        int? originalConfirmationStatusId = null;

        // Apply partial match confirmation status override if configured
        if (isPartialMatch && diseaseConfig?.PartialMatchConfirmationStatusId != null)
        {
            originalConfirmationStatusId = confirmationStatusId;
            confirmationStatusId = diseaseConfig.PartialMatchConfirmationStatusId.Value;

            _logger.LogInformation(
                "[CASE DEFINITION] Partial match - overriding confirmation status from {Original} to {Override}",
                originalConfirmationStatusId,
                confirmationStatusId);
        }

        // Load the effective confirmation status object
        // This ensures we have the correct CaseStatus entity, not just the ID
        CaseStatus? effectiveConfirmationStatus = caseDefinition.ConfirmationStatus;

        if (isPartialMatch && confirmationStatusId != originalConfirmationStatusId)
        {
            // Confirmation status was overridden - load the actual CaseStatus entity
            effectiveConfirmationStatus = await _context.CaseStatuses
                .FirstOrDefaultAsync(cs => cs.Id == confirmationStatusId, cancellationToken)
                ?? caseDefinition.ConfirmationStatus; // Fallback if not found

            if (effectiveConfirmationStatus?.Id == confirmationStatusId)
            {
                _logger.LogInformation(
                    "[CASE DEFINITION] Loaded overridden confirmation status: {StatusName} (ID: {StatusId})",
                    effectiveConfirmationStatus.Name,
                    effectiveConfirmationStatus.Id);
            }
            else
            {
                _logger.LogWarning(
                    "[CASE DEFINITION] Failed to load confirmation status ID {StatusId}, using case definition default: {DefaultStatus}",
                    confirmationStatusId,
                    caseDefinition.ConfirmationStatus?.Name ?? "NULL");
            }
        }

        return new CaseDefinitionMatchResult
        {
            CaseDefinition = caseDefinition,
            Disease = caseDefinition.Disease,
            ConfirmationStatus = effectiveConfirmationStatus,
            DiseaseId = caseDefinition.DiseaseId,
            ConfirmationStatusId = confirmationStatusId,
            MissingFields = missingFields,
            IsPartialMatch = isPartialMatch,
            OriginalConfirmationStatusId = originalConfirmationStatusId
        };
    }

    private async Task<bool> EvaluateLaboratoryCriterion(
        CaseDefinitionCriteria criterion,
        MarkerResolutionResult resolvedMarker,
        DiseaseHL7MatchingConfig? diseaseConfig,
        List<string> missingFields,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[CRITERION] Evaluating criterion {CritId} (Parent={ParentId}, GroupNum={GroupNum}, Operator={Operator})",
            criterion.Id,
            criterion.ParentCriteriaId?.ToString() ?? "ROOT",
            criterion.GroupNumber,
            criterion.LogicalOperator);

        var matches = new List<bool>();
        var missingFieldCount = 0;

        // Evaluate Specimen Type
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableSpecimenTypesJson))
        {
            var acceptableSpecimenTypes = await ResolveAcceptableSpecimenTypeIdsAsync(
                criterion.AcceptableSpecimenTypesJson,
                cancellationToken);

            // Check if field is missing and disease config allows it
            if (resolvedMarker.SpecimenTypeStatus == FieldResolutionStatus.NotPresent)
            {
                if (diseaseConfig?.AllowMissingSpecimenType == true)
                {
                    missingFieldCount++;
                    if (missingFieldCount > (diseaseConfig.MaxMissingFieldsAllowed))
                    {
                        _logger.LogDebug("[CRITERION] Too many missing fields - exceeds MaxMissingFieldsAllowed={Max}", diseaseConfig.MaxMissingFieldsAllowed);
                        return false;
                    }

                    missingFields.Add("SpecimenType");
                    _logger.LogDebug("[CRITERION] Specimen: NotPresent but allowed by disease config - skipping evaluation");
                    // Skip this evaluation - don't add to matches
                }
                else
                {
                    matches.Add(false);
                    _logger.LogDebug("[CRITERION] Specimen: NotPresent and NOT allowed - fail");
                }
            }
            else if (resolvedMarker.SpecimenTypeStatus == FieldResolutionStatus.ParseFailed)
            {
                // Parse failures always fail, even if disease allows missing
                matches.Add(false);
                _logger.LogDebug("[CRITERION] Specimen: ParseFailed - fail");
            }
            else
            {
                // Normal matching logic
                var specimenMatch = resolvedMarker.SpecimenTypeId.HasValue &&
                                    acceptableSpecimenTypes.Contains(resolvedMarker.SpecimenTypeId.Value);
                matches.Add(specimenMatch);

                _logger.LogDebug(
                    "[CRITERION] Specimen: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                    resolvedMarker.SpecimenTypeId?.ToString() ?? "NULL",
                    string.Join(",", acceptableSpecimenTypes),
                    specimenMatch);
            }
        }

        // Evaluate Pathogen/Biomarker
        if (!string.IsNullOrWhiteSpace(criterion.AcceptablePathogensJson))
        {
            var acceptablePathogens = await ResolveAcceptablePathogenIdsAsync(
                criterion.AcceptablePathogensJson,
                cancellationToken);

            if (resolvedMarker.PathogenStatus == FieldResolutionStatus.NotPresent)
            {
                if (diseaseConfig?.AllowMissingPathogen == true)
                {
                    missingFieldCount++;
                    if (missingFieldCount > (diseaseConfig.MaxMissingFieldsAllowed))
                    {
                        _logger.LogDebug("[CRITERION] Too many missing fields - exceeds MaxMissingFieldsAllowed={Max}", diseaseConfig.MaxMissingFieldsAllowed);
                        return false;
                    }

                    missingFields.Add("Pathogen");
                    _logger.LogDebug("[CRITERION] Pathogen: NotPresent but allowed by disease config - skipping evaluation");
                }
                else
                {
                    matches.Add(false);
                    _logger.LogDebug("[CRITERION] Pathogen: NotPresent and NOT allowed - fail");
                }
            }
            else if (resolvedMarker.PathogenStatus == FieldResolutionStatus.ParseFailed)
            {
                matches.Add(false);
                _logger.LogDebug("[CRITERION] Pathogen: ParseFailed - fail");
            }
            else
            {
                // Use disease hierarchy awareness for progressive typing
                // This allows child pathogens to match parent disease criteria
                bool pathogenMatch = false;
                if (resolvedMarker.PathogenId.HasValue)
                {
                    pathogenMatch = await IsPathogenAcceptableAsync(
                        resolvedMarker.PathogenId.Value,
                        acceptablePathogens,
                        cancellationToken);
                }
                matches.Add(pathogenMatch);

                _logger.LogDebug(
                    "[CRITERION] Pathogen: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                    resolvedMarker.PathogenId?.ToString() ?? "NULL",
                    string.Join(",", acceptablePathogens),
                    pathogenMatch);
            }
        }

        // Evaluate Test Method
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableTestMethodsJson))
        {
            var acceptableTestMethods = await ResolveAcceptableTestMethodIdsAsync(
                criterion.AcceptableTestMethodsJson,
                cancellationToken);

            if (resolvedMarker.TestMethodStatus == FieldResolutionStatus.NotPresent)
            {
                if (diseaseConfig?.AllowMissingTestMethod == true)
                {
                    missingFieldCount++;
                    if (missingFieldCount > (diseaseConfig.MaxMissingFieldsAllowed))
                    {
                        _logger.LogDebug("[CRITERION] Too many missing fields - exceeds MaxMissingFieldsAllowed={Max}", diseaseConfig.MaxMissingFieldsAllowed);
                        return false;
                    }

                    missingFields.Add("TestMethod");
                    _logger.LogDebug("[CRITERION] TestMethod: NotPresent but allowed by disease config - skipping evaluation");
                }
                else
                {
                    matches.Add(false);
                    _logger.LogDebug("[CRITERION] TestMethod: NotPresent and NOT allowed - fail");
                }
            }
            else if (resolvedMarker.TestMethodStatus == FieldResolutionStatus.ParseFailed)
            {
                matches.Add(false);
                _logger.LogDebug("[CRITERION] TestMethod: ParseFailed - fail");
            }
            else
            {
                var testMethodMatch = resolvedMarker.TestMethodId.HasValue &&
                                      acceptableTestMethods.Contains(resolvedMarker.TestMethodId.Value);
                matches.Add(testMethodMatch);

                _logger.LogDebug(
                    "[CRITERION] TestMethod: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                    resolvedMarker.TestMethodId?.ToString() ?? "NULL",
                    string.Join(",", acceptableTestMethods),
                    testMethodMatch);
            }
        }

        // Evaluate Test Result
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableResultsJson))
        {
            var acceptableResults = await ResolveAcceptableResultIdsAsync(
                criterion.AcceptableResultsJson,
                cancellationToken);

            if (resolvedMarker.TestResultStatus == FieldResolutionStatus.NotPresent)
            {
                if (diseaseConfig?.AllowMissingResult == true)
                {
                    missingFieldCount++;
                    if (missingFieldCount > (diseaseConfig.MaxMissingFieldsAllowed))
                    {
                        _logger.LogDebug("[CRITERION] Too many missing fields - exceeds MaxMissingFieldsAllowed={Max}", diseaseConfig.MaxMissingFieldsAllowed);
                        return false;
                    }

                    missingFields.Add("Result");
                    _logger.LogDebug("[CRITERION] Result: NotPresent but allowed by disease config - skipping evaluation");
                }
                else
                {
                    matches.Add(false);
                    _logger.LogDebug("[CRITERION] Result: NotPresent and NOT allowed - fail");
                }
            }
            else if (resolvedMarker.TestResultStatus == FieldResolutionStatus.ParseFailed)
            {
                matches.Add(false);
                _logger.LogDebug("[CRITERION] Result: ParseFailed - fail");
            }
            else
            {
                var resultMatch = resolvedMarker.TestResultId.HasValue &&
                                  acceptableResults.Contains(resolvedMarker.TestResultId.Value);
                matches.Add(resultMatch);

                _logger.LogDebug(
                    "[CRITERION] Result: Resolved={Resolved}, Acceptable={Acceptable}, Match={Match}",
                    resolvedMarker.TestResultId?.ToString() ?? "NULL",
                    string.Join(",", acceptableResults),
                    resultMatch);
            }
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

    /// <summary>
    /// Resolves acceptable test result IDs from JSON that may contain either int array or string array (result names)
    /// </summary>
    private async Task<List<int>> ResolveAcceptableResultIdsAsync(
        string json,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            // First, try to deserialize as int array (new format)
            var intArray = DeserializeIntArray(json);
            if (intArray.Any())
            {
                _logger.LogDebug("[RESULT RESOLUTION] Deserialized {Count} result IDs from case definition", intArray.Count);
                return intArray;
            }

            // If that didn't work, try to deserialize as string array (legacy format - result names)
            var nameArray = JsonSerializer.Deserialize<List<string>>(json);
            if (nameArray == null || !nameArray.Any())
            {
                _logger.LogWarning("[RESULT RESOLUTION] Failed to deserialize result JSON as int or string array: {Json}", json);
                return new List<int>();
            }

            _logger.LogDebug("[RESULT RESOLUTION] Deserialized {Count} result names, resolving to IDs: {Names}",
                nameArray.Count,
                string.Join(", ", nameArray));

            // Look up result IDs by name
            var results = await _context.Set<TestResult>()
                .IgnoreQueryFilters()
                .Where(r => nameArray.Contains(r.Name) && r.IsActive)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync(cancellationToken);

            if (results.Count != nameArray.Count)
            {
                var foundNames = results.Select(r => r.Name).ToList();
                var missingNames = nameArray.Except(foundNames).ToList();
                _logger.LogWarning(
                    "[RESULT RESOLUTION] Could not resolve all result names. Found: {Found}, Missing: {Missing}",
                    string.Join(", ", foundNames),
                    string.Join(", ", missingNames));
            }

            var resolvedIds = results.Select(r => r.Id).ToList();
            _logger.LogDebug("[RESULT RESOLUTION] Resolved {Count} result names to IDs: {Ids}",
                resolvedIds.Count,
                string.Join(", ", resolvedIds));

            return resolvedIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RESULT RESOLUTION] Error resolving result IDs from JSON: {Json}", json);
            return new List<int>();
        }
    }

    /// <summary>
    /// Resolves acceptable specimen type IDs from JSON that may contain either int array or string array (specimen names)
    /// </summary>
    private async Task<List<int>> ResolveAcceptableSpecimenTypeIdsAsync(
        string json,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            // First, try to deserialize as int array (new format)
            var intArray = DeserializeIntArray(json);
            if (intArray.Any())
            {
                _logger.LogDebug("[SPECIMEN RESOLUTION] Deserialized {Count} specimen type IDs from case definition", intArray.Count);
                return intArray;
            }

            // If that didn't work, try to deserialize as string array (legacy format - specimen names)
            var nameArray = JsonSerializer.Deserialize<List<string>>(json);
            if (nameArray == null || !nameArray.Any())
            {
                _logger.LogWarning("[SPECIMEN RESOLUTION] Failed to deserialize specimen JSON as int or string array: {Json}", json);
                return new List<int>();
            }

            _logger.LogDebug("[SPECIMEN RESOLUTION] Deserialized {Count} specimen names, resolving to IDs: {Names}",
                nameArray.Count,
                string.Join(", ", nameArray));

            // Look up specimen type IDs by name
            var specimenTypes = await _context.SpecimenTypes
                .IgnoreQueryFilters()
                .Where(s => nameArray.Contains(s.Name) && s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync(cancellationToken);

            if (specimenTypes.Count != nameArray.Count)
            {
                var foundNames = specimenTypes.Select(s => s.Name).ToList();
                var missingNames = nameArray.Except(foundNames).ToList();
                _logger.LogWarning(
                    "[SPECIMEN RESOLUTION] Could not resolve all specimen names. Found: {Found}, Missing: {Missing}",
                    string.Join(", ", foundNames),
                    string.Join(", ", missingNames));
            }

            var resolvedIds = specimenTypes.Select(s => s.Id).ToList();
            _logger.LogDebug("[SPECIMEN RESOLUTION] Resolved {Count} specimen names to IDs: {Ids}",
                resolvedIds.Count,
                string.Join(", ", resolvedIds));

            return resolvedIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SPECIMEN RESOLUTION] Error resolving specimen type IDs from JSON: {Json}", json);
            return new List<int>();
        }
    }

    /// <summary>
    /// Resolves acceptable test method IDs from JSON that may contain either int array or string array (method names)
    /// </summary>
    private async Task<List<int>> ResolveAcceptableTestMethodIdsAsync(
        string json,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            // First, try to deserialize as int array (new format)
            var intArray = DeserializeIntArray(json);
            if (intArray.Any())
            {
                _logger.LogDebug("[TEST METHOD RESOLUTION] Deserialized {Count} test method IDs from case definition", intArray.Count);
                return intArray;
            }

            // If that didn't work, try to deserialize as string array (legacy format - method names)
            var nameArray = JsonSerializer.Deserialize<List<string>>(json);
            if (nameArray == null || !nameArray.Any())
            {
                _logger.LogWarning("[TEST METHOD RESOLUTION] Failed to deserialize test method JSON as int or string array: {Json}", json);
                return new List<int>();
            }

            _logger.LogDebug("[TEST METHOD RESOLUTION] Deserialized {Count} method names, resolving to IDs: {Names}",
                nameArray.Count,
                string.Join(", ", nameArray));

            // Look up test method IDs by name
            var testMethods = await _context.TestMethods
                .IgnoreQueryFilters()
                .Where(m => nameArray.Contains(m.Name) && m.IsActive)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync(cancellationToken);

            if (testMethods.Count != nameArray.Count)
            {
                var foundNames = testMethods.Select(m => m.Name).ToList();
                var missingNames = nameArray.Except(foundNames).ToList();
                _logger.LogWarning(
                    "[TEST METHOD RESOLUTION] Could not resolve all method names. Found: {Found}, Missing: {Missing}",
                    string.Join(", ", foundNames),
                    string.Join(", ", missingNames));
            }

            var resolvedIds = testMethods.Select(m => m.Id).ToList();
            _logger.LogDebug("[TEST METHOD RESOLUTION] Resolved {Count} method names to IDs: {Ids}",
                resolvedIds.Count,
                string.Join(", ", resolvedIds));

            return resolvedIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TEST METHOD RESOLUTION] Error resolving test method IDs from JSON: {Json}", json);
            return new List<int>();
        }
    }

    private List<int> DeserializeIntArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch (Exception)
        {
            // Not an int array - caller will handle string array format
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

    /// <summary>
    /// Evaluates all markers in a lab result against case definitions requiring multiple markers.
    /// This is called when no individual markers match single-marker case definitions.
    /// </summary>
    public async Task<List<CaseDefinitionMatchResult>> MatchCaseDefinitionsForLabResultAsync(
        LabResult labResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ZZTEST123 Method entry checkpoint");
        _logger.LogInformation("Method entry at {Time}", DateTime.Now);

        var results = new List<CaseDefinitionMatchResult>();

        if (labResult.Markers == null || !labResult.Markers.Any())
        {
            _logger.LogWarning("🔥 [MULTI-MARKER] No markers in lab result - returning empty");
            return results;
        }

        _logger.LogWarning(
            "🔥 [MULTI-MARKER] Evaluating {Count} markers together for LabResult {LabResultId}",
            labResult.Markers.Count,
            labResult.FriendlyId);

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
            _logger.LogDebug("[MULTI-MARKER] No active case definitions found");
            return results;
        }

        _logger.LogInformation("[MULTI-MARKER] Found {Count} active case definitions to evaluate", caseDefinitions.Count);

        foreach (var caseDefinition in caseDefinitions)
        {
            _logger.LogInformation(
                "[MULTI-MARKER] 🔍 Checking: '{CaseDefName}' (Disease: {Disease})",
                caseDefinition.Name,
                caseDefinition.Disease?.Name ?? "NULL");

            var matchResult = await EvaluateMultiMarkerCaseDefinitionAsync(
                caseDefinition,
                labResult,
                cancellationToken);

            if (matchResult != null)
            {
                _logger.LogInformation(
                    "[MULTI-MARKER] ✅ MATCHED: {CaseDefName} → {Disease}",
                    caseDefinition.Name,
                    matchResult.Disease?.Name ?? "NULL");

                results.Add(matchResult);
            }
            else
            {
                _logger.LogInformation(
                    "[MULTI-MARKER] ❌ NO MATCH: {CaseDefName}",
                    caseDefinition.Name);
            }
        }

        if (!results.Any())
        {
            _logger.LogWarning("🔥 [MULTI-MARKER] No case definitions matched - returning empty list");
            return results;
        }

        _logger.LogWarning(
            "🔥🔥🔥 [BEFORE FILTER] About to call FilterBySpecificityAsync with {Count} results",
            results.Count);

        // Filter out less-specific case definitions when multiple definitions for the same disease match
        // Example: If both "STM9 Presumptive" (serovar only) and "STM9 Confirmed" (serovar + PCR/Culture) match,
        // only keep the Confirmed one since it requires more evidence
        var filteredResults = await FilterBySpecificityAsync(results, labResult, cancellationToken);

        _logger.LogWarning(
            "🔥🔥🔥 [AFTER FILTER] FilterBySpecificityAsync returned {Count} results",
            filteredResults.Count);

        if (filteredResults.Count < results.Count)
        {
            _logger.LogWarning(
                "🔥 [MULTI-MARKER] Filtered {Original} matches to {Filtered} most specific case definition(s)",
                results.Count,
                filteredResults.Count);
        }

        return filteredResults;
    }

    /// <summary>
    /// Filters case definition matches to keep only the most specific ones when multiple definitions
    /// for the same disease match. Specificity is determined by the number of laboratory criteria
    /// (more criteria = more specific, indicating more evidence required).
    /// If multiple definitions have the same criteria count, all are kept to avoid arbitrary selection.
    /// </summary>
    /// <summary>
    /// Two-phase filtering to select the most specific case definition match:
    /// Phase 1: Within each disease, keep only matches with highest specificity score
    /// Phase 2: Across disease hierarchy, keep only the most specific (child) disease
    /// </summary>
    private async Task<List<CaseDefinitionMatchResult>> FilterBySpecificityAsync(
        List<CaseDefinitionMatchResult> matches,
        LabResult labResult,
        CancellationToken cancellationToken = default)
    {
        // CRITICAL: Warning-level log to verify this method is being called
        _logger.LogWarning(
            "🔥 [SPECIFICITY FILTER ENTRY] FilterBySpecificityAsync called with {Count} matches",
            matches.Count);

        if (matches.Count <= 1)
        {
            _logger.LogWarning("🔥 [SPECIFICITY FILTER] Only 1 match - skipping filter, returning as-is");
            return matches;
        }

        // PHASE 1: Filter within each disease by specificity score
        _logger.LogWarning(
            "🔥 [SPECIFICITY FILTER] Phase 1: Filtering {Count} matches by specificity score within each disease",
            matches.Count);

        var groupedByDisease = matches
            .GroupBy(m => m.DiseaseId)
            .ToList();

        var filteredWithinDisease = new List<CaseDefinitionMatchResult>();

        foreach (var diseaseGroup in groupedByDisease)
        {
            if (diseaseGroup.Count() == 1)
            {
                // Only one definition for this disease - keep it
                filteredWithinDisease.Add(diseaseGroup.First());
                continue;
            }

            // Multiple case definitions for the same disease
            _logger.LogInformation(
                "[SPECIFICITY FILTER] Found {Count} case definitions for disease '{Disease}' - filtering by score",
                diseaseGroup.Count(),
                diseaseGroup.First().Disease?.Name ?? "NULL");

            //  Calculate marker-based specificity scores
            var scoresById = new Dictionary<int, int>();
            foreach (var match in diseaseGroup)
            {
                var specificityScore = match.MarkerBasedSpecificityScore;
                scoresById[match.CaseDefinition!.Id] = specificityScore;
            }

            // Log all candidates with their scores
            foreach (var match in diseaseGroup)
            {
                var recalculatedScore = scoresById[match.CaseDefinition!.Id];
                _logger.LogWarning(
                    "🔥 [SPECIFICITY FILTER]   Candidate: '{Name}' - OLD Score: {OldScore}, NEW Score: {NewScore}",
                    match.CaseDefinition?.Name,
                    match.SpecificityScore,
                    recalculatedScore);
            }

            // Find the maximum specificity score
            var maxScore = scoresById.Values.Max();

            _logger.LogWarning(
                "🔥 [SPECIFICITY FILTER] Max score for disease '{Disease}': {MaxScore}",
                diseaseGroup.First().Disease?.Name,
                maxScore);

            // Keep ALL definitions with the maximum score (handle ties)
            var mostSpecific = diseaseGroup
                .Where(m => scoresById[m.CaseDefinition!.Id] == maxScore)
                .ToList();

            if (mostSpecific.Count == 1)
            {
                var winner = mostSpecific[0];
                _logger.LogWarning(
                    "?? [SPECIFICITY FILTER] ? Selected '{Name}' as most specific (Score: {Score})",
                    winner.CaseDefinition?.Name,
                    scoresById[winner.CaseDefinition!.Id]);
            }
            else
            {
                _logger.LogWarning(
                    "🔥 [SPECIFICITY FILTER] ⚠️ Multiple definitions with score {Score} remain after filtering - keeping all {Count}",
                    maxScore,
                    mostSpecific.Count);

                foreach (var match in mostSpecific)
                {
                    var tieScore = scoresById[match.CaseDefinition!.Id];
                    _logger.LogWarning(
                        "🔥 [SPECIFICITY FILTER]   Tie: '{Name}' (Score: {Score})",
                        match.CaseDefinition?.Name,
                        tieScore);
                }
            }

            filteredWithinDisease.AddRange(mostSpecific);
        }

        if (filteredWithinDisease.Count < matches.Count)
        {
            _logger.LogWarning(
                "🔥 [SPECIFICITY FILTER] Phase 1 complete: Filtered {Original} to {Filtered} matches",
                matches.Count,
                filteredWithinDisease.Count);
        }
        else
        {
            _logger.LogWarning(
                "🔥 [SPECIFICITY FILTER] Phase 1 complete: No filtering occurred (all {Count} matches kept)",
                matches.Count);
        }

        // PHASE 2: Filter by disease hierarchy - keep only most specific disease
        if (filteredWithinDisease.Count <= 1)
            return filteredWithinDisease;

        _logger.LogInformation(
            "[HIERARCHY FILTER] Phase 2: Filtering {Count} matches by disease hierarchy",
            filteredWithinDisease.Count);

        var filtered = await FilterByDiseaseHierarchyAsync(filteredWithinDisease, cancellationToken);

        if (filtered.Count < filteredWithinDisease.Count)
        {
            _logger.LogInformation(
                "[HIERARCHY FILTER] Phase 2 complete: Filtered {Original} to {Filtered} matches",
                filteredWithinDisease.Count,
                filtered.Count);
        }

        return filtered;
    }

    /// <summary>
    /// Filters matches by disease hierarchy - keeps only the most specific (child) disease
    /// when both parent and child diseases match
    /// </summary>
    private async Task<List<CaseDefinitionMatchResult>> FilterByDiseaseHierarchyAsync(
        List<CaseDefinitionMatchResult> matches,
        CancellationToken cancellationToken)
    {
        if (matches.Count <= 1)
            return matches;

        var toRemove = new HashSet<CaseDefinitionMatchResult>();

        // Compare each pair of matches
        for (int i = 0; i < matches.Count; i++)
        {
            for (int j = i + 1; j < matches.Count; j++)
            {
                var match1 = matches[i];
                var match2 = matches[j];

                if (match1.DiseaseId == null || match2.DiseaseId == null)
                    continue;

                if (match1.DiseaseId == match2.DiseaseId)
                    continue; // Same disease - already handled in Phase 1

                // Check if they're in the same family (ancestor/descendant)
                var disease1IsAncestor = await IsAncestorOfAsync(
                    match1.DiseaseId.Value,
                    match2.DiseaseId.Value,
                    cancellationToken);

                var disease2IsAncestor = await IsAncestorOfAsync(
                    match2.DiseaseId.Value,
                    match1.DiseaseId.Value,
                    cancellationToken);

                if (disease1IsAncestor)
                {
                    // match1 is parent, match2 is child - keep child (more specific)
                    toRemove.Add(match1);
                    _logger.LogInformation(
                        "[HIERARCHY FILTER] Removing '{Name1}' (parent disease: {Disease1}) in favor of '{Name2}' (child disease: {Disease2})",
                        match1.CaseDefinition?.Name,
                        match1.Disease?.Name,
                        match2.CaseDefinition?.Name,
                        match2.Disease?.Name);
                }
                else if (disease2IsAncestor)
                {
                    // match2 is parent, match1 is child - keep child
                    toRemove.Add(match2);
                    _logger.LogInformation(
                        "[HIERARCHY FILTER] Removing '{Name2}' (parent disease: {Disease2}) in favor of '{Name1}' (child disease: {Disease1})",
                        match2.CaseDefinition?.Name,
                        match2.Disease?.Name,
                        match1.CaseDefinition?.Name,
                        match1.Disease?.Name);
                }
                // else: diseases are siblings or unrelated - keep both
            }
        }

        return matches.Except(toRemove).ToList();
    }

    /// <summary>
    /// Evaluates whether all laboratory criteria in a case definition can be satisfied
    /// by the combination of markers in the lab result
    /// </summary>
    private async Task<CaseDefinitionMatchResult?> EvaluateMultiMarkerCaseDefinitionAsync(
        CaseDefinition caseDefinition,
        LabResult labResult,
        CancellationToken cancellationToken)
    {
        var laboratoryCriteria = caseDefinition.Criteria
            .Where(c => c.CriterionType == CriterionType.Laboratory)
            .ToList();

        if (!laboratoryCriteria.Any())
            return null;

        _logger.LogInformation(
            "[MULTI-MARKER] 🔍 Evaluating '{CaseDefName}' (Disease: {Disease}) - {Count} lab criteria using tree-based evaluation",
            caseDefinition.Name,
            caseDefinition.Disease?.Name ?? "NULL",
            laboratoryCriteria.Count);

        // Track which criteria were satisfied for specificity scoring
        var satisfiedCriteriaIds = new List<int>();
        var markerTracker = new Dictionary<int, List<Guid>>();

        // Use tree-based evaluator instead of flattening by GroupNumber
        var matches = await _treeEvaluator.EvaluateAsync(
            laboratoryCriteria,
            async (criterion) =>
            {
                _logger.LogWarning(
                    "?? [LAMBDA] Evaluating criterion {CritId}, markerTracker has {Count} entries",
                    criterion.Id,
                    markerTracker.Count);

                // Check if ANY marker in the lab result satisfies this criterion
                bool matched = await EvaluateMultiMarkerLaboratoryCriterion(
                    criterion,
                    labResult,
                    cancellationToken,
                    markerTracker);

                _logger.LogWarning(
                    "?? [LAMBDA] After evaluation, criterion {CritId} matched={Matched}, markerTracker now has {Count} entries",
                    criterion.Id,
                    matched,
                    markerTracker.Count);

                if (matched)
                {
                    _logger.LogDebug(
                        "[MULTI-MARKER EVAL] ✅ Criterion {Id} MATCHED: {Display}",
                        criterion.Id,
                        criterion.DisplayText);
                }
                else
                {
                    _logger.LogDebug(
                        "[MULTI-MARKER EVAL] ❌ Criterion {Id} NOT matched: {Display}",
                        criterion.Id,
                        criterion.DisplayText);
                }

                return matched;
            },
            $"MultiMarker-CaseDef={caseDefinition.Id}",
            markerTracker);

        if (!matches)
        {
            _logger.LogInformation(
                "[MULTI-MARKER] ❌ NO MATCH: {CaseDefName}",
                caseDefinition.Name);
            return null;
        }

        // Derive satisfiedCriteriaIds from markerTracker AFTER tree evaluation
        satisfiedCriteriaIds = markerTracker.Keys.ToList();
        _logger.LogWarning(
            "POST-EVAL Derived {Count} criteria: [{Ids}]",
            satisfiedCriteriaIds.Count,
            string.Join(", ", satisfiedCriteriaIds));

        // Identify completed groups (parent criteria where ALL children matched)
        var completedGroupIds = new List<int>();
        var groupParents = caseDefinition.Criteria
            .Where(c => c.ChildCriteria?.Any() == true)
            .ToList();

        foreach (var parent in groupParents)
        {
            var childIds = parent.ChildCriteria.Select(ch => ch.Id).ToList();
            var allChildrenSatisfied = childIds.All(id => satisfiedCriteriaIds.Contains(id));

            if (allChildrenSatisfied)
            {
                completedGroupIds.Add(parent.Id);
                _logger.LogDebug(
                    "[MULTI-MARKER EVAL] 🎯 Group {ParentId} COMPLETE: all {Count} children satisfied",
                    parent.Id,
                    childIds.Count);
            }
        }

        // All groups passed - this case definition matches!
        _logger.LogInformation(
            "[MULTI-MARKER] ✅ MATCH with specificity: {CaseDefName} (Satisfied: {Satisfied}, Groups: {Groups}, Score: {Score})",
            caseDefinition.Name,
            satisfiedCriteriaIds.Count,
            completedGroupIds.Count,
            (satisfiedCriteriaIds.Count * 10) + (completedGroupIds.Count * 5));

        // Diagnostic: Log marker tracking results
        var distinctPathogenCount = markerTracker.Values.SelectMany(m => m).Distinct().Count();
        _logger.LogWarning(
            "?? [MARKER TRACKING] Case Def '{Name}': {CriteriaCount} criteria tracked, {PathogenCount} distinct pathogens",
            caseDefinition.Name,
            markerTracker.Count,
            distinctPathogenCount);
        foreach (var kvp in markerTracker)
        {
            _logger.LogWarning(
                "?? [MARKER TRACKING]   Criterion {CritId}: {PathogenCount} pathogens [{Pathogens}]",
                kvp.Key,
                kvp.Value.Count,
                string.Join(", ", kvp.Value.Select(p => p.ToString().Substring(0, 8))));
        }

        return new CaseDefinitionMatchResult
        {
            CaseDefinition = caseDefinition,
            Disease = caseDefinition.Disease,
            ConfirmationStatus = caseDefinition.ConfirmationStatus,
            DiseaseId = caseDefinition.DiseaseId,
            ConfirmationStatusId = caseDefinition.ConfirmationStatusId,
            SatisfiedCriteriaIds = satisfiedCriteriaIds,
            CompletedGroupIds = completedGroupIds,
            MarkersByCriterion = markerTracker
        };
    }

    /// <summary>
    /// Checks if ANY marker in the lab result satisfies the laboratory criterion.
    /// If markerTracker is provided, records which pathogen IDs satisfied this criterion.
    /// </summary>
    private async Task<bool> EvaluateMultiMarkerLaboratoryCriterion(
        CaseDefinitionCriteria criterion,
        LabResult labResult,
        CancellationToken cancellationToken,
        Dictionary<int, List<Guid>>? markerTracker = null)
    {
        var specimenTypeId = labResult.SpecimenTypeId;

        // Check specimen type constraint (applies to all markers)
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableSpecimenTypesJson))
        {
            var acceptableSpecimenTypes = DeserializeIntArray(criterion.AcceptableSpecimenTypesJson);
            if (!specimenTypeId.HasValue || !acceptableSpecimenTypes.Contains(specimenTypeId.Value))
            {
                _logger.LogInformation(
                    "[MULTI-MARKER] ❌ Specimen type mismatch: Resolved={Resolved}, Acceptable=[{Acceptable}]",
                    specimenTypeId?.ToString() ?? "NULL",
                    string.Join(",", acceptableSpecimenTypes));
                return false;
            }
        }

        // Check if ANY marker matches the pathogen/test method/result criteria
        var matchingPathogenIds = new List<Guid>();

        foreach (var marker in labResult.Markers)
        {
            var markerMatches = await EvaluateMarkerAgainstCriterion(
                marker,
                criterion,
                cancellationToken);

            if (markerMatches)
            {
                _logger.LogWarning(
                    "DIAG MATCH: Marker Pathogen={PathogenId} MATCHED criterion {CritId}",
                    marker.PathogenId?.ToString() ?? "NULL",
                    criterion.Id);

                _logger.LogInformation(
                    "[MULTI-MARKER] ✅ Marker {MarkerId} (Pathogen={PathogenId}) matches criterion {CritId}",
                    marker.Id,
                    marker.PathogenId?.ToString() ?? "NULL",
                    criterion.Id);

                // Track by pathogen ID (marker IDs are Guid.Empty during staging)
                if (marker.PathogenId.HasValue)
                {
                    matchingPathogenIds.Add(marker.PathogenId.Value);
                }
            }
            else
            {
                _logger.LogDebug(
                    "[MULTI-MARKER] Marker {MarkerId} (Pathogen={PathogenId}, Method={MethodId}, Result={ResultId}) does NOT match criterion {CritId}",
                    marker.Id,
                    marker.PathogenId?.ToString() ?? "NULL",
                    marker.TestMethodId?.ToString() ?? "NULL",
                    marker.TestResultId?.ToString() ?? "NULL",
                    criterion.Id);
            }
        }

        // If we found matching markers, record them in the tracker
        if (matchingPathogenIds.Any())
        {
            if (markerTracker != null)
            {
                if (!markerTracker.ContainsKey(criterion.Id))
                {
                    markerTracker[criterion.Id] = new List<Guid>();
                }
                markerTracker[criterion.Id].AddRange(matchingPathogenIds);
            }
            return true;
        }

        _logger.LogInformation(
            "[MULTI-MARKER] ❌ No markers satisfy criterion {CritId}",
            criterion.Id);

        return false;
    }

    /// <summary>
    /// Evaluates a single marker against criterion constraints (pathogen, test method, result)
    /// </summary>
    private async Task<bool> EvaluateMarkerAgainstCriterion(
        LabResultMarker marker,
        CaseDefinitionCriteria criterion,
        CancellationToken cancellationToken)
    {
        var matches = new List<bool>();

        // DIAGNOSTIC: Log criterion pathogen configuration
        _logger.LogWarning(
            "DIAG EvalMarker: Criterion {CritId}, Marker Pathogen {PathogenId}, PathogenJson = '{Json}' (IsNull={IsNull}, IsWhitespace={IsWS})",
            criterion.Id,
            marker.PathogenId?.ToString() ?? "NULL",
            criterion.AcceptablePathogensJson ?? "NULL",
            criterion.AcceptablePathogensJson == null,
            string.IsNullOrWhiteSpace(criterion.AcceptablePathogensJson));

        // Evaluate Pathogen/Biomarker with disease hierarchy support
        if (!string.IsNullOrWhiteSpace(criterion.AcceptablePathogensJson))
        {
            var acceptablePathogens = await ResolveAcceptablePathogenIdsAsync(
                criterion.AcceptablePathogensJson,
                cancellationToken);

            bool pathogenMatch = false;
            if (marker.PathogenId.HasValue)
            {
                // Check with disease hierarchy awareness for progressive typing
                pathogenMatch = await IsPathogenAcceptableAsync(
                    marker.PathogenId.Value,
                    acceptablePathogens,
                    cancellationToken);
            }

            matches.Add(pathogenMatch);
        }

        // Evaluate Test Method
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableTestMethodsJson))
        {
            var acceptableTestMethods = await ResolveAcceptableTestMethodIdsAsync(
                criterion.AcceptableTestMethodsJson,
                cancellationToken);
            var testMethodMatch = marker.TestMethodId.HasValue &&
                                  acceptableTestMethods.Contains(marker.TestMethodId.Value);
            matches.Add(testMethodMatch);
        }

        // Evaluate Test Result
        if (!string.IsNullOrWhiteSpace(criterion.AcceptableResultsJson))
        {
            var acceptableResults = await ResolveAcceptableResultIdsAsync(
                criterion.AcceptableResultsJson,
                cancellationToken);
            var resultMatch = marker.TestResultId.HasValue &&
                              acceptableResults.Contains(marker.TestResultId.Value);
            matches.Add(resultMatch);
        }

        // If no criteria were specified, treat as no match
        if (!matches.Any())
            return false;

        // ALL specified criteria must match
        return matches.All(m => m);
    }

    /// <summary>
    /// Retrieves disease-specific HL7 matching configuration with parent inheritance
    /// Results are cached for 5 minutes
    /// </summary>
    private async Task<DiseaseHL7MatchingConfig?> GetDiseaseConfigAsync(
        Guid diseaseId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"DiseaseHL7Config_{diseaseId}";

        if (!_cache.TryGetValue(cacheKey, out DiseaseHL7MatchingConfig? config))
        {
            config = await _context.DiseaseHL7MatchingConfigs
                .IgnoreQueryFilters()
                .Include(c => c.PartialMatchConfirmationStatus)
                .FirstOrDefaultAsync(c => c.DiseaseId == diseaseId, cancellationToken);

            // If not found or doesn't override parent rules, check parent disease
            if (config == null || !config.OverrideParentRules)
            {
                var disease = await _context.Diseases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(d => d.Id == diseaseId, cancellationToken);

                if (disease?.ParentDiseaseId != null)
                {
                    var parentConfig = await GetDiseaseConfigAsync(disease.ParentDiseaseId.Value, cancellationToken);

                    // If child has no config, use parent's
                    if (config == null)
                    {
                        config = parentConfig;
                    }
                }
            }

            // Cache for 5 minutes (even if null to avoid repeated DB lookups)
            _cache.Set(cacheKey, config, TimeSpan.FromMinutes(5));
        }

        return config;
    }

    #region Disease Hierarchy Helpers

    /// <summary>
    /// Checks if a marker's pathogen is acceptable for a case definition criterion,
    /// considering disease hierarchy for progressive typing scenarios.
    /// </summary>
    /// <param name="markerPathogenId">The pathogen ID from the lab result marker</param>
    /// <param name="acceptablePathogenIds">The pathogen IDs acceptable by the criterion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the marker pathogen is acceptable (exact match or related via disease hierarchy)</returns>
    private async Task<bool> IsPathogenAcceptableAsync(
        Guid markerPathogenId,
        List<Guid> acceptablePathogenIds,
        CancellationToken cancellationToken)
    {
        // Quick path: exact match
        if (acceptablePathogenIds.Contains(markerPathogenId))
            return true;

        // Load pathogen with disease information (ignore soft delete filters for hierarchy checks)
        var markerPathogen = await _context.Pathogens
            .IgnoreQueryFilters()
            .Include(p => p.Disease)
            .FirstOrDefaultAsync(p => p.Id == markerPathogenId, cancellationToken);

        if (markerPathogen?.DiseaseId == null)
        {
            _logger.LogDebug(
                "[PATHOGEN HIERARCHY] Marker pathogen {PathogenId} has no disease association",
                markerPathogenId);
            return false;
        }

        // Load acceptable pathogens with disease information (ignore soft delete filters)
        var acceptablePathogens = await _context.Pathogens
            .IgnoreQueryFilters()
            .Include(p => p.Disease)
            .Where(p => acceptablePathogenIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Check if marker's pathogen disease is related to any acceptable pathogen disease
        foreach (var acceptablePathogen in acceptablePathogens)
        {
            if (acceptablePathogen.DiseaseId == null)
                continue;

            // Check if the diseases are related via hierarchy
            if (await AreDiseasesRelatedAsync(
                markerPathogen.DiseaseId.Value,
                acceptablePathogen.DiseaseId.Value,
                cancellationToken))
            {
                _logger.LogDebug(
                    "[PATHOGEN HIERARCHY] ✅ Marker pathogen '{MarkerPathogen}' (Disease: {MarkerDisease}) is related to acceptable pathogen '{AcceptablePathogen}' (Disease: {AcceptableDisease})",
                    markerPathogen.Name,
                    markerPathogen.Disease?.Name,
                    acceptablePathogen.Name,
                    acceptablePathogen.Disease?.Name);
                return true;
            }
        }

        _logger.LogDebug(
            "[PATHOGEN HIERARCHY] ❌ Marker pathogen '{MarkerPathogen}' (Disease: {MarkerDisease}) is NOT related to any acceptable pathogens",
            markerPathogen.Name,
            markerPathogen.Disease?.Name);

        return false;
    }

    /// <summary>
    /// Checks if marker disease is acceptable for a criterion's pathogen disease.
    /// Returns true if:
    /// - They are the same disease
    /// - The criterion disease is an ancestor of the marker disease (marker is more specific)
    /// 
    /// Does NOT allow sibling matching (e.g., STM9 cannot match Salmonella 135 even though both descend from Salmonella).
    /// This ensures serovar-specific case definitions only match their specific serovar markers.
    /// </summary>
    /// <param name="markerDiseaseId">Disease associated with the lab result marker</param>
    /// <param name="criterionDiseaseId">Disease associated with the case definition criterion's acceptable pathogen</param>
    private async Task<bool> AreDiseasesRelatedAsync(
        Guid markerDiseaseId,
        Guid criterionDiseaseId,
        CancellationToken cancellationToken)
    {
        // ONLY allow marker disease to be a descendant of criterion disease (more specific marker matches less specific criterion)
        // Example: STM9 marker (specific) CAN match generic Salmonella criterion
        // Example: STM9 marker (specific) CANNOT match Salmonella 135 criterion (siblings, not ancestor-descendant)
        // 
        // NOTE: We do NOT return true for exact disease match here because that would bypass the pathogen ID check.
        // The exact pathogen ID check in IsPathogenAcceptableAsync (line 1354) already handles same-disease scenarios correctly.
        if (await IsAncestorOfAsync(criterionDiseaseId, markerDiseaseId, cancellationToken))
        {
            _logger.LogDebug(
                "[PATHOGEN HIERARCHY] Marker disease {MarkerId} is a descendant of criterion disease {CriterionId} - MATCH",
                markerDiseaseId,
                criterionDiseaseId);
            return true;
        }

        _logger.LogDebug(
            "[PATHOGEN HIERARCHY] Marker disease {MarkerId} is NOT same or descendant of criterion disease {CriterionId} - NO MATCH",
            markerDiseaseId,
            criterionDiseaseId);

        return false;
    }

    /// <summary>
    /// Checks if ancestorId is an ancestor of descendantId in the disease hierarchy
    /// </summary>
    private async Task<bool> IsAncestorOfAsync(
        Guid ancestorId,
        Guid descendantId,
        CancellationToken cancellationToken)
    {
        var descendant = await _context.Diseases
            .IgnoreQueryFilters()
            .Include(d => d.ParentDisease)
            .FirstOrDefaultAsync(d => d.Id == descendantId, cancellationToken);

        if (descendant == null)
            return false;

        var current = descendant;
        while (current.ParentDiseaseId.HasValue)
        {
            if (current.ParentDiseaseId.Value == ancestorId)
                return true;

            current = await _context.Diseases
                .IgnoreQueryFilters()
                .Include(d => d.ParentDisease)
                .FirstOrDefaultAsync(d => d.Id == current.ParentDiseaseId.Value, cancellationToken);

            if (current == null)
                break;
        }

        return false;
    }

    #endregion
}

#region Result Classes

public class CaseDefinitionMatchResult
{
    public CaseDefinition? CaseDefinition { get; set; }
    public Disease? Disease { get; set; }
    public CaseStatus? ConfirmationStatus { get; set; }
    public Guid? DiseaseId { get; set; }
    public int? ConfirmationStatusId { get; set; }

    // Partial match tracking
    public List<string> MissingFields { get; set; } = new();
    public bool IsPartialMatch { get; set; }
    public int? OriginalConfirmationStatusId { get; set; }

    // Specificity tracking for filtering multiple matches
    public List<int> SatisfiedCriteriaIds { get; set; } = new();
    public List<int> CompletedGroupIds { get; set; } = new();

    /// <summary>
    /// Specificity score: higher = more specific match.
    /// Calculated as: (satisfied criteria × 10) + (completed groups × 5)
    /// </summary>
    public int SpecificityScore => (SatisfiedCriteriaIds.Count * 10) + (CompletedGroupIds.Count * 5);

    /// <summary>
    /// Maps criterion IDs to the pathogen IDs (not marker IDs) that supported them in the successful evaluation path.
    /// Pathogen IDs are used because marker IDs are Guid.Empty during staging before database commit.
    /// Used for specificity scoring: counts distinct pathogens across the winning branch.
    /// </summary>
    public Dictionary<int, List<Guid>> MarkersByCriterion { get; set; } = new();

    /// <summary>
    /// Calculates specificity score based on the number of distinct pathogens
    /// supporting the SUCCESSFUL evaluation path only (ignores failed branches).
    /// Only counts markers from criteria that are in SatisfiedCriteriaIds.
    /// </summary>
    public int MarkerBasedSpecificityScore => 
        MarkersByCriterion
            .Where(kvp => SatisfiedCriteriaIds.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value)
            .Distinct()
            .Count();
}

/// <summary>
/// Represents the result of evaluating a case definition's criteria tree,
/// capturing which evaluation paths succeeded and which markers supported them.
/// Used to determine specificity when multiple definitions match.
/// </summary>
public class EvaluationPathResult
{
    /// <summary>
    /// Overall evaluation result: true if the definition matched
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of root-level branches or groups that evaluated to true.
    /// In OR scenarios, this captures ALL successful paths, not just the first.
    /// </summary>
    public List<PathElement> SuccessfulPaths { get; set; } = new();

    /// <summary>
    /// All criteria IDs that were satisfied during evaluation
    /// </summary>
    public List<int> SatisfiedCriteriaIds { get; set; } = new();
}

/// <summary>
/// Represents a single successful evaluation path element (criterion or group)
/// with the markers that supported it.
/// </summary>
public class PathElement
{
    /// <summary>
    /// The criterion ID that succeeded (individual criterion or group parent)
    /// </summary>
    public int CriterionId { get; set; }

    /// <summary>
    /// The marker IDs (from LabResult.Markers) that satisfied this criterion/group
    /// </summary>
    public List<Guid> SupportingMarkerIds { get; set; } = new();

    /// <summary>
    /// User-friendly description of this path element for diagnostics
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// Whether this element is a group (has children) or an individual criterion
    /// </summary>
    public bool IsGroup { get; set; }
}

#endregion
