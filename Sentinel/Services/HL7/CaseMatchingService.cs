using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.HL7;
using Sentinel.Models.Lookups;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Pathogens;
using Sentinel.Services.CaseDefinitionEvaluation;
using System.Text.Json;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for matching lab results to cases and managing disease identification
    /// </summary>
    public class CaseMatchingService : ICaseMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly DefinitionEvaluator _definitionEvaluator;
        private readonly ICaseDefinitionMatchingService _caseDefinitionMatchingService;
        private readonly ILogger<CaseMatchingService> _logger;
        private readonly IAuditService _auditService;

        public CaseMatchingService(
            ApplicationDbContext context,
            DefinitionEvaluator definitionEvaluator,
            ICaseDefinitionMatchingService caseDefinitionMatchingService,
            ILogger<CaseMatchingService> logger,
            IAuditService auditService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _definitionEvaluator = definitionEvaluator ?? throw new ArgumentNullException(nameof(definitionEvaluator));
            _caseDefinitionMatchingService = caseDefinitionMatchingService ?? throw new ArgumentNullException(nameof(caseDefinitionMatchingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        public async Task<CaseMatchingResult> ProcessLabResultAsync(
            LabResult labResult,
            Patient patient,
            CancellationToken cancellationToken = default)
        {
            var result = new CaseMatchingResult { Success = true };
            var diagnosticLog = new List<string>();

            try
            {
                // CRITICAL: Reload entities in this service's context to avoid disposed-context issues
                // The labResult and patient may have been loaded in a different context scope
                var labResultId = labResult.Id;
                var patientId = patient.Id;

                labResult = await _context.LabResults
                    .Include(lr => lr.Markers)
                    .FirstOrDefaultAsync(lr => lr.Id == labResultId, cancellationToken)
                    ?? throw new InvalidOperationException($"LabResult {labResultId} not found in database");

                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
                    ?? throw new InvalidOperationException($"Patient {patientId} not found in database");

                diagnosticLog.Add($"[CASE CREATION DIAGNOSTICS]");
                diagnosticLog.Add($"LabResult: {labResult.FriendlyId}, Patient: {patient.FriendlyId}");
                diagnosticLog.Add($"Marker Count: {labResult.Markers?.Count ?? 0}");

                // Step 1: Identify all diseases from markers
                var diseaseIdentifications = await IdentifyDiseasesFromMarkersAsync(labResult, cancellationToken);

                // Store identifications in result for audit trail
                result.DiseaseIdentifications = diseaseIdentifications;

                diagnosticLog.Add($"Disease Identifications Found: {diseaseIdentifications.Count}");

                if (!diseaseIdentifications.Any())
                {
                    diagnosticLog.Add("❌ No configured diseases identified from lab result markers");

                    // Add detailed marker information
                    if (labResult.Markers != null && labResult.Markers.Any())
                    {
                        diagnosticLog.Add($"Markers present but not mapped:");
                        foreach (var marker in labResult.Markers)
                        {
                            var testCode = marker.TestCode ?? "NULL";
                            var resultValue = marker.QualitativeResultText ?? marker.QuantitativeValue?.ToString() ?? "NULL";
                            diagnosticLog.Add($"  - TestCode: '{testCode}', Result: '{resultValue}'");

                            // Skip if test code is null/empty
                            if (string.IsNullOrWhiteSpace(marker.TestCode))
                            {
                                diagnosticLog.Add($"    ❌ Test code is null or empty");
                                continue;
                            }

                            // Check if pathogen exists
                            Pathogen? pathogen = null;
                            try
                            {
                                if (_context != null && _context.Database.GetDbConnection().State != System.Data.ConnectionState.Broken)
                                {
                                    pathogen = await _context.Pathogens
                                        .Where(p => p.LOINCCode != null && p.LOINCCode == marker.TestCode && p.IsActive)
                                        .Include(p => p.Disease)
                                        .FirstOrDefaultAsync(cancellationToken);
                                }
                                else
                                {
                                    diagnosticLog.Add($"    ⚠️ Cannot query - context is null or disposed");
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                diagnosticLog.Add($"    ⚠️ Cannot query - context has been disposed");
                            }
                            catch (Exception ex)
                            {
                                diagnosticLog.Add($"    ⚠️ Error querying pathogen: {ex.Message}");
                                _logger.LogWarning(ex, "Error querying pathogen in diagnostic section");
                            }

                            if (pathogen == null)
                            {
                                diagnosticLog.Add($"    ❌ No active pathogen found with LOINC code '{marker.TestCode}'");
                            }
                            else if (pathogen.Disease == null)
                            {
                                diagnosticLog.Add($"    ℹ️ Pathogen '{pathogen.Name ?? "Unknown"}' exists but not linked to disease (will use case definition)");
                            }
                            else
                            {
                                diagnosticLog.Add($"    ✅ Pathogen '{pathogen.Name ?? "Unknown"}' → Disease '{pathogen.Disease.Name}'");
                            }
                        }
                    }
                    else
                    {
                        diagnosticLog.Add("❌ No markers found on lab result");
                    }

                    result.Warnings.Add("No configured diseases identified from lab result markers");
                    result.Warnings.Add(string.Join("\n", diagnosticLog));
                    return result;
                }

                foreach (var identification in diseaseIdentifications)
                {
                    if (identification?.Disease != null)
                    {
                        diagnosticLog.Add($"Identified Disease: {identification.Disease.Name} (Source: {identification.Source})");
                    }
                    else
                    {
                        diagnosticLog.Add($"⚠️ Disease identification with null Disease property");
                    }
                }

                _logger.LogInformation(
                    "Identified {Count} potential diseases from LabResult {LabResultId}",
                    diseaseIdentifications.Count, labResult.FriendlyId);

                // Step 2: Filter out parent diseases when more-specific child diseases are present
                // This ensures we process only the most specific disease in each family
                var diseasesToProcess = FilterMostSpecificDiseases(diseaseIdentifications);

                if (diseasesToProcess.Count < diseaseIdentifications.Count)
                {
                    var filtered = diseaseIdentifications.Except(diseasesToProcess).ToList();
                    foreach (var filteredDisease in filtered)
                    {
                        diagnosticLog.Add($"⚠️ Filtered out parent disease: {filteredDisease.Disease?.Name} (more specific child disease present)");
                    }
                    _logger.LogInformation(
                        "Filtered {FilteredCount} parent diseases, processing {ProcessCount} most-specific diseases",
                        filtered.Count,
                        diseasesToProcess.Count);
                }

                // Step 3: Process each most-specific identified disease
                bool isFirstDisease = true;
                LabResult currentLabResult = labResult;

                foreach (var identification in diseasesToProcess.Where(d => d.IsPositiveResult))
                {
                    try
                    {
                        // Defensive null check
                        if (identification?.Disease == null)
                        {
                            diagnosticLog.Add($"⚠️ Skipping identification with null Disease");
                            _logger.LogWarning("Skipping disease identification with null Disease property");
                            continue;
                        }

                        diagnosticLog.Add($"Processing disease: {identification.Disease.Name}");

                        // For multiplex results, we need to findor create the case first,
                        // then create a clone WITH the CaseId already set
                        Case existingCase;

                        if (!isFirstDisease)
                        {
                            diagnosticLog.Add($"🔄 Processing multiplex disease: {identification.Disease.Name}");
                            _logger.LogInformation(
                                "Processing multiplex disease {Disease} for LabResult {LabResultId}",
                                identification.Disease.Name,
                                labResult.FriendlyId);

                            // Find/create case for the second+ disease WITHOUT linking the original lab result
                            // We'll create a clone and link that instead
                            existingCase = await FindOrCreateCaseForMultiplexAsync(
                                patient,
                                identification.Disease,
                                labResult,
                                identification.ConfirmationStatusId,
                                cancellationToken);

                            // Now create the clone and link it to this case
                            currentLabResult = await CloneLabResultForMultiplexAsync(
                                labResult,
                                existingCase.Id,
                                identification.Disease,
                                cancellationToken);

                            diagnosticLog.Add($"✅ Created lab result clone {currentLabResult.FriendlyId} for case {existingCase.FriendlyId}");
                        }
                        else
                        {
                            // First disease: use the normal flow which will link the original lab result
                            existingCase = await FindOrCreateCaseAsync(
                                patient,
                                identification.Disease,
                                currentLabResult,
                                identification.ConfirmationStatusId,
                                cancellationToken);

                            // The original lab result is now linked to the first case
                            await _context.SaveChangesAsync(cancellationToken);
                        }

                        if (string.IsNullOrEmpty(existingCase.FriendlyId))
                        {
                            // New case created
                            diagnosticLog.Add($"✅ Created new case (ID will be assigned on save)");
                            result.CasesCreated.Add(existingCase);
                            _logger.LogInformation(
                                "Created new case {CaseId} for disease {Disease}",
                                existingCase.FriendlyId, identification.Disease.Name);
                        }
                        else
                        {
                            // Existing case linked
                            diagnosticLog.Add($"✅ Linked to existing case: {existingCase.FriendlyId}");
                            result.CasesLinked.Add(existingCase);
                            _logger.LogInformation(
                                "Linked to existing case {CaseId} for disease {Disease}",
                                existingCase.FriendlyId, identification.Disease.Name);
                        }

                        // Check if disease refinement is possible
                        var refinementResult = await EvaluateDiseaseRefinementAsync(
                            existingCase,
                            cancellationToken);

                        if (refinementResult.ShouldRefine)
                        {
                            if (refinementResult.RequiresReview)
                            {
                                result.RequiresManualReview = true;
                                result.ManualReviewReason = refinementResult.Reason;
                                await CreateReviewQueueItemAsync(
                                    existingCase,
                                    refinementResult,
                                    cancellationToken);
                            }
                            else
                            {
                                await RefineCaseDiseaseAsync(
                                    existingCase,
                                    refinementResult.NewDisease!,
                                    refinementResult.Reason ?? "Disease refinement based on case definition",
                                    cancellationToken);
                            }
                        }

                        // Extract custom field values if mapped
                        await ExtractCustomFieldValuesAsync(existingCase, currentLabResult, cancellationToken);

                        isFirstDisease = false;
                    }
                    catch (Exception ex)
                    {
                        diagnosticLog.Add($"❌ Error processing disease {identification.Disease.Name}: {ex.Message}");
                        _logger.LogError(ex,
                            "Error processing disease {Disease} for LabResult {LabResultId}",
                            identification.Disease.Name, labResult.FriendlyId);
                        result.Errors.Add($"Error processing {identification.Disease.Name}: {ex.Message}");
                        result.Success = false;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Add diagnostic log to warnings for visibility
                if (diagnosticLog.Any())
                {
                    result.Warnings.Add(string.Join("\n", diagnosticLog));
                }
            }
            catch (Exception ex)
            {
                diagnosticLog.Add($"❌ Fatal error: {ex.Message}");
                _logger.LogError(ex, "Error in ProcessLabResultAsync for LabResult {LabResultId}", labResult.FriendlyId);
                result.Errors.Add($"Processing failed: {ex.Message}");
                result.Warnings.Add(string.Join("\n", diagnosticLog));
                result.Success = false;
            }

            return result;
        }

        public async Task<List<DiseaseIdentification>> IdentifyDiseasesFromMarkersAsync(
            LabResult labResult,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "🔬 START IdentifyDiseasesFromMarkersAsync for LabResult {LabResultId} with {MarkerCount} markers",
                labResult.FriendlyId,
                labResult.Markers?.Count ?? 0);

            var identifications = new List<DiseaseIdentification>();

            // Markers should already be loaded by ProcessLabResultAsync
            if (labResult.Markers == null || !labResult.Markers.Any())
            {
                _logger.LogWarning("LabResult {LabResultId} has no markers to analyze", labResult.FriendlyId);
                return identifications;
            }

            // Load specimen type for the lab result
            int? specimenTypeId = labResult.SpecimenTypeId;

            foreach (var marker in labResult.Markers)
            {
                // Skip markers without resolved fields
                if (!marker.PathogenId.HasValue && !marker.TestMethodId.HasValue && !marker.TestResultId.HasValue)
                {
                    _logger.LogDebug(
                        "Skipping marker {MarkerId} - no resolved fields (PathogenId, TestMethodId, TestResultId all null)",
                        marker.Id);
                    continue;
                }

                // Build resolution result from marker
                var resolution = new MarkerResolutionResult
                {
                    PathogenId = marker.PathogenId,
                    SpecimenTypeId = specimenTypeId,
                    TestMethodId = marker.TestMethodId,
                    TestResultId = marker.TestResultId,
                    QuantitativeValue = marker.QuantitativeValue,
                    // Set field resolution statuses for partial matching
                    PathogenStatus = marker.PathogenId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent,
                    TestMethodStatus = marker.TestMethodId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent,
                    SpecimenTypeStatus = specimenTypeId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent,
                    TestResultStatus = marker.TestResultId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent
                };

                try
                {
                    // Use case-definition matching to identify disease
                    var caseDefinitionMatch = await _caseDefinitionMatchingService.MatchCaseDefinitionAsync(
                        resolution,
                        cancellationToken);

                    if (caseDefinitionMatch != null && caseDefinitionMatch.Disease != null)
                    {
                        var existing = identifications.FirstOrDefault(i => i.Disease.Id == caseDefinitionMatch.Disease.Id);
                        if (existing != null)
                        {
                            existing.MatchingMarkers.Add(marker);

                            // Track partial match details
                            if (caseDefinitionMatch.IsPartialMatch)
                            {
                                existing.IsPartialMatch = true;
                                existing.MissingFields.AddRange(caseDefinitionMatch.MissingFields);
                                if (caseDefinitionMatch.OriginalConfirmationStatusId.HasValue)
                                {
                                    existing.OriginalConfirmationStatusId = caseDefinitionMatch.OriginalConfirmationStatusId;
                                }
                            }
                        }
                        else
                        {
                            identifications.Add(new DiseaseIdentification
                            {
                                Disease = caseDefinitionMatch.Disease,
                                MatchingMarkers = new List<LabResultMarker> { marker },
                                Source = IdentificationSource.CaseDefinition,
                                SpecificityScore = CalculateDiseaseSpecificity(caseDefinitionMatch.Disease),
                                IsPositiveResult = true,
                                ConfirmationStatus = caseDefinitionMatch.ConfirmationStatus,
                                ConfirmationStatusId = caseDefinitionMatch.ConfirmationStatusId,
                                IsPartialMatch = caseDefinitionMatch.IsPartialMatch,
                                MissingFields = new List<string>(caseDefinitionMatch.MissingFields),
                                OriginalConfirmationStatusId = caseDefinitionMatch.OriginalConfirmationStatusId
                            });
                        }

                        _logger.LogInformation(
                            "Marker {MarkerId} matched to disease {Disease} via case definition (Confirmation: {ConfirmationStatus}){PartialMatchInfo}",
                            marker.Id,
                            caseDefinitionMatch.Disease.Name,
                            caseDefinitionMatch.ConfirmationStatus?.Name ?? "NULL",
                            caseDefinitionMatch.IsPartialMatch ? $" [PARTIAL MATCH - Missing: {string.Join(", ", caseDefinitionMatch.MissingFields)}]" : "");
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Marker {MarkerId} did not match any case definition (PathogenId={PathogenId}, SpecimenTypeId={SpecimenTypeId}, TestMethodId={TestMethodId}, TestResultId={TestResultId})",
                            marker.Id,
                            marker.PathogenId,
                            specimenTypeId,
                            marker.TestMethodId,
                            marker.TestResultId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error matching marker {MarkerId} to case definition",
                        marker.Id);
                }
            }

            _logger.LogInformation(
                "✅ Completed single-marker evaluation. Found {Count} disease identification(s). Now starting multi-marker evaluation...",
                identifications.Count);

            // MULTI-MARKER EVALUATION: Always run to detect more-specific diseases requiring multiple markers
            // This ensures child diseases (e.g., Salmonella Typhimurium) can be identified even when
            // parent diseases (e.g., generic Salmonella) already matched on individual markers
            _logger.LogInformation(
                "Running multi-marker evaluation for LabResult {LabResultId} with {Count} markers to detect specific diseases.",
                labResult.FriendlyId,
                labResult.Markers.Count);

            try
            {
                var multiMarkerMatches = await _caseDefinitionMatchingService
                    .MatchCaseDefinitionsForLabResultAsync(labResult, cancellationToken);

                foreach (var match in multiMarkerMatches)
                {
                    if (match?.Disease != null)
                    {
                        // Only add if not already identified, or if this is a more-specific disease
                        var existing = identifications.FirstOrDefault(i => i.Disease.Id == match.Disease.Id);
                        if (existing == null)
                        {
                            identifications.Add(new DiseaseIdentification
                            {
                                Disease = match.Disease,
                                MatchingMarkers = labResult.Markers.ToList(), // All markers contributed
                                Source = IdentificationSource.CaseDefinition,
                                SpecificityScore = CalculateDiseaseSpecificity(match.Disease),
                                IsPositiveResult = true,
                                ConfirmationStatus = match.ConfirmationStatus,
                                ConfirmationStatusId = match.ConfirmationStatusId
                            });

                            _logger.LogInformation(
                                "Multi-marker evaluation matched disease {Disease} (Confirmation: {ConfirmationStatus}, Specificity: {Specificity}) using {Count} markers",
                                match.Disease.Name,
                                match.ConfirmationStatus?.Name ?? "NULL",
                                CalculateDiseaseSpecificity(match.Disease),
                                labResult.Markers.Count);
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Disease {Disease} already identified, skipping duplicate",
                                match.Disease.Name);
                        }
                    }
                }

                if (!multiMarkerMatches.Any())
                {
                    _logger.LogDebug(
                        "Multi-marker evaluation found no additional matches for LabResult {LabResultId}",
                        labResult.FriendlyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during multi-marker evaluation for LabResult {LabResultId}",
                    labResult.FriendlyId);
            }

            // Sort by specificity (most specific first)
            var sortedIdentifications = identifications.OrderByDescending(i => i.SpecificityScore).ToList();

            _logger.LogInformation(
                "🏁 END IdentifyDiseasesFromMarkersAsync - Returning {Count} disease identification(s): {Diseases}",
                sortedIdentifications.Count,
                string.Join(", ", sortedIdentifications.Select(i => $"{i.Disease?.Name} (Specificity={i.SpecificityScore})")));

            return sortedIdentifications;
        }

        /// <summary>
        /// Find or create case for multiplex results WITHOUT modifying the original lab result.
        /// This is used for second+ diseases in multiplex results to avoid overwriting the original lab result's CaseId.
        /// </summary>
        private async Task<Case> FindOrCreateCaseForMultiplexAsync(
            Patient patient,
            Disease disease,
            LabResult originalLabResult,
            int? confirmationStatusId,
            CancellationToken cancellationToken = default)
        {
            // Find reinfection rule for this disease
            var reinfectionRule = await _context.DiseaseReinfectionRules
                .FirstOrDefaultAsync(r =>
                    r.DiseaseId == disease.Id &&
                    r.IsActive,
                    cancellationToken);

            // Search for existing cases (using original lab result's specimen date for matching logic)
            var existingCase = await FindExistingCaseForLabResultAsync(
                patient,
                disease,
                originalLabResult,
                reinfectionRule,
                cancellationToken);

            if (existingCase != null)
            {
                // Found existing case - don't modify the original lab result
                _logger.LogInformation(
                    "Found existing Case {CaseId} for multiplex disease {Disease}",
                    existingCase.FriendlyId, disease.Name);
                return existingCase;
            }

            // Check if manual review required before case creation
            if (reinfectionRule?.RequireConfirmationForNewCase == true)
            {
                _logger.LogInformation(
                    "Case creation requires manual review for disease {Disease}",
                    disease.Name);

                // Create pending case - we'll link the clone later
                var pendingCase = await CreateNewCaseForMultiplexAsync(patient, disease, confirmationStatusId, cancellationToken);

                await CreateReviewQueueItemAsync(
                    pendingCase,
                    new DiseaseRefinementResult
                    {
                        RequiresReview = true,
                        Reason = reinfectionRule.NotificationMessage ?? "New case requires confirmation"
                    },
                    cancellationToken);

                return pendingCase;
            }

            // Create new case - we'll link the clone later
            var newCase = await CreateNewCaseForMultiplexAsync(patient, disease, confirmationStatusId, cancellationToken);
            _logger.LogInformation(
                "Created new Case {CaseId} for multiplex disease {Disease}",
                newCase.FriendlyId, disease.Name);

            return newCase;
        }

        /// <summary>
        /// Create a new case for multiplex results WITHOUT linking a lab result yet.
        /// The lab result clone will be linked separately after creation.
        /// </summary>
        private async Task<Case> CreateNewCaseForMultiplexAsync(
            Patient patient,
            Disease disease,
            int? confirmationStatusId,
            CancellationToken cancellationToken = default)
        {
            var newCase = new Case
            {
                PatientId = patient.Id,
                DiseaseId = disease.Id,
                ConfirmationStatusId = confirmationStatusId,
                Type = CaseType.Case,
                DateOfNotification = DateTime.UtcNow
                // Note: FriendlyId will be auto-generated by ApplicationDbContext on SaveChanges
                // Note: Audit properties (Created/Modified) handled by IAuditable
            };

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync(cancellationToken);

            // Log audit entry for case creation
            await _auditService.LogChangeAsync(
                "Case",
                newCase.Id.ToString(),
                "Created",
                null,
                $"Case {newCase.FriendlyId} created from HL7 multiplex result for disease {disease.Name}",
                null, // System-generated, no user ID
                "HL7 System"
            );

            return newCase;
        }

        public async Task<Case> FindOrCreateCaseAsync(
            Patient patient,
            Disease disease,
            LabResult labResult,
            int? confirmationStatusId,
            CancellationToken cancellationToken = default)
        {
            // Find reinfection rule for this disease
            var reinfectionRule = await _context.DiseaseReinfectionRules
                .FirstOrDefaultAsync(r =>
                    r.DiseaseId == disease.Id &&
                    r.IsActive,
                    cancellationToken);

            // Search for existing cases
            var existingCase = await FindExistingCaseForLabResultAsync(
                patient,
                disease,
                labResult,
                reinfectionRule,
                cancellationToken);

            if (existingCase != null)
            {
                // Link lab result to existing case
                labResult.CaseId = existingCase.Id;
                _logger.LogInformation(
                    "Linking LabResult {LabResultId} to existing Case {CaseId}",
                    labResult.FriendlyId, existingCase.FriendlyId);
                return existingCase;
            }

            // Check if manual review required before case creation
            if (reinfectionRule?.RequireConfirmationForNewCase == true)
            {
                _logger.LogInformation(
                    "Case creation requires manual review for disease {Disease}",
                    disease.Name);

                // Create pending case with manual review flag
                var pendingCase = await CreateNewCaseAsync(patient, disease, labResult, confirmationStatusId, cancellationToken);

                await CreateReviewQueueItemAsync(
                    pendingCase,
                    new DiseaseRefinementResult
                    {
                        RequiresReview = true,
                        Reason = reinfectionRule.NotificationMessage ?? "New case requires confirmation"
                    },
                    cancellationToken);

                return pendingCase;
            }

            // Create new case
            var newCase = await CreateNewCaseAsync(patient, disease, labResult, confirmationStatusId, cancellationToken);
            _logger.LogInformation(
                "Created new Case {CaseId} for disease {Disease}",
                newCase.FriendlyId, disease.Name);

            return newCase;
        }

        private async Task<LabResult> CloneLabResultForMultiplexAsync(
            LabResult originalLabResult,
            Guid newCaseId,
            Disease targetDisease,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Cloning LabResult {OriginalId} for multiplex processing (Disease: {DiseaseName})",
                originalLabResult.FriendlyId,
                targetDisease.Name);

            // Create clone with same specimen/test data
            var clonedLabResult = new LabResult
            {
                // Link to parent
                ParentLabResultId = originalLabResult.Id,
                IsMultiplexClone = true,

                // Link to new case
                CaseId = newCaseId,

                // Copy all specimen and test data
                PatientId = originalLabResult.PatientId,
                AccessionNumber = originalLabResult.AccessionNumber,
                SpecimenCollectionDate = originalLabResult.SpecimenCollectionDate,
                SpecimenTypeId = originalLabResult.SpecimenTypeId,
                ResultDate = originalLabResult.ResultDate,
                ResultUnitsId = originalLabResult.ResultUnitsId,
                TestedDiseaseId = originalLabResult.TestedDiseaseId,
                LaboratoryId = originalLabResult.LaboratoryId,
                OrderingProviderId = originalLabResult.OrderingProviderId,
                LabInterpretation = originalLabResult.LabInterpretation,
                Notes = originalLabResult.Notes,
                IsAmended = originalLabResult.IsAmended,
                AttachmentPath = originalLabResult.AttachmentPath,
                AttachmentFileName = originalLabResult.AttachmentFileName,
                AttachmentSize = originalLabResult.AttachmentSize,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = null
                // Note: FriendlyId will be auto-generated by ApplicationDbContext on SaveChanges
            };

            _context.LabResults.Add(clonedLabResult);
            await _context.SaveChangesAsync(cancellationToken);

            // Clone only disease-relevant markers
            var originalMarkers = await _context.LabResultMarkers
                .Where(m => m.LabResultId == originalLabResult.Id)
                .ToListAsync(cancellationToken);

            var clonedMarkersCount = 0;
            foreach (var originalMarker in originalMarkers)
            {
                // Filter: only clone markers relevant to this disease family
                if (await IsMarkerRelevantToDiseaseAsync(originalMarker, targetDisease, cancellationToken))
                {
                    var clonedMarker = new LabResultMarker
                    {
                        LabResultId = clonedLabResult.Id,
                        PathogenId = originalMarker.PathogenId,
                        TestMethodId = originalMarker.TestMethodId,
                        TestResultId = originalMarker.TestResultId,
                        QualitativeResultText = originalMarker.QualitativeResultText,
                        QuantitativeValue = originalMarker.QuantitativeValue,
                        QuantitativeUnit = originalMarker.QuantitativeUnit,
                        ReferenceRangeLow = originalMarker.ReferenceRangeLow,
                        ReferenceRangeHigh = originalMarker.ReferenceRangeHigh,
                        InterpretationFlag = originalMarker.InterpretationFlag,
                        LOINCCode = originalMarker.LOINCCode,
                        Notes = originalMarker.Notes,
                        DisplayOrder = originalMarker.DisplayOrder,
                        ResultStatus = originalMarker.ResultStatus,
                        ResultFinalizedDate = originalMarker.ResultFinalizedDate,
                        TestCode = originalMarker.TestCode,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.LabResultMarkers.Add(clonedMarker);
                    clonedMarkersCount++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created clone LabResult {CloneId} from {OriginalId} with {ClonedCount}/{TotalCount} disease-relevant markers for {DiseaseName}",
                clonedLabResult.FriendlyId,
                originalLabResult.FriendlyId,
                clonedMarkersCount,
                originalMarkers.Count,
                targetDisease.Name);

            return clonedLabResult;
        }

        public async Task<DiseaseRefinementResult> EvaluateDiseaseRefinementAsync(
            Case existingCase,
            CancellationToken cancellationToken = default)
        {
            var result = new DiseaseRefinementResult();

            // Load case with all related data
            await _context.Entry(existingCase)
                .Reference(c => c.Disease)
                .LoadAsync(cancellationToken);

            await _context.Entry(existingCase)
                .Collection(c => c.LabResults)
                .Query()
                .Include(lr => lr.Markers)
                .LoadAsync(cancellationToken);

            // Get all active case definitions for disease family (parent + children)
            if (existingCase.DiseaseId == null)
                return result;

            var diseaseIds = new List<Guid> { existingCase.DiseaseId.Value };

            // Add child diseases
            var childDiseases = await _context.Diseases
                .IgnoreQueryFilters()  // Background service - bypass access control
                .Where(d => d.ParentDiseaseId == existingCase.DiseaseId)
                .ToListAsync(cancellationToken);
            diseaseIds.AddRange(childDiseases.Select(d => d.Id));

            // Add parent disease if exists
            if (existingCase.Disease?.ParentDiseaseId != null)
            {
                diseaseIds.Add(existingCase.Disease.ParentDiseaseId.Value);

                var siblings = await _context.Diseases
                    .IgnoreQueryFilters()  // Background service - bypass access control
                    .Where(d => d.ParentDiseaseId == existingCase.Disease.ParentDiseaseId)
                    .ToListAsync(cancellationToken);
                diseaseIds.AddRange(siblings.Select(d => d.Id));
            }

            var definitions = await _context.CaseDefinitions
                .Include(cd => cd.Disease)
                .Where(cd => diseaseIds.Contains(cd.DiseaseId) && cd.Status == CaseDefinitionStatus.Current)
                .ToListAsync(cancellationToken);

            // Evaluate each definition
            var matchingDefinitions = new List<(CaseDefinition definition, Disease disease, int specificity)>();

            foreach (var definition in definitions)
            {
                var evalResult = await _definitionEvaluator.EvaluateDefinitionAsync(
                    existingCase,
                    definition.Id);

                if (evalResult.IsMatch)
                {
                    var specificity = CalculateDiseaseSpecificity(definition.Disease!);
                    matchingDefinitions.Add((definition, definition.Disease!, specificity));

                    _logger.LogDebug(
                        "Case {CaseId} matches definition for {Disease} (specificity: {Specificity})",
                        existingCase.FriendlyId, definition.Disease!.Name, specificity);
                }
            }

            if (!matchingDefinitions.Any())
            {
                return result; // No refinement needed
            }

            // Select most specific disease (highest specificity score = deepest child)
            var bestMatch = matchingDefinitions.OrderByDescending(m => m.specificity).First();

            if (bestMatch.disease.Id != existingCase.DiseaseId)
            {
                result.ShouldRefine = true;
                result.NewDisease = bestMatch.disease;
                result.MatchingDefinition = bestMatch.definition;
                result.Reason = $"Case matches more specific disease: {bestMatch.disease.Name}";

                // Check if requires manual review based on case definition action
                result.RequiresReview = bestMatch.definition.CreateReviewQueueOnSuggestion || bestMatch.definition.CreateReviewQueueOnChange;

                _logger.LogInformation(
                    "Case {CaseId} should be refined from {OldDisease} to {NewDisease} (requires review: {RequiresReview})",
                    existingCase.FriendlyId, existingCase.Disease?.Name, bestMatch.disease.Name, result.RequiresReview);
            }

            return result;
        }

        #region Private Helper Methods

        private async Task<Case?> FindExistingCaseForLabResultAsync(
            Patient patient,
            Disease disease,
            LabResult labResult,
            DiseaseReinfectionRule? reinfectionRule,
            CancellationToken cancellationToken)
        {
            // PHASE 2 FIX: Enhanced disease family search for progressive typing
            // Build disease family IDs: self + parent + siblings + DIRECT children only
            var diseaseIds = new List<Guid> { disease.Id };

            // Add parent
            if (disease.ParentDiseaseId != null)
            {
                diseaseIds.Add(disease.ParentDiseaseId.Value);
            }

            // Add siblings (same parent)
            if (disease.ParentDiseaseId != null)
            {
                var siblings = await _context.Diseases
                    .IgnoreQueryFilters()  // Background service - bypass access control
                    .Where(d => d.ParentDiseaseId == disease.ParentDiseaseId && d.Id != disease.Id)
                    .Select(d => d.Id)
                    .ToListAsync(cancellationToken);
                diseaseIds.AddRange(siblings);
            }

            // CRITICAL: Add DIRECT children only (not all descendants)
            // This enables generic Salmonella to find existing STM9/STM135 cases
            // without pulling in unrelated deeper descendants or sibling subtypes
            var directChildren = await _context.Diseases
                .IgnoreQueryFilters()
                .Where(d => d.ParentDiseaseId == disease.Id)  // Direct children only
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            if (directChildren.Any())
            {
                diseaseIds.AddRange(directChildren);
                _logger.LogDebug(
                    "Expanded case search for {Disease} to include {ChildCount} direct child disease(s)",
                    disease.Name, directChildren.Count);
            }

            var openCases = await _context.Cases
                .Where(c =>
                    c.PatientId == patient.Id &&
                    c.DiseaseId.HasValue && diseaseIds.Contains(c.DiseaseId.Value) &&
                    !c.IsDeleted)
                .OrderByDescending(c => c.DateOfOnset ?? c.DateOfNotification ?? DateTime.MinValue)
                .ToListAsync(cancellationToken);

            if (!openCases.Any())
                return null;

            var mostRecentCase = openCases.First();
            var specimenDate = labResult.SpecimenCollectionDate ?? DateTime.UtcNow;
            var caseDate = mostRecentCase.DateOfOnset ?? mostRecentCase.DateOfNotification ?? DateTime.MinValue;
            var daysSinceCase = (specimenDate - caseDate).Days;

            // Apply reinfection rules
            if (reinfectionRule != null)
            {
                switch (reinfectionRule.RuleType)
                {
                    case ReinfectionRuleType.NoReinfection:
                        return mostRecentCase; // Always link to existing case

                    case ReinfectionRuleType.ChronicDisease:
                        return mostRecentCase; // Always link to existing case

                    case ReinfectionRuleType.AlwaysNewCase:
                        return null; // Always create new case

                    case ReinfectionRuleType.TimeWindow:
                        if (reinfectionRule.ReinfectionWindowDays.HasValue &&
                            daysSinceCase < reinfectionRule.ReinfectionWindowDays.Value)
                        {
                            return mostRecentCase; // Within window - link to existing
                        }
                        return null; // Outside window - create new case

                    case ReinfectionRuleType.ManualReview:
                        // Will be handled in FindOrCreateCaseAsync
                        return mostRecentCase;
                }
            }

            // Default: 30-day window for follow-up results
            if (daysSinceCase <= 30)
            {
                _logger.LogDebug(
                    "Linking to case within 30-day follow-up window ({Days} days)",
                    daysSinceCase);
                return mostRecentCase;
            }

            return null;
        }

        private async Task<Case> CreateNewCaseAsync(
            Patient patient,
            Disease disease,
            LabResult labResult,
            int? confirmationStatusId,
            CancellationToken cancellationToken)
        {
            var newCase = new Case
            {
                PatientId = patient.Id,
                DiseaseId = disease.Id,
                Type = CaseType.Case,
                DateOfOnset = labResult.SpecimenCollectionDate,
                DateOfNotification = DateTime.UtcNow,
                ConfirmationStatusId = confirmationStatusId
            };

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync(cancellationToken); // Save to generate FriendlyId

            // Log audit entry for case creation
            await _auditService.LogChangeAsync(
                "Case",
                newCase.Id.ToString(),
                "Created",
                null,
                $"Case {newCase.FriendlyId} created from HL7 lab result {labResult.FriendlyId}",
                null, // System-generated, no user ID
                "HL7 System"
            );

            // Add note about HL7 creation
            var note = new Note
            {
                CaseId = newCase.Id,
                Content = $"Case created from HL7 lab result {labResult.FriendlyId}",
                Type = NoteType.Note,
                CreatedBy = "HL7 System"
            };
            _context.Notes.Add(note);

            labResult.CaseId = newCase.Id;

            return newCase;
        }

        private async Task RefineCaseDiseaseAsync(
            Case existingCase,
            Disease newDisease,
            string reason,
            CancellationToken cancellationToken)
        {
            var oldDiseaseName = existingCase.Disease?.Name ?? "Unknown";
            existingCase.DiseaseId = newDisease.Id;

            // Add note about disease refinement
            var note = new Note
            {
                CaseId = existingCase.Id,
                Content = $"Disease refined from '{oldDiseaseName}' to '{newDisease.Name}': {reason}",
                Type = NoteType.Note,
                CreatedBy = "HL7 System"
            };
            _context.Notes.Add(note);

            _logger.LogInformation(
                "Refined Case {CaseId} from {OldDisease} to {NewDisease}",
                existingCase.FriendlyId, oldDiseaseName, newDisease.Name);

            // Note: Case definition re-evaluation should be triggered by the application
            // after this method completes using ICaseDefinitionEvaluationService
        }

        private async Task CreateReviewQueueItemAsync(
            Case case_,
            DiseaseRefinementResult refinementResult,
            CancellationToken cancellationToken)
        {
            var reviewItem = new ReviewQueue
            {
                CaseId = case_.Id,
                EntityType = ReviewEntityTypes.CaseChange,
                EntityId = 0, // Will be set when entity is created
                ChangeType = ReviewChangeTypes.New,
                ReviewStatus = ReviewStatuses.Pending,
                ChangeSnapshot = refinementResult.Reason ?? "Case requires manual review",
                Priority = ReviewPriorities.Medium
            };

            _context.ReviewQueue.Add(reviewItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created review queue item for Case {CaseId}: {Reason}",
                case_.FriendlyId, refinementResult.Reason);
        }

        private async Task ExtractCustomFieldValuesAsync(
            Case case_,
            LabResult labResult,
            CancellationToken cancellationToken)
        {
            // Get custom field mappings for this disease
            var mappings = await _context.HL7CustomFieldMappings
                .Include(m => m.CustomFieldDefinition)
                .Where(m => m.DiseaseId == case_.DiseaseId && m.IsActive)
                .ToListAsync(cancellationToken);

            if (!mappings.Any())
                return;

            foreach (var marker in labResult.Markers)
            {
                var relevantMappings = mappings.Where(m => m.HL7TestCode == marker.TestCode).ToList();

                foreach (var mapping in relevantMappings)
                {
                    string? value = null;

                    if (mapping.ExtractQualitativeResult)
                        value = marker.QualitativeResultText;
                    else if (mapping.ExtractQuantitativeResult)
                        value = marker.QuantitativeValue?.ToString();

                    if (!string.IsNullOrEmpty(value) && mapping.CustomFieldDefinition != null)
                    {
                        // Store in appropriate typed table based on field type
                        switch (mapping.CustomFieldDefinition.FieldType)
                        {
                            case CustomFieldType.Text:
                            case CustomFieldType.TextArea:
                            case CustomFieldType.Email:
                            case CustomFieldType.Phone:
                                await StoreStringCustomFieldAsync(case_.Id, mapping.CustomFieldDefinition.Id, value, cancellationToken);
                                break;

                            case CustomFieldType.Number:
                                if (decimal.TryParse(value, out var numValue))
                                {
                                    await StoreNumberCustomFieldAsync(case_.Id, mapping.CustomFieldDefinition.Id, numValue, cancellationToken);
                                }
                                break;

                            case CustomFieldType.Date:
                                if (DateTime.TryParse(value, out var dateValue))
                                {
                                    await StoreDateCustomFieldAsync(case_.Id, mapping.CustomFieldDefinition.Id, dateValue, cancellationToken);
                                }
                                break;

                            case CustomFieldType.Checkbox:
                                if (bool.TryParse(value, out var boolValue))
                                {
                                    await StoreBooleanCustomFieldAsync(case_.Id, mapping.CustomFieldDefinition.Id, boolValue, cancellationToken);
                                }
                                break;
                        }

                        _logger.LogDebug(
                            "Extracted custom field value for Case {CaseId}: {Field} = {Value}",
                            case_.FriendlyId, mapping.CustomFieldDefinition.Name, value);
                    }
                }
            }
        }

        private async Task StoreStringCustomFieldAsync(Guid caseId, int fieldDefinitionId, string value, CancellationToken cancellationToken)
        {
            var existing = await _context.CaseCustomFieldStrings
                .FirstOrDefaultAsync(f => f.CaseId == caseId && f.FieldDefinitionId == fieldDefinitionId, cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldStrings.Add(new CaseCustomFieldString
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value
                });
            }
        }

        private async Task StoreNumberCustomFieldAsync(Guid caseId, int fieldDefinitionId, decimal value, CancellationToken cancellationToken)
        {
            var existing = await _context.CaseCustomFieldNumbers
                .FirstOrDefaultAsync(f => f.CaseId == caseId && f.FieldDefinitionId == fieldDefinitionId, cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldNumbers.Add(new CaseCustomFieldNumber
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value
                });
            }
        }

        private async Task StoreDateCustomFieldAsync(Guid caseId, int fieldDefinitionId, DateTime value, CancellationToken cancellationToken)
        {
            var existing = await _context.CaseCustomFieldDates
                .FirstOrDefaultAsync(f => f.CaseId == caseId && f.FieldDefinitionId == fieldDefinitionId, cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldDates.Add(new CaseCustomFieldDate
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value
                });
            }
        }

        private async Task StoreBooleanCustomFieldAsync(Guid caseId, int fieldDefinitionId, bool value, CancellationToken cancellationToken)
        {
            var existing = await _context.CaseCustomFieldBooleans
                .FirstOrDefaultAsync(f => f.CaseId == caseId && f.FieldDefinitionId == fieldDefinitionId, cancellationToken);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldBooleans.Add(new CaseCustomFieldBoolean
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value
                });
            }
        }

        private async Task<Disease?> FindDiseaseFromCaseDefinitionAsync(
            string testCode,
            CancellationToken cancellationToken)
        {
            var definitions = await _context.CaseDefinitions
                .Include(cd => cd.Disease)
                .Include(cd => cd.Criteria)
                .Where(cd => cd.Status == CaseDefinitionStatus.Current)
                .ToListAsync(cancellationToken);

            foreach (var definition in definitions)
            {
                // Search for test code in criteria ValueJson or FieldPath
                var hasTestCode = definition.Criteria.Any(c =>
                    (c.ValueJson?.Contains(testCode, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.FieldPath?.Contains(testCode, StringComparison.OrdinalIgnoreCase) ?? false));

                if (!hasTestCode)
                    continue;

                try
                {
                    _logger.LogDebug(
                        "Found disease {Disease} via case definition for test code {TestCode}",
                        definition.Disease?.Name, testCode);
                    return definition.Disease;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error searching case definition {DefinitionId} for test code",
                        definition.Id);
                }
            }

            return null;
        }

        private int CalculateDiseaseSpecificity(Disease disease)
        {
            // Calculate depth in hierarchy (deeper = more specific)
            int depth = 0;
            var current = disease;

            while (current?.ParentDiseaseId != null)
            {
                depth++;
                current = _context.Diseases
                    .IgnoreQueryFilters()  // Background service - bypass access control
                    .FirstOrDefault(d => d.Id == current.ParentDiseaseId);
            }

            return depth;
        }

        /// <summary>
        /// Filters disease identifications to keep only the most specific diseases in each family.
        /// Removes parent diseases when their children are present.
        /// </summary>
        private List<DiseaseIdentification> FilterMostSpecificDiseases(List<DiseaseIdentification> identifications)
        {
            var result = new List<DiseaseIdentification>();

            foreach (var identification in identifications.OrderByDescending(i => i.SpecificityScore))
            {
                if (identification?.Disease == null)
                    continue;

                // Check if this disease is a parent of any already-selected disease
                bool isParentOfSelected = result.Any(r =>
                    r.Disease != null &&
                    (r.Disease.ParentDiseaseId == identification.Disease.Id ||
                     IsAncestorOf(identification.Disease.Id, r.Disease)));

                if (!isParentOfSelected)
                {
                    // Check if any already-selected disease is a parent of this one
                    // If so, remove the parent and add this more-specific child
                    result.RemoveAll(r =>
                        r.Disease != null &&
                        (identification.Disease.ParentDiseaseId == r.Disease.Id ||
                         IsAncestorOf(r.Disease.Id, identification.Disease)));

                    result.Add(identification);

                    _logger.LogDebug(
                        "Selected disease: {Disease} (Specificity: {Specificity})",
                        identification.Disease.Name,
                        identification.SpecificityScore);
                }
                else
                {
                    _logger.LogDebug(
                        "Filtered out parent disease: {Disease} (child already selected)",
                        identification.Disease.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if ancestorId is an ancestor of the given disease in the hierarchy
        /// </summary>
        private bool IsAncestorOf(Guid ancestorId, Disease disease)
        {
            var current = disease;
            while (current?.ParentDiseaseId != null)
            {
                if (current.ParentDiseaseId == ancestorId)
                    return true;

                current = _context.Diseases
                    .IgnoreQueryFilters()
                    .FirstOrDefault(d => d.Id == current.ParentDiseaseId);
            }
            return false;
        }

        /// <summary>
        /// Check if a marker is relevant to a disease by checking if the marker's pathogen
        /// belongs to the disease family (the disease itself or any of its ancestors/descendants)
        /// </summary>
        private async Task<bool> IsMarkerRelevantToDiseaseAsync(LabResultMarker marker, Disease targetDisease, CancellationToken cancellationToken)
        {
            if (marker.PathogenId == null)
                return false;

            if (targetDisease == null)
                return false;

            // Extract all values before queries to avoid EF Core translation issues
            var pathogenId = marker.PathogenId.Value;
            var targetDiseaseId = targetDisease.Id;

            // Get the pathogen - no need to include Disease navigation, just get DiseaseId
            var pathogen = await _context.Pathogens
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pathogenId, cancellationToken);

            if (pathogen?.DiseaseId == null)
                return false;

            var pathogenDiseaseId = pathogen.DiseaseId.Value;

            // Check if the marker's disease matches the target disease
            if (pathogenDiseaseId == targetDiseaseId)
                return true;

            // Check if marker's disease is an ancestor of target disease
            if (IsAncestorOf(pathogenDiseaseId, targetDisease))
                return true;

            // Check if marker's disease is a descendant of target disease
            var markerDisease = await _context.Diseases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == pathogenDiseaseId, cancellationToken);

            if (markerDisease != null && IsAncestorOf(targetDiseaseId, markerDisease))
                return true;

            return false;
        }

        #endregion
    }
}
