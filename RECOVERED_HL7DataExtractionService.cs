﻿using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Pathogens;
using Sentinel.Services.HL7.Models;

namespace Sentinel.Services.HL7;

public class HL7DataExtractionService : IHL7DataExtractionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HL7DataExtractionService> _logger;
    private readonly IDuplicateDetectionService _duplicateDetectionService;
    private readonly ICaseMatchingService _caseMatchingService;
    private readonly IHL7MarkerResolutionService _markerResolutionService;
    private readonly IHL7FieldMappingService _fieldMappingService;
    private readonly IGeocodingQueueService? _geocodingQueue;
    private readonly ICaseDefinitionMatchingService _caseDefinitionMatchingService;

    public HL7DataExtractionService(
        ApplicationDbContext context,
        ILogger<HL7DataExtractionService> logger,
        IDuplicateDetectionService duplicateDetectionService,
        ICaseMatchingService caseMatchingService,
        IHL7MarkerResolutionService markerResolutionService,
        IHL7FieldMappingService fieldMappingService,
        ICaseDefinitionMatchingService caseDefinitionMatchingService,
        IGeocodingQueueService? geocodingQueue = null)
    {
        _context = context;
        _logger = logger;
        _duplicateDetectionService = duplicateDetectionService;
        _caseMatchingService = caseMatchingService;
        _markerResolutionService = markerResolutionService;
        _fieldMappingService = fieldMappingService;
        _caseDefinitionMatchingService = caseDefinitionMatchingService;
        _geocodingQueue = geocodingQueue;
    }

    public async Task<DataExtractionResult> ExtractAndCreateEntitiesAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DataExtractionResult();
        const int maxRetries = 3;
        int retryCount = 0;

        // Check if this message was already marked as a duplicate by the parser
        if (message.IsDuplicate && message.DuplicateOfMessageId.HasValue)
        {
            _logger.LogInformation(
                "Message {MessageControlId} is marked as duplicate of {OriginalId}, skipping entity extraction",
                message.MessageControlId,
                message.DuplicateOfMessageId);

            result.Warnings.Add($"Duplicate message detected - skipping entity extraction");
            result.Warnings.Add($"This message is a duplicate of message ID: {message.DuplicateOfMessageId}");

            // Update the message status to show it was processed (but as duplicate)
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingNotes = $"Duplicate of message {message.DuplicateOfMessageId}. No new entities created.";
            await _context.SaveChangesAsync(cancellationToken);

            return result;
        }

        while (retryCount < maxRetries)
        {
            try
            {
                // 1. Extract and match/create Patient
                var patientMatchStrategy = configuration?.PatientMatchingStrategy ?? PatientMatchingStrategy.StrictMatch;
                var autoCreatePatients = configuration?.AutoCreatePatients ?? true;

                var patientResult = await FindOrCreatePatientAsync(
                    message,
                    patientMatchStrategy,
                    autoCreatePatients,
                    cancellationToken);

                if (patientResult.RequiresManualReview)
                {
                    result.RequiresManualReview = true;
                    result.ManualReviewReason = patientResult.ConflictReason;
                    result.Errors.Add($"Patient matching requires manual review: {patientResult.ConflictReason}");

                    // Update HL7Message status
                    message.Status = HL7ProcessingStatus.AwaitingManualReview;
                    message.RequiresManualReview = true;
                    message.ProcessingNotes = patientResult.ConflictReason;
                    await _context.SaveChangesAsync(cancellationToken);

                    return result;
                }

                if (patientResult.Patient == null)
                {
                    result.Errors.Add("Failed to create or find patient");
                    message.Status = HL7ProcessingStatus.ProcessingFailed;
                    message.ErrorMessage = "Patient creation failed";
                    await _context.SaveChangesAsync(cancellationToken);
                    return result;
                }

            result.Patient = patientResult.Patient;
            if (patientResult.IsNewPatient)
            {
                // Note: FriendlyId will be generated on SaveChanges
                result.Warnings.Add($"Created new patient (ID will be assigned on save)");
            }
            else
            {
                result.Warnings.Add($"Matched existing patient: {patientResult.Patient.FriendlyId ?? "ID pending"}");
            }

            // 2. Extract and match/create Organizations
            var autoCreateOrgs = configuration?.AutoCreateOrganizations ?? false;

            // Laboratory - use configuration-driven mapping when available
            var labName = await ExtractMappedValueAsync(
                message,
                configuration,
                null, // Disease not yet resolved at this stage
                "Organization",
                "LaboratoryName",
                (msg) => ExtractLaboratoryName(msg, configuration));

            result.Warnings.Add($"[LAB EXTRACTION] Extracted lab name: '{labName ?? "NULL"}'");
            result.Warnings.Add($"[LAB EXTRACTION] Auto-create orgs: {autoCreateOrgs}");

            if (!string.IsNullOrEmpty(labName))
            {
                result.Laboratory = await FindOrCreateOrganizationAsync(
                    labName,
                    "Laboratory",
                    autoCreateOrgs,
                    cancellationToken);

                if (result.Laboratory != null)
                {
                    result.Warnings.Add($"[LAB EXTRACTION] ✅ Laboratory found/created: {result.Laboratory.Name}");
                }
                else
                {
                    result.Warnings.Add($"[LAB EXTRACTION] ❌ Laboratory '{labName}' not found and auto-create is disabled");
                }
            }
            else
            {
                result.Warnings.Add($"[LAB EXTRACTION] ❌ Could not extract laboratory name from HL7 message");
            }

            // Use default lab if extraction failed
            if (result.Laboratory == null && configuration?.DefaultLaboratoryId != null)
            {
                result.Laboratory = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Id == configuration.DefaultLaboratoryId, cancellationToken);
                result.Warnings.Add("Using default laboratory from configuration");
            }
            else if (result.Laboratory == null)
            {
                result.Warnings.Add("[LAB EXTRACTION] ❌ No default laboratory configured");
                result.Errors.Add("Cannot create lab result without a laboratory. Configure a default laboratory or enable auto-create organizations.");
            }

            // Ordering Provider - use configuration-driven mapping when available
            var providerName = await ExtractMappedValueAsync(
                message,
                configuration,
                null, // Disease not yet resolved at this stage
                "Organization",
                "OrderingProviderName",
                (msg) => ExtractOrderingProviderName(msg, configuration)); // Pass configuration through lambda

            if (!string.IsNullOrEmpty(providerName))
            {
                result.OrderingProvider = await FindOrCreateOrganizationAsync(
                    providerName,
                    "Healthcare Provider",
                    autoCreateOrgs,
                    cancellationToken);
            }

            // 3. Create or Update LabResult
            if (result.Laboratory != null)
            {
                result.Warnings.Add($"[LAB RESULT] Attempting to create/update lab result...");

                try
                {
                    result.LabResult = await CreateOrUpdateLabResultAsync(
                        message,
                        result.Patient,
                        result.Laboratory,
                        result.OrderingProvider,
                        configuration,
                        cancellationToken);

                    if (result.LabResult != null)
                    {
                        result.Warnings.Add($"[LAB RESULT] ✅ Lab result created with {result.LabResult.Markers?.Count ?? 0} markers");
                    }
                    else
                    {
                        result.Warnings.Add($"[LAB RESULT] ❌ Lab result creation returned null");
                        result.Warnings.Add($"[LAB RESULT] This usually means duplicate detection blocked the message");
                        result.Warnings.Add($"[LAB RESULT] Message Control ID '{message.MessageControlId}' may have been processed before");
                        result.Errors.Add("Lab result not created - duplicate message detected or no markers extracted");
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"[LAB RESULT] ❌ Error creating lab result: {ex.Message}");
                    result.Errors.Add($"Lab result creation failed: {ex.Message}");
                    _logger.LogError(ex, "Error creating lab result for message {MessageControlId}", message.MessageControlId);
                }

                // 4. Process case matching if lab result was created/updated
                if (result.LabResult != null)
                {
                    try
                    {
                        _logger.LogInformation("Processing case matching for LabResult {LabResultId}", result.LabResult.Id);

                        var caseMatchingResult = await _caseMatchingService.ProcessLabResultAsync(
                            result.LabResult,
                            result.Patient,
                            cancellationToken);

                        if (caseMatchingResult.Success)
                        {
                            if (caseMatchingResult.CasesCreated.Any())
                            {
                                result.Warnings.Add($"Created {caseMatchingResult.CasesCreated.Count} case(s): " +
                                    string.Join(", ", caseMatchingResult.CasesCreated.Select(c => c.FriendlyId)));
                            }
                            if (caseMatchingResult.CasesLinked.Any())
                            {
                                result.Warnings.Add($"Linked to {caseMatchingResult.CasesLinked.Count} existing case(s): " +
                                    string.Join(", ", caseMatchingResult.CasesLinked.Select(c => c.FriendlyId)));
                            }
                            if (caseMatchingResult.RequiresManualReview)
                            {
                                result.RequiresManualReview = true;
                                result.Warnings.Add("Case matching requires manual review");
                            }

                            // Track partial match details from disease identifications
                            var partialMatchDetails = BuildPartialMatchDetails(caseMatchingResult);
                            if (partialMatchDetails != null)
                            {
                                message.PartialMatchDetailsJson = System.Text.Json.JsonSerializer.Serialize(partialMatchDetails);
                                var missingFieldsList = caseMatchingResult.DiseaseIdentifications
                                    .Where(id => id.IsPartialMatch)
                                    .SelectMany(id => id.MissingFields)
                                    .Distinct()
                                    .ToList();
                                result.Warnings.Add($"⚠️ PARTIAL MATCH detected - Missing fields: {string.Join(", ", missingFieldsList)}");
                            }
                        }
                        else
                        {
                            result.Warnings.Add($"Case matching completed with warnings: {string.Join("; ", caseMatchingResult.Warnings)}");
                            if (caseMatchingResult.Errors.Any())
                            {
                                result.Errors.AddRange(caseMatchingResult.Errors.Select(e => $"Case matching error: {e}"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during case matching for LabResult {LabResultId}", result.LabResult.Id);
                        result.Warnings.Add($"Case matching failed: {ex.Message}");
                        // Continue processing - don't fail the entire extraction if case matching fails
                    }
                }

                // Link HL7Message to created entities
                message.PatientId = result.Patient.Id;
                message.LaboratoryOrganizationId = result.Laboratory.Id;
                message.OrderingProviderOrganizationId = result.OrderingProvider?.Id;
                message.LabResultId = result.LabResult?.Id;
                message.Status = HL7ProcessingStatus.ProcessedSuccessfully;
                message.ProcessedAt = DateTime.UtcNow;

                // Store processing diagnostics
                if (result.Warnings.Any() || result.Errors.Any())
                {
                    var diagnostics = new List<string>();
                    if (result.Warnings.Any())
                    {
                        diagnostics.Add("WARNINGS:");
                        diagnostics.AddRange(result.Warnings.Select(w => $"  • {w}"));
                    }
                    if (result.Errors.Any())
                    {
                        diagnostics.Add("ERRORS:");
                        diagnostics.AddRange(result.Errors.Select(e => $"  • {e}"));
                    }
                    message.ProcessingNotes = string.Join("\n", diagnostics);
                }
            }
            else
            {
                result.Errors.Add("No laboratory information found and no default configured");
                message.Status = HL7ProcessingStatus.ProcessingFailed;
                message.ErrorMessage = "Laboratory not found";
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Create history entries for newly created markers (they now have IDs after SaveChanges)
            if (result.LabResult != null && result.LabResult.Markers != null)
            {
                foreach (var marker in result.LabResult.Markers)
                {
                    // Only create history if marker doesn't already have a history entry
                    var hasHistory = await _context.LabResultMarkerHistories
                        .AnyAsync(h => h.LabResultMarkerId == marker.Id, cancellationToken);

                    if (!hasHistory)
                    {
                        var history = new LabResultMarkerHistory
                        {
                            LabResultMarkerId = marker.Id,
                            HL7MessageId = message.Id,
                            ChangedAt = DateTime.UtcNow,
                            ChangeType = MarkerChangeType.Created,
                            NewQualitativeValue = marker.QualitativeResultText,
                            NewQuantitativeValue = marker.QuantitativeValue,
                            NewResultStatus = marker.ResultStatus,
                            NewAbnormalFlag = marker.InterpretationFlag,
                            ChangeReason = $"Initial result from HL7 message {message.MessageControlId}",
                            ChangedBySystem = true
                        };
                        _context.LabResultMarkerHistories.Add(history);
                    }
                }

                // Save history entries
                await _context.SaveChangesAsync(cancellationToken);
            }

            result.Success = result.Errors.Count == 0;

            return result;
        }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) 
                        when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                              (sqlEx.Number == 2601 || sqlEx.Number == 2627)) // Unique constraint violation
                    {
                        retryCount++;

                        if (retryCount >= maxRetries)
                        {
                            _logger.LogError(ex, "Failed to save patient after {RetryCount} retries due to duplicate key: {MessageControlId}", 
                                retryCount, message.MessageControlId);
                            result.Errors.Add($"Failed to create unique patient ID after {retryCount} attempts. Please try again.");
                            result.Success = false;

                            message.Status = HL7ProcessingStatus.ProcessingFailed;
                            message.ErrorMessage = "Duplicate patient ID conflict - please retry";

                            // Clear the context to avoid tracking issues
                            _context.ChangeTracker.Clear();

                            await _context.SaveChangesAsync(cancellationToken);
                            return result;
                        }

                        _logger.LogWarning("Duplicate key detected on attempt {Attempt}, retrying... Message: {MessageControlId}", 
                            retryCount, message.MessageControlId);

                        // Clear tracked entities and wait briefly before retry
                        _context.ChangeTracker.Clear();
                        await Task.Delay(100 * retryCount, cancellationToken); // Exponential backoff

                        // Loop will retry
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error extracting data from HL7 message: {MessageControlId}", message.MessageControlId);
                        result.Errors.Add($"Extraction failed: {ex.Message}");
                        result.Success = false;

                        message.Status = HL7ProcessingStatus.ProcessingFailed;
                        message.ErrorMessage = ex.Message;

                        // Clear the context to avoid tracking issues
                        _context.ChangeTracker.Clear();

                        await _context.SaveChangesAsync(cancellationToken);

                        return result;
                    }
                }

                // Should never reach here, but just in case
                result.Errors.Add("Unexpected error: exceeded retry limit without proper exception");
                result.Success = false;
                return result;
            }

    /// <summary>
    /// NEW STAGING-BASED WORKFLOW: Process HL7 message using staging pattern
    /// This method builds a staging structure first, then commits atomically
    /// </summary>
    public async Task<DataExtractionResult> ExtractAndCreateEntitiesWithStagingAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[STAGING WORKFLOW] Starting staged processing for message {MessageControlId}", 
                message.MessageControlId);

            // STEP 1: Build staging structure (no DB writes)
            var stage = await BuildStagingStructureAsync(message, configuration, cancellationToken);

            // Output staging log to console for visibility
            if (stage.Warnings.Any())
            {
                _logger.LogInformation("[STAGING LOG] === BEGIN STAGING LOG ===");
                foreach (var logLine in stage.Warnings)
                {
                    _logger.LogInformation("[STAGING LOG] {LogLine}", logLine);
                }
                _logger.LogInformation("[STAGING LOG] === END STAGING LOG ===");
            }

            // STEP 2: Validate staging decisions and check if commit is needed
            if (stage.StagedPatient == null)
            {
                var result = new DataExtractionResult
                {
                    Success = false,
                    RequiresManualReview = stage.RequiresManualReview,
                    ManualReviewReason = stage.ManualReviewReason
                };
                result.Errors.Add(stage.ManualReviewReason ?? "Failed to stage patient");

                message.Status = HL7ProcessingStatus.AwaitingManualReview;
                message.ErrorMessage = stage.ManualReviewReason;

                // Save staging diagnostics even on early exit
                if (stage.Warnings.Any())
                {
                    message.ProcessingNotes = string.Join("\n", stage.Warnings);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return result;
            }

            if (stage.StagedLaboratory == null)
            {
                var result = new DataExtractionResult
                {
                    Success = false
                };
                result.Errors.Add("No laboratory information found and no default configured");

                message.Status = HL7ProcessingStatus.ProcessingFailed;
                message.ErrorMessage = "Laboratory not found";

                // Save staging diagnostics even on early exit
                if (stage.Warnings.Any())
                {
                    message.ProcessingNotes = string.Join("\n", stage.Warnings);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return result;
            }

            // CRITICAL: Check if this is a NoSurveillance or Duplicate case
            // These should NOT create any database records
            if (stage.Decision == ProcessingDecision.NoSurveillance)
            {
                var result = new DataExtractionResult
                {
                    Success = true  // "Success" means we processed it, not that we created records
                };
                result.Warnings.AddRange(stage.Warnings);

                message.Status = HL7ProcessingStatus.ProcessedSuccessfully;
                message.ProcessedAt = DateTime.UtcNow;
                message.ProcessingNotes = string.Join("\n", stage.Warnings);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[STAGING WORKFLOW] NoSurveillance - no database records created for message {MessageControlId}", 
                    message.MessageControlId);

                return result;
            }

            if (stage.Decision == ProcessingDecision.Duplicate)
            {
                var result = new DataExtractionResult
                {
                    Success = true
                };
                result.Warnings.AddRange(stage.Warnings);

                message.Status = HL7ProcessingStatus.DuplicateDetected;
                message.ProcessedAt = DateTime.UtcNow;
                message.ProcessingNotes = string.Join("\n", stage.Warnings);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[STAGING WORKFLOW] Duplicate detected - no database records created for message {MessageControlId}", 
                    message.MessageControlId);

                return result;
            }

            if (stage.Decision == ProcessingDecision.ManualReview)
            {
                var result = new DataExtractionResult
                {
                    Success = false,
                    RequiresManualReview = true,
                    ManualReviewReason = stage.ManualReviewReason
                };
                result.Warnings.AddRange(stage.Warnings);
                result.Errors.AddRange(stage.Errors);

                message.Status = HL7ProcessingStatus.AwaitingManualReview;
                message.ErrorMessage = stage.ManualReviewReason;
                message.ProcessingNotes = string.Join("\n", stage.Warnings);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[STAGING WORKFLOW] Manual review required - no database records created for message {MessageControlId}", 
                    message.MessageControlId);

                return result;
            }

            // STEP 3: Check if configuration is in test mode
            if (configuration?.IsTestMode == true)
            {
                _logger.LogInformation("[STAGING WORKFLOW] Configuration is in TEST MODE - skipping commit");

                var testModeResult = new DataExtractionResult
                {
                    Success = true
                };

                testModeResult.Warnings.AddRange(stage.Warnings);
                testModeResult.Warnings.Add("⚠️ TEST MODE: No data was committed to the database");
                testModeResult.Warnings.Add("📋 STAGED DATA PREVIEW:");

                if (stage.StagedPatient != null)
                {
                    testModeResult.Warnings.Add($"  Patient: {stage.StagedPatient.FirstName} {stage.StagedPatient.LastName} (DOB: {stage.StagedPatient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"})");
                    testModeResult.Warnings.Add($"    - MRN: {stage.StagedPatient.MRN ?? "N/A"}");
                    testModeResult.Warnings.Add($"    - Status: {(stage.StagedPatient.IsNew ? "NEW" : "EXISTING")}");
                }

                if (stage.StagedLaboratory != null)
                {
                    testModeResult.Warnings.Add($"  Laboratory: {stage.StagedLaboratory.Name}");
                    testModeResult.Warnings.Add($"    - Status: {(stage.StagedLaboratory.IsNew ? "NEW" : "EXISTING")}");
                }

                if (stage.StagedLabResult != null)
                {
                    testModeResult.Warnings.Add($"  Lab Result: Accession #{stage.StagedLabResult.AccessionNumber ?? "N/A"}");
                    testModeResult.Warnings.Add($"    - Specimen Date: {stage.StagedLabResult.SpecimenCollectionDate?.ToString("yyyy-MM-dd") ?? "N/A"}");
                    testModeResult.Warnings.Add($"    - Result Date: {stage.StagedLabResult.ResultDate?.ToString("yyyy-MM-dd") ?? "N/A"}");
                    testModeResult.Warnings.Add($"    - Markers: {stage.StagedLabResult.Markers?.Count ?? 0}");

                    foreach (var marker in stage.StagedLabResult.Markers ?? new List<StagedMarker>())
                    {
                        testModeResult.Warnings.Add($"      • {marker.TestName ?? marker.TestCode}: {marker.QualitativeResult ?? marker.QuantitativeValue?.ToString() ?? "N/A"}");
                    }
                }

                if (stage.DiseaseMatches.Any())
                {
                    testModeResult.Warnings.Add($"  Diseases Identified: {string.Join(", ", stage.DiseaseMatches.Select(d => d.Disease.Name))}");
                }

                message.Status = HL7ProcessingStatus.AwaitingManualReview;
                message.ProcessingNotes = string.Join("\n", testModeResult.Warnings);
                message.RequiresManualReview = true;

                await _context.SaveChangesAsync(cancellationToken);

                return testModeResult;
            }

            // STEP 4: Commit staged entities within transaction (ONLY if not in test mode)
            // Only reached if Decision = CreateNewCase or LinkToExistingCase
            var commitResult = await CommitStagedEntitiesToDatabaseAsync(stage, message, cancellationToken);

            // STEP 5: Update HL7 message status
            if (commitResult.Success)
            {
                message.PatientId = commitResult.Patient?.Id;
                message.LaboratoryOrganizationId = commitResult.Laboratory?.Id;
                message.OrderingProviderOrganizationId = commitResult.OrderingProvider?.Id;
                message.LabResultId = commitResult.LabResult?.Id;
                message.CaseId = commitResult.Cases.FirstOrDefault()?.Id;
                message.Status = commitResult.RequiresManualReview 
                    ? HL7ProcessingStatus.AwaitingManualReview 
                    : HL7ProcessingStatus.ProcessedSuccessfully;
                message.ProcessedAt = DateTime.UtcNow;

                // Store ALL processing diagnostics (staging + commit logs are in commitResult.Warnings)
                if (commitResult.Warnings.Any() || commitResult.Errors.Any())
                {
                    // The commitResult.Warnings already contains both staging logs and commit logs
                    // because we added stage.Warnings to the commitLog, and commitLog was added to result.Warnings
                    message.ProcessingNotes = string.Join("\n", commitResult.Warnings);

                    // Add errors section if there are any
                    if (commitResult.Errors.Any())
                    {
                        message.ProcessingNotes += "\n\nERRORS:\n" + string.Join("\n", commitResult.Errors.Select(e => $"  • {e}"));
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                message.Status = HL7ProcessingStatus.ProcessingFailed;
                message.ErrorMessage = string.Join("; ", commitResult.Errors);

                // Save diagnostics even on failure
                if (commitResult.Warnings.Any())
                {
                    message.ProcessingNotes = string.Join("\n", commitResult.Warnings);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("[STAGING WORKFLOW] Completed with status {Status}", message.Status);

            return commitResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING WORKFLOW] Error processing message {MessageControlId}", message.MessageControlId);

            var result = new DataExtractionResult
            {
                Success = false
            };
            result.Errors.Add($"Staging workflow failed: {ex.Message}");

            message.Status = HL7ProcessingStatus.ProcessingFailed;
            message.ErrorMessage = ex.Message;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "[STAGING WORKFLOW] Failed to save error status");
            }

            return result;
        }
    }

    public async Task<PatientMatchResult> FindOrCreatePatientAsync(
        HL7Message message,
        PatientMatchingStrategy strategy,
        bool autoCreate,
        CancellationToken cancellationToken = default)
    {
        var result = new PatientMatchResult();

        // Extract patient data from PID segment
        var pidSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "PID");
        if (pidSegment == null)
        {
            result.RequiresManualReview = true;
            result.ConflictReason = "No PID segment found in message";
            return result;
        }

        var patientData = ParsePIDSegment(pidSegment.RawSegment);

        // Try to match based on strategy
        Patient? matchedPatient = null;
        List<Patient> allMatches = new();

        switch (strategy)
        {
            case PatientMatchingStrategy.IdentifierOnly:
                matchedPatient = await MatchByIdentifierAsync(patientData, cancellationToken);
                result.MatchMethod = "IdentifierOnly";
                break;

            case PatientMatchingStrategy.StrictMatch:
                var strictMatches = await MatchByDemographicsAsync(patientData, strict: true, cancellationToken);
                if (strictMatches.Count == 1)
                {
                    matchedPatient = strictMatches[0];
                    result.MatchMethod = "StrictMatch";
                }
                else if (strictMatches.Count > 1)
                {
                    result.ConflictingPatients = strictMatches;
                }
                break;

            case PatientMatchingStrategy.FuzzyMatch:
                var fuzzyMatches = await MatchByDemographicsAsync(patientData, strict: false, cancellationToken);
                if (fuzzyMatches.Count == 1)
                {
                    matchedPatient = fuzzyMatches[0];
                    result.MatchMethod = "FuzzyMatch";
                }
                else if (fuzzyMatches.Count > 1)
                {
                    result.ConflictingPatients = fuzzyMatches;
                }
                break;

            case PatientMatchingStrategy.ManualReviewRequired:
                result.RequiresManualReview = true;
                result.MatchMethod = "ManualReviewRequired";
                result.ConflictReason = "Configuration requires manual review for all patients";
                return result;
        }

        // Check for conflicts: both MRN and demographics match different patients
        if (matchedPatient != null && !string.IsNullOrEmpty(patientData.MRN))
        {
            var mrnMatch = await MatchByIdentifierAsync(patientData, cancellationToken);
            if (mrnMatch != null && mrnMatch.Id != matchedPatient.Id)
            {
                // CONFLICT: MRN points to different patient than demographics
                result.RequiresManualReview = true;
                result.ConflictReason = $"MRN {patientData.MRN} belongs to different patient than demographics match. Possible duplicate in system.";
                result.ConflictingPatients = new List<Patient> { matchedPatient, mrnMatch };
                return result;
            }
        }

        if (matchedPatient != null)
        {
            // Update sex if missing and HL7 provides it
            if (!matchedPatient.SexAtBirthId.HasValue && !string.IsNullOrEmpty(patientData.Sex))
            {
                var sexCode = patientData.Sex.Trim().ToUpper();
                _logger.LogInformation("[SEX MAPPING] Existing patient missing sex, attempting to populate from HL7: '{RawSex}'", 
                    patientData.Sex);

                // Try HL7 code mapping
                var sexName = sexCode switch
                {
                    "M" => "Male",
                    "F" => "Female",
                    "O" => "Other",
                    "U" => "Unknown",
                    "A" => "Ambiguous",
                    "N" => "Not Applicable",
                    _ => null
                };

                if (sexName != null)
                {
                    var sexAtBirth = await _context.SexAtBirths
                        .FirstOrDefaultAsync(s => s.Name.ToLower() == sexName.ToLower(), cancellationToken);

                    if (sexAtBirth != null)
                    {
                        matchedPatient.SexAtBirthId = sexAtBirth.Id;
                        _logger.LogInformation("[SEX MAPPING] ✅ Updated existing patient's sex to '{DBSex}' (ID: {SexId})", 
                            sexAtBirth.Name, sexAtBirth.Id);
                    }
                    else
                    {
                        _logger.LogWarning("[SEX MAPPING] ❌ Failed to find '{MappedName}' in database", sexName);
                    }
                }
            }

            result.Patient = matchedPatient;
            result.IsNewPatient = false;
            return result;
        }

        // No match found - create new if allowed
        if (autoCreate && !result.RequiresManualReview)
        {
            // VALIDATION: Ensure minimum required patient data is present
            // Require at least 2 out of 3: FirstName, LastName, or DateOfBirth
            int fieldsPresent = 0;
            var missingFields = new List<string>();

            if (!string.IsNullOrWhiteSpace(patientData.FirstName))
                fieldsPresent++;
            else
                missingFields.Add("FirstName");

            if (!string.IsNullOrWhiteSpace(patientData.LastName))
                fieldsPresent++;
            else
                missingFields.Add("LastName");

            if (patientData.DateOfBirth.HasValue)
                fieldsPresent++;
            else
                missingFields.Add("DateOfBirth");

            if (fieldsPresent < 2)
            {
                result.RequiresManualReview = true;
                result.ConflictReason = $"Cannot create patient: At least 2 of 3 fields (FirstName, LastName, DateOfBirth) are required. Missing: {string.Join(", ", missingFields)}";
                _logger.LogWarning(
                    "Cannot create patient from HL7 message - insufficient demographic data. " +
                    "FirstName='{FirstName}', LastName='{LastName}', DateOfBirth='{DateOfBirth}'. " +
                    "At least 2 of these 3 fields are required.",
                    patientData.FirstName ?? "NULL", 
                    patientData.LastName ?? "NULL", 
                    patientData.DateOfBirth?.ToString("yyyy-MM-dd") ?? "NULL");
                return result;
            }

            var newPatient = new Patient
            {
                GivenName = patientData.FirstName ?? "",
                FamilyName = patientData.LastName ?? "",
                DateOfBirth = patientData.DateOfBirth,
                AddressLine = patientData.Address,
                City = patientData.City,
                PostalCode = patientData.Zip,
                HomePhone = patientData.Phone
            };

            // Map sex - supports both HL7 codes (M, F, O, U) and full names (Male, Female, Other, Unknown)
            if (!string.IsNullOrEmpty(patientData.Sex))
            {
                var sexCode = patientData.Sex.Trim().ToUpper();
                _logger.LogInformation("[SEX MAPPING] Raw sex value from HL7: '{RawSex}', Normalized: '{SexCode}'", 
                    patientData.Sex, sexCode);

                // Try exact match first
                var sexAtBirth = await _context.SexAtBirths
                    .FirstOrDefaultAsync(s => s.Name.ToUpper() == sexCode, cancellationToken);

                if (sexAtBirth != null)
                {
                    _logger.LogInformation("[SEX MAPPING] Exact match found: {SexName} (ID: {SexId})", 
                        sexAtBirth.Name, sexAtBirth.Id);
                }

                // If no exact match, try mapping HL7 codes to full names
                if (sexAtBirth == null)
                {
                    var sexName = sexCode switch
                    {
                        "M" => "Male",
                        "F" => "Female",
                        "O" => "Other",
                        "U" => "Unknown",
                        "A" => "Ambiguous",
                        "N" => "Not Applicable",
                        _ => null
                    };

                    _logger.LogInformation("[SEX MAPPING] Attempting HL7 code mapping: '{Code}' -> '{MappedName}'", 
                        sexCode, sexName ?? "NULL");

                    if (sexName != null)
                    {
                        sexAtBirth = await _context.SexAtBirths
                            .FirstOrDefaultAsync(s => s.Name.ToLower() == sexName.ToLower(), cancellationToken);

                        if (sexAtBirth != null)
                        {
                            _logger.LogInformation("[SEX MAPPING] Code mapping match found: {SexName} (ID: {SexId})", 
                                sexAtBirth.Name, sexAtBirth.Id);
                        }
                        else
                        {
                            _logger.LogWarning("[SEX MAPPING] No match found for mapped name '{MappedName}'", sexName);
                        }
                    }
                }

                // If still no match, try starts-with for flexibility
                if (sexAtBirth == null && sexCode.Length > 0)
                {
                    var allSexOptions = await _context.SexAtBirths
                        .Where(s => s.IsActive)
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation("[SEX MAPPING] Available sex options in DB: {SexOptions}", 
                        string.Join(", ", allSexOptions.Select(s => $"'{s.Name}'")));

                    sexAtBirth = allSexOptions.FirstOrDefault(s => 
                        s.Name.StartsWith(sexCode, StringComparison.OrdinalIgnoreCase));

                    if (sexAtBirth != null)
                    {
                        _logger.LogInformation("[SEX MAPPING] Starts-with match found: {SexName} (ID: {SexId})", 
                            sexAtBirth.Name, sexAtBirth.Id);
                    }
                }

                if (sexAtBirth != null)
                {
                    newPatient.SexAtBirthId = sexAtBirth.Id;
                    _logger.LogInformation("[SEX MAPPING] ✅ Successfully mapped sex '{HL7Sex}' to '{DBSex}' (ID: {SexId})", 
                        patientData.Sex, sexAtBirth.Name, sexAtBirth.Id);
                }
                else
                {
                    _logger.LogWarning("[SEX MAPPING] ❌ Failed to map sex value '{HL7Sex}' to any database entry", 
                        patientData.Sex);
                }
            }

            // Map state
            if (!string.IsNullOrEmpty(patientData.State))
            {
                var state = await _context.States
                    .FirstOrDefaultAsync(s => s.Code == patientData.State || s.Name == patientData.State, cancellationToken);
                if (state != null)
                {
                    newPatient.StateId = state.Id;
                }
            }

            _context.Patients.Add(newPatient);
            // FriendlyId will be auto-generated on SaveChanges

            result.Patient = newPatient;
            result.IsNewPatient = true;
            result.MatchMethod = "New Patient";

            return result;
        }

        // Can't create and no match found
        result.RequiresManualReview = true;
        result.ConflictReason = "No matching patient found and auto-create is disabled";
        return result;
    }

    public async Task<Organization?> FindOrCreateOrganizationAsync(
        string organizationName,
        string organizationTypeName,
        bool autoCreate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
            return null;

        // Normalize name for matching
        var normalizedName = NormalizeOrganizationName(organizationName);

        // Get organization type ID
        var orgType = await _context.OrganizationTypes
            .FirstOrDefaultAsync(ot => ot.Name == organizationTypeName, cancellationToken);

        if (orgType == null)
        {
            _logger.LogWarning("Organization type '{TypeName}' not found", organizationTypeName);
            return null;
        }

        // Try exact match first
        var existing = await _context.Organizations
            .FirstOrDefaultAsync(o =>
                o.Name == organizationName &&
                o.OrganizationTypeId == orgType.Id,
                cancellationToken);

        if (existing != null)
            return existing;

        // Try fuzzy match
        var allOrgs = await _context.Organizations
            .Where(o => o.OrganizationTypeId == orgType.Id)
            .ToListAsync(cancellationToken);

        foreach (var org in allOrgs)
        {
            var orgNormalized = NormalizeOrganizationName(org.Name);
            if (orgNormalized == normalizedName)
            {
                return org;
            }
        }

        // No match - create if allowed
        if (autoCreate)
        {
            var newOrg = new Organization
            {
                Name = organizationName,
                OrganizationTypeId = orgType.Id
            };

            _context.Organizations.Add(newOrg);
            // Will be saved by caller

            _logger.LogInformation("Created new organization: {Name} ({Type})",
                organizationName, organizationTypeName);

            return newOrg;
        }

        return null;
    }

    public async Task<LabResult?> CreateOrUpdateLabResultAsync(
        HL7Message message,
        Patient patient,
        Organization laboratory,
        Organization? orderingProvider,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError("[🔍 ENTRY] CreateOrUpdateLabResultAsync called for MessageControlId={MsgId}", message.MessageControlId);

        // Check for duplicate message first
        var duplicateCheck = await _duplicateDetectionService.CheckForDuplicateAsync(
            message,
            configuration: null,
            cancellationToken);

        _logger.LogError("[🔍 DUPLICATE CHECK] IsDuplicate={IsDup}", duplicateCheck.IsDuplicate);

        if (duplicateCheck.IsDuplicate)
        {
            _logger.LogInformation("Message {MessageControlId} is a duplicate of {OriginalMessageId}, skipping lab result creation",
                message.MessageControlId, duplicateCheck.OriginalMessageId);

            _logger.LogError("[🔍 PATH] Taking DUPLICATE path - returning existing lab result");

            // Try to find the lab result from the original message
            var originalMessage = await _context.HL7Messages
                .FirstOrDefaultAsync(m => m.Id == duplicateCheck.OriginalMessageId, cancellationToken);

            if (originalMessage?.LabResultId != null)
            {
                var existingLabResultFromDuplicate = await _context.LabResults
                    .Include(lr => lr.Markers)
                    .FirstOrDefaultAsync(lr => lr.Id == originalMessage.LabResultId, cancellationToken);

                return existingLabResultFromDuplicate; // Return existing lab result so case matching can still run
            }

            return null; // Original message didn't create a lab result either
        }

        var accessionNumber = ExtractAccessionNumber(message, configuration);
        var specimenDate = ExtractSpecimenDate(message, configuration);

        _logger.LogError("[🔍 ACCESSION] AccessionNumber={Acc}", accessionNumber ?? "NULL");

        // Extract specimen type and test method for this lab result (applies to all markers)
        var (specimenCode, specimenText, specimenSystem) = ExtractSpecimenType(message, configuration);
        var (testMethodCode, testMethodText, testMethodSystem) = ExtractTestMethod(message, configuration);

        // Try to find existing LabResult by accession number
        LabResult? existingLabResult = null;
        if (!string.IsNullOrEmpty(accessionNumber))
        {
            existingLabResult = await _context.LabResults
                .Include(lr => lr.Markers)
                .FirstOrDefaultAsync(lr =>
                    lr.AccessionNumber == accessionNumber &&
                    lr.LaboratoryId == laboratory.Id &&
                    !lr.IsDeleted,
                    cancellationToken);
        }

        _logger.LogError("[🔍 EXISTING LAB RESULT] Found={Found}", existingLabResult != null);

        if (existingLabResult != null)
        {
            _logger.LogError("[🔍 PATH] Taking EXISTING lab result path - will append/update markers");

            // APPEND new markers to existing LabResult
            _logger.LogInformation("Found existing LabResult {FriendlyId} for accession {Accession}, appending markers",
                existingLabResult.FriendlyId, accessionNumber);

            var newMarkers = ExtractMarkersFromOBX(message);
            int addedCount = 0;
            int updatedCount = 0;

            foreach (var markerData in newMarkers)
            {
                var existingMarker = existingLabResult.Markers
                    .FirstOrDefault(m => m.TestCode == markerData.TestCode);

                if (existingMarker == null)
                {
                    // ADD new marker - use centralized resolution
                    var newMarker = await CreateLabResultMarkerAsync(
                        markerData,
                        specimenCode,
                        specimenText,
                        specimenSystem,
                        testMethodCode,
                        testMethodText,
                        testMethodSystem,
                        cancellationToken);
                    existingLabResult.Markers.Add(newMarker);
                    addedCount++;

                    // Note: History will be created by caller after SaveChanges
                }
                else
                {
                    // UPDATE existing marker if result changed
                    bool changed = false;
                    var changeType = MarkerChangeType.Updated;

                    var history = new LabResultMarkerHistory
                    {
                        LabResultMarkerId = existingMarker.Id,
                        HL7MessageId = message.Id,
                        ChangedAt = DateTime.UtcNow,
                        PreviousQualitativeValue = existingMarker.QualitativeResultText,
                        PreviousQuantitativeValue = existingMarker.QuantitativeValue,
                        PreviousResultStatus = existingMarker.ResultStatus,
                        PreviousAbnormalFlag = existingMarker.InterpretationFlag
                    };

                    if (existingMarker.QualitativeResultText != markerData.QualitativeResult)
                    {
                        existingMarker.QualitativeResultText = markerData.QualitativeResult;
                        changed = true;
                    }

                    if (existingMarker.QuantitativeValue != markerData.QuantitativeValue)
                    {
                        existingMarker.QuantitativeValue = markerData.QuantitativeValue;
                        changed = true;
                    }

                    var newStatus = markerData.ResultStatus ?? "F";
                    if (existingMarker.ResultStatus != newStatus)
                    {
                        // Determine change type based on status
                        if (newStatus == "C")
                            changeType = MarkerChangeType.Corrected;
                        else if (newStatus == "F" && existingMarker.ResultStatus == "P")
                            changeType = MarkerChangeType.Finalized;

                        existingMarker.ResultStatus = newStatus;
                        changed = true;

                        // Set finalized date if moving to Final
                        if (newStatus == "F" && existingMarker.ResultFinalizedDate == null)
                        {
                            existingMarker.ResultFinalizedDate = DateTime.UtcNow;
                        }
                    }

                    if (existingMarker.InterpretationFlag != markerData.AbnormalFlag)
                    {
                        existingMarker.InterpretationFlag = markerData.AbnormalFlag;
                        changed = true;
                    }

                    if (changed)
                    {
                        history.ChangeType = changeType;
                        history.NewQualitativeValue = existingMarker.QualitativeResultText;
                        history.NewQuantitativeValue = existingMarker.QuantitativeValue;
                        history.NewResultStatus = existingMarker.ResultStatus;
                        history.NewAbnormalFlag = existingMarker.InterpretationFlag;
                        history.ChangeReason = $"Result updated from HL7 message {message.MessageControlId} - {changeType}";
                        history.ChangedBySystem = true;

                        _context.LabResultMarkerHistories.Add(history);
                        existingMarker.ModifiedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
            }

            existingLabResult.ModifiedAt = DateTime.UtcNow;
            existingLabResult.Notes = $"{existingLabResult.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Updated from HL7: {addedCount} markers added, {updatedCount} markers updated".Trim();

            return existingLabResult;
        }
        else
        {
            _logger.LogError("[🔍 PATH] Taking NEW lab result path - creating fresh lab result");

            // CREATE new LabResult
            var newLabResult = new LabResult
            {
                PatientId = patient.Id,
                LaboratoryId = laboratory.Id,
                OrderingProviderId = orderingProvider?.Id,
                AccessionNumber = accessionNumber,
                SpecimenCollectionDate = specimenDate,
                ResultDate = specimenDate,
                Notes = $"Created from HL7 message {message.MessageControlId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
            };

            // Resolve specimen type for the lab result
            if (!string.IsNullOrWhiteSpace(specimenCode) || !string.IsNullOrWhiteSpace(specimenText))
            {
                newLabResult.SpecimenTypeId = await _markerResolutionService.ResolveMarkerFieldsAsync(
                    testCode: null,
                    testName: null,
                    qualitativeResult: null,
                    quantitativeValue: null,
                    abnormalFlag: null,
                    specimenCode: specimenCode,
                    specimenText: specimenText,
                    specimenCodingSystem: specimenSystem,
                    testMethodCode: null,
                    testMethodText: null,
                    testMethodCodingSystem: null,
                    enableTextSearchFallback: true,
                    cancellationToken: cancellationToken)
                    .ContinueWith(t => t.Result.SpecimenTypeId, cancellationToken);
            }

            // Add all markers from OBX segments
            var markerDataList = ExtractMarkersFromOBX(message);

            _logger.LogError("[🔍 LAB RESULT DEBUG] Creating NEW lab result with {Count} markers from OBX segments", markerDataList.Count);

            foreach (var markerData in markerDataList)
            {
                _logger.LogError("[🔍 LAB RESULT DEBUG] Processing marker: TestCode='{TC}'", markerData.TestCode ?? "NULL");

                var marker = await CreateLabResultMarkerAsync(
                    markerData,
                    specimenCode,
                    specimenText,
                    specimenSystem,
                    testMethodCode,
                    testMethodText,
                    testMethodSystem,
                    cancellationToken);
                newLabResult.Markers.Add(marker);
                // Note: History will be created by caller after SaveChanges
            }

            _context.LabResults.Add(newLabResult);
            // FriendlyId will be auto-generated on SaveChanges

            _logger.LogInformation("Created new LabResult for accession {Accession} with {Count} markers",
                accessionNumber, markerDataList.Count);

            return newLabResult;
        }
    }

    #region Private Helper Methods

    private PatientData ParsePIDSegment(string pidSegment, HL7Configuration? configuration = null)
    {
        var fields = pidSegment.Split('|');
        var data = new PatientData();

        // Helper to extract from configured mapping or fallback to standard field
        string? GetConfiguredOrDefault(string targetProperty, int defaultFieldIndex, int componentIndex = -1)
        {
            // Check user configuration first
            if (configuration != null)
            {
                var mapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive && 
                                        m.SegmentType == "PID" && 
                                        m.TargetProperty == targetProperty);

                if (mapping != null)
                {
                    var pathParts = mapping.FieldPath.Split('-');
                    if (pathParts.Length == 2 && int.TryParse(pathParts[1].Split('.')[0], out int fieldNum))
                    {
                        int fieldIdx = fieldNum - 1;
                        if (fields.Length > fieldIdx && !string.IsNullOrWhiteSpace(fields[fieldIdx]))
                        {
                            var fieldValue = fields[fieldIdx];

                            // Handle component if specified in path (e.g., PID-5.1)
                            if (pathParts[1].Contains('.') && pathParts[1].Split('.').Length > 1)
                            {
                                if (int.TryParse(pathParts[1].Split('.')[1], out int compIdx))
                                {
                                    var components = fieldValue.Split('^');
                                    if (components.Length > compIdx - 1)
                                    {
                                        _logger.LogDebug("[PATIENT] Extracted {Property} from configured mapping {Path}: '{Value}'",
                                            targetProperty, mapping.FieldPath, components[compIdx - 1]);
                                        return components[compIdx - 1];
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogDebug("[PATIENT] Extracted {Property} from configured mapping {Path}: '{Value}'",
                                    targetProperty, mapping.FieldPath, fieldValue);
                                return fieldValue;
                            }
                        }
                    }
                }
            }

            // Fallback to standard field location
            if (fields.Length > defaultFieldIndex && !string.IsNullOrWhiteSpace(fields[defaultFieldIndex]))
            {
                var fieldValue = fields[defaultFieldIndex];
                if (componentIndex >= 0)
                {
                    var components = fieldValue.Split('^');
                    return components.Length > componentIndex ? components[componentIndex] : null;
                }
                return fieldValue;
            }

            return null;
        }

        // PID-3: Patient Identifier (MRN)
        var mrn = GetConfiguredOrDefault("MRN", 3, 0);
        if (mrn != null) data.MRN = mrn;

        // PID-5: Patient Name
        data.LastName = GetConfiguredOrDefault("LastName", 5, 0);
        data.FirstName = GetConfiguredOrDefault("FirstName", 5, 1);
        data.MiddleName = GetConfiguredOrDefault("MiddleName", 5, 2);

        // PID-7: Date of Birth
        var dobString = GetConfiguredOrDefault("DateOfBirth", 7);
        if (dobString != null) data.DateOfBirth = ParseHL7Date(dobString);

        // PID-8: Sex
        data.Sex = GetConfiguredOrDefault("Sex", 8);

        // PID-11: Address components
        data.Address = GetConfiguredOrDefault("Address", 11, 0);
        data.City = GetConfiguredOrDefault("City", 11, 2);
        data.State = GetConfiguredOrDefault("State", 11, 3);
        data.Zip = GetConfiguredOrDefault("Zip", 11, 4);

        // PID-13: Phone
        data.Phone = GetConfiguredOrDefault("Phone", 13, 0);

        return data;
    }

    private async Task<Patient?> MatchByIdentifierAsync(PatientData data, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(data.MRN))
            return null;

        // TEMPORARY: Match against FriendlyId until ExternalIds system is implemented
        // In production, this should search Patient.ExternalIds JSON field for matching MRN
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => 
                p.FriendlyId == data.MRN && !p.IsDeleted, 
                cancellationToken);

        return patient;
    }

    private async Task<List<Patient>> MatchByDemographicsAsync(PatientData data, bool strict, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(data.FirstName) || string.IsNullOrEmpty(data.LastName))
            return new List<Patient>();

        var query = _context.Patients.Where(p => !p.IsDeleted);

        if (strict)
        {
            // Strict: exact name and DOB match
            query = query.Where(p =>
                p.GivenName == data.FirstName &&
                p.FamilyName == data.LastName &&
                p.DateOfBirth == data.DateOfBirth);
        }
        else
        {
            // Fuzzy: case-insensitive name, exact DOB
            var firstName = data.FirstName.ToLower();
            var lastName = data.LastName.ToLower();

            query = query.Where(p =>
                p.GivenName.ToLower() == firstName &&
                p.FamilyName.ToLower() == lastName &&
                p.DateOfBirth == data.DateOfBirth);
        }

        return await query.ToListAsync(cancellationToken);
    }

    private string NormalizeOrganizationName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        // Remove punctuation, convert to uppercase, trim
        return new string(name.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray())
            .ToUpperInvariant()
            .Trim();
    }

    /// <summary>
    /// Extract a mapped value using configuration-driven mappings when available,
    /// falling back to hardcoded extraction methods
    /// </summary>
    private async Task<string?> ExtractMappedValueAsync(
        HL7Message message,
        HL7Configuration? configuration,
        Guid? diseaseId,
        string targetEntity,
        string targetProperty,
        Func<HL7Message, string?> fallbackExtractor)
    {
        // If configuration is available and has mappings, try to use them
        if (configuration != null)
        {
            try
            {
                // Parse the HL7 message for field mapping service
                var parsedMessage = await ParseHL7MessageForMappingAsync(message);
                if (parsedMessage != null)
                {
                    var mappedValue = await _fieldMappingService.GetMappedValueAsync(
                        parsedMessage,
                        configuration.Id,
                        diseaseId,
                        targetEntity,
                        targetProperty);

                    if (mappedValue != null)
                    {
                        _logger.LogDebug(
                            "Used configuration mapping for {Entity}.{Property}: {Value}",
                            targetEntity, targetProperty, mappedValue);
                        return mappedValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to use configuration mapping for {Entity}.{Property}, falling back to hardcoded extraction",
                    targetEntity, targetProperty);
            }
        }

        // Fallback to hardcoded extraction
        var fallbackValue = fallbackExtractor(message);
        if (fallbackValue != null)
        {
            _logger.LogDebug(
                "Used fallback extraction for {Entity}.{Property}: {Value}",
                targetEntity, targetProperty, fallbackValue);
        }

        return fallbackValue;
    }

    /// <summary>
    /// Parse the HL7 message from raw text for use with the mapping service
    /// </summary>
    private async Task<NHapi.Base.Model.IMessage?> ParseHL7MessageForMappingAsync(HL7Message message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message.RawMessage))
                return null;

            var parser = new NHapi.Base.Parser.PipeParser();
            return parser.Parse(message.RawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse HL7 message for mapping service");
            return null;
        }
    }


    private string? ExtractLaboratoryName(HL7Message message, HL7Configuration? configuration = null)
    {
        // PRIORITY 1: Check if there's a configured mapping for laboratory name
        if (configuration != null)
        {
            try
            {
                _logger.LogDebug("[LAB EXTRACTION] Checking for configured field mappings. Total mappings: {Count}",
                    configuration.FieldMappings?.Count ?? 0);

                var labMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive &&
                                        (m.TargetProperty == "LaboratoryName" || m.TargetProperty == "Laboratory"));

                if (labMapping != null)
                {
                    _logger.LogInformation("[LAB EXTRACTION] Found user-configured mapping: FieldPath='{Path}'", 
                        labMapping.FieldPath);

                    // Use unified helper to extract value (handles coded values too)
                    var extractedValue = ExtractCodedValue(message, labMapping.FieldPath, "LABORATORY");
                    var value = extractedValue.text ?? extractedValue.code; // Prefer text, fallback to code

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _logger.LogInformation("[LAB EXTRACTION] ✅ Extracted from user-configured mapping {FieldPath}: '{Value}'",
                            labMapping.FieldPath, value);
                        return value;
                    }
                    else
                    {
                        _logger.LogWarning("[LAB EXTRACTION] Mapped field {FieldPath} found but contained no data, falling back to defaults",
                            labMapping.FieldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LAB EXTRACTION] Error checking field mapping configuration, falling back to default");
            }
        }

        // PRIORITY 2 (FALLBACK): From MSH-4 (Sending Facility) - typically the laboratory/hospital name
        var mshSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "MSH");
        if (mshSegment != null)
        {
            var fields = mshSegment.RawSegment.Split('|');
            _logger.LogDebug("[LAB EXTRACTION] MSH segment has {Count} fields", fields.Length);

            // fields[0]=MSH, fields[1]=^~\&, fields[2]=MSH-3, fields[3]=MSH-4
            if (fields.Length > 3)
            {
                var sendingFacility = fields[3]; // MSH-4 Sending Facility
                _logger.LogDebug("[LAB EXTRACTION] MSH-4 (Sending Facility): '{Value}'", sendingFacility);
                if (!string.IsNullOrWhiteSpace(sendingFacility))
                    return sendingFacility;
            }

            // Fallback to MSH-3 (Sending Application) if MSH-4 is empty
            if (fields.Length > 2 && !string.IsNullOrWhiteSpace(fields[2]))
            {
                _logger.LogDebug("[LAB EXTRACTION] Using MSH-3 (Sending Application): '{Value}'", fields[2]);
                return fields[2]; // MSH-3 Sending Application
            }
        }

        _logger.LogWarning("[LAB EXTRACTION] No MSH segment found or no laboratory name extracted");
        return null;
    }

    private string? ExtractOrderingProviderName(HL7Message message, HL7Configuration? configuration = null)
    {
        _logger.LogInformation("[ORDERING PROVIDER] === EXTRACTION START ===");
        _logger.LogInformation("[ORDERING PROVIDER] Message has {Count} segments in collection", message.Segments?.Count ?? 0);

        if (message.Segments == null || !message.Segments.Any())
        {
            _logger.LogError("[ORDERING PROVIDER] ❌ Segments collection is NULL or EMPTY - cannot extract provider");
            return null;
        }

        // List all segment types for debugging
        var segmentTypes = string.Join(", ", message.Segments.Select(s => s.SegmentType));
        _logger.LogInformation("[ORDERING PROVIDER] Available segment types: {Types}", segmentTypes);

        // Helper to parse provider name from XCN (Extended Composite ID Number and Name) field
        string? ParseProviderName(string fieldValue, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
                return null;

            var parts = fieldValue.Split('^');
            _logger.LogInformation("[ORDERING PROVIDER] {Field} has {Count} parts: {Raw}", 
                fieldName, parts.Length, fieldValue);

            // XCN format can be:
            // Format 1: LastName^FirstName^Middle^Title
            // Format 2: ID^LastName^FirstName^Middle^^Title
            // We need to detect which format by checking if the first part looks like an ID

            string? firstName = null;
            string? lastName = null;
            string? middle = null;
            string? title = null;

            // Check if parts[0] looks like an ID (contains hyphens or is all digits)
            bool firstPartIsId = parts.Length > 0 && 
                (parts[0].Contains('-') || parts[0].All(char.IsDigit));

            if (firstPartIsId && parts.Length >= 3)
            {
                // Format 2: ID^LastName^FirstName^Middle^^Title^^^Facility
                lastName = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;
                firstName = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;
                middle = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null;
                title = parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]) ? parts[5] : null;

                _logger.LogInformation("[ORDERING PROVIDER] Parsed as Format 2 (with ID): ID={Id}, Last={Last}, First={First}, Middle={Middle}, Title={Title}",
                    parts[0], lastName ?? "NULL", firstName ?? "NULL", middle ?? "NULL", title ?? "NULL");
            }
            else if (parts.Length >= 2)
            {
                // Format 1: LastName^FirstName^Middle^Title
                lastName = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
                firstName = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;
                middle = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;
                title = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null;

                _logger.LogInformation("[ORDERING PROVIDER] Parsed as Format 1 (no ID): Last={Last}, First={First}, Middle={Middle}, Title={Title}",
                    lastName ?? "NULL", firstName ?? "NULL", middle ?? "NULL", title ?? "NULL");
            }
            else
            {
                _logger.LogWarning("[ORDERING PROVIDER] {Field} has insufficient parts ({Count}) to extract provider name", 
                    fieldName, parts.Length);
                return null;
            }

            // Build provider name
            if (!string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(firstName))
            {
                var nameBuilder = new System.Text.StringBuilder();

                // Add title if present and looks like a title (DR, MD, etc.)
                if (!string.IsNullOrEmpty(title) && (title.Equals("DR", StringComparison.OrdinalIgnoreCase) || 
                    title.Equals("MD", StringComparison.OrdinalIgnoreCase) ||
                    title.Equals("DOCTOR", StringComparison.OrdinalIgnoreCase)))
                {
                    nameBuilder.Append("Dr. ");
                }
                else if (!string.IsNullOrEmpty(firstName) && firstName.Equals("DR", StringComparison.OrdinalIgnoreCase))
                {
                    // Sometimes "DR" is in the first name field
                    nameBuilder.Append("Dr. ");
                    firstName = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : "";
                }

                // Build full name
                nameBuilder.Append(firstName);
                if (!string.IsNullOrEmpty(middle))
                {
                    nameBuilder.Append(" ").Append(middle);
                }
                nameBuilder.Append(" ").Append(lastName);

                var fullName = nameBuilder.ToString().Trim();
                _logger.LogInformation("[ORDERING PROVIDER] ✅ Extracted from {Field}: {Name}", fieldName, fullName);
                return fullName;
            }

            _logger.LogWarning("[ORDERING PROVIDER] {Field} missing required name components (Last={Last}, First={First})", 
                fieldName, lastName ?? "NULL", firstName ?? "NULL");
            return null;
        }

        // PRIORITY 1: Check if there's a configured mapping for ordering provider
        if (configuration != null)
        {
            try
            {
                _logger.LogInformation("[ORDERING PROVIDER] Configuration provided, checking for field mappings. Total mappings: {Count}", 
                    configuration.FieldMappings?.Count ?? 0);

                // Debug: List all active mappings
                if (configuration.FieldMappings != null && configuration.FieldMappings.Any())
                {
                    foreach (var mapping in configuration.FieldMappings.Where(m => m.IsActive))
                    {
                        _logger.LogInformation("[ORDERING PROVIDER] Available mapping: TargetProperty='{Property}', FieldPath='{Path}', IsActive={Active}", 
                            mapping.TargetProperty, mapping.FieldPath, mapping.IsActive);
                    }
                }

                // Look for any mapping targeting OrderingProvider
                var providerMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive && 
                                        (m.TargetProperty == "OrderingProvider" ||
                                         m.TargetProperty == "OrderingProviderName"));

                if (providerMapping != null)
                {
                    _logger.LogInformation("[ORDERING PROVIDER] Found configured mapping: Segment={Segment}, FieldPath={Path}, TargetEntity='{Entity}'", 
                        providerMapping.SegmentType, providerMapping.FieldPath, providerMapping.TargetEntity);

                    // Extract segment type and field number from FieldPath (e.g., "OBR-16" → "OBR" and 16)
                    var pathParts = providerMapping.FieldPath.Split('-');
                    if (pathParts.Length == 2 && int.TryParse(pathParts[1], out int fieldNumber))
                    {
                        var segment = message.Segments.FirstOrDefault(s => s.SegmentType == providerMapping.SegmentType);
                        if (segment != null)
                        {
                            var fields = segment.RawSegment.Split('|');
                            _logger.LogInformation("[ORDERING PROVIDER] {SegmentType} segment has {Count} fields", providerMapping.SegmentType, fields.Length);
                            _logger.LogInformation("[ORDERING PROVIDER] {SegmentType} raw segment: {Segment}", providerMapping.SegmentType, segment.RawSegment);

                            // Log fields around the target position for debugging
                            _logger.LogInformation("[ORDERING PROVIDER] Field mapping expects {FieldPath} (0-based index {Index})", 
                                providerMapping.FieldPath, fieldNumber - 1);
                            for (int i = Math.Max(0, fieldNumber - 3); i < Math.Min(fields.Length, fieldNumber + 2); i++)
                            {
                                _logger.LogInformation("[ORDERING PROVIDER] {SegmentType} Field[{Index}] (HL7 {Segment}-{FieldNum}) = '{Value}'", 
                                    providerMapping.SegmentType, i, providerMapping.SegmentType, i + 1, fields[i]);
                            }

                            // Convert HL7 field number to 0-based array index
                            // When split by |, fields[0] = segment name, fields[1] = field 1, etc.
                            // So OBR-16 → fields[16] (not fields[15])
                            int fieldIndex = fieldNumber;

                            if (fields.Length > fieldIndex && !string.IsNullOrWhiteSpace(fields[fieldIndex]))
                            {
                                _logger.LogInformation("[ORDERING PROVIDER] {FieldPath} raw value: '{Value}'", providerMapping.FieldPath, fields[fieldIndex]);
                                var providerName = ParseProviderName(fields[fieldIndex], providerMapping.FieldPath);
                                if (providerName != null)
                                {
                                    _logger.LogInformation("[ORDERING PROVIDER] ✅ Extracted from configured mapping {FieldPath}: '{Name}'", 
                                        providerMapping.FieldPath, providerName);
                                    return providerName;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("[ORDERING PROVIDER] Configured field {FieldPath} is empty or doesn't exist (segment has {Count} fields)", 
                                    providerMapping.FieldPath, fields.Length);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("[ORDERING PROVIDER] Configured segment type {SegmentType} not found in message", 
                                providerMapping.SegmentType);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[ORDERING PROVIDER] Invalid FieldPath format: '{Path}'. Expected format: SEGMENT-NUMBER (e.g., OBR-16)", 
                            providerMapping.FieldPath);
                    }
                }
                else
                {
                    _logger.LogInformation("[ORDERING PROVIDER] No configured mapping found for OrderingProvider. Using default extraction.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ORDERING PROVIDER] Error checking field mapping configuration, falling back to default extraction");
            }
        }
        else
        {
            _logger.LogDebug("[ORDERING PROVIDER] No configuration provided, using default extraction");
        }

        // PRIORITY 2 (FALLBACK): Try standard HL7 locations for ordering provider

        // Try ORC-12 (Ordering Provider) first - preferred location per HL7 v2.5
        var orcSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "ORC");
        if (orcSegment != null)
        {
            var fields = orcSegment.RawSegment.Split('|');
            _logger.LogInformation("[ORDERING PROVIDER] ORC segment found with {Count} fields", fields.Length);
            _logger.LogInformation("[ORDERING PROVIDER] ORC raw segment: {Segment}", orcSegment.RawSegment);

            // Log fields around index 12 for debugging (ORC-12 = fields[12])
            for (int i = Math.Max(0, 9); i < Math.Min(fields.Length, 14); i++)
            {
                _logger.LogInformation("[ORDERING PROVIDER] ORC Field[{Index}] = '{Value}'", i, fields[i]);
            }

            // NOTE: When split by |, fields[0] = "ORC", fields[1] = ORC-1, fields[12] = ORC-12, etc.
            if (fields.Length > 12 && !string.IsNullOrWhiteSpace(fields[12]))
            {
                _logger.LogInformation("[ORDERING PROVIDER] Attempting to parse ORC-12 (fields[12]): '{Value}'", fields[12]);
                var providerName = ParseProviderName(fields[12], "ORC-12");
                if (providerName != null)
                    return providerName;
            }
            else
            {
                if (fields.Length <= 12)
                    _logger.LogWarning("[ORDERING PROVIDER] ORC segment has only {Count} fields, cannot access field[12] (ORC-12)", fields.Length);
                else
                    _logger.LogWarning("[ORDERING PROVIDER] ORC field[12] (ORC-12) is empty or whitespace");
            }

            // Try ORC-13 as additional fallback (some labs use non-standard positions)
            // NOTE: ORC-13 = fields[13] when split by |
            if (fields.Length > 13 && !string.IsNullOrWhiteSpace(fields[13]))
            {
                _logger.LogInformation("[ORDERING PROVIDER] Attempting to parse ORC-13 (fields[13]): '{Value}'", fields[13]);
                var providerName = ParseProviderName(fields[13], "ORC-13");
                if (providerName != null)
                    return providerName;
            }

            // Try ORC-14 as additional fallback (YOUR LAB USES THIS POSITION!)
            // NOTE: ORC-14 = fields[14] when split by |
            if (fields.Length > 14 && !string.IsNullOrWhiteSpace(fields[14]))
            {
                _logger.LogInformation("[ORDERING PROVIDER] Attempting to parse ORC-14 (fields[14]): '{Value}'", fields[14]);
                var providerName = ParseProviderName(fields[14], "ORC-14");
                if (providerName != null)
                    return providerName;
            }
        }
        else
        {
            _logger.LogWarning("[ORDERING PROVIDER] No ORC segment found in message");
        }

        // Try OBR-16 (Ordering Provider) - common location
        var obrSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "OBR");
        if (obrSegment != null)
        {
            var fields = obrSegment.RawSegment.Split('|');
            _logger.LogInformation("[ORDERING PROVIDER] OBR segment has {Count} fields", fields.Length);

            // NOTE: When split by |, fields[0] = "OBR", fields[1] = OBR-1, fields[16] = OBR-16, etc.
            if (fields.Length > 16 && !string.IsNullOrWhiteSpace(fields[16]))
            {
                var providerName = ParseProviderName(fields[16], "OBR-16");
                if (providerName != null)
                    return providerName;
            }

            // Try OBR-28 (Result Copies To) as fallback
            // NOTE: OBR-28 = fields[28] when split by |
            if (fields.Length > 28 && !string.IsNullOrWhiteSpace(fields[28]))
            {
                var providerName = ParseProviderName(fields[28], "OBR-28");
                if (providerName != null)
                    return providerName;
            }

            // Try OBR-32 (Principal Result Interpreter) as final fallback
            // NOTE: OBR-32 = fields[32] when split by |
            if (fields.Length > 32 && !string.IsNullOrWhiteSpace(fields[32]))
            {
                var providerName = ParseProviderName(fields[32], "OBR-32");
                if (providerName != null)
                    return providerName;
            }
        }

        _logger.LogWarning("[ORDERING PROVIDER] No ordering provider found in ORC-12/13/14, OBR-16/28/32");
        return null;
    }

    /// <summary>
    /// Match test method from HL7 data to TestMethod lookup table.
    /// Tries exact code match first, then falls back to fuzzy text matching.
    /// NOTE: Uses IgnoreQueryFilters() since HL7 processing runs in background service without HTTP context.
    /// </summary>
    private async Task<int?> MatchTestMethodAsync(
        string? methodCode,
        string? methodText,
        string? codingSystem,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(methodText))
            return null;

        try
        {
            // Get all active test methods - use IgnoreQueryFilters for background service
            var testMethods = await _context.Set<TestMethod>()
                .IgnoreQueryFilters()  // Bypass soft delete filter
                .Where(tm => tm.IsActive)
                .ToListAsync(cancellationToken);

            if (!testMethods.Any())
            {
                _logger.LogWarning("[TEST METHOD] No active test methods found in database");
                return null;
            }

            // STRATEGY 1: Try exact code match (StandardCode + CodingSystem)
            if (!string.IsNullOrWhiteSpace(methodCode) && !string.IsNullOrWhiteSpace(codingSystem))
            {
                var exactMatch = testMethods.FirstOrDefault(tm =>
                    !string.IsNullOrWhiteSpace(tm.SnomedCode) &&
                    !string.IsNullOrWhiteSpace(tm.SnomedDisplay) &&
                    tm.SnomedCode.Equals(methodCode, StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                {
                    _logger.LogDebug("[TEST METHOD] Exact code match: {Code} ({System}) → {Name}", 
                        methodCode, codingSystem, exactMatch.Name);
                    return exactMatch.Id;
                }
            }

            // STRATEGY 2: Try code match without coding system
            if (!string.IsNullOrWhiteSpace(methodCode))
            {
                var codeMatch = testMethods.FirstOrDefault(tm =>
                    !string.IsNullOrWhiteSpace(tm.SnomedCode) &&
                    tm.SnomedCode.Equals(methodCode, StringComparison.OrdinalIgnoreCase));

                if (codeMatch != null)
                {
                    _logger.LogDebug("[TEST METHOD] Code match (no system): {Code} → {Name}", 
                        methodCode, codeMatch.Name);
                    return codeMatch.Id;
                }
            }

            // STRATEGY 3: Fuzzy text matching on method text
            if (!string.IsNullOrWhiteSpace(methodText))
            {
                var normalizedText = NormalizeTestMethodName(methodText);

                // Try exact normalized match
                var exactTextMatch = testMethods.FirstOrDefault(tm =>
                    NormalizeTestMethodName(tm.Name) == normalizedText);

                if (exactTextMatch != null)
                {
                    _logger.LogDebug("[TEST METHOD] Exact text match: '{Text}' → {Name}", 
                        methodText, exactTextMatch.Name);
                    return exactTextMatch.Id;
                }

                // Try fuzzy match (contains)
                var fuzzyMatch = testMethods.FirstOrDefault(tm =>
                {
                    var normalizedName = NormalizeTestMethodName(tm.Name);
                    return normalizedName.Contains(normalizedText) || normalizedText.Contains(normalizedName);
                });

                if (fuzzyMatch != null)
                {
                    _logger.LogDebug("[TEST METHOD] Fuzzy text match: '{Text}' → {Name}", 
                        methodText, fuzzyMatch.Name);
                    return fuzzyMatch.Id;
                }
            }

            _logger.LogWarning("[TEST METHOD] No match found for Code='{Code}', Text='{Text}', System='{System}'",
                methodCode ?? "NULL", methodText ?? "NULL", codingSystem ?? "NULL");

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TEST METHOD] Error matching test method: Code={Code}, Text={Text}", 
                methodCode, methodText);
            return null;
        }
    }

    /// <summary>
    /// Normalize test method name for fuzzy matching
    /// </summary>
    private string NormalizeTestMethodName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove punctuation, convert to uppercase, collapse whitespace
        var normalized = new string(name
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .ToUpperInvariant()
            .Trim();

        // Collapse multiple spaces to single space
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    /// <summary>
    /// Extract specimen type from OBR-15 (Specimen Source), OBR-12 (Danger Code), or SPM-4
    /// Some labs put specimen info in non-standard fields, so we check multiple locations
    /// </summary>
    private (string? specimenCode, string? specimenText, string? specimenSystem) ExtractSpecimenType(HL7Message message, HL7Configuration? configuration = null)
    {
        // PRIORITY 1: Check if there's a configured mapping for specimen type
        if (configuration != null)
        {
            try
            {
                _logger.LogDebug("[SPECIMEN] Checking for configured field mappings. Total mappings: {Count}",
                    configuration.FieldMappings?.Count ?? 0);

                var specimenMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive &&
                                        (m.TargetProperty == "SpecimenType" || m.TargetProperty == "SpecimenTypeCode"));

                if (specimenMapping != null)
                {
                    _logger.LogDebug("[SPECIMEN] Found configured mapping: Segment={Segment}, FieldPath={Path}",
                        specimenMapping.SegmentType, specimenMapping.FieldPath);

                    var pathParts = specimenMapping.FieldPath.Split('-');
                    if (pathParts.Length == 2 && int.TryParse(pathParts[1], out int fieldNumber))
                    {
                        var segment = message.Segments.FirstOrDefault(s => s.SegmentType == specimenMapping.SegmentType);
                        if (segment != null)
                        {
                            var fields = segment.RawSegment.Split('|');
                            // When split by |, fields[0] = segment name, fields[1] = field 1, etc.
                            int fieldIndex = fieldNumber;

                            if (fields.Length > fieldIndex && !string.IsNullOrWhiteSpace(fields[fieldIndex]))
                            {
                                var fieldValue = fields[fieldIndex];
                                var parts = fieldValue.Split('^');
                                var code = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
                                var text = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] :
                                          (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null);
                                var system = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;

                                if (code != null || text != null)
                                {
                                    _logger.LogInformation("[SPECIMEN] ✅ Extracted from configured mapping {FieldPath}: Code='{Code}', Text='{Text}', System='{System}'",
                                        specimenMapping.FieldPath, code ?? "NULL", text ?? "NULL", system ?? "NULL");
                                    return (code, text, system);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SPECIMEN] Error checking field mapping configuration, falling back to default");
            }
        }

        // PRIORITY 2 (FALLBACK): Try OBR-15 (Specimen Source) first - standard location
        var obrSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "OBR");
        if (obrSegment != null)
        {
            var fields = obrSegment.RawSegment.Split('|');
            _logger.LogInformation("[SPECIMEN] OBR segment has {Count} fields", fields.Length);

            // Helper function to parse a specimen field
            (string? code, string? text, string? system, bool looksLikeProvider) ParseSpecimenField(string fieldValue)
            {
                if (string.IsNullOrWhiteSpace(fieldValue))
                    return (null, null, null, false);

                var parts = fieldValue.Split('^');
                var code = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
                var text = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : 
                          (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null);
                var system = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;

                // Check if this looks like provider data
                bool looksLikeProvider = false;
                if (text != null)
                {
                    var upperText = text.ToUpperInvariant();
                    looksLikeProvider = upperText.Contains("MD") || upperText.Contains("DR") || 
                                       upperText.Contains("NPI") || upperText.Contains("DOCTOR") ||
                                       upperText.Contains("^") || // Provider names often have ^ separators
                                       (code != null && code.Length >= 10 && code.All(char.IsDigit)); // NPI is 10 digits
                }

                return (code, text, system, looksLikeProvider);
            }

            // Try OBR-15 (Specimen Source) - standard location
            // NOTE: When split by |, fields[0] = "OBR", fields[1] = OBR-1, fields[15] = OBR-15
            if (fields.Length > 15 && !string.IsNullOrWhiteSpace(fields[15]))
            {
                var (code, text, system, looksLikeProvider) = ParseSpecimenField(fields[15]);
                _logger.LogInformation("[SPECIMEN] OBR-15: Code='{Code}', Text='{Text}', System='{System}', LooksLikeProvider={Provider}", 
                    code ?? "NULL", text ?? "NULL", system ?? "NULL", looksLikeProvider);

                if (!looksLikeProvider && (code != null || text != null))
                {
                    _logger.LogInformation("[SPECIMEN] ✅ Extracted from OBR-15 (standard): Code='{Code}', Text='{Text}', System='{System}'", 
                        code ?? "NULL", text ?? "NULL", system ?? "NULL");
                    return (code, text, system);
                }
            }

            // STRATEGY 1b: Try OBR-12 (Danger Code) - some labs incorrectly put specimen here
            if (fields.Length > 12 && !string.IsNullOrWhiteSpace(fields[12]))
            {
                var (code, text, system, looksLikeProvider) = ParseSpecimenField(fields[12]);
                _logger.LogInformation("[SPECIMEN] OBR-12: Code='{Code}', Text='{Text}', System='{System}', LooksLikeProvider={Provider}", 
                    code ?? "NULL", text ?? "NULL", system ?? "NULL", looksLikeProvider);

                if (!looksLikeProvider && (code != null || text != null))
                {
                    _logger.LogInformation("[SPECIMEN] ✅ Extracted from OBR-12 (non-standard): Code='{Code}', Text='{Text}', System='{System}'", 
                        code ?? "NULL", text ?? "NULL", system ?? "NULL");
                    return (code, text, system);
                }
            }

            // STRATEGY 1c: Try OBR-16 (Ordering Provider) - sometimes specimen is here if OBR-15 is empty
            if (fields.Length > 16 && !string.IsNullOrWhiteSpace(fields[16]))
            {
                var (code, text, system, looksLikeProvider) = ParseSpecimenField(fields[16]);
                _logger.LogInformation("[SPECIMEN] OBR-16: Code='{Code}', Text='{Text}', System='{System}', LooksLikeProvider={Provider}", 
                    code ?? "NULL", text ?? "NULL", system ?? "NULL", looksLikeProvider);

                // Only use OBR-16 if it doesn't look like provider data
                if (!looksLikeProvider && (code != null || text != null))
                {
                    _logger.LogInformation("[SPECIMEN] ✅ Extracted from OBR-16 (alternate): Code='{Code}', Text='{Text}', System='{System}'", 
                        code ?? "NULL", text ?? "NULL", system ?? "NULL");
                    return (code, text, system);
                }
            }

            _logger.LogWarning("[SPECIMEN] Could not find valid specimen data in OBR-12, OBR-15, or OBR-16");
        }

        // STRATEGY 2: Fallback to SPM segment (SPM-4: Specimen Type)
        var spmSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "SPM");
        if (spmSegment != null)
        {
            var fields = spmSegment.RawSegment.Split('|');
            // SPM-4: Specimen Type (index 4 in 0-based array)
            if (fields.Length > 4 && !string.IsNullOrWhiteSpace(fields[4]))
            {
                // Format: code^text^coding_system (e.g., 258500001^Nasopharyngeal swab^SCT)
                var specimenField = fields[4];
                var parts = specimenField.Split('^');

                var code = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
                var text = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : 
                          (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null);
                var system = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;

                if (code != null || text != null)
                {
                    _logger.LogInformation("[SPECIMEN] Extracted from SPM-4: Code='{Code}', Text='{Text}', System='{System}'", 
                        code, text, system);
                    return (code, text, system);
                }
            }
        }

        _logger.LogWarning("[SPECIMEN] No specimen type found in OBR-15 or SPM-4");
        return (null, null, null);
    }

    private (string? testMethodCode, string? testMethodText, string? testMethodSystem) ExtractTestMethod(HL7Message message, HL7Configuration? configuration = null)
    {
        // PRIORITY 1: Check if there's a configured mapping for test method/test type
        // This allows users to configure which field contains the test method (e.g., OBX-17, OBX-3.2, OBR-4)
        if (configuration != null)
        {
            try
            {
                _logger.LogInformation("[TEST METHOD] Configuration provided, checking for field mappings. Total mappings: {Count}", 
                    configuration.FieldMappings?.Count ?? 0);

                // Check for ANY mapping for TestName or TestMethod (not just hardcoded OBX-17)
                var testMethodMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive && 
                                        (m.TargetProperty == "TestName" || m.TargetProperty == "TestMethod"));

                if (testMethodMapping != null)
                {
                    _logger.LogInformation("[TEST METHOD] Found user-configured mapping: FieldPath='{Path}', TargetEntity='{Entity}', TargetProperty='{Property}'", 
                        testMethodMapping.FieldPath, testMethodMapping.TargetEntity, testMethodMapping.TargetProperty);

                    // Extract from user's configured field path
                    var extractedValue = ExtractCodedValue(message, testMethodMapping.FieldPath, "TEST METHOD");
                    if (extractedValue.code != null || extractedValue.text != null)
                    {
                        _logger.LogInformation("[TEST METHOD] ✅ Extracted from user-configured mapping {FieldPath}: Code='{Code}', Text='{Text}', System='{System}'", 
                            testMethodMapping.FieldPath, extractedValue.code ?? "NULL", extractedValue.text ?? "NULL", extractedValue.system ?? "NULL");
                        return extractedValue;
                    }
                    else
                    {
                        _logger.LogWarning("[TEST METHOD] User-configured field {FieldPath} is empty or not found, falling back to standard locations", 
                            testMethodMapping.FieldPath);
                    }
                }
                else
                {
                    _logger.LogInformation("[TEST METHOD] No user mapping configured for TestName/TestMethod. Using standard fallback locations.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TEST METHOD] Error checking field mapping configuration, falling back to standard locations");
            }
        }
        else
        {
            _logger.LogDebug("[TEST METHOD] No configuration provided, using standard extraction locations");
        }

        // PRIORITY 2 (FALLBACK): Try standard locations in order of likelihood
        // 2a. OBX-17 (Observation Method) - common for lab results with test method per observation
        _logger.LogInformation("[TEST METHOD] Trying standard location: OBX-17 (Observation Method)");
        var obxSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "OBX");
        if (obxSegment != null)
        {
            var obxResult = ExtractCodedValue(message, "OBX-17", "TEST METHOD");
            if (obxResult.code != null || obxResult.text != null)
            {
                _logger.LogInformation("[TEST METHOD] ✅ Extracted from OBX-17: Code='{Code}', Text='{Text}', System='{System}'", 
                    obxResult.code, obxResult.text, obxResult.system);
                return obxResult;
            }
        }

        // 2b. OBR-4 (Universal Service Identifier) - standard location for test/service type
        _logger.LogInformation("[TEST METHOD] Trying standard location: OBR-4 (Universal Service Identifier)");
        var obrResult = ExtractCodedValue(message, "OBR-4", "TEST METHOD");
        if (obrResult.code != null || obrResult.text != null)
        {
            _logger.LogInformation("[TEST METHOD] ✅ Extracted from OBR-4: Code='{Code}', Text='{Text}', System='{System}'", 
                obrResult.code, obrResult.text, obrResult.system);
            return obrResult;
        }

        // 2c. OBX-3.2 (Observation Identifier text) - some labs put test method in observation name
        _logger.LogInformation("[TEST METHOD] Trying alternate location: OBX-3.2 (Observation Identifier text)");
        var obx3Result = ExtractCodedValue(message, "OBX-3.2", "TEST METHOD");
        if (obx3Result.text != null)
        {
            _logger.LogInformation("[TEST METHOD] ✅ Extracted from OBX-3.2: Text='{Text}'", obx3Result.text);
            return (null, obx3Result.text, null);
        }

        _logger.LogWarning("[TEST METHOD] No test method found in any standard location");
        return (null, null, null);
    }

    /// <summary>
    /// Match specimen type from HL7 data to SpecimenType lookup table.
    /// Tries exact code match first, then falls back to fuzzy text matching.
    /// NOTE: Uses IgnoreQueryFilters() since HL7 processing runs in background service without HTTP context.
    /// </summary>
    private async Task<int?> MatchSpecimenTypeAsync(
        string? specimenCode,
        string? specimenText,
        string? codingSystem,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(specimenCode) && string.IsNullOrWhiteSpace(specimenText))
            return null;

        try
        {
            // Get all specimen types - use IgnoreQueryFilters for background service
            var specimenTypes = await _context.Set<SpecimenType>()
                .IgnoreQueryFilters()  // Bypass soft delete filter
                .ToListAsync(cancellationToken);

            if (!specimenTypes.Any())
            {
                _logger.LogWarning("[SPECIMEN TYPE] No specimen types found in database");
                return null;
            }

            // STRATEGY 1: Try exact SNOMED CT code match
            if (!string.IsNullOrWhiteSpace(specimenCode) && 
                (codingSystem?.Equals("SCT", StringComparison.OrdinalIgnoreCase) == true ||
                 codingSystem?.Equals("SNOMED CT", StringComparison.OrdinalIgnoreCase) == true))
            {
                var snomedMatch = specimenTypes.FirstOrDefault(st =>
                    !string.IsNullOrWhiteSpace(st.SnomedCode) &&
                    st.SnomedCode == specimenCode);

                if (snomedMatch != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] SNOMED CT match: {Code} → {Name}", 
                        specimenCode, snomedMatch.Name);
                    return snomedMatch.Id;
                }
            }

            // STRATEGY 2: Try HL7 v2 code match
            if (!string.IsNullOrWhiteSpace(specimenCode))
            {
                var hl7Match = specimenTypes.FirstOrDefault(st =>
                    !string.IsNullOrWhiteSpace(st.Hl7Code) &&
                    st.Hl7Code.Equals(specimenCode, StringComparison.OrdinalIgnoreCase));

                if (hl7Match != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] HL7 code match: {Code} → {Name}", 
                        specimenCode, hl7Match.Name);
                    return hl7Match.Id;
                }
            }

            // STRATEGY 3: Try LOINC system code match
            if (!string.IsNullOrWhiteSpace(specimenCode))
            {
                var loincMatch = specimenTypes.FirstOrDefault(st =>
                    !string.IsNullOrWhiteSpace(st.LoincSystemCode) &&
                    st.LoincSystemCode.Equals(specimenCode, StringComparison.OrdinalIgnoreCase));

                if (loincMatch != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] LOINC code match: {Code} → {Name}", 
                        specimenCode, loincMatch.Name);
                    return loincMatch.Id;
                }
            }

            // STRATEGY 4: Fuzzy text matching on specimen text
            if (!string.IsNullOrWhiteSpace(specimenText))
            {
                var normalizedText = NormalizeSpecimenTypeName(specimenText);

                // Try exact normalized match
                var exactTextMatch = specimenTypes.FirstOrDefault(st =>
                    NormalizeSpecimenTypeName(st.Name) == normalizedText);

                if (exactTextMatch != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] Exact text match: '{Text}' → {Name}", 
                        specimenText, exactTextMatch.Name);
                    return exactTextMatch.Id;
                }

                // Try SNOMED Display match
                var snomedDisplayMatch = specimenTypes.FirstOrDefault(st =>
                    !string.IsNullOrWhiteSpace(st.SnomedDisplay) &&
                    NormalizeSpecimenTypeName(st.SnomedDisplay) == normalizedText);

                if (snomedDisplayMatch != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] SNOMED display match: '{Text}' → {Name}", 
                        specimenText, snomedDisplayMatch.Name);
                    return snomedDisplayMatch.Id;
                }

                // Try fuzzy match (contains)
                var fuzzyMatch = specimenTypes.FirstOrDefault(st =>
                {
                    var normalizedName = NormalizeSpecimenTypeName(st.Name);
                    return normalizedName.Contains(normalizedText) || normalizedText.Contains(normalizedName);
                });

                if (fuzzyMatch != null)
                {
                    _logger.LogDebug("[SPECIMEN TYPE] Fuzzy text match: '{Text}' → {Name}", 
                        specimenText, fuzzyMatch.Name);
                    return fuzzyMatch.Id;
                }
            }

            _logger.LogWarning("[SPECIMEN TYPE] No match found for Code='{Code}', Text='{Text}', System='{System}'",
                specimenCode ?? "NULL", specimenText ?? "NULL", codingSystem ?? "NULL");

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SPECIMEN TYPE] Error matching specimen type: Code={Code}, Text={Text}", 
                specimenCode, specimenText);
            return null;
        }
    }

    /// <summary>
    /// Normalize specimen type name for fuzzy matching
    /// </summary>
    private string NormalizeSpecimenTypeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove punctuation, convert to uppercase, collapse whitespace
        var normalized = new string(name
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .ToUpperInvariant()
            .Trim();

        // Collapse multiple spaces to single space
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    private string? ExtractAccessionNumber(HL7Message message, HL7Configuration? configuration = null)
    {
        // PRIORITY 1: Check if there's a configured mapping for accession number
        if (configuration != null)
        {
            try
            {
                var accessionMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive &&
                                        (m.TargetProperty == "AccessionNumber" || m.TargetProperty == "FillerOrderNumber"));

                if (accessionMapping != null)
                {
                    _logger.LogInformation("[ACCESSION] Found user-configured mapping: FieldPath='{Path}'", 
                        accessionMapping.FieldPath);

                    // Use unified helper to extract value (handles coded values)
                    var extractedValue = ExtractCodedValue(message, accessionMapping.FieldPath, "ACCESSION");
                    var accession = extractedValue.code ?? extractedValue.text; // Prefer code for accession numbers

                    if (!string.IsNullOrWhiteSpace(accession))
                    {
                        _logger.LogInformation("[ACCESSION] ✅ Extracted from user-configured mapping {FieldPath}: '{Value}'",
                            accessionMapping.FieldPath, accession);
                        return accession;
                    }
                    else
                    {
                        _logger.LogWarning("[ACCESSION] Mapped field {FieldPath} found but contained no data, falling back to OBR-3",
                            accessionMapping.FieldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ACCESSION] Error checking field mapping configuration, falling back to default");
            }
        }

        // PRIORITY 2 (FALLBACK): Use OBR-3 (standard location)
        _logger.LogInformation("[ACCESSION] Using fallback: extracting from OBR-3");
        var extractedFallback = ExtractCodedValue(message, "OBR-3", "ACCESSION");
        return extractedFallback.code ?? extractedFallback.text;
    }

    private DateTime? ExtractSpecimenDate(HL7Message message, HL7Configuration? configuration = null)
    {
        // PRIORITY 1: Check if there's a configured mapping for specimen date
        if (configuration != null)
        {
            try
            {
                var dateMapping = configuration.FieldMappings
                    .FirstOrDefault(m => m.IsActive &&
                                        (m.TargetProperty == "SpecimenCollectionDate" ||
                                         m.TargetProperty == "SpecimenDate" ||
                                         m.TargetProperty == "CollectionDateTime"));

                if (dateMapping != null)
                {
                    _logger.LogInformation("[SPECIMEN DATE] Found user-configured mapping: FieldPath='{Path}'", 
                        dateMapping.FieldPath);

                    // Use unified helper to extract value
                    var extractedValue = ExtractCodedValue(message, dateMapping.FieldPath, "SPECIMEN DATE");
                    var dateString = extractedValue.text ?? extractedValue.code; // Try text first, then code

                    if (!string.IsNullOrWhiteSpace(dateString))
                    {
                        var date = ParseHL7Date(dateString);
                        if (date != null)
                        {
                            _logger.LogInformation("[SPECIMEN DATE] ✅ Extracted from user-configured mapping {FieldPath}: {Date}",
                                dateMapping.FieldPath, date.Value.ToString("yyyy-MM-dd HH:mm"));
                            return date;
                        }
                        else
                        {
                            _logger.LogWarning("[SPECIMEN DATE] Mapped field {FieldPath} contained unparseable date: '{Value}', falling back to defaults",
                                dateMapping.FieldPath, dateString);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[SPECIMEN DATE] Mapped field {FieldPath} found but contained no data, falling back to defaults",
                            dateMapping.FieldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SPECIMEN DATE] Error checking field mapping configuration, falling back to default");
            }
        }

        // PRIORITY 2 (FALLBACK): Use OBR-7 and OBR-14 (standard locations)
        _logger.LogInformation("[SPECIMEN DATE] Using fallback: trying OBR-7 then OBR-14");

        // Try OBR-7 (Observation Date/Time)
        var obr7Value = ExtractCodedValue(message, "OBR-7", "SPECIMEN DATE");
        if (obr7Value.text != null || obr7Value.code != null)
        {
            var date = ParseHL7Date(obr7Value.text ?? obr7Value.code);
            if (date != null) return date;
        }

        // Try OBR-14 (Specimen Received Date/Time)
        var obr14Value = ExtractCodedValue(message, "OBR-14", "SPECIMEN DATE");
        if (obr14Value.text != null || obr14Value.code != null)
        {
            return ParseHL7Date(obr14Value.text ?? obr14Value.code);
        }

        return null;
    }

    /// <summary>
    /// Extract markers from OBX segments using configuration-driven dynamic mappings with hardcoded fallback
    /// Supports vendor-specific HL7 variations through configurable field paths
    /// </summary>
    private async Task<List<MarkerData>> ExtractMarkersFromOBXAsync(
        HL7Message message,
        HL7Configuration? configuration,
        CancellationToken cancellationToken = default)
    {
        var markers = new List<MarkerData>();

        var obxSegments = message.Segments
            .Where(s => s.SegmentType == "OBX")
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        if (!obxSegments.Any())
        {
            _logger.LogDebug("[OBX EXTRACTION] No OBX segments found in message");
            return markers;
        }

        _logger.LogInformation("[OBX EXTRACTION] Processing {Count} OBX segments with {Mode} mode", 
            obxSegments.Count, 
            configuration != null ? "DYNAMIC MAPPING" : "HARDCODED FALLBACK");

        // If configuration is available, parse message and load ALL field mappings
        NHapi.Base.Model.IMessage? parsedMessage = null;
        List<HL7FieldMapping>? effectiveMappings = null;

        if (configuration != null)
        {
            parsedMessage = await ParseHL7MessageForMappingAsync(message);

            // Load ALL effective mappings for this configuration once
            // This allows user to map ANY field to ANY property name without code changes
            effectiveMappings = await _fieldMappingService.GetEffectiveMappingsAsync(
                configuration.Id, 
                null); // No disease filter at OBX level

            _logger.LogInformation("[OBX EXTRACTION] Loaded {Count} effective field mappings for configuration",
                effectiveMappings?.Count ?? 0);
        }

        // Use indexed loop to track OBX segment repetition for proper field mapping
        for (int obxIndex = 0; obxIndex < obxSegments.Count; obxIndex++)
        {
            var segment = obxSegments[obxIndex];
            var marker = new MarkerData();
            var fields = segment.RawSegment.Split('|');

            // Log raw OBX segment for debugging
            _logger.LogInformation("[OBX RAW] ═══ Processing OBX[{Index}] ═══", obxIndex);
            _logger.LogInformation("[OBX RAW] Full segment: {Segment}", segment.RawSegment);
            _logger.LogInformation("[OBX RAW] OBX-3 (field[3]): '{Value}'", fields.Length > 3 ? fields[3] : "MISSING");
            _logger.LogInformation("[OBX RAW] OBX-17 (field[17]): '{Value}'", fields.Length > 17 ? fields[17] : "MISSING");

            if (fields.Length < 5)
            {
                _logger.LogWarning("[OBX EXTRACTION] Skipping malformed OBX segment (< 5 fields)");
                continue;
            }

            // OBX-3: Observation Identifier (Test Code/Name)
            // Use field-path-based extraction - respects user configuration for ANY field mapping
            marker.TestCode = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-3.1",
                obxIndex,
                () => ExtractOBX3TestCode(fields),
                cancellationToken);

            marker.TestName = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-3.2",
                obxIndex,
                () => ExtractOBX3TestName(fields),
                cancellationToken);

            _logger.LogInformation("[OBX EXTRACTION] OBX[{Index}] - Extracted TestName: '{TestName}'", 
                obxIndex, marker.TestName ?? "NULL");

            // OBX-5: Observation Value (Result)
            var rawValue = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-5",
                obxIndex,
                () => ExtractOBX5Value(fields),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(rawValue))
            {
                // Parse result as numeric or qualitative
                if (decimal.TryParse(rawValue, out decimal numericValue))
                {
                    marker.QuantitativeValue = numericValue;
                }
                else
                {
                    // Parse coded/structured result: code^text^coding_system
                    var components = rawValue.Split('^');
                    if (components.Length > 1 && !string.IsNullOrWhiteSpace(components[1]))
                    {
                        marker.QualitativeResult = components[1].Trim();
                    }
                    else if (components.Length > 0)
                    {
                        marker.QualitativeResult = components[0].Trim();
                    }
                    else
                    {
                        marker.QualitativeResult = rawValue;
                    }

                    if (components.Length > 1)
                    {
                        _logger.LogDebug("[OBX-5 PARSE] Raw: '{Raw}' → Display: '{Display}'", 
                            rawValue, marker.QualitativeResult);
                    }
                }
            }

            // OBX-6: Units
            marker.Units = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-6",
                obxIndex,
                () => fields.Length > 6 ? fields[6] : null,
                cancellationToken);

            // OBX-7: Reference Range
            marker.ReferenceRange = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-7",
                obxIndex,
                () => fields.Length > 7 ? fields[7] : null,
                cancellationToken);

            // OBX-8: Abnormal Flags
            marker.AbnormalFlag = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-8",
                obxIndex,
                () => fields.Length > 8 ? fields[8] : null,
                cancellationToken);

            // OBX-11: Result Status
            marker.ResultStatus = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-11",
                obxIndex,
                () => fields.Length > 11 ? fields[11] : null,
                cancellationToken);

            // OBX-17: Observation Method (Test Method)
            var methodField = await ExtractValueByFieldPathAsync(
                parsedMessage,
                effectiveMappings,
                "OBX",
                "OBX-17",
                obxIndex,
                () => fields.Length > 17 ? fields[17] : null,
                cancellationToken);

            _logger.LogInformation("[OBX EXTRACTION] OBX[{Index}] - methodField from OBX-17: '{MethodField}'",
                obxIndex, methodField ?? "NULL");

            if (!string.IsNullOrWhiteSpace(methodField))
            {
                var methodParts = methodField.Split('^');
                marker.TestMethodCode = methodParts.Length > 0 ? methodParts[0] : null;
                marker.TestMethodText = methodParts.Length > 1 ? methodParts[1] : null;
                marker.TestMethodCodingSystem = methodParts.Length > 2 ? methodParts[2] : null;
                _logger.LogInformation("[OBX EXTRACTION] OBX[{Index}] - Parsed TestMethodText: '{TestMethodText}'",
                    obxIndex, marker.TestMethodText);
            }
            else
            {
                // FALLBACK: If OBX-17 is empty, use this marker's OBX-3.2 (test name) as the test method
                // This ensures each marker gets its own test method instead of reusing the first marker's value
                marker.TestMethodText = marker.TestName;
                _logger.LogInformation("[OBX EXTRACTION] OBX[{Index}]-17 empty, using OBX[{Index}]-3.2 as test method: '{TestName}'", 
                    obxIndex, obxIndex, marker.TestName);
            }

            markers.Add(marker);
        }

        _logger.LogInformation("[OBX EXTRACTION] Extracted {Count} markers", markers.Count);
        return markers;
    }

    /// <summary>
    /// Extract a mapped value from OBX with configuration-driven dynamic mapping and hardcoded fallback
    /// This helper method centralizes the dynamic mapping pattern for all OBX fields
    /// </summary>
    /// <param name="obxRepetition">Zero-based index of the OBX segment being processed (for multi-OBX messages)</param>
    private async Task<string?> ExtractMappedValueFromOBXAsync(
        NHapi.Base.Model.IMessage? parsedMessage,
        string rawSegment,
        HL7Configuration? configuration,
        Guid? diseaseId,
        string targetEntity,
        string targetProperty,
        int obxRepetition,
        Func<string?> fallbackExtractor,
        CancellationToken cancellationToken = default)
    {
        // Try configuration-driven dynamic mapping first
        if (configuration != null && parsedMessage != null)
        {
            try
            {
                // Get the mapping configuration
                var mapping = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == configuration.Id &&
                               m.TargetEntity == targetEntity &&
                               m.TargetProperty == targetProperty &&
                               m.IsActive)
                    .OrderByDescending(m => m.DiseaseId == diseaseId ? 1 : 0)
                    .ThenByDescending(m => m.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (mapping != null)
                {
                    _logger.LogInformation(
                        "[OBX DYNAMIC] Found mapping for {Entity}.{Property} - FieldPath: {Path}, Processing OBX[{Repetition}]",
                        targetEntity, targetProperty, mapping.FieldPath, obxRepetition);

                    // Use the repetition-aware extraction method to get the value from the correct OBX segment
                    var mappedValue = await _fieldMappingService.ExtractValueFromSegmentRepetitionAsync(
                        parsedMessage,
                        mapping,
                        obxRepetition);

                    _logger.LogInformation(
                        "[OBX DYNAMIC] Mapped value for {Entity}.{Property} (OBX[{Repetition}]): '{Value}'",
                        targetEntity, targetProperty, obxRepetition, mappedValue ?? "NULL");

                    if (mappedValue != null)
                    {
                        return mappedValue;
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "[OBX DYNAMIC] No mapping found for {Entity}.{Property}, will use fallback extraction",
                        targetEntity, targetProperty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[OBX DYNAMIC] Failed to use configuration mapping for {Entity}.{Property} (OBX[{Repetition}]), falling back to hardcoded extraction",
                    targetEntity, targetProperty, obxRepetition);
            }
        }

        // Try raw segment mapping as a secondary fallback for vendor-specific messages
        if (configuration != null)
        {
            try
            {
                var rawMappedValue = await _fieldMappingService.GetMappedValueFromRawSegmentAsync(
                    rawSegment,
                    configuration.Id,
                    diseaseId,
                    targetEntity,
                    targetProperty);

                if (rawMappedValue != null)
                {
                    _logger.LogDebug(
                        "[OBX RAW FALLBACK] Used raw segment mapping for {Entity}.{Property}: {Value}",
                        targetEntity, targetProperty, rawMappedValue);
                    return rawMappedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex,
                    "[OBX RAW FALLBACK] Raw segment mapping failed for {Entity}.{Property}",
                    targetEntity, targetProperty);
            }
        }

        // Final fallback to hardcoded extraction
        var fallbackValue = fallbackExtractor();
        if (fallbackValue != null)
        {
            _logger.LogDebug(
                "[OBX HARDCODED] Used fallback extraction for {Entity}.{Property}: {Value}",
                targetEntity, targetProperty, fallbackValue);
        }

        return fallbackValue;
    }

    // Hardcoded extraction helpers for OBX fields
    private string? ExtractOBX3TestCode(string[] fields)
    {
        if (fields.Length > 3)
        {
            var identifier = fields[3];
            var parts = identifier.Split('^');
            return parts.Length > 0 ? parts[0] : null;
        }
        return null;
    }

    private string? ExtractOBX3TestName(string[] fields)
    {
        if (fields.Length > 3)
        {
            var identifier = fields[3];
            var parts = identifier.Split('^');
            return parts.Length > 1 ? parts[1] : null;
        }
        return null;
    }

    private string? ExtractOBX5Value(string[] fields)
    {
        return fields.Length > 5 ? fields[5] : null;
    }

    /// <summary>
    /// Extract value from HL7 using field-path-based mapping (fully dynamic approach)
    /// Queries by segment type + field path (e.g., OBX-3.2) instead of hardcoded entity/property names
    /// Falls back to hardcoded extraction if no mapping configured
    /// </summary>
    private async Task<string?> ExtractValueByFieldPathAsync(
        NHapi.Base.Model.IMessage? parsedMessage,
        List<HL7FieldMapping>? effectiveMappings,
        string segmentType,
        string fieldPath,
        int repetition,
        Func<string?> fallbackExtractor,
        CancellationToken cancellationToken = default)
    {
        // Try to find a user-configured mapping for this specific field path
        if (effectiveMappings != null && parsedMessage != null)
        {
            var mapping = effectiveMappings.FirstOrDefault(m => 
                m.SegmentType == segmentType && 
                m.FieldPath == fieldPath &&
                m.IsActive);

            if (mapping != null)
            {
                _logger.LogInformation(
                    "[FIELD PATH] Found mapping {Segment}-{Path} → {Entity}.{Property} (rep {Rep})",
                    segmentType, fieldPath, mapping.TargetEntity, mapping.TargetProperty, repetition);

                try
                {
                    var mappedValue = await _fieldMappingService.ExtractValueFromSegmentRepetitionAsync(
                        parsedMessage,
                        mapping,
                        repetition);

                    _logger.LogInformation(
                        "[FIELD PATH] Extracted {Segment}-{Path}[{Rep}]: '{Value}'",
                        segmentType, fieldPath, repetition, mappedValue ?? "NULL");

                    if (!string.IsNullOrWhiteSpace(mappedValue))
                    {
                        return mappedValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[FIELD PATH] Extract failed for {Segment}-{Path}[{Rep}], using fallback",
                        segmentType, fieldPath, repetition);
                }
            }
            else
            {
                _logger.LogDebug(
                    "[FIELD PATH] No mapping configured for {Segment}-{Path}, using hardcoded fallback",
                    segmentType, fieldPath);
            }
        }

        // Fallback to hardcoded extraction
        var fallbackValue = fallbackExtractor();
        if (fallbackValue != null)
        {
            _logger.LogDebug(
                "[FIELD PATH FALLBACK] Hardcoded extraction for {Segment}-{Path}[{Rep}]: '{Value}'",
                segmentType, fieldPath, repetition, fallbackValue);
        }

        return fallbackValue;
    }

    /// <summary>
    /// DEPRECATED: Old synchronous OBX extraction - replaced by ExtractMarkersFromOBXAsync
    /// Kept for backward compatibility with non-configuration-driven code paths
    /// </summary>
    [Obsolete("Use ExtractMarkersFromOBXAsync instead for configuration-driven dynamic mapping support")]
    private List<MarkerData> ExtractMarkersFromOBX(HL7Message message)
    {
        var markers = new List<MarkerData>();

        var obxSegments = message.Segments
            .Where(s => s.SegmentType == "OBX")
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        foreach (var segment in obxSegments)
        {
            var fields = segment.RawSegment.Split('|');
            if (fields.Length < 5)
                continue;

            var marker = new MarkerData();

            // OBX-3: Observation Identifier
            if (fields.Length > 3)
            {
                var identifier = fields[3];
                var parts = identifier.Split('^');
                marker.TestCode = parts.Length > 0 ? parts[0] : "";
                marker.TestName = parts.Length > 1 ? parts[1] : "";
            }

            // OBX-5: Observation Value
            if (fields.Length > 5)
            {
                var value = fields[5];

                // Try to parse as numeric first
                if (decimal.TryParse(value, out decimal numericValue))
                {
                    marker.QuantitativeValue = numericValue;
                }
                else
                {
                    // Parse coded/structured result: code^text^coding_system
                    // Example: "Salmonella typhi^Salmonella typhi^SCT"
                    // We want to store the display text (component 2), or component 1 if no text
                    var components = value.Split('^');

                    if (components.Length > 1 && !string.IsNullOrWhiteSpace(components[1]))
                    {
                        // Use display text (second component)
                        marker.QualitativeResult = components[1].Trim();
                    }
                    else if (components.Length > 0)
                    {
                        // Use first component (code) if no display text
                        marker.QualitativeResult = components[0].Trim();
                    }
                    else
                    {
                        // Fallback to raw value if parsing fails
                        marker.QualitativeResult = value;
                    }

                    // Log the parsing for debugging
                    if (components.Length > 1)
                    {
                        _logger.LogDebug("[OBX-5 PARSE] Raw: '{Raw}' → Display: '{Display}'", 
                            value, marker.QualitativeResult);
                    }
                }
            }

            // OBX-6: Units
            if (fields.Length > 6)
            {
                marker.Units = fields[6];
            }

            // OBX-7: Reference Range
            if (fields.Length > 7)
            {
                marker.ReferenceRange = fields[7];
            }

            // OBX-8: Abnormal Flags
            if (fields.Length > 8)
            {
                marker.AbnormalFlag = fields[8];
            }

            // OBX-11: Result Status
            if (fields.Length > 11)
            {
                marker.ResultStatus = fields[11];
            }

            // OBX-17: Observation Method (Test Method)
            if (fields.Length > 17)
            {
                var methodField = fields[17];
                if (!string.IsNullOrWhiteSpace(methodField))
                {
                    // Parse: code^text^coding_system^alt_code^alt_text^alt_coding_system
                    var methodParts = methodField.Split('^');
                    marker.TestMethodCode = methodParts.Length > 0 ? methodParts[0] : null;
                    marker.TestMethodText = methodParts.Length > 1 ? methodParts[1] : null;
                    marker.TestMethodCodingSystem = methodParts.Length > 2 ? methodParts[2] : null;
                }
            }

            markers.Add(marker);
        }

        return markers;
    }

    private async Task<LabResultMarker> CreateLabResultMarkerAsync(
        MarkerData data,
        string? specimenCode,
        string? specimenText,
        string? specimenSystem,
        string? testMethodCode,
        string? testMethodText,
        string? testMethodSystem,
        CancellationToken cancellationToken)
    {
        _logger.LogError("[🔍 CreateLabResultMarkerAsync ENTERED] TestCode='{TC}'", data.TestCode ?? "NULL");

        _logger.LogError("[🔍 MARKER DATA DEBUG] TestCode='{TC}', TestName='{TN}', QualitativeResult='{QR}', QuantValue={QV}, AbnormalFlag='{AF}'",
            data.TestCode ?? "NULL", data.TestName ?? "NULL", data.QualitativeResult ?? "NULL", data.QuantitativeValue, data.AbnormalFlag ?? "NULL");

        // Use centralized resolution service to resolve all four key fields
        var resolution = await _markerResolutionService.ResolveMarkerFieldsAsync(
            testCode: data.TestCode,
            testName: data.TestName,
            qualitativeResult: data.QualitativeResult,
            quantitativeValue: data.QuantitativeValue,
            abnormalFlag: data.AbnormalFlag,
            specimenCode: specimenCode,
            specimenText: specimenText,
            specimenCodingSystem: specimenSystem,
            testMethodCode: testMethodCode ?? data.TestMethodCode,
            testMethodText: testMethodText ?? data.TestMethodText,
            testMethodCodingSystem: testMethodSystem ?? data.TestMethodCodingSystem,
            enableTextSearchFallback: true,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "[MARKER RESOLUTION] TestCode={TestCode}, PathogenId={PathogenId}, SpecimenTypeId={SpecimenTypeId}, TestMethodId={TestMethodId}, TestResultId={TestResultId}",
            data.TestCode,
            resolution.PathogenId,
            resolution.SpecimenTypeId,
            resolution.TestMethodId,
            resolution.TestResultId);

        // Note: SpecimenTypeId is stored at LabResult level, not marker level
        var marker = new LabResultMarker
        {
            PathogenId = resolution.PathogenId,
            TestMethodId = resolution.TestMethodId,
            TestResultId = resolution.TestResultId,
            TestCode = data.TestCode,
            QualitativeResultText = data.QualitativeResult,
            QuantitativeValue = resolution.QuantitativeValue ?? data.QuantitativeValue,
            QuantitativeUnit = data.Units,
            InterpretationFlag = data.AbnormalFlag,
            LOINCCode = data.TestCode,
            ResultStatus = data.ResultStatus ?? "F",
            Notes = data.TestName
        };

        return marker;
    }

    /// <summary>
    /// Universal helper to extract coded value (code^text^coding_system) from any HL7 field path.
    /// Supports paths like "OBX-17", "OBR-4", "OBX-3.2", "PID-5.1", etc.
    /// </summary>
    /// <param name="message">HL7 message containing segments</param>
    /// <param name="fieldPath">Field path in format "SEGMENT-FIELD" or "SEGMENT-FIELD.COMPONENT" (e.g., "OBX-17", "OBR-4.2")</param>
    /// <param name="logContext">Context name for logging (e.g., "TEST METHOD", "SPECIMEN")</param>
    /// <returns>Tuple of (code, text, system) extracted from the field</returns>
    private (string? code, string? text, string? system) ExtractCodedValue(HL7Message message, string fieldPath, string logContext)
    {
        try
        {
            var pathParts = fieldPath.Split('-');
            if (pathParts.Length != 2)
            {
                _logger.LogWarning("[{Context}] Invalid field path format: {Path}", logContext, fieldPath);
                return (null, null, null);
            }

            var segmentType = pathParts[0];
            var fieldSpec = pathParts[1];

            // Parse field number and optional component (e.g., "4" or "3.2")
            var fieldParts = fieldSpec.Split('.');
            if (!int.TryParse(fieldParts[0], out int fieldNumber))
            {
                _logger.LogWarning("[{Context}] Invalid field number in path: {Path}", logContext, fieldPath);
                return (null, null, null);
            }

            int? componentNumber = null;
            if (fieldParts.Length > 1 && int.TryParse(fieldParts[1], out int comp))
            {
                componentNumber = comp;
            }

            // Find the segment
            var segment = message.Segments.FirstOrDefault(s => s.SegmentType == segmentType);
            if (segment == null)
            {
                _logger.LogDebug("[{Context}] Segment {Segment} not found in message", logContext, segmentType);
                return (null, null, null);
            }

            var fields = segment.RawSegment.Split('|');
            int fieldIndex = fieldNumber; // HL7 field numbering accounts for segment ID as field 0

            if (fields.Length <= fieldIndex || string.IsNullOrWhiteSpace(fields[fieldIndex]))
            {
                _logger.LogDebug("[{Context}] Field {Path} not found or empty", logContext, fieldPath);
                return (null, null, null);
            }

            var fieldValue = fields[fieldIndex];

            // If a specific component is requested (e.g., OBX-3.2)
            if (componentNumber.HasValue)
            {
                var components = fieldValue.Split('^');
                int compIndex = componentNumber.Value - 1; // Components are 1-based

                if (components.Length > compIndex && !string.IsNullOrWhiteSpace(components[compIndex]))
                {
                    // Return just the text component (no code/system for subcomponents)
                    return (null, components[compIndex], null);
                }
                else
                {
                    _logger.LogDebug("[{Context}] Component {Component} not found in field {Path}", 
                        logContext, componentNumber.Value, fieldPath);
                    return (null, null, null);
                }
            }

            // Parse full coded value: code^text^coding_system
            var parts = fieldValue.Split('^');

            var code = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null;
            var text = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : 
                      (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : null);
            var system = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null;

            _logger.LogDebug("[{Context}] Extracted from {Path}: Code='{Code}', Text='{Text}', System='{System}'",
                logContext, fieldPath, code ?? "NULL", text ?? "NULL", system ?? "NULL");

            return (code, text, system);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Context}] Error extracting value from field path {Path}", logContext, fieldPath);
            return (null, null, null);
        }
    }

    private DateTime? ParseHL7Date(string? hl7Date)
    {
        if (string.IsNullOrWhiteSpace(hl7Date))
            return null;

        try
        {
            // HL7 format: YYYYMMDD or YYYYMMDDHHMMSS
            if (hl7Date.Length >= 8)
            {
                var year = int.Parse(hl7Date.Substring(0, 4));
                var month = int.Parse(hl7Date.Substring(4, 2));
                var day = int.Parse(hl7Date.Substring(6, 2));

                var hour = hl7Date.Length >= 10 ? int.Parse(hl7Date.Substring(8, 2)) : 0;
                var minute = hl7Date.Length >= 12 ? int.Parse(hl7Date.Substring(10, 2)) : 0;
                var second = hl7Date.Length >= 14 ? int.Parse(hl7Date.Substring(12, 2)) : 0;

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse HL7 date: {Date}", hl7Date);
        }

        return null;
    }

    #endregion

    #region Helper Classes

    private class PatientData
    {
        public string? MRN { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Sex { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
    }

    private class MarkerData
    {
        public string TestCode { get; set; } = string.Empty;
        public string? TestName { get; set; }
        public string? QualitativeResult { get; set; }
        public decimal? QuantitativeValue { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? AbnormalFlag { get; set; }
        public string? ResultStatus { get; set; }
        public string? TestMethodCode { get; set; }
        public string? TestMethodText { get; set; }
        public string? TestMethodCodingSystem { get; set; }
    }

    #endregion

    #region Staging-Based Processing (New Implementation)

    /// <summary>
    /// NEW: Build complete staging structure WITHOUT database commits
    /// This is the entry point for the new staging-based processing logic
    /// </summary>
    private async Task<HL7ProcessingStage> BuildStagingStructureAsync(
        HL7Message message,
        HL7Configuration? configuration,
        CancellationToken cancellationToken)
    {
        var stage = new HL7ProcessingStage { HL7Message = message };
        var stagingLog = new List<string>();

        try
        {
            _logger.LogInformation("[STAGING] Building staging structure for message {MessageControlId}", message.MessageControlId);
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== STAGING WORKFLOW START ==========");
            stagingLog.Add($"Message Control ID: {message.MessageControlId}");
            stagingLog.Add($"Message Type: {message.MessageType}");
            stagingLog.Add($"Message DateTime: {message.MessageDateTime}");
            stagingLog.Add("");

            // STEP 1: Match/stage patient
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 1: PATIENT STAGING ---");
            stagingLog.Add($"Configuration: Strategy={configuration?.PatientMatchingStrategy ?? PatientMatchingStrategy.StrictMatch}, AutoCreate={configuration?.AutoCreatePatients ?? true}");

            stage.StagedPatient = await StagePatientAsync(message, configuration, cancellationToken);

            if (stage.StagedPatient == null)
            {
                stagingLog.Add($"❌ FAILED: Patient staging failed - no PID segment or matching error");
                stage.Decision = ProcessingDecision.ManualReview;
                stage.ManualReviewReason = "Failed to match or stage patient";
                stage.Errors.Add("Patient staging failed");
                stage.Warnings.AddRange(stagingLog);
                return stage;
            }

            var patientInfo = stage.StagedPatient.IsNew 
                ? $"NEW patient to be created: {stage.StagedPatient.FirstName} {stage.StagedPatient.LastName}, DOB: {stage.StagedPatient.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}, MRN: {stage.StagedPatient.MRN ?? "N/A"}"
                : $"EXISTING patient matched: ID={stage.StagedPatient.ExistingPatientId}, {stage.StagedPatient.FirstName} {stage.StagedPatient.LastName}";

            stagingLog.Add($"✅ SUCCESS: {patientInfo}");
            stagingLog.Add("");
            stage.Warnings.Add($"[STAGING] Patient: {(stage.StagedPatient.IsNew ? "NEW" : "EXISTING")} - {stage.StagedPatient.FirstName} {stage.StagedPatient.LastName}");

            // STEP 2: Match/stage organizations
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 2: ORGANIZATION STAGING ---");

            var labName = ExtractLaboratoryName(message, configuration);
            stagingLog.Add($"Laboratory name extracted: '{labName ?? "NULL"}'");
            stagingLog.Add($"Configuration: AutoCreate={configuration?.AutoCreateOrganizations ?? false}, DefaultLabId={configuration?.DefaultLaboratoryId}");

            if (!string.IsNullOrWhiteSpace(labName))
            {
                stagingLog.Add($"  Laboratory name length: {labName.Length} characters");
                stagingLog.Add($"  Normalized: '{NormalizeOrganizationName(labName)}'");
            }

            stage.StagedLaboratory = await StageOrganizationAsync(
                labName, 
                "Laboratory", 
                configuration?.AutoCreateOrganizations ?? false,
                configuration?.DefaultLaboratoryId,
                cancellationToken);

            if (stage.StagedLaboratory == null)
            {
                stagingLog.Add($"❌ FAILED: No laboratory found and auto-create disabled");
                stage.Errors.Add("No laboratory found and auto-create disabled");
                stage.Decision = ProcessingDecision.ManualReview;
                stage.ManualReviewReason = "Laboratory not found";
                stage.Warnings.AddRange(stagingLog);
                return stage;
            }

            var labInfo = stage.StagedLaboratory.IsNew
                ? $"NEW laboratory to be created: {stage.StagedLaboratory.Name}"
                : $"EXISTING laboratory matched: ID={stage.StagedLaboratory.ExistingOrganizationId}, Name={stage.StagedLaboratory.Name}";

            stagingLog.Add($"✅ SUCCESS: {labInfo}");
            stage.Warnings.Add($"[STAGING] Laboratory: {(stage.StagedLaboratory.IsNew ? "NEW" : "EXISTING")} - {stage.StagedLaboratory.Name}");

            stagingLog.Add($"[ORDERING PROVIDER] Calling ExtractOrderingProviderName...");
            _logger.LogInformation("[STAGING] About to call ExtractOrderingProviderName for message {MessageControlId}", message.MessageControlId);

            var providerName = ExtractOrderingProviderName(message, configuration);

            stagingLog.Add($"Ordering provider name extracted: '{providerName ?? "NULL"}'");
            _logger.LogInformation("[STAGING] ExtractOrderingProviderName returned: '{ProviderName}'", providerName ?? "NULL");
            if (!string.IsNullOrWhiteSpace(providerName))
            {
                stagingLog.Add($"  Provider name length: {providerName.Length} characters");
                stagingLog.Add($"  Normalized: '{NormalizeOrganizationName(providerName)}'");

                // Try multiple OrganizationType names in order of preference
                // Ordering providers are typically individual practitioners, not organizations
                string[] providerTypeNames = { "Medical Practitioner", "Healthcare Provider", "Provider", "Clinic" };

                foreach (var typeName in providerTypeNames)
                {
                    stagingLog.Add($"  Trying OrganizationType: '{typeName}'");

                    stage.StagedOrderingProvider = await StageOrganizationAsync(
                        providerName,
                        typeName,
                        configuration?.AutoCreateOrganizations ?? false,
                        null,
                        cancellationToken);

                    if (stage.StagedOrderingProvider != null)
                    {
                        stagingLog.Add($"  ✅ SUCCESS: Using OrganizationType '{typeName}'");
                        break;
                    }
                    else
                    {
                        stagingLog.Add($"  ⚠️ OrganizationType '{typeName}' not found in database");
                    }
                }

                // If none of the type names worked, list available types for debugging
                if (stage.StagedOrderingProvider == null)
                {
                    var availableTypes = await _context.OrganizationTypes
                        .IgnoreQueryFilters()
                        .Select(ot => ot.Name)
                        .ToListAsync(cancellationToken);

                    if (availableTypes.Any())
                    {
                        stagingLog.Add($"  📋 Available OrganizationTypes in database: {string.Join(", ", availableTypes)}");
                        stagingLog.Add($"  💡 Tip: Add one of these types to the database or update the code to use an existing type");
                    }
                    else
                    {
                        stagingLog.Add($"  ❌ CRITICAL: No OrganizationTypes found in database at all!");
                    }
                }
            }
            else
            {
                stagingLog.Add($"  ⚠️ No ordering provider extracted from OBR segment");
            }

            if (stage.StagedOrderingProvider != null)
            {
                var providerInfo = stage.StagedOrderingProvider.IsNew
                    ? $"NEW ordering provider to be created: {stage.StagedOrderingProvider.Name} (Type: {stage.StagedOrderingProvider.OrganizationTypeName})"
                    : $"EXISTING ordering provider matched: ID={stage.StagedOrderingProvider.ExistingOrganizationId}, Name={stage.StagedOrderingProvider.Name} (Type: {stage.StagedOrderingProvider.OrganizationTypeName})";
                stagingLog.Add($"✅ SUCCESS: {providerInfo}");
            }
            else
            {
                stagingLog.Add($"ℹ️ No ordering provider found, staged, or OrganizationType not available");
                stagingLog.Add($"  Note: Ordering provider is optional and can be added manually later");
            }

            stagingLog.Add("");

            // STEP 3: Stage lab result with duplicate detection
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 3: LAB RESULT STAGING & DUPLICATE DETECTION ---");

            var (stagedLabResult, duplicateCheck) = await StageLabResultAsync(
                message,
                stage.StagedPatient,
                stage.StagedLaboratory,
                configuration,
                cancellationToken);

            stage.StagedLabResult = stagedLabResult;
            stage.DuplicateCheck = duplicateCheck;

            if (stagedLabResult != null)
            {
                stagingLog.Add($"Accession Number: {stagedLabResult.AccessionNumber}");
                stagingLog.Add($"Specimen Collection Date: {stagedLabResult.SpecimenCollectionDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A"}");
                stagingLog.Add($"Result Date: {stagedLabResult.ResultDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A"}");
                stagingLog.Add($"Markers extracted: {stagedLabResult.Markers.Count}");
                foreach (var marker in stagedLabResult.Markers)
                {
                    stagingLog.Add($"  • TestCode: {marker.TestCode}, Qualitative: {marker.QualitativeResult ?? "N/A"}, Quantitative: {marker.QuantitativeValue?.ToString() ?? "N/A"} {marker.Units ?? ""}");
                }
            }

            // Handle duplicate scenarios
            if (duplicateCheck.IsDuplicate)
            {
                stagingLog.Add($"🔍 DUPLICATE CHECK: Match found!");
                stagingLog.Add($"Reason: {duplicateCheck.Reason}");

                if (duplicateCheck.IsIdentical)
                {
                    stagingLog.Add($"✅ DECISION: Identical duplicate - no processing needed");
                    stage.Decision = ProcessingDecision.Duplicate;
                    stage.Warnings.Add("✅ Identical duplicate detected - no action required");
                    stage.Warnings.AddRange(stagingLog);
                    return stage;
                }

                if (duplicateCheck.IsPatientMismatch)
                {
                    stagingLog.Add($"❌ CRITICAL: Patient mismatch detected!");
                    stagingLog.Add($"Same accession belongs to different patient - MANUAL REVIEW REQUIRED");
                    stage.Decision = ProcessingDecision.ManualReview;
                    stage.ManualReviewReason = "Same accession number but different patient details";
                    stage.Errors.Add($"Accession {stagedLabResult?.AccessionNumber} already exists for a different patient");
                    stage.Warnings.AddRange(stagingLog);
                    return stage;
                }

                // Different results for same accession → create NEW lab result
                stagingLog.Add($"✅ DECISION: Create NEW lab result (amended/corrected report for same accession)");
                stagingLog.Add($"Rationale: Preserves audit trail of lab amendments/corrections");
                stage.Warnings.Add("⚠️ Creating new lab result for amended/corrected report with same accession");

                // Mark as new lab result even though accession exists
                // This preserves the history of lab corrections/amendments
                stage.StagedLabResult.IsNew = true;
            }
            else
            {
                stagingLog.Add($"🔍 DUPLICATE CHECK: No duplicates found - new lab result");
            }

            stagingLog.Add("");

            if (stage.StagedLabResult == null || !stage.StagedLabResult.Markers.Any())
            {
                stagingLog.Add($"❌ FAILED: No markers extracted from HL7 message");
                stage.Decision = ProcessingDecision.NoSurveillance;
                stage.Warnings.Add("No markers extracted from HL7 message");
                stage.Warnings.AddRange(stagingLog);
                return stage;
            }

            stage.Warnings.Add($"[STAGING] Lab Result: {(stage.StagedLabResult.IsNew ? "NEW" : "UPDATE")} - {stage.StagedLabResult.Markers.Count} markers");

            // STEP 4: RESOLVE ALL HL7 FIELDS (Specimen, TestName, Pathogen, Result)
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 4: RESOLVE HL7 FIELDS ---");
            stagingLog.Add($"Resolving 4 key fields for {stage.StagedLabResult.Markers.Count} markers:");
            stagingLog.Add($"  1. Specimen Type (from OBR-15 or SPM-4)");
            stagingLog.Add($"  2. Test Method (from OBR-4 Universal Service Identifier)");
            stagingLog.Add($"  3. Pathogen/Biomarker (OBX-3.1 LOINC or OBX-3.2 Text)");
            stagingLog.Add($"  4. Result Value (OBX-5)");
            stagingLog.Add($"Strategy: LOINC/SNOMED exact match first, then text matching fallback");
            stagingLog.Add("");

            // Get all diseases and their text matching configurations for universal field resolution
            var allDiseaseConfigs = await _context.Set<Sentinel.Models.HL7.DiseaseHL7MatchingConfig>()
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            stagingLog.Add($"Loaded {allDiseaseConfigs.Count} disease text-matching configurations");

            // Check if ANY disease has text matching enabled (will use most permissive)
            bool anySpecimenTextEnabled = allDiseaseConfigs.Any(c => c.SpecimenType_UseTextFallback);
            bool anyPathogenTextEnabled = allDiseaseConfigs.Any(c => c.Pathogen_UseTextFallback);
            bool anyTestMethodTextEnabled = allDiseaseConfigs.Any(c => c.TestMethod_UseTextFallback);
            bool anyTestResultTextEnabled = allDiseaseConfigs.Any(c => c.TestResult_UseTextFallback);

            stagingLog.Add($"Text matching availability (across all diseases):");
            stagingLog.Add($"  - Specimen: {anySpecimenTextEnabled}");
            stagingLog.Add($"  - Pathogen: {anyPathogenTextEnabled}");
            stagingLog.Add($"  - Test Method: {anyTestMethodTextEnabled}");
            stagingLog.Add($"  - Test Result: {anyTestResultTextEnabled}");
            stagingLog.Add("");

            // Resolve all fields universally (using most permissive text matching settings)
            await ResolveAllHL7FieldsAsync(
                stage.StagedLabResult,
                anySpecimenTextEnabled,
                anyPathogenTextEnabled,
                anyTestMethodTextEnabled,
                stagingLog,
                cancellationToken);

            stagingLog.Add("");
            stagingLog.Add($"Field resolution summary:");
            stagingLog.Add($"  Specimen: {(stage.StagedLabResult.ResolvedSpecimenTypeId.HasValue ? "RESOLVED" : "NOT RESOLVED")} (Method: {stage.StagedLabResult.SpecimenMatchMethod})");
            foreach (var marker in stage.StagedLabResult.Markers)
            {
                stagingLog.Add($"  Marker '{marker.TestName ?? marker.TestCode ?? "UNNAMED"}':");
                stagingLog.Add($"    - Pathogen: {(marker.ResolvedPathogenId.HasValue ? "RESOLVED" : "NOT RESOLVED")} (Method: {marker.PathogenMatchMethod})");
                stagingLog.Add($"    - Test Method: {(marker.ResolvedTestMethodId.HasValue ? "RESOLVED" : "NOT RESOLVED")} (Method: {marker.TestMethodMatchMethod})");
                stagingLog.Add($"    - Result: {(marker.ResolvedTestResultId.HasValue ? "RESOLVED" : "NOT RESOLVED")} (Value: {marker.NormalizedResultValue ?? "N/A"})");
            }
            stagingLog.Add("");

            // STEP 4.4: Load existing case markers for progressive typing
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 4.4: LOAD EXISTING CASE MARKERS FOR REFINEMENT ---");

            var existingCaseMarkers = await LoadExistingCaseMarkersForRefinementAsync(
                stage.StagedPatient,
                stage.StagedLabResult,
                stagingLog,
                cancellationToken);

            stagingLog.Add("");

            // STEP 4.5: Identify diseases using case-definition matching (supports partial matching)
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 4.5: IDENTIFY DISEASES VIA CASE DEFINITION MATCHING ---");
            stagingLog.Add($"Using case-definition matching with partial field support");

            if (existingCaseMarkers.Any())
            {
                stagingLog.Add($"✅ Combining {stage.StagedLabResult.Markers.Count} new + {existingCaseMarkers.Count} existing marker(s)");
            }
            stagingLog.Add($"");

            // Temporarily combine markers for disease identification
            var originalMarkers = stage.StagedLabResult.Markers.ToList();
            if (existingCaseMarkers.Any())
            {
                stage.StagedLabResult.Markers.AddRange(existingCaseMarkers);
            }

            stage.DiseaseMatches = await IdentifyDiseasesViaCaseDefinitionsAsync(
                stage.StagedLabResult,
                stagingLog,
                cancellationToken);

            // Restore original markers
            if (existingCaseMarkers.Any())
            {
                stage.StagedLabResult.Markers = originalMarkers;
            }

            if (!stage.DiseaseMatches.Any())
            {
                stagingLog.Add($"❌ No diseases identified from resolved pathogens");
                stagingLog.Add($"");
                stage.Warnings.Add("❌ No diseases identified from resolved pathogen/biomarker fields");
            }
            else
            {
                stagingLog.Add($"✅ SUCCESS: {stage.DiseaseMatches.Count} disease(s) identified from resolved pathogens:");
                foreach (var diseaseMatch in stage.DiseaseMatches)
                {
                    stagingLog.Add($"  • {diseaseMatch.Disease.Name}");
                    if (diseaseMatch.MatchedPathogen != null)
                    {
                        stagingLog.Add($"    Pathogen: {diseaseMatch.MatchedPathogen.Name}");
                    }
                    stagingLog.Add($"    Matched Markers: {diseaseMatch.MatchedMarkers.Count}");
                }

                stage.Warnings.Add($"[STAGING] Diseases Identified: {stage.DiseaseMatches.Count}");

                // Filter to most specific disease(s) only
                // If multiple diseases are identified (e.g., Salmonella + Salmonella Typhimurium 9),
                // keep only the most specific child disease(s) and remove parent diseases
                // This ensures multiplex results (e.g., Salmonella + Campylobacter) are kept as separate diseases
                if (stage.DiseaseMatches.Count > 1)
                {
                    stagingLog.Add("");
                    stagingLog.Add($"🔍 Multiple diseases identified - filtering to most specific:");

                    var originalCount = stage.DiseaseMatches.Count;
                    var filtered = new List<DiseaseMatch>();

                    foreach (var diseaseMatch in stage.DiseaseMatches)
                    {
                        // Check if this disease is an ancestor of any other disease in the list
                        bool isAncestorOfAnother = stage.DiseaseMatches.Any(other => 
                            other.Disease.Id != diseaseMatch.Disease.Id && 
                            IsAncestorOfDisease(diseaseMatch.Disease.Id, other.Disease));

                        if (!isAncestorOfAnother)
                        {
                            filtered.Add(diseaseMatch);
                        }
                        else
                        {
                            stagingLog.Add($"  ❌ Filtered out parent: {diseaseMatch.Disease.Name} (more specific child exists)");
                        }
                    }

                    stage.DiseaseMatches = filtered;

                    stagingLog.Add($"  Filtered from {originalCount} to {stage.DiseaseMatches.Count} most specific disease(s):");
                    foreach (var diseaseMatch in stage.DiseaseMatches)
                    {
                        stagingLog.Add($"  ✅ {diseaseMatch.Disease.Name}");
                    }

                    stage.Warnings.Add($"[STAGING] Filtered to {stage.DiseaseMatches.Count} most specific disease(s)");
                }
            }
            stagingLog.Add("");

            // STEP 4.6: Verify case definition matches
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 4.6: VERIFY CASE DEFINITION MATCHES ---");

            // Evaluate case definitions for each identified disease
            if (!stage.DiseaseMatches.Any())
            {
                stagingLog.Add($"⚠️ No diseases identified - cannot evaluate case definitions");
                stagingLog.Add($"");
                stagingLog.Add($"Decision: NoSurveillance - no diseases could be identified from resolved fields");
                stage.Decision = ProcessingDecision.NoSurveillance;
                stage.Warnings.Add("❌ No diseases identified - result marked as NoSurveillance");
                stage.Warnings.AddRange(stagingLog);
                return stage;
            }

            // Verify that each disease match has case definitions
            // (STEP 4.5 already did the matching, this is just validation)
            stagingLog.Add($"");
            stagingLog.Add($"Verifying case definition matches from STEP 4.5:");

            foreach (var diseaseMatch in stage.DiseaseMatches)
            {
                stagingLog.Add($"");
                stagingLog.Add($"Disease: {diseaseMatch.Disease.Name}");

                if (diseaseMatch.MatchedCaseDefinitions.Any())
                {
                    stagingLog.Add($"  ✅ Has {diseaseMatch.MatchedCaseDefinitions.Count} matched case definition(s):");
                    foreach (var caseDef in diseaseMatch.MatchedCaseDefinitions)
                    {
                        stagingLog.Add($"     • {caseDef.Name} → {caseDef.ConfirmationStatus?.Name ?? "Unknown Status"}");
                        stage.Warnings.Add($"[CASE DEF] {diseaseMatch.Disease.Name}: Matched '{caseDef.Name}'");
                    }
                }
                else if (diseaseMatch.MatchedCaseDefinition != null)
                {
                    stagingLog.Add($"  ✅ Has matched case definition: {diseaseMatch.MatchedCaseDefinition.Name}");
                    stagingLog.Add($"     Confirmation Status: {diseaseMatch.MatchedCaseDefinition.ConfirmationStatus?.Name ?? "Unknown"}");
                    stage.Warnings.Add($"[CASE DEF] {diseaseMatch.Disease.Name}: Matched '{diseaseMatch.MatchedCaseDefinition.Name}'");
                }
                else
                {
                    stagingLog.Add($"  ⚠️ WARNING: Disease matched but no case definition assigned");
                    stagingLog.Add($"     This indicates a logic error in STEP 4.5");
                }
            }

            stagingLog.Add($"");
            stagingLog.Add($"✅ Case definition verification complete");
            stagingLog.Add($"   Total diseases with matched case definitions: {stage.DiseaseMatches.Count}");
            stagingLog.Add($"");

            // All diseases that made it through STEP 4.5 have valid case definitions
            // Continue to next steps (existing case checking, etc.)

            stagingLog.Add($"Case definition evaluation complete. {stage.DiseaseMatches.Count} disease(es) with valid case definitions.");
            stagingLog.Add("");

            // STEP 5: For each disease, check reinfection rules and evaluate hierarchy
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 5: REINFECTION & HIERARCHY EVALUATION ---");

            var positiveMatches = stage.DiseaseMatches.Where(d => d.IsPositiveResult).ToList();
            if (!positiveMatches.Any())
            {
                stagingLog.Add($"ℹ️ No positive results - skipping case matching");
                stagingLog.Add("");
            }
            else
            {
                stagingLog.Add($"Evaluating {positiveMatches.Count} positive disease match(es) for case creation/linkage:");

                foreach (var diseaseMatch in positiveMatches)
                {
                    stagingLog.Add($"");
                    stagingLog.Add($"➤ Disease: {diseaseMatch.Disease.Name}");

                    await EvaluateReinfectionAndHierarchyAsync(
                        diseaseMatch,
                        stage.StagedPatient,
                        stage.StagedLabResult,
                        cancellationToken);

                    // Log reinfection decision
                    stagingLog.Add($"  Reinfection Decision: {diseaseMatch.ReinfectionDecision}");
                    stagingLog.Add($"  Reason: {diseaseMatch.ReinfectionReason ?? "N/A"}");

                    if (diseaseMatch.ExistingCase != null)
                    {
                        stagingLog.Add($"  Existing Case: {diseaseMatch.ExistingCase.FriendlyId} (Disease: {diseaseMatch.ExistingCase.Disease?.Name ?? "N/A"})");
                        if (diseaseMatch.ShouldRefineDiseaseOnExistingCase && diseaseMatch.RefinedDisease != null)
                        {
                            stagingLog.Add($"  ⚠️ Disease Refinement: Will update to {diseaseMatch.RefinedDisease.Name}");
                        }
                    }

                    if (diseaseMatch.MultipleActiveCasesDetected)
                    {
                        stagingLog.Add($"  ⚠️ WARNING: Multiple active cases detected - MANUAL REVIEW REQUIRED");
                    }

                    if (diseaseMatch.ShouldCreateNewCase)
                    {
                        var finalDisease = diseaseMatch.FinalDiseaseForCase?.Name ?? diseaseMatch.Disease.Name;
                        stagingLog.Add($"  ✅ ACTION: Will create new case for disease: {finalDisease}");
                    }

                    stage.Warnings.Add($"  - {diseaseMatch.Disease.Name}: {diseaseMatch.ReinfectionDecision} ({diseaseMatch.ReinfectionReason ?? "N/A"})");
                }
            }

            stagingLog.Add("");
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== STAGING COMPLETE ==========");

            // CRITICAL FIX: Check if there are any valid positive disease matches that require database commit
            // A valid match must have:
            // 1. IsPositiveResult = true
            // 2. EITHER ShouldCreateNewCase = true OR ReinfectionDecision = LinkToExisting
            // 3. At least one matched marker with a resolved pathogen ID (ensures actual disease identification)
            var validPositiveMatches = stage.DiseaseMatches
                .Where(d => d.IsPositiveResult && 
                           (d.ShouldCreateNewCase || d.ReinfectionDecision == ReinfectionDecision.LinkToExisting) &&
                           d.MatchedMarkers.Any(m => m.ResolvedPathogenId.HasValue))
                .ToList();

            if (!validPositiveMatches.Any())
            {
                // Check if there are matches without pathogen linkages
                var matchesWithoutPathogens = stage.DiseaseMatches
                    .Where(d => d.IsPositiveResult && 
                               (d.ShouldCreateNewCase || d.ReinfectionDecision == ReinfectionDecision.LinkToExisting) &&
                               !d.MatchedMarkers.Any(m => m.ResolvedPathogenId.HasValue))
                    .ToList();

                if (matchesWithoutPathogens.Any())
                {
                    // Disease matched via case definition but no markers have pathogen linkages
                    stagingLog.Add($"❌ {matchesWithoutPathogens.Count} disease match(es) found via case definition, but no markers have valid pathogen linkages");
                    foreach (var match in matchesWithoutPathogens)
                    {
                        stagingLog.Add($"  - {match.Disease.Name}: {match.MatchedMarkers.Count} marker(s), but 0 have resolved pathogens");
                    }
                    stagingLog.Add($"Decision: NoSurveillance - no markers with valid pathogen identification");
                    stage.Decision = ProcessingDecision.NoSurveillance;
                    stage.Warnings.Add($"❌ Case definition matched but no markers have pathogen linkages - no case will be created");
                    stage.Warnings.Add($"   Fix: Add pathogen records with matching LOINC codes, or configure case definition with specific pathogen criteria");
                    _logger.LogInformation("[STAGING] Disease matched but no pathogen linkages - NoSurveillance");
                }
                else
                {
                    // No valid disease matches found at all
                    stagingLog.Add($"❌ No valid positive disease matches found for case creation or linking");
                    stagingLog.Add($"Decision: NoSurveillance - no surveillance diseases identified");
                    stage.Decision = ProcessingDecision.NoSurveillance;
                    stage.Warnings.Add("❌ No surveillance diseases identified - no case will be created");
                    _logger.LogInformation("[STAGING] No valid disease matches - NoSurveillance");
                }
            }
            else
            {
                // Valid positive matches exist - determine final decision
                var hasNewCases = validPositiveMatches.Any(m => m.ShouldCreateNewCase);
                var hasLinkages = validPositiveMatches.Any(m => m.ReinfectionDecision == ReinfectionDecision.LinkToExisting);

                stagingLog.Add($"✅ {validPositiveMatches.Count} valid positive disease match(es) found with pathogen linkages");
                foreach (var match in validPositiveMatches)
                {
                    var pathogenCount = match.MatchedMarkers.Count(m => m.ResolvedPathogenId.HasValue);
                    var action = match.ShouldCreateNewCase ? "CREATE NEW CASE" : "LINK TO EXISTING";
                    stagingLog.Add($"  - {match.Disease.Name}: {pathogenCount} marker(s) with resolved pathogens [{action}]");
                }

                // Set decision based on whether we're creating new cases or just linking
                if (hasNewCases)
                {
                    stage.Decision = ProcessingDecision.CreateNewCase;
                    stagingLog.Add($"Decision: CreateNewCase - ready for database commit");
                }
                else if (hasLinkages)
                {
                    stage.Decision = ProcessingDecision.LinkToExistingCase;
                    stagingLog.Add($"Decision: LinkToExistingCase - ready for database commit");
                }

                _logger.LogInformation("[STAGING] Staging complete - ready for commit with {Count} disease(s)", validPositiveMatches.Count);
            }

            stagingLog.Add($"Final Decision: {stage.Decision}");
            stagingLog.Add($"Ready for database commit: {stage.Decision == ProcessingDecision.CreateNewCase || stage.Decision == ProcessingDecision.LinkToExistingCase}");
            stagingLog.Add("");

            // Add all logs to warnings for storage
            stage.Warnings.AddRange(stagingLog);

            return stage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error building staging structure for message {MessageControlId}", message.MessageControlId);

            stagingLog.Add("");
            stagingLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== STAGING ERROR ==========");
            stagingLog.Add($"❌ EXCEPTION: {ex.Message}");
            stagingLog.Add($"Stack Trace: {ex.StackTrace}");

            stage.Decision = ProcessingDecision.ManualReview;
            stage.ManualReviewReason = $"Staging error: {ex.Message}";
            stage.Errors.Add($"Staging failed: {ex.Message}");
            stage.Warnings.AddRange(stagingLog);

            return stage;
        }
    }

    /// <summary>
    /// STEP 1a: Match or stage patient from HL7 PID segment
    /// </summary>
    private async Task<StagedPatient?> StagePatientAsync(
        HL7Message message,
        HL7Configuration? configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract patient demographics from HL7 message
            var pidSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "PID");
            if (pidSegment == null)
            {
                _logger.LogWarning("[STAGING] No PID segment found in message {MessageControlId}", message.MessageControlId);
                return null;
            }

            var patientData = ParsePIDSegment(pidSegment.RawSegment, configuration);

            // Apply matching strategy
            var strategy = configuration?.PatientMatchingStrategy ?? PatientMatchingStrategy.StrictMatch;
            Patient? existingPatient = null;

            if (strategy == PatientMatchingStrategy.StrictMatch || strategy == PatientMatchingStrategy.IdentifierOnly)
            {
                existingPatient = await MatchByIdentifierAsync(patientData, cancellationToken);
            }
            else if (strategy == PatientMatchingStrategy.FuzzyMatch)
            {
                var matches = await MatchByDemographicsAsync(patientData, false, cancellationToken);
                existingPatient = matches.FirstOrDefault();
            }
            // CreateAlways → existingPatient remains null

            // VALIDATION: If creating a NEW patient, ensure minimum required demographic data
            // Require at least 2 out of 3: FirstName, LastName, or DateOfBirth
            if (existingPatient == null)
            {
                int fieldsPresent = 0;
                var missingFields = new List<string>();

                if (!string.IsNullOrWhiteSpace(patientData.FirstName))
                    fieldsPresent++;
                else
                    missingFields.Add("FirstName");

                if (!string.IsNullOrWhiteSpace(patientData.LastName))
                    fieldsPresent++;
                else
                    missingFields.Add("LastName");

                if (patientData.DateOfBirth.HasValue)
                    fieldsPresent++;
                else
                    missingFields.Add("DateOfBirth");

                if (fieldsPresent < 2)
                {
                    _logger.LogWarning(
                        "[STAGING] Cannot create patient from HL7 message {MessageControlId} - insufficient demographic data. " +
                        "FirstName='{FirstName}', LastName='{LastName}', DateOfBirth='{DateOfBirth}'. " +
                        "At least 2 of these 3 fields are required.",
                        message.MessageControlId,
                        patientData.FirstName ?? "NULL",
                        patientData.LastName ?? "NULL",
                        patientData.DateOfBirth?.ToString("yyyy-MM-dd") ?? "NULL");

                    // Return null to trigger manual review in BuildStagingStructureAsync
                    return null;
                }
            }

            var stagedPatient = new StagedPatient
            {
                ExistingPatient = existingPatient,
                ExistingPatientId = existingPatient?.Id,
                IsNew = existingPatient == null,
                MRN = patientData.MRN,
                FirstName = patientData.FirstName,
                LastName = patientData.LastName,
                MiddleName = patientData.MiddleName,
                DateOfBirth = patientData.DateOfBirth,
                Sex = patientData.Sex,
                Phone = patientData.Phone,
                Address = patientData.Address,
                City = patientData.City,
                State = patientData.State,
                Zip = patientData.Zip
            };

            return stagedPatient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error staging patient for message {MessageControlId}", message.MessageControlId);
            return null;
        }
    }

    /// <summary>
    /// STEP 2: Match or stage organization (laboratory or ordering provider)
    /// </summary>
    private async Task<StagedOrganization?> StageOrganizationAsync(
        string? organizationName,
        string organizationTypeName,
        bool autoCreate,
        Guid? defaultOrganizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get organization type first (needed for matching)
            var orgType = await _context.OrganizationTypes
                .IgnoreQueryFilters()  // Bypass soft delete filter (OrganizationType has no other filters)
                .FirstOrDefaultAsync(ot => ot.Name == organizationTypeName, cancellationToken);

            if (orgType == null)
            {
                _logger.LogWarning("[STAGING] Organization type '{Type}' not found in database", organizationTypeName);

                // List available types for debugging
                var availableTypes = await _context.OrganizationTypes
                    .IgnoreQueryFilters()
                    .Select(ot => ot.Name)
                    .ToListAsync(cancellationToken);
                _logger.LogWarning("[STAGING] Available organization types: {Types}", string.Join(", ", availableTypes));

                return null;
            }

            // If name provided, try to match it
            if (!string.IsNullOrWhiteSpace(organizationName))
            {
                // Try exact match - use IgnoreQueryFilters for background service
                var existing = await _context.Organizations
                    .IgnoreQueryFilters()  // Bypass soft delete filter (Organization has no other filters)
                    .FirstOrDefaultAsync(o =>
                        o.Name == organizationName &&
                        o.OrganizationTypeId == orgType.Id,
                        cancellationToken);

                if (existing != null)
                {
                    _logger.LogDebug("[STAGING] ✅ Exact match found for '{Name}' (Type: {Type})", organizationName, organizationTypeName);
                    return new StagedOrganization
                    {
                        ExistingOrganization = existing,
                        ExistingOrganizationId = existing.Id,
                        IsNew = false,
                        Name = existing.Name,
                        OrganizationTypeName = organizationTypeName
                    };
                }

                _logger.LogDebug("[STAGING] ❌ No exact match found for '{Name}' (Type: {Type}), trying fuzzy match...", organizationName, organizationTypeName);

                // Try fuzzy match
                var normalizedName = NormalizeOrganizationName(organizationName);
                var allOrgs = await _context.Organizations
                    .IgnoreQueryFilters()  // Bypass soft delete filter (Organization has no other filters)
                    .Where(o => o.OrganizationTypeId == orgType.Id)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("[STAGING] Found {Count} organizations of type '{Type}' for fuzzy matching", allOrgs.Count, organizationTypeName);

                if (allOrgs.Any())
                {
                    _logger.LogDebug("[STAGING] Existing organizations: {Names}", 
                        string.Join(", ", allOrgs.Take(10).Select(o => $"'{o.Name}' (Normalized: '{NormalizeOrganizationName(o.Name)}')")));
                }

                foreach (var org in allOrgs)
                {
                    if (NormalizeOrganizationName(org.Name) == normalizedName)
                    {
                        _logger.LogDebug("[STAGING] ✅ Fuzzy match found: '{ExistingName}' matches '{SearchName}'", org.Name, organizationName);
                        return new StagedOrganization
                        {
                            ExistingOrganization = org,
                            ExistingOrganizationId = org.Id,
                            IsNew = false,
                            Name = org.Name,
                            OrganizationTypeName = organizationTypeName
                        };
                    }
                }

                _logger.LogWarning("[STAGING] ❌ No fuzzy match found for '{Name}' (Normalized: '{Normalized}') among {Count} {Type} organizations", 
                    organizationName, normalizedName, allOrgs.Count, organizationTypeName);

                // No match found - if autoCreate enabled, stage new organization
                if (autoCreate)
                {
                    _logger.LogInformation("[STAGING] Auto-create enabled, staging new organization '{Name}'", organizationName);
                    return new StagedOrganization
                    {
                        IsNew = true,
                        Name = organizationName,
                        OrganizationTypeName = organizationTypeName
                    };
                }

                // No match and auto-create disabled - fall through to try default
                _logger.LogInformation("[STAGING] No match found for '{Name}' and auto-create disabled, checking for default organization", organizationName);
            }

            // FALLBACK: Use default organization (when no name provided OR when name didn't match and auto-create is disabled)
            if (defaultOrganizationId.HasValue)
            {
                var defaultOrg = await _context.Organizations
                    .IgnoreQueryFilters()  // Bypass soft delete filter (Organization has no other filters)
                    .FirstOrDefaultAsync(o => o.Id == defaultOrganizationId.Value, cancellationToken);

                if (defaultOrg != null)
                {
                    _logger.LogInformation("[STAGING] Using default organization: '{Name}' (ID: {Id})", defaultOrg.Name, defaultOrg.Id);
                    return new StagedOrganization
                    {
                        ExistingOrganization = defaultOrg,
                        ExistingOrganizationId = defaultOrg.Id,
                        IsNew = false,
                        Name = defaultOrg.Name,
                        OrganizationTypeName = organizationTypeName
                    };
                }
                else
                {
                    _logger.LogWarning("[STAGING] Default organization ID '{Id}' not found in database", defaultOrganizationId.Value);
                }
            }

            _logger.LogWarning("[STAGING] No organization found and no valid fallback available");
            return null; // No name match, no default available
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error staging organization '{Name}'", organizationName);
            return null;
        }
    }

    /// <summary>
    /// STEP 3: Stage lab result with duplicate detection by accession + specimen date + lab
    /// </summary>
    private async Task<(StagedLabResult? stagedLabResult, DuplicateCheckResult duplicateCheck)> StageLabResultAsync(
        HL7Message message,
        StagedPatient stagedPatient,
        StagedOrganization stagedLaboratory,
        HL7Configuration? configuration,
        CancellationToken cancellationToken)
    {
        var duplicateCheck = new DuplicateCheckResult();

        try
        {
            // Extract lab result metadata
            var accessionNumber = ExtractAccessionNumber(message, configuration);
            var specimenDate = ExtractSpecimenDate(message, configuration);
            var resultDate = specimenDate; // Can be different in some systems

            // Extract specimen type from OBR-15 or SPM-4
            var (specimenCode, specimenText, specimenSystem) = ExtractSpecimenType(message, configuration);

            // NOTE: Test method extraction removed from lab-result level
            // Test methods are now extracted PER-MARKER in ExtractMarkersFromOBXAsync
            // This allows each marker to have its own test method (e.g., Culture vs PCR)
            // and supports dynamic field mapping where users can configure OBX-17, OBX-3.2, or any field per observation

            // Extract markers from OBX segments (using configuration-driven dynamic mappings when available)
            var markerDataList = await ExtractMarkersFromOBXAsync(message, configuration, cancellationToken);

            if (!markerDataList.Any())
            {
                _logger.LogWarning("[STAGING] No markers found in message {MessageControlId}", message.MessageControlId);
                return (null, duplicateCheck);
            }

            // Convert to staged markers
            var stagedMarkers = markerDataList.Select(md => new StagedMarker
            {
                IsNew = true,
                TestCode = md.TestCode,
                TestName = md.TestName,
                QualitativeResult = md.QualitativeResult,
                QuantitativeValue = md.QuantitativeValue,
                Units = md.Units,
                ReferenceRange = md.ReferenceRange,
                ResultStatus = md.ResultStatus ?? "F",
                InterpretationFlag = md.AbnormalFlag,
                // Use per-marker test method values extracted from each OBX observation
                // Each marker can have its own test method (e.g., Culture, PCR, NAAT)
                TestMethodCode = md.TestMethodCode,
                TestMethodText = md.TestMethodText,
                TestMethodCodingSystem = md.TestMethodCodingSystem
            }).ToList();

            // DUPLICATE DETECTION: Check by accession + specimen date + lab
            if (!string.IsNullOrEmpty(accessionNumber) && stagedLaboratory.ExistingOrganizationId.HasValue)
            {
                var existingLabResult = await _context.LabResults
                        .IgnoreQueryFilters()  // Bypass soft delete filter (we check !lr.IsDeleted manually)
                        .Include(lr => lr.Markers)
                        .Include(lr => lr.Patient)
                        .FirstOrDefaultAsync(lr =>
                            lr.AccessionNumber == accessionNumber &&
                            lr.SpecimenCollectionDate == specimenDate &&
                            lr.LaboratoryId == stagedLaboratory.ExistingOrganizationId.Value &&
                            !lr.IsDeleted,
                            cancellationToken);

                if (existingLabResult != null)
                {
                    duplicateCheck.IsDuplicate = true;
                    duplicateCheck.ExistingLabResult = existingLabResult;

                    // Check if patient matches
                    if (stagedPatient.ExistingPatientId.HasValue &&
                        existingLabResult.PatientId != stagedPatient.ExistingPatientId.Value)
                    {
                        duplicateCheck.IsPatientMismatch = true;
                        duplicateCheck.Reason = $"Accession {accessionNumber} exists for patient {existingLabResult.Patient?.FriendlyId}, but HL7 specifies different patient";

                        _logger.LogWarning("[STAGING] Patient mismatch for accession {Accession}", accessionNumber);

                        return (null, duplicateCheck);
                    }
                    else if (stagedPatient.IsNew)
                    {
                        // New patient but accession exists → check demographics similarity
                        var demographicsMatch = AreDemographicsSimilar(stagedPatient, existingLabResult.Patient);
                        if (!demographicsMatch)
                        {
                            duplicateCheck.IsPatientMismatch = true;
                            duplicateCheck.Reason = $"Accession {accessionNumber} exists but patient demographics don't match";

                            _logger.LogWarning("[STAGING] Demographics mismatch for accession {Accession}", accessionNumber);

                            return (null, duplicateCheck);
                        }
                    }

                    // Patient matches - check if markers are identical
                    var existingMarkerKeys = existingLabResult.Markers
                        .Select(m => $"{m.TestCode}|{m.QualitativeResultText}|{m.QuantitativeValue}")
                        .OrderBy(k => k)
                        .ToList();

                    var newMarkerKeys = stagedMarkers
                        .Select(m => $"{m.TestCode}|{m.QualitativeResult}|{m.QuantitativeValue}")
                        .OrderBy(k => k)
                        .ToList();

                    if (existingMarkerKeys.SequenceEqual(newMarkerKeys))
                    {
                        duplicateCheck.IsIdentical = true;
                        duplicateCheck.Reason = "Identical lab result already exists";

                        _logger.LogInformation("[STAGING] Identical duplicate found for accession {Accession}", accessionNumber);

                        return (null, duplicateCheck);
                    }

                    // Different markers - will update
                    duplicateCheck.IsIdentical = false;
                    duplicateCheck.Reason = "Lab result exists but with different markers - will update";

                    // Mark which markers are new vs updated
                    foreach (var stagedMarker in stagedMarkers)
                    {
                        var existingMarker = existingLabResult.Markers
                            .FirstOrDefault(m => m.TestCode == stagedMarker.TestCode);

                        if (existingMarker != null)
                        {
                            stagedMarker.IsNew = false;
                            stagedMarker.IsUpdated = true;
                            stagedMarker.ExistingMarker = existingMarker;
                            stagedMarker.ExistingMarkerId = existingMarker.Id;
                            stagedMarker.PreviousQualitativeResult = existingMarker.QualitativeResultText;
                            stagedMarker.PreviousQuantitativeValue = existingMarker.QuantitativeValue;
                        }
                    }

                    var stagedLabResult = new StagedLabResult
                    {
                        ExistingLabResult = existingLabResult,
                        ExistingLabResultId = existingLabResult.Id,
                        IsNew = false,
                        IsUpdate = true,
                        AccessionNumber = accessionNumber,
                        SpecimenCollectionDate = specimenDate,
                        ResultDate = resultDate,
                        SpecimenTypeCode = specimenCode,
                        SpecimenTypeText = specimenText,
                        SpecimenTypeCodingSystem = specimenSystem,
                        Markers = stagedMarkers
                    };

                    return (stagedLabResult, duplicateCheck);
                }
            }

            // No duplicate - new lab result
            var newStagedLabResult = new StagedLabResult
            {
                IsNew = true,
                IsUpdate = false,
                AccessionNumber = accessionNumber,
                SpecimenCollectionDate = specimenDate,
                ResultDate = resultDate,
                Notes = $"Created from HL7 message {message.MessageControlId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                SpecimenTypeCode = specimenCode,
                SpecimenTypeText = specimenText,
                SpecimenTypeCodingSystem = specimenSystem,
                Markers = stagedMarkers
            };

            return (newStagedLabResult, duplicateCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error staging lab result for message {MessageControlId}", message.MessageControlId);
            return (null, duplicateCheck);
        }
    }

    /// <summary>
    /// Helper: Check if patient demographics are similar enough to be the same person
    /// </summary>
    private bool AreDemographicsSimilar(StagedPatient stagedPatient, Patient? existingPatient)
    {
        if (existingPatient == null) return false;

        // Check DOB (must match)
        if (stagedPatient.DateOfBirth != existingPatient.DateOfBirth)
            return false;

        // Check name (fuzzy match)
        var stagedLastName = (stagedPatient.LastName ?? "").ToUpperInvariant().Trim();
        var existingLastName = (existingPatient.FamilyName ?? "").ToUpperInvariant().Trim();

        if (stagedLastName != existingLastName)
            return false;

        var stagedFirstName = (stagedPatient.FirstName ?? "").ToUpperInvariant().Trim();
        var existingFirstName = (existingPatient.GivenName ?? "").ToUpperInvariant().Trim();

        if (stagedFirstName != existingFirstName)
            return false;

        return true;
    }

    /// <summary>
    /// DEPRECATED: Old disease identification approach
    /// Kept for reference - replaced by ResolveAllHL7FieldsAsync + IdentifyDiseasesFromResolvedPathogensAsync
    /// </summary>
    [Obsolete("Use ResolveAllHL7FieldsAsync + IdentifyDiseasesFromResolvedPathogensAsync instead")]
    private async Task<List<DiseaseMatch>> IdentifyDiseasesFromStagedMarkersAsync(
        List<StagedMarker> markers,
        StagedPatient? stagedPatient,
        List<string> stagingLog,
        CancellationToken cancellationToken)
    {
        var diseaseMatches = new List<DiseaseMatch>();

        try
        {
            stagingLog.Add($"========== DISEASE IDENTIFICATION DETAILS ==========");
            stagingLog.Add($"Total markers to analyze: {markers.Count}");
            _logger.LogInformation("[DISEASE ID] Starting disease identification for {Count} markers", markers.Count);

            // First, let's see what LOINC codes are in the database for comparison
            var allPathogens = await _context.Pathogens
                .Where(p => p.IsActive && p.LOINCCode != null)
                .Select(p => new { p.LOINCCode, p.Name, p.DiseaseId, p.IsActive })
                .ToListAsync(cancellationToken);

            stagingLog.Add($"Database has {allPathogens.Count} active pathogens with LOINC codes");
            _logger.LogInformation("[DISEASE ID] Found {Count} active pathogens with LOINC codes in database", allPathogens.Count);

            if (allPathogens.Count > 0)
            {
                stagingLog.Add($"Sample pathogens in DB:");
                foreach (var p in allPathogens.Take(5))
                {
                    stagingLog.Add($"  - LOINC: '{p.LOINCCode}', Name: '{p.Name}', HasDisease: {p.DiseaseId.HasValue}");
                }
            }

            foreach (var marker in markers)
            {
                stagingLog.Add($"");
                stagingLog.Add($"--- Analyzing Marker ---");
                stagingLog.Add($"TestCode: '{marker.TestCode ?? "NULL"}'");
                stagingLog.Add($"TestName: '{marker.TestName ?? "NULL"}'");
                stagingLog.Add($"QualitativeResult: '{marker.QualitativeResult ?? "NULL"}'");
                stagingLog.Add($"QuantitativeValue: {marker.QuantitativeValue?.ToString() ?? "NULL"}");

                _logger.LogInformation("[DISEASE ID] Analyzing marker: TestCode='{TestCode}', TestName='{TestName}', Result='{Result}'", 
                    marker.TestCode, marker.TestName, marker.QualitativeResult);

                // STRATEGY 1: Try LOINC code matching first
                object? matchingPathogen = null;
                bool usedTextMatching = false;

                if (!string.IsNullOrWhiteSpace(marker.TestCode))
                {
                    // Check if this LOINC code exists in our database
                    matchingPathogen = allPathogens.FirstOrDefault(p => 
                        p.LOINCCode != null && 
                        p.LOINCCode.Trim().Equals(marker.TestCode.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                // STRATEGY 2: If no LOINC match and TestName is available, try text matching
                if (matchingPathogen == null && !string.IsNullOrWhiteSpace(marker.TestName))
                {
                    stagingLog.Add($"⚠️ No LOINC code or no match - attempting text matching on TestName");
                    _logger.LogInformation("[DISEASE ID] Attempting text matching for TestName='{TestName}'", marker.TestName);

                    // Query pathogens with text matching enabled via parent disease
                    var pathogensWithTextMatching = await _context.Pathogens
                        .Where(p => p.IsActive && p.DiseaseId != null)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.LOINCCode,
                            p.DiseaseId,
                            p.Disease!.ParentDiseaseId
                        })
                        .ToListAsync(cancellationToken);

                    // Get HL7 matching configs for diseases
                    var diseaseIdsToCheck = pathogensWithTextMatching
                        .Where(p => p.DiseaseId.HasValue)
                        .Select(p => p.DiseaseId!.Value)
                        .Concat(pathogensWithTextMatching
                            .Where(p => p.ParentDiseaseId.HasValue)
                            .Select(p => p.ParentDiseaseId!.Value))
                        .Distinct()
                        .ToList();

                    var matchingConfigs = await _context.Set<Sentinel.Models.HL7.DiseaseHL7MatchingConfig>()
                        .Where(c => diseaseIdsToCheck.Contains(c.DiseaseId) && c.Pathogen_UseTextFallback)
                        .ToDictionaryAsync(c => c.DiseaseId, cancellationToken);

                    // Normalize the test name for matching
                    var normalizedTestName = NormalizeTextForMatching(marker.TestName, true, true, true);
                    stagingLog.Add($"  Normalized TestName: '{normalizedTestName}'");

                    // Try to match against pathogen names
                    foreach (var candidatePathogen in pathogensWithTextMatching)
                    {
                        if (candidatePathogen.DiseaseId == null) continue;

                        // Check if this disease or its parent has text matching enabled
                        bool textMatchingEnabled = false;
                        if (matchingConfigs.ContainsKey(candidatePathogen.DiseaseId.Value))
                        {
                            textMatchingEnabled = true;
                        }
                        else if (candidatePathogen.ParentDiseaseId.HasValue && matchingConfigs.ContainsKey(candidatePathogen.ParentDiseaseId.Value))
                        {
                            textMatchingEnabled = true;
                        }

                        if (!textMatchingEnabled) continue;

                        // Normalize pathogen name
                        var normalizedPathogenName = NormalizeTextForMatching(candidatePathogen.Name, true, true, true);

                        // Check for match
                        if (normalizedPathogenName == normalizedTestName)
                        {
                            stagingLog.Add($"  ✅ TEXT MATCH: Pathogen '{candidatePathogen.Name}' matches TestName");
                            _logger.LogInformation("[DISEASE ID] ✅ TEXT MATCH: Pathogen '{Name}' matches TestName '{TestName}'", 
                                candidatePathogen.Name, marker.TestName);

                            matchingPathogen = new { candidatePathogen.LOINCCode, candidatePathogen.Name, DiseaseId = candidatePathogen.DiseaseId, IsActive = true };
                            usedTextMatching = true;
                            break;
                        }
                    }

                    if (matchingPathogen == null)
                    {
                        stagingLog.Add($"  ❌ No text match found for TestName");
                        _logger.LogInformation("[DISEASE ID] No text match found for TestName='{TestName}'", marker.TestName);
                    }
                }

                if (matchingPathogen == null && string.IsNullOrWhiteSpace(marker.TestCode) && string.IsNullOrWhiteSpace(marker.TestName))
                {
                    stagingLog.Add($"❌ SKIP: Marker has no test code or test name");
                    _logger.LogWarning("[DISEASE ID] Marker has no test code or test name, skipping");
                    continue;
                }

                if (matchingPathogen == null)
                {
                    if (!string.IsNullOrWhiteSpace(marker.TestCode))
                    {
                        stagingLog.Add($"❌ NO MATCH: LOINC code '{marker.TestCode}' not found in database");

                        // Show similar codes (with null safety)
                        try
                        {
                            var similarCodes = allPathogens
                                .Where(p => p.LOINCCode != null && 
                                           marker.TestCode != null && 
                                           marker.TestCode.Length >= 3 &&
                                           p.LOINCCode.Contains(marker.TestCode.Substring(0, 3)))
                                .Select(p => p.LOINCCode)
                                .Take(3)
                                .ToList();

                            if (similarCodes.Any())
                            {
                                stagingLog.Add($"  Similar codes in DB: {string.Join(", ", similarCodes)}");
                            }
                        }
                        catch (Exception simEx)
                        {
                            _logger.LogWarning(simEx, "[DISEASE ID] Error finding similar codes");
                        }

                        _logger.LogWarning("[DISEASE ID] NO MATCH: LOINC code '{LOINC}' not found in database", marker.TestCode);
                    }
                    continue;
                }

                // Extract pathogen details from the matched object
                dynamic matchedPathogenData = matchingPathogen;
                string matchedPathogenName = matchedPathogenData.Name;
                Guid? matchedPathogenDiseaseId = matchedPathogenData.DiseaseId;

                stagingLog.Add($"✅ {(usedTextMatching ? "TEXT" : "LOINC")} MATCH FOUND");
                stagingLog.Add($"  Matched Pathogen: '{matchedPathogenName}'");
                stagingLog.Add($"  Has DiseaseId: {matchedPathogenDiseaseId.HasValue}");

                _logger.LogInformation("[DISEASE ID] ✅ {MatchType} MATCH FOUND: Pathogen '{Name}'", 
                    usedTextMatching ? "TEXT" : "LOINC", matchedPathogenName);

                if (!matchedPathogenDiseaseId.HasValue)
                {
                    stagingLog.Add($"❌ PATHOGEN NOT LINKED TO DISEASE");
                    stagingLog.Add($"  Fix: Update Pathogen '{matchedPathogenName}' to set DiseaseId");
                    _logger.LogWarning("[DISEASE ID] ❌ PATHOGEN NOT LINKED: Pathogen '{Name}' has no DiseaseId", 
                        matchedPathogenName);
                    continue;
                }

                // Now fetch the full pathogen with disease
                stagingLog.Add($"  Fetching full pathogen record with disease...");
                _logger.LogInformation("[DISEASE ID] Fetching pathogen details with disease...");

                Sentinel.Models.Pathogens.Pathogen? pathogen = null;

                try
                {
                    // Query for the pathogen by name (works for both LOINC and text matches)
                    _logger.LogInformation("[DISEASE ID] Querying for Pathogen Name='{Name}'", matchedPathogenName);
                    stagingLog.Add($"  Querying Pathogens table for Name='{matchedPathogenName}'");

                    // Defensive checks
                    if (_context == null)
                    {
                        stagingLog.Add($"❌ CRITICAL: _context is null!");
                        _logger.LogError("[DISEASE ID] _context is null");
                        continue;
                    }

                    if (_context.Pathogens == null)
                    {
                        stagingLog.Add($"❌ CRITICAL: _context.Pathogens DbSet is null!");
                        _logger.LogError("[DISEASE ID] _context.Pathogens is null");
                        continue;
                    }

                    // CHANGE: Query by pathogen name instead of LOINC code (works for both LOINC and text matches)
                    pathogen = await _context.Pathogens
                        .AsNoTracking()  // Add AsNoTracking to avoid caching issues
                        .Where(p => p.Name == matchedPathogenName && p.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (pathogen == null)
                    {
                        stagingLog.Add($"❌ ERROR: Pathogen not found for Name '{matchedPathogenName}'");
                        _logger.LogError("[DISEASE ID] Pathogen not found");
                        continue;
                    }

                    stagingLog.Add($"  Pathogen loaded: '{pathogen.Name}'");
                    stagingLog.Add($"  DiseaseId: {pathogen.DiseaseId?.ToString() ?? "NULL"}");

                    // Mark this marker with the text matching flag if used
                    if (usedTextMatching)
                    {
                        marker.PathogenMatchMethod = MatchMethod.Text;
                        stagingLog.Add($"  Match method: Text (fallback)");
                    }
                    else
                    {
                        marker.PathogenMatchMethod = MatchMethod.Exact;
                        stagingLog.Add($"  Match method: Exact (LOINC code)");
                    }

                    // Now manually load the Disease if DiseaseId is set
                    if (pathogen.DiseaseId.HasValue)
                    {
                        var diseaseIdToFind = pathogen.DiseaseId.Value;
                        stagingLog.Add($"  Loading disease with Id={diseaseIdToFind}");
                        _logger.LogInformation("[DISEASE ID] Loading disease with Id={DiseaseId}", diseaseIdToFind);

                        if (_context.Diseases == null)
                        {
                            stagingLog.Add($"  ❌ CRITICAL: _context.Diseases DbSet is null!");
                            _logger.LogError("[DISEASE ID] _context.Diseases is null");
                            continue;
                        }

                        try
                        {
                            // CRITICAL: IgnoreQueryFilters() to bypass the Disease access filter
                            // HL7 processing runs in a background service without HTTP context,
                            // which causes the global Disease query filter to fail with NullReferenceException
                            pathogen.Disease = await _context.Diseases
                                .IgnoreQueryFilters()  // <-- This is the fix!
                                .AsNoTracking()
                                .Where(d => d.Id == diseaseIdToFind)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (pathogen.Disease != null)
                            {
                                stagingLog.Add($"  ✅ Disease loaded: '{pathogen.Disease.Name}'");
                                _logger.LogInformation("[DISEASE ID] Disease loaded: '{Name}', DiseaseId={DiseaseId}", 
                                    pathogen.Disease.Name, pathogen.Disease.Id);
                            }
                            else
                            {
                                stagingLog.Add($"  ❌ Disease NOT FOUND with Id={diseaseIdToFind}");
                                _logger.LogError("[DISEASE ID] Disease NOT FOUND with Id={DiseaseId}", diseaseIdToFind);
                                continue;
                            }
                        }
                        catch (Exception diseaseEx)
                        {
                            stagingLog.Add($"  ❌ Disease query failed: {diseaseEx.Message}");
                            stagingLog.Add($"     Exception Type: {diseaseEx.GetType().Name}");
                            if (diseaseEx.InnerException != null)
                            {
                                stagingLog.Add($"     Inner: {diseaseEx.InnerException.Message}");
                            }
                            if (diseaseEx.StackTrace != null)
                            {
                                var stackLines = diseaseEx.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(3);
                                foreach (var line in stackLines)
                                {
                                    stagingLog.Add($"     {line.Trim()}");
                                }
                            }
                            _logger.LogError(diseaseEx, "[DISEASE ID] Disease query failed for Id={DiseaseId}", diseaseIdToFind);
                            continue;
                        }
                    }
                    else
                    {
                        stagingLog.Add($"  ❌ Pathogen has no DiseaseId set");
                        _logger.LogWarning("[DISEASE ID] Pathogen '{Name}' has no DiseaseId", pathogen.Name);
                        continue;
                    }

                    stagingLog.Add($"  Disease object: {(pathogen.Disease != null ? $"'{pathogen.Disease.Name}'" : "NULL")}");

                    _logger.LogInformation("[DISEASE ID] Pathogen loaded: '{Name}', DiseaseId={DiseaseId}, Disease={Disease}", 
                        pathogen.Name, pathogen.DiseaseId?.ToString() ?? "NULL", pathogen.Disease?.Name ?? "NULL");
                }
                catch (Exception queryEx)
                {
                    stagingLog.Add($"❌ DATABASE ERROR: {queryEx.Message}");
                    stagingLog.Add($"   Exception Type: {queryEx.GetType().Name}");
                    if (queryEx.InnerException != null)
                    {
                        stagingLog.Add($"   Inner Exception: {queryEx.InnerException.Message}");
                    }
                    if (queryEx.StackTrace != null)
                    {
                        var stackLines = queryEx.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(3);
                        foreach (var line in stackLines)
                        {
                            stagingLog.Add($"   {line.Trim()}");
                        }
                    }

                    _logger.LogError(queryEx, "[DISEASE ID] DATABASE ERROR querying pathogen for LOINC '{LOINC}'", marker.TestCode);
                    continue;
                }

                if (pathogen != null && pathogen.Disease != null)
                {
                    var isPositive = IsPositiveResult(marker);

                    stagingLog.Add($"✅✅✅ SUCCESS: Disease identified!");
                    stagingLog.Add($"  Disease: '{pathogen.Disease.Name}' (ID: {pathogen.Disease.Id})");
                    stagingLog.Add($"  Result interpretation: {(isPositive ? "POSITIVE/DETECTED" : "NEGATIVE/NOT DETECTED")}");

                    _logger.LogInformation("[DISEASE ID] ✅✅✅ SUCCESS: Disease '{DiseaseName}' identified, IsPositive={IsPositive}", 
                        pathogen.Disease.Name, isPositive);

                    // Check if we already have this disease
                    var existingMatch = diseaseMatches.FirstOrDefault(dm => dm.Disease.Id == pathogen.Disease.Id);
                    if (existingMatch != null)
                    {
                        stagingLog.Add($"  Adding marker to existing disease match");
                        existingMatch.MatchedMarkers.Add(marker);
                    }
                    else
                    {
                        stagingLog.Add($"  Creating new disease match");
                        diseaseMatches.Add(new DiseaseMatch
                        {
                            Disease = pathogen.Disease,
                            OriginalTopLevelDisease = pathogen.Disease,
                            MatchedPathogen = pathogen,
                            Source = MatchSource.LOINCPathogenMatch,
                            IsPositiveResult = isPositive,
                            MatchedMarkers = new List<StagedMarker> { marker }
                        });
                    }

                    continue;
                }
                else if (pathogen != null)
                {
                    stagingLog.Add($"❌ Pathogen found but Disease is NULL");
                    stagingLog.Add($"  Pathogen: '{pathogen.Name}', DiseaseId: {pathogen.DiseaseId?.ToString() ?? "NULL"}");
                    stagingLog.Add($"  Fix: Verify Disease record exists with Id={pathogen.DiseaseId}");

                    _logger.LogWarning("[DISEASE ID] Pathogen found but Disease is NULL: '{Name}', DiseaseId={DiseaseId}", 
                        pathogen.Name, pathogen.DiseaseId?.ToString() ?? "NULL");
                }
                else
                {
                    stagingLog.Add($"❌ Pathogen is NULL after query");
                    _logger.LogWarning("[DISEASE ID] Pathogen is NULL after query");
                }

                stagingLog.Add($"No disease match for this marker");
            }

            stagingLog.Add($"");
            stagingLog.Add($"========== DISEASE IDENTIFICATION COMPLETE ==========");
            stagingLog.Add($"Total disease matches: {diseaseMatches.Count}");

            _logger.LogInformation("[DISEASE ID] Disease identification complete: {Count} matches", diseaseMatches.Count);

            if (diseaseMatches.Count == 0)
            {
                stagingLog.Add($"❌❌❌ NO DISEASES IDENTIFIED - Result will be NoSurveillance");
                stagingLog.Add($"");
                stagingLog.Add($"Troubleshooting checklist:");
                stagingLog.Add($"1. Verify LOINC codes in HL7 match Pathogen.LOINCCode exactly");
                stagingLog.Add($"2. Verify Pathogen.IsActive = true");
                stagingLog.Add($"3. Verify Pathogen.DiseaseId is set (not NULL)");
                stagingLog.Add($"4. Verify Disease record exists with that Id");
                stagingLog.Add($"5. Check that .Include(p => p.Disease) works (no navigation issues)");

                _logger.LogWarning("[DISEASE ID] ❌❌❌ NO DISEASES IDENTIFIED");
            }

            return diseaseMatches;
        }
        catch (Exception ex)
        {
            stagingLog.Add($"❌ CRITICAL ERROR: {ex.Message}");
            _logger.LogError(ex, "[DISEASE ID] CRITICAL ERROR identifying diseases");
            return diseaseMatches;
        }
    }

    /// <summary>
    /// NEW APPROACH: Resolve all 4 HL7 fields universally with text fallback
    /// (Specimen, TestName, Pathogen, Result) before attempting disease identification
    /// </summary>
    private async Task ResolveAllHL7FieldsAsync(
        StagedLabResult stagedLabResult,
        bool enableSpecimenTextSearch,
        bool enablePathogenTextSearch,
        bool enableTestMethodTextSearch,
        List<string> stagingLog,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[FIELD RESOLUTION] Resolving all HL7 fields with text fallback");
        stagingLog.Add($"[FIELD RESOLUTION] Starting universal field resolution...");

        // 1. Resolve Specimen Type (from OBR-15)
        stagingLog.Add($"");
        stagingLog.Add($"1. Resolving Specimen Type:");
        stagingLog.Add($"   Code: '{stagedLabResult.SpecimenTypeCode ?? "NULL"}'");
        stagingLog.Add($"   Text: '{stagedLabResult.SpecimenTypeText ?? "NULL"}'");

        await ResolveSpecimenTypeAsync(
            stagedLabResult,
            enableSpecimenTextSearch,
            cancellationToken);

        if (stagedLabResult.ResolvedSpecimenTypeId.HasValue)
        {
            var specimen = await _context.SpecimenTypes
                .FirstOrDefaultAsync(s => s.Id == stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken);
            stagingLog.Add($"   ✅ RESOLVED: {specimen?.Name} (Method: {stagedLabResult.SpecimenMatchMethod})");
        }
        else
        {
            stagingLog.Add($"   ❌ NOT RESOLVED");
        }

        // 2. Resolve each marker's fields
        foreach (var marker in stagedLabResult.Markers)
        {
            stagingLog.Add($"");
            stagingLog.Add($"2. Resolving Marker: '{marker.TestName ?? marker.TestCode ?? "UNNAMED"}'");

            // 2a. Resolve Pathogen/Biomarker
            stagingLog.Add($"   2a. Pathogen/Biomarker:");
            stagingLog.Add($"       LOINC: '{marker.TestCode ?? "NULL"}'");
            stagingLog.Add($"       Name: '{marker.TestName ?? "NULL"}'");

            await ResolvePathogenAsync(marker, enablePathogenTextSearch, cancellationToken);

            if (marker.ResolvedPathogenId.HasValue)
            {
                var pathogen = await _context.Pathogens
                    .FirstOrDefaultAsync(p => p.Id == marker.ResolvedPathogenId.Value, cancellationToken);
                stagingLog.Add($"       ✅ RESOLVED: {pathogen?.Name} (Method: {marker.PathogenMatchMethod})");
            }
            else
            {
                stagingLog.Add($"       ❌ NOT RESOLVED");
            }

            // 2b. Resolve Test Method
            stagingLog.Add($"   2b. Test Method:");
            stagingLog.Add($"       Code: '{marker.TestMethodCode ?? "NULL"}'");
            stagingLog.Add($"       Text: '{marker.TestMethodText ?? "NULL"}'");

            await ResolveTestMethodAsync(marker, enableTestMethodTextSearch, cancellationToken);

            if (marker.ResolvedTestMethodId.HasValue)
            {
                var testMethod = await _context.TestMethods
                    .FirstOrDefaultAsync(t => t.Id == marker.ResolvedTestMethodId.Value, cancellationToken);
                stagingLog.Add($"       ✅ RESOLVED: {testMethod?.Name} (Method: {marker.TestMethodMatchMethod})");
            }
            else
            {
                stagingLog.Add($"       ❌ NOT RESOLVED");
            }

            // 2c. Resolve and Normalize Result Value
            stagingLog.Add($"   2c. Result Value:");
            stagingLog.Add($"       Qualitative: '{marker.QualitativeResult ?? "NULL"}'");
            stagingLog.Add($"       Quantitative: {marker.QuantitativeValue?.ToString() ?? "NULL"}");

            // First normalize the text
            NormalizeResultValue(marker);

            // Then resolve to test result ID
            await ResolveTestResultAsync(marker, true, cancellationToken);

            if (marker.ResolvedTestResultId.HasValue)
            {
                var testResult = await _context.Set<TestResult>()
                    .FirstOrDefaultAsync(r => r.Id == marker.ResolvedTestResultId.Value, cancellationToken);
                stagingLog.Add($"       ✅ RESOLVED: {testResult?.Name} (Normalized: {marker.NormalizedResultValue ?? "N/A"})");
            }
            else
            {
                stagingLog.Add($"       ✅ NORMALIZED: {marker.NormalizedResultValue ?? "N/A"} (Not resolved to ID)");
            }
        }

        _logger.LogInformation("[FIELD RESOLUTION] Field resolution complete");
    }

    /// <summary>
    /// NEW APPROACH: Identify diseases by evaluating ALL disease case definitions against resolved fields
    /// This does NOT require pathogens to be pre-linked to diseases - it matches based on criteria
    /// </summary>
    private async Task<List<DiseaseMatch>> IdentifyDiseasesFromResolvedPathogensAsync(
        StagedLabResult stagedLabResult,
        List<string> stagingLog,
        CancellationToken cancellationToken)
    {
        var diseaseMatches = new List<DiseaseMatch>();

        try
        {
            stagingLog.Add($"[DISEASE ID] Evaluating ALL disease case definitions against resolved fields...");
            stagingLog.Add($"");

            // Show what we're matching against
            stagingLog.Add($"========== RESOLVED HL7 FIELDS FOR MATCHING ==========");
            stagingLog.Add($"");

            // Specimen
            if (stagedLabResult.ResolvedSpecimenTypeId.HasValue)
            {
                var specimenName = await GetSpecimenTypeNameAsync(stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken);
                stagingLog.Add($"✅ Specimen Type: {specimenName} (ID: {stagedLabResult.ResolvedSpecimenTypeId.Value})");
            }
            else
            {
                stagingLog.Add($"❌ Specimen Type: NOT RESOLVED");
            }
            stagingLog.Add($"");

            // Markers with pathogens, test methods, results
            var resolvedMarkers = stagedLabResult.Markers.Where(m => m.ResolvedPathogenId.HasValue).ToList();
            if (resolvedMarkers.Any())
            {
                stagingLog.Add($"Resolved Markers ({resolvedMarkers.Count}):");
                foreach (var marker in resolvedMarkers)
                {
                    var pathogenName = await GetPathogenNameAsync(marker.ResolvedPathogenId!.Value, cancellationToken);
                    var testMethodName = marker.ResolvedTestMethodId.HasValue 
                        ? await GetTestMethodNameAsync(marker.ResolvedTestMethodId.Value, cancellationToken) 
                        : "NOT RESOLVED";

                    stagingLog.Add($"  • Marker: '{marker.TestName ?? "UNNAMED"}'");
                    stagingLog.Add($"    - Pathogen: {pathogenName} (ID: {marker.ResolvedPathogenId.Value})");
                    stagingLog.Add($"    - Test Method: {testMethodName}");
                    stagingLog.Add($"    - Result: {marker.NormalizedResultValue ?? "NULL"}");
                }
            }
            else
            {
                stagingLog.Add($"❌ No markers with resolved pathogens");
            }

            stagingLog.Add($"");
            stagingLog.Add($"=======================================================");
            stagingLog.Add($"");

            // VALIDATION: Check if we have minimum required fields for case definition matching
            stagingLog.Add($"Validating minimum required fields...");

            bool hasSpecimen = stagedLabResult.ResolvedSpecimenTypeId.HasValue;
            bool hasPathogen = resolvedMarkers.Any();
            bool hasResult = stagedLabResult.Markers.Any(m => !string.IsNullOrWhiteSpace(m.NormalizedResultValue));

            stagingLog.Add($"  Specimen: {(hasSpecimen ? "✅ RESOLVED" : "❌ MISSING")}");
            stagingLog.Add($"  Pathogen: {(hasPathogen ? "✅ RESOLVED" : "❌ MISSING")}");
            stagingLog.Add($"  Result: {(hasResult ? "✅ PRESENT" : "❌ MISSING")}");
            stagingLog.Add($"");

            if (!hasSpecimen || !hasPathogen || !hasResult)
            {
                stagingLog.Add($"❌ CANNOT EVALUATE CASE DEFINITIONS");
                stagingLog.Add($"   Reason: Missing required fields for matching");
                stagingLog.Add($"   Decision: NoSurveillance");
                stagingLog.Add($"");
                stagingLog.Add($"Required fields for case definition matching:");
                stagingLog.Add($"  • Specimen Type must be resolved");
                stagingLog.Add($"  • At least one Pathogen/Biomarker must be resolved");
                stagingLog.Add($"  • At least one Result value must be present");
                stagingLog.Add($"");
                return diseaseMatches; // Return empty list
            }

            stagingLog.Add($"✅ Minimum required fields present - proceeding with case definition evaluation");
            stagingLog.Add($"");

            // Load all pathogens to create an ID-to-name lookup for case definition matching
            var allPathogens = await _context.Pathogens
                .IgnoreQueryFilters()
                .Where(p => p.IsActive)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);

            var pathogenIdToName = allPathogens.ToDictionary(p => p.Id, p => p.Name);
            stagingLog.Add($"Loaded {allPathogens.Count} active pathogens for name-based matching");
            stagingLog.Add($"");

            // Get all active diseases
            var allDiseases = await _context.Diseases
                .IgnoreQueryFilters()
                .Where(d => d.IsActive)
                .ToListAsync(cancellationToken);

            stagingLog.Add($"Found {allDiseases.Count} active diseases to check");
            stagingLog.Add($"");

            // For each disease, get its case definitions and evaluate them
            foreach (var disease in allDiseases)
            {
                // Get active case definitions for this disease
                var caseDefinitions = await _context.CaseDefinitions
                    .IgnoreQueryFilters()
                    .Where(cd => cd.DiseaseId == disease.Id && 
                                 cd.Status == CaseDefinitionStatus.Current &&
                                 cd.DateActiveFrom <= DateTime.UtcNow &&
                                 (cd.DateActiveTo == null || cd.DateActiveTo >= DateTime.UtcNow))
                    .Include(cd => cd.Criteria)
                        .ThenInclude(c => c.CanonicalPathogen)
                    .Include(cd => cd.Criteria)
                        .ThenInclude(c => c.CanonicalSpecimenType)
                    .Include(cd => cd.Criteria)
                        .ThenInclude(c => c.CanonicalTestMethod)
                    .Include(cd => cd.Criteria)
                        .ThenInclude(c => c.CanonicalTestResult)
                    .Include(cd => cd.ConfirmationStatus)
                    .ToListAsync(cancellationToken);

                if (!caseDefinitions.Any())
                {
                    _logger.LogDebug("[DISEASE ID] Disease '{DiseaseName}' has no active case definitions", disease.Name);
                    continue;
                }

                stagingLog.Add($"Checking disease: '{disease.Name}' ({caseDefinitions.Count} active case definition(s))");

                // Check each case definition
                foreach (var caseDefinition in caseDefinitions)
                {
                    // Get laboratory criteria (these are what we match against HL7 fields)
                    var labCriteria = caseDefinition.Criteria
                        .Where(c => c.CriterionType == CriterionType.Laboratory)
                        .ToList();

                    if (!labCriteria.Any())
                    {
                        _logger.LogDebug("[DISEASE ID] Case definition '{DefName}' has no laboratory criteria", caseDefinition.Name);
                        continue;
                    }

                    stagingLog.Add($"  Case Definition: '{caseDefinition.Name}'");
                    stagingLog.Add($"    Status: {caseDefinition.ConfirmationStatus?.Name ?? "Unknown"}");
                    stagingLog.Add($"    Lab Criteria: {labCriteria.Count}");

                    // Evaluate if this case definition matches the resolved fields
                    var criteriaMatches = new List<(CaseDefinitionCriteria criteria, StagedMarker? marker, bool matched)>();

                    foreach (var criterion in labCriteria)
                    {
                        stagingLog.Add($"      Evaluating criterion:");

                        // Parse acceptable values from JSON
                        // UPDATED: AcceptablePathogensJson now stores GUIDs (new criteria) OR NAMES (legacy criteria)
                        // Try parsing as GUIDs first, then fall back to names for backward compatibility
                        var acceptablePathogenIds = ParseGuidArray(criterion.AcceptablePathogensJson);
                        var acceptablePathogenNames = acceptablePathogenIds.Any() 
                            ? new List<string>()  // If GUIDs parsed successfully, don't parse as names
                            : ParseStringArray(criterion.AcceptablePathogensJson);  // Legacy: parse as names

                        var acceptableSpecimens = ParseIntArray(criterion.AcceptableSpecimenTypesJson);
                        var acceptableTestMethods = ParseIntArray(criterion.AcceptableTestMethodsJson);
                        var acceptableResults = ParseIntArray(criterion.AcceptableResultsJson);

                        // Log acceptable values (show names when possible)
                        if (acceptablePathogenIds.Any())
                        {
                            var pathogenNamesForLog = acceptablePathogenIds
                                .Select(id => pathogenIdToName.TryGetValue(id, out var name) ? name : id.ToString())
                                .ToList();
                            stagingLog.Add($"        Pathogens (by ID): {string.Join(", ", pathogenNamesForLog.Take(3)) + (pathogenNamesForLog.Count > 3 ? "..." : "")}");
                        }
                        else if (acceptablePathogenNames.Any())
                        {
                            stagingLog.Add($"        Pathogens (by name): {string.Join(", ", acceptablePathogenNames.Take(3)) + (acceptablePathogenNames.Count > 3 ? "..." : "")}");
                        }
                        else
                        {
                            stagingLog.Add($"        Pathogens: ANY");
                        }

                        stagingLog.Add($"        Specimens: {(acceptableSpecimens.Any() ? string.Join(", ", acceptableSpecimens.Take(3)) + (acceptableSpecimens.Count > 3 ? "..." : "") : "ANY")}");
                        stagingLog.Add($"        Test Methods: {(acceptableTestMethods.Any() ? string.Join(", ", acceptableTestMethods.Take(3)) + (acceptableTestMethods.Count > 3 ? "..." : "") : "ANY")}");

                        // Check each marker to see if it matches this criterion
                        bool criterionMatched = false;
                        StagedMarker? matchedMarker = null;

                        foreach (var marker in stagedLabResult.Markers)
                        {
                            // CRITICAL FIX: A criterion field matches if:
                            // 1. The criterion specifies acceptable values AND the field matches one of them
                            // 2. If no acceptable values are specified, the field is not evaluated (skip to next marker)

                            // PATHOGEN MATCHING: Must have acceptable pathogens AND marker must match one of them
                            // Supports both GUID-based (new) and name-based (legacy) matching
                            bool pathogenMatch;
                            bool hasPathogenRestriction = acceptablePathogenIds.Any() || acceptablePathogenNames.Any();

                            if (!hasPathogenRestriction)
                            {
                                // CRITICAL FIX: No pathogen restriction means this criterion doesn't apply to pathogen-based matching
                                // Skip to next criterion or marker - don't match everything
                                continue; // Skip this criterion if no pathogens specified
                            }
                            else
                            {
                                // Pathogen restriction exists - must be resolved AND match
                                if (!marker.ResolvedPathogenId.HasValue)
                                {
                                    // Pathogen not resolved - skip this marker
                                    continue;
                                }

                                // STRATEGY 1: Try GUID-based matching first (new criteria)
                                if (acceptablePathogenIds.Any())
                                {
                                    pathogenMatch = acceptablePathogenIds.Contains(marker.ResolvedPathogenId.Value);

                                    if (!pathogenMatch)
                                    {
                                        // Log failure with pathogen name for clarity
                                        var resolvedName = pathogenIdToName.TryGetValue(marker.ResolvedPathogenId.Value, out var name) 
                                            ? name 
                                            : marker.ResolvedPathogenId.Value.ToString();
                                        var acceptableNames = acceptablePathogenIds
                                            .Select(id => pathogenIdToName.TryGetValue(id, out var n) ? n : id.ToString())
                                            .ToList();
                                        stagingLog.Add($"        ❌ Pathogen '{resolvedName}' (ID: {marker.ResolvedPathogenId.Value}) does not match acceptable IDs: {string.Join(", ", acceptableNames)}");
                                        continue;
                                    }

                                    // Success
                                    var matchedName = pathogenIdToName.TryGetValue(marker.ResolvedPathogenId.Value, out var matchName) 
                                        ? matchName 
                                        : marker.ResolvedPathogenId.Value.ToString();
                                    stagingLog.Add($"        ✅ Pathogen '{matchedName}' (ID: {marker.ResolvedPathogenId.Value}) matches criterion by ID");
                                }
                                // STRATEGY 2: Fall back to name-based matching (legacy criteria)
                                else if (acceptablePathogenNames.Any())
                                {
                                    // Look up the resolved pathogen's name
                                    if (!pathogenIdToName.TryGetValue(marker.ResolvedPathogenId.Value, out var resolvedPathogenName))
                                    {
                                        // Pathogen ID not found in lookup - skip this marker
                                        stagingLog.Add($"        ⚠️ Warning: Pathogen ID {marker.ResolvedPathogenId.Value} not found in lookup");
                                        continue;
                                    }

                                    // Compare the resolved pathogen name against acceptable names (case-insensitive)
                                    pathogenMatch = acceptablePathogenNames.Any(acceptableName => 
                                        string.Equals(acceptableName, resolvedPathogenName, StringComparison.OrdinalIgnoreCase));

                                    // If pathogen doesn't match, skip to next marker
                                    if (!pathogenMatch)
                                    {
                                        stagingLog.Add($"        ❌ Pathogen '{resolvedPathogenName}' does not match acceptable names: {string.Join(", ", acceptablePathogenNames)}");
                                        continue;
                                    }

                                    stagingLog.Add($"        ✅ Pathogen '{resolvedPathogenName}' matches criterion by name (legacy)");
                                }
                                else
                                {
                                    // Should never happen due to hasPathogenRestriction check above
                                    continue;
                                }
                            }

                            bool specimenMatch;
                            if (!acceptableSpecimens.Any())
                            {
                                // No specimen restriction - any specimen (or none) is OK
                                specimenMatch = true;
                            }
                            else
                            {
                                // Specimen restriction exists - must be resolved AND in the list
                                specimenMatch = stagedLabResult.ResolvedSpecimenTypeId.HasValue && 
                                               acceptableSpecimens.Contains(stagedLabResult.ResolvedSpecimenTypeId.Value);
                            }

                            bool testMethodMatch;
                            if (!acceptableTestMethods.Any())
                            {
                                // No test method restriction - any method (or none) is OK
                                testMethodMatch = true;
                            }
                            else
                            {
                                // Test method restriction exists - must be resolved AND in the list
                                testMethodMatch = marker.ResolvedTestMethodId.HasValue && 
                                                 acceptableTestMethods.Contains(marker.ResolvedTestMethodId.Value);
                            }

                            // For results, check if the normalized value is in the acceptable list or contains expected text
                            bool resultMatch;
                            if (!acceptableResults.Any())
                            {
                                // No result restriction - check for positive/detected
                                if (marker.NormalizedResultValue != null)
                                {
                                    resultMatch = marker.NormalizedResultValue.Contains("Positive", StringComparison.OrdinalIgnoreCase) ||
                                                 marker.NormalizedResultValue.Contains("Detected", StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    resultMatch = false;
                                }
                            }
                            else
                            {
                                // Result restriction exists - must match one of the acceptable results
                                // TODO: This needs proper TestResult lookup, for now just check text
                                resultMatch = marker.NormalizedResultValue != null &&
                                            (marker.NormalizedResultValue.Contains("Positive", StringComparison.OrdinalIgnoreCase) ||
                                             marker.NormalizedResultValue.Contains("Detected", StringComparison.OrdinalIgnoreCase));
                            }

                            if (pathogenMatch && specimenMatch && testMethodMatch && resultMatch)
                            {
                                criterionMatched = true;
                                matchedMarker = marker;
                                stagingLog.Add($"        ✅ MATCH on marker '{marker.TestName}'");
                                stagingLog.Add($"           Pathogen: {(marker.ResolvedPathogenId.HasValue ? await GetPathogenNameAsync(marker.ResolvedPathogenId.Value, cancellationToken) : "N/A")} {(pathogenMatch ? "✓" : "✗")}");
                                stagingLog.Add($"           Specimen: {(stagedLabResult.ResolvedSpecimenTypeId.HasValue ? await GetSpecimenTypeNameAsync(stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken) : "N/A")} {(specimenMatch ? "✓" : "✗")}");
                                stagingLog.Add($"           Test Method: {(marker.ResolvedTestMethodId.HasValue ? await GetTestMethodNameAsync(marker.ResolvedTestMethodId.Value, cancellationToken) : "N/A")} {(testMethodMatch ? "✓" : "✗")}");
                                stagingLog.Add($"           Result: {marker.NormalizedResultValue ?? "NULL"} {(resultMatch ? "✓" : "✗")}");

                                // Show WHY it matched
                                if (!acceptablePathogenNames.Any()) stagingLog.Add($"           (Pathogen: no restriction)");
                                if (!acceptableSpecimens.Any()) stagingLog.Add($"           (Specimen: no restriction)");
                                if (!acceptableTestMethods.Any()) stagingLog.Add($"           (Test Method: no restriction)");
                                if (!acceptableResults.Any()) stagingLog.Add($"           (Result: no restriction, positive/detected check)");

                                break;
                            }
                        }

                        criteriaMatches.Add((criterion, matchedMarker, criterionMatched));

                        if (!criterionMatched)
                        {
                            stagingLog.Add($"        ❌ No match");
                        }
                    }

                    // Determine if the case definition as a whole matches
                    // Simple logic: all required criteria must match
                    var requiredCriteria = criteriaMatches.Where(cm => cm.criteria.IsRequired == true).ToList();
                    var allRequiredMatched = requiredCriteria.All(cm => cm.matched) && requiredCriteria.Any();

                    if (allRequiredMatched || (criteriaMatches.Any() && criteriaMatches.All(cm => cm.matched)))
                    {
                        stagingLog.Add($"    ✅ CASE DEFINITION MATCHED: '{caseDefinition.Name}'");
                        stagingLog.Add($"       Confirmation Status: {caseDefinition.ConfirmationStatus?.Name ?? "Unknown"}");

                        // Determine if result is positive
                        var matchedMarkers = criteriaMatches.Where(cm => cm.marker != null).Select(cm => cm.marker!).Distinct().ToList();
                        var isPositive = matchedMarkers.Any(m => IsPositiveResult(m));

                        // Check if we already have this disease
                        var existingMatch = diseaseMatches.FirstOrDefault(dm => dm.Disease.Id == disease.Id);
                        if (existingMatch != null)
                        {
                            stagingLog.Add($"       Adding to existing disease match");
                            foreach (var marker in matchedMarkers.Where(m => !existingMatch.MatchedMarkers.Contains(m)))
                            {
                                existingMatch.MatchedMarkers.Add(marker);
                            }
                            if (!existingMatch.MatchedCaseDefinitions.Contains(caseDefinition))
                            {
                                existingMatch.MatchedCaseDefinitions.Add(caseDefinition);
                            }
                        }
                        else
                        {
                            stagingLog.Add($"       Creating new disease match");

                            // Load the first matched pathogen
                            Pathogen? matchedPathogen = null;
                            var firstMarkerWithPathogen = matchedMarkers.FirstOrDefault(m => m.ResolvedPathogenId.HasValue);
                            if (firstMarkerWithPathogen?.ResolvedPathogenId.HasValue == true)
                            {
                                matchedPathogen = await _context.Pathogens
                                    .IgnoreQueryFilters()
                                    .FirstOrDefaultAsync(p => p.Id == firstMarkerWithPathogen.ResolvedPathogenId.Value, cancellationToken);
                            }

                            diseaseMatches.Add(new DiseaseMatch
                            {
                                Disease = disease,
                                OriginalTopLevelDisease = disease,
                                MatchedPathogen = matchedPathogen,
                                MatchedCaseDefinition = caseDefinition,
                                MatchedCaseDefinitions = new List<CaseDefinition> { caseDefinition },
                                Source = MatchSource.CaseDefinitionMatch,
                                IsPositiveResult = isPositive,
                                MatchedMarkers = matchedMarkers
                            });
                        }
                    }
                    else
                    {
                        stagingLog.Add($"    ❌ Case definition NOT matched");
                    }
                }

                stagingLog.Add($"");
            }

            stagingLog.Add($"Disease identification complete: {diseaseMatches.Count} disease(s) matched via case definitions");

            if (diseaseMatches.Count == 0)
            {
                stagingLog.Add($"");
                stagingLog.Add($"❌ TROUBLESHOOTING: Why no diseases identified?");
                stagingLog.Add($"");
                stagingLog.Add($"Resolved fields summary:");
                stagingLog.Add($"  Specimen: {(stagedLabResult.ResolvedSpecimenTypeId.HasValue ? await GetSpecimenTypeNameAsync(stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken) : "NOT RESOLVED")}");

                var resolvedMarkerCount = stagedLabResult.Markers.Count(m => m.ResolvedPathogenId.HasValue);
                stagingLog.Add($"  Pathogens resolved: {resolvedMarkerCount} out of {stagedLabResult.Markers.Count} markers");

                foreach (var marker in stagedLabResult.Markers.Where(m => m.ResolvedPathogenId.HasValue))
                {
                    var pathogenName = await GetPathogenNameAsync(marker.ResolvedPathogenId!.Value, cancellationToken);
                    var testMethodName = marker.ResolvedTestMethodId.HasValue 
                        ? await GetTestMethodNameAsync(marker.ResolvedTestMethodId.Value, cancellationToken) 
                        : "NOT RESOLVED";

                    stagingLog.Add($"    • Pathogen: {pathogenName}");
                    stagingLog.Add($"      Test Method: {testMethodName}");
                    stagingLog.Add($"      Result: {marker.NormalizedResultValue ?? "NULL"}");
                }

                stagingLog.Add($"");
                stagingLog.Add($"Possible reasons:");
                stagingLog.Add($"  1. No active case definitions exist for the resolved pathogen/specimen/test combinations");
                stagingLog.Add($"  2. Case definitions exist but their criteria don't match the resolved fields");
                stagingLog.Add($"  3. Case definitions require additional fields that were not resolved");
                stagingLog.Add($"");
                stagingLog.Add($"Action: Review case definitions in the admin UI for diseases that should match these fields");
            }

            return diseaseMatches;
        }
        catch (Exception ex)
        {
            stagingLog.Add($"❌ ERROR: {ex.Message}");
            _logger.LogError(ex, "[DISEASE ID] Error identifying diseases from resolved fields");
            return diseaseMatches;
        }
    }

    private List<Guid> ParseGuidArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<Guid>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    private List<int> ParseIntArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<int>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    private List<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<string> GetPathogenNameAsync(Guid pathogenId, CancellationToken cancellationToken)
    {
        var pathogen = await _context.Pathogens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == pathogenId, cancellationToken);
        return pathogen?.Name ?? $"Unknown (ID: {pathogenId})";
    }

    private async Task<string> GetSpecimenTypeNameAsync(int specimenTypeId, CancellationToken cancellationToken)
    {
        var specimen = await _context.SpecimenTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == specimenTypeId, cancellationToken);
        return specimen?.Name ?? $"Unknown (ID: {specimenTypeId})";
    }

    private async Task<string> GetTestMethodNameAsync(int testMethodId, CancellationToken cancellationToken)
    {
        var method = await _context.TestMethods
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == testMethodId, cancellationToken);
        return method?.Name ?? $"Unknown (ID: {testMethodId})";
    }

    /// <summary>
    /// Helper: Normalize text for matching based on HL7 matching configuration rules
    /// </summary>
    private string NormalizeTextForMatching(string text, bool normalizeWhitespace, bool ignorePunctuation, bool caseInsensitive)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var result = text;

        // Case insensitive
        if (caseInsensitive)
        {
            result = result.ToUpperInvariant();
        }

        // Ignore punctuation
        if (ignorePunctuation)
        {
            result = new string(result.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
        }

        // Normalize whitespace
        if (normalizeWhitespace)
        {
            result = result.Trim();
            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }
        }

        return result;
    }

    /// <summary>
    /// Helper: Determine if a marker result indicates a positive/abnormal finding
    /// </summary>
    private bool IsPositiveResult(StagedMarker marker)
    {
        // Check qualitative result
        if (!string.IsNullOrWhiteSpace(marker.QualitativeResult))
        {
            var result = marker.QualitativeResult.ToUpperInvariant().Trim();

            if (result.Contains("POSITIVE") || 
                result.Contains("DETECTED") || 
                result.Contains("PRESENT") ||
                result.Contains("REACTIVE"))
            {
                return true;
            }

            if (result.Contains("NEGATIVE") || 
                result.Contains("NOT DETECTED") || 
                result.Contains("ABSENT") ||
                result.Contains("NON-REACTIVE"))
            {
                return false;
            }
        }

        // Check interpretation flag
        if (!string.IsNullOrWhiteSpace(marker.InterpretationFlag))
        {
            var flag = marker.InterpretationFlag.ToUpperInvariant();
            if (flag == "A" || flag == "H" || flag == "L" || flag == "AA" || flag == "HH" || flag == "LL")
            {
                return true; // Abnormal, High, or Low
            }
        }

        // If quantitative value exists and qualitative doesn't, assume positive
        if (marker.QuantitativeValue.HasValue && string.IsNullOrWhiteSpace(marker.QualitativeResult))
        {
            return true;
        }

        // Default to positive if we have a result at all
        return true;
    }

    /// <summary>
    /// Load existing case markers for progressive typing / disease refinement
    /// When a serovar-only result arrives, combine it with existing PCR/Culture markers
    /// </summary>
    private async Task<List<StagedMarker>> LoadExistingCaseMarkersForRefinementAsync(
        StagedPatient stagedPatient,
        StagedLabResult stagedLabResult,
        List<string> stagingLog,
        CancellationToken cancellationToken)
    {
        var existingMarkers = new List<StagedMarker>();

        if (stagedPatient.IsNew || !stagedPatient.ExistingPatientId.HasValue)
        {
            stagingLog.Add($"Patient is new - no existing cases to check");
            return existingMarkers;
        }

        stagingLog.Add($"Patient is existing - checking for open cases with related diseases...");

        // Get pathogen IDs from new markers
        var newPathogenIds = stagedLabResult.Markers
            .Where(m => m.ResolvedPathogenId.HasValue)
            .Select(m => m.ResolvedPathogenId!.Value)
            .ToList();

        if (!newPathogenIds.Any())
        {
            stagingLog.Add($"ℹ️ No pathogens resolved in new markers - skipping");
            return existingMarkers;
        }

        // Find diseases + parent diseases for these pathogens
        var relatedDiseaseIds = await _context.Pathogens
            .IgnoreQueryFilters()
            .Where(p => newPathogenIds.Contains(p.Id) && p.DiseaseId.HasValue)
            .Select(p => p.DiseaseId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!relatedDiseaseIds.Any())
        {
            stagingLog.Add($"ℹ️ New pathogens not associated with any diseases");
            return existingMarkers;
        }

        // CRITICAL FIX: Only expand to DIRECT ANCESTOR diseases (parents up the chain)
        // Do NOT include sibling diseases or unrelated branches
        // This prevents loading markers from sibling disease cases (e.g., STM135, StPaul, Hessarek)
        // when processing a new STM9 result
        var ancestorDiseaseIds = new HashSet<Guid>(relatedDiseaseIds);

        foreach (var diseaseId in relatedDiseaseIds)
        {
            var disease = await _context.Diseases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == diseaseId, cancellationToken);

            if (!string.IsNullOrEmpty(disease?.PathIds))
            {
                // PathIds contains the DIRECT ANCESTOR chain (e.g., "Salmonella/Typhimurium/STM9")
                // Parse ONLY the ancestors, not siblings or cousins
                var ancestorIds = disease.PathIds.Split('/')
                    .Where(id => Guid.TryParse(id, out _))
                    .Select(Guid.Parse)
                    .Where(id => id != diseaseId); // Exclude self (already in the set)

                foreach (var ancestorId in ancestorIds)
                {
                    ancestorDiseaseIds.Add(ancestorId);
                }

                stagingLog.Add($"  Disease: {disease.Name} (ID: {diseaseId})");
                stagingLog.Add($"    Ancestors: {string.Join(" → ", ancestorIds.Select(id => id.ToString().Substring(0, 8) + "..."))}");
            }
        }

        stagingLog.Add($"Searching for open cases with {ancestorDiseaseIds.Count} disease(s) in DIRECT ANCESTOR chain only...");
        stagingLog.Add($"  (Excludes sibling diseases to prevent cross-contamination)");

        // Find open cases ONLY for diseases in the direct ancestor chain
        // BEFORE: This was finding cases for ANY disease in expandedDiseaseIds (including siblings)
        // AFTER: Only finds cases whose disease is in the DIRECT ANCESTOR path
        var existingCases = await _context.Cases
            .IgnoreQueryFilters()
            .Include(c => c.ConfirmationStatus)
            .Include(c => c.Disease) // Load disease for filtering
            .Where(c => c.PatientId == stagedPatient.ExistingPatientId.Value &&
                       c.DiseaseId.HasValue &&
                       c.ConfirmationStatus != null &&
                       c.ConfirmationStatus.Name != "Closed" &&
                       !c.IsDeleted)
            .ToListAsync(cancellationToken);

        // CRITICAL FILTER: Only include cases whose disease is in the DIRECT ANCESTOR chain
        // This means the case disease must be either:
        // 1. One of the new pathogen diseases (exact match)
        // 2. A direct ancestor of one of the new pathogen diseases
        var filteredCases = existingCases
            .Where(c => ancestorDiseaseIds.Contains(c.DiseaseId!.Value))
            .ToList();

        if (filteredCases.Count < existingCases.Count)
        {
            var excludedCount = existingCases.Count - filteredCases.Count;
            stagingLog.Add($"🛡️ SAFEGUARD: Filtered out {excludedCount} case(s) with sibling/unrelated diseases");

            var excludedCases = existingCases.Except(filteredCases).ToList();
            foreach (var excludedCase in excludedCases)
            {
                var excludedDisease = await _context.Diseases
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(d => d.Id == excludedCase.DiseaseId!.Value, cancellationToken);
                stagingLog.Add($"  Excluded: Case {excludedCase.FriendlyId} ({excludedDisease?.Name ?? "Unknown"}) - not in direct ancestor chain");
            }
        }

        existingCases = filteredCases;

        if (!existingCases.Any())
        {
            stagingLog.Add($"ℹ️ No existing open cases found");
            return existingMarkers;
        }

        stagingLog.Add($"✅ Found {existingCases.Count} existing open case(s):");

        // Load markers from existing cases
        foreach (var existingCase in existingCases)
        {
            var disease = await _context.Diseases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == existingCase.DiseaseId!.Value, cancellationToken);

            var labResults = await _context.LabResults
                .IgnoreQueryFilters()
                .Include(lr => lr.Markers)
                .Where(lr => lr.CaseId == existingCase.Id && !lr.IsDeleted)
                .ToListAsync(cancellationToken);

            stagingLog.Add($"  • Case {existingCase.FriendlyId}: {disease?.Name ?? "Unknown"} with {labResults.Count} lab result(s)");

            foreach (var labResult in labResults)
            {
                foreach (var marker in labResult.Markers.Where(m => m.PathogenId.HasValue))
                {
                    var pathogen = await _context.Pathogens
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == marker.PathogenId.Value, cancellationToken);

                    var testMethod = marker.TestMethodId.HasValue
                        ? await _context.TestMethods
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(tm => tm.Id == marker.TestMethodId.Value, cancellationToken)
                        : null;

                    // Resolve test result ID from qualitative text if FK is not set
                    int? testResultId = marker.TestResultId;
                    if (!testResultId.HasValue && !string.IsNullOrWhiteSpace(marker.QualitativeResultText))
                    {
                        // Try to resolve qualitative text to TestResult ID
                        // Use case-insensitive comparison by loading all active results and comparing in-memory
                        var qualTextUpper = marker.QualitativeResultText.ToUpperInvariant();
                        var allActiveResults = await _context.Set<TestResult>()
                            .IgnoreQueryFilters()
                            .Where(r => r.IsActive)
                            .ToListAsync(cancellationToken);

                        var testResult = allActiveResults
                            .FirstOrDefault(r => r.Name.ToUpperInvariant() == qualTextUpper);

                        testResultId = testResult?.Id;

                        if (testResultId.HasValue)
                        {
                            stagingLog.Add($"    - Resolved result text '{marker.QualitativeResultText}' → TestResultId={testResultId.Value}");
                        }
                        else
                        {
                            stagingLog.Add($"    - ⚠️ Could not resolve result text '{marker.QualitativeResultText}' to TestResult ID");
                        }
                    }

                    existingMarkers.Add(new StagedMarker
                    {
                        IsNew = false,
                        TestCode = marker.TestCode ?? marker.LOINCCode,
                        TestName = pathogen?.Name,
                        PathogenName = pathogen?.Name,
                        ResolvedPathogenId = marker.PathogenId,
                        PathogenStatus = FieldResolutionStatus.Resolved,
                        PathogenMatchMethod = MatchMethod.FromExistingCase,
                        TestMethodName = testMethod?.Name,
                        ResolvedTestMethodId = marker.TestMethodId,
                        TestMethodStatus = marker.TestMethodId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent,
                        TestMethodMatchMethod = marker.TestMethodId.HasValue ? MatchMethod.FromExistingCase : MatchMethod.None,
                        QualitativeResult = marker.QualitativeResultText,
                        NormalizedResultValue = marker.QualitativeResultText,
                        ResolvedTestResultId = testResultId,
                        TestResultStatus = testResultId.HasValue ? FieldResolutionStatus.Resolved : FieldResolutionStatus.NotPresent
                    });

                    stagingLog.Add($"    - Loaded: {pathogen?.Name ?? "Unknown"} / {testMethod?.Name ?? "N/A"} = {marker.QualitativeResultText ?? "N/A"}");
                }
            }
        }

        if (existingMarkers.Any())
        {
            stagingLog.Add($"✅ Loaded {existingMarkers.Count} marker(s) from existing case(s)");
        }

        return existingMarkers;
    }

    /// <summary>
    /// Helper overload to check if a result string is positive
    /// </summary>
    private bool IsPositiveResult(string? resultValue)
    {
        if (string.IsNullOrWhiteSpace(resultValue))
            return false;

        var result = resultValue.ToUpperInvariant().Trim();

        if (result.Contains("POSITIVE") ||
            result.Contains("DETECTED") ||
            result.Contains("PRESENT") ||
            result.Contains("REACTIVE"))
        {
            return true;
        }

        if (result.Contains("NEGATIVE") ||
            result.Contains("NOT DETECTED") ||
            result.Contains("ABSENT") ||
            result.Contains("NON-REACTIVE"))
        {
            return false;
        }

        // Default to positive if we have a non-empty result
        return true;
    }

    /// <summary>
    /// STEP 5: Evaluate reinfection rules and disease hierarchy for each matched disease
    /// </summary>
    private async Task EvaluateReinfectionAndHierarchyAsync(
        DiseaseMatch diseaseMatch,
        StagedPatient stagedPatient,
        StagedLabResult stagedLabResult,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[STAGING] Evaluating reinfection for disease {Disease}", diseaseMatch.Disease.Name);

            // If this is a new patient, no existing case check needed
            if (stagedPatient.IsNew)
            {
                diseaseMatch.ReinfectionDecision = ReinfectionDecision.NewCase;
                diseaseMatch.ReinfectionReason = "New patient - no existing cases";

                // Still evaluate disease hierarchy for initial classification
                await EvaluateDiseaseHierarchyAsync(diseaseMatch, stagedLabResult, cancellationToken);

                // Set case creation flags
                diseaseMatch.ShouldCreateNewCase = true;
                diseaseMatch.FinalDiseaseForCase = diseaseMatch.Disease;

                return;
            }

            // Check for existing cases for this patient + disease (exact match OR parent disease)
            // NOTE: IgnoreQueryFilters() required because:
            // 1. Case has a global query filter that checks Disease access permissions
            // 2. Disease navigation has a global query filter that requires HTTP context
            // 3. HL7 processing runs in background service without HTTP context

            // STRATEGY:
            // 1. Look for exact disease match (e.g., existing Salmonella Typhimurium case)
            // 2. Look for cases where the case disease is a DESCENDANT of the incoming disease
            //    (e.g., existing STM9 case when generic "Salmonella" result arrives)
            // 3. Look for cases where the INCOMING disease is a DESCENDANT of the case disease
            //    (e.g., existing generic "Salmonella" case when STM9 result arrives - REFINEMENT)
            // This allows disease refinement: Salmonella → Salmonella Typhimurium

            _logger.LogInformation("[REINFECTION CHECK] Looking for existing cases for patient {PatientId}, disease {Disease} (ID: {DiseaseId})",
                stagedPatient.ExistingPatientId, diseaseMatch.Disease.Name, diseaseMatch.Disease.Id);
            _logger.LogInformation("[REINFECTION CHECK] Incoming disease PathIds: '{PathIds}'", diseaseMatch.Disease.PathIds);

            var existingCases = await _context.Cases
                .IgnoreQueryFilters()  // Bypass Case soft delete + disease access filter
                .Include(c => c.Disease)
                .Include(c => c.ConfirmationStatus)
                .Where(c => c.PatientId == stagedPatient.ExistingPatientId &&
                           c.DiseaseId.HasValue &&
                           (c.DiseaseId == diseaseMatch.Disease.Id ||  // Exact match
                            // Check if the existing case's disease is a DESCENDANT of the incoming disease
                            // (i.e., existing case is MORE SPECIFIC than what we just detected)
                            (c.Disease != null && c.Disease.PathIds != null && 
                             c.Disease.PathIds.Contains(diseaseMatch.Disease.Id.ToString())) ||
                            // Check if the INCOMING disease is a DESCENDANT of the existing case's disease
                            // (i.e., existing case can be REFINED to more specific disease)
                            (c.Disease != null && diseaseMatch.Disease.PathIds != null &&
                             diseaseMatch.Disease.PathIds.Contains(c.DiseaseId.ToString()))) &&
                           c.ConfirmationStatus != null &&
                           c.ConfirmationStatus.Name != "Closed" &&
                           !c.IsDeleted)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("[REINFECTION CHECK] Found {Count} existing case(s)", existingCases.Count);
            if (existingCases.Any())
            {
                foreach (var ec in existingCases)
                {
                    _logger.LogInformation("[REINFECTION CHECK]   • Case {CaseId}, Disease: {Disease} (ID: {DiseaseId})",
                        ec.FriendlyId, ec.Disease?.Name ?? "NULL", ec.DiseaseId);
                }
            }

            if (!existingCases.Any())
            {
                diseaseMatch.ReinfectionDecision = ReinfectionDecision.NewCase;
                diseaseMatch.ReinfectionReason = "No existing active cases for this disease or parent diseases";

                // Evaluate disease hierarchy
                await EvaluateDiseaseHierarchyAsync(diseaseMatch, stagedLabResult, cancellationToken);

                // Set case creation flags
                diseaseMatch.ShouldCreateNewCase = true;
                diseaseMatch.FinalDiseaseForCase = diseaseMatch.Disease;

                return;
            }

            // Multiple active cases - flag for manual review
            if (existingCases.Count > 1)
            {
                diseaseMatch.ReinfectionDecision = ReinfectionDecision.ManualReview;
                diseaseMatch.ReinfectionReason = $"Multiple active cases found ({existingCases.Count}) - requires manual review";
                diseaseMatch.ExistingCase = existingCases.First(); // Just pick one for reference

                _logger.LogWarning("[STAGING] Multiple active cases found for patient {PatientId} and disease {Disease}", 
                    stagedPatient.ExistingPatientId, diseaseMatch.Disease.Name);

                return;
            }

            // Single existing case - check reinfection window
            var existingCase = existingCases.First();
            diseaseMatch.ExistingCase = existingCase;

            // Enhanced logging for date debugging
            _logger.LogInformation("[REINFECTION CHECK] Existing Case: {CaseId}", existingCase.FriendlyId);
            _logger.LogInformation("[REINFECTION CHECK] Existing Case DateOfOnset: {DateOfOnset}", existingCase.DateOfOnset?.ToString("yyyy-MM-dd") ?? "NULL");
            _logger.LogInformation("[REINFECTION CHECK] Existing Case DateOfNotification: {DateOfNotification}", existingCase.DateOfNotification?.ToString("yyyy-MM-dd") ?? "NULL");
            _logger.LogInformation("[REINFECTION CHECK] New Lab Result SpecimenCollectionDate: {SpecimenDate}", stagedLabResult.SpecimenCollectionDate?.ToString("yyyy-MM-dd") ?? "NULL");

            // TODO: Load reinfection window configuration from disease settings
            // For now, use a default 90-day window
            var reinfectionWindowDays = 90;

            // Use fallback chain: DateOfOnset → DateOfNotification → DateTime.MinValue
            // This matches the logic in CaseMatchingService for consistency
            var caseDate = existingCase.DateOfOnset ?? existingCase.DateOfNotification ?? DateTime.MinValue;
            var specimenDate = stagedLabResult.SpecimenCollectionDate ?? DateTime.UtcNow;
            var daysSinceCase = (specimenDate - caseDate).TotalDays;

            _logger.LogInformation("[REINFECTION CHECK] Using case date: {CaseDate} (source: {Source})", 
                caseDate.ToString("yyyy-MM-dd"), 
                existingCase.DateOfOnset.HasValue ? "DateOfOnset" : existingCase.DateOfNotification.HasValue ? "DateOfNotification" : "MinValue");
            _logger.LogInformation("[REINFECTION CHECK] Calculated days since case: {Days:F0}", daysSinceCase);
            _logger.LogInformation("[REINFECTION CHECK] Reinfection window: {Window} days", reinfectionWindowDays);

            if (Math.Abs(daysSinceCase) <= reinfectionWindowDays)
            {
                // Within reinfection window - link to existing case
                diseaseMatch.ReinfectionDecision = ReinfectionDecision.LinkToExisting;
                diseaseMatch.ReinfectionReason = $"Within reinfection window ({Math.Abs(daysSinceCase):F0} days from existing case)";

                _logger.LogInformation("[STAGING] Linking to existing case {CaseId}", existingCase.FriendlyId);

                // PRIORITY 1: Check if disease should be refined via multi-marker evaluation (progressive typing)
                // This combines existing case markers with new markers to find the best matching case definition
                await EvaluateDiseaseRefinementForExistingCaseAsync(diseaseMatch, stagedLabResult, cancellationToken);

                // PRIORITY 2: If multi-marker evaluation didn't find a refinement, but the directly matched disease is more specific,
                // apply direct disease refinement (single new result is enough to be more specific)
                if (!diseaseMatch.ShouldRefineDiseaseOnExistingCase && 
                    existingCase.DiseaseId.HasValue && 
                    diseaseMatch.Disease.Id != existingCase.DiseaseId.Value)
                {
                    var currentCaseDisease = await _context.Diseases
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(d => d.Id == existingCase.DiseaseId.Value, cancellationToken);

                    if (currentCaseDisease != null)
                    {
                        // Check if matched disease is a descendant (more specific) of the current case disease
                        if (diseaseMatch.Disease.Level > currentCaseDisease.Level &&
                            diseaseMatch.Disease.PathIds.Contains(currentCaseDisease.Id.ToString()))
                        {
                            // Direct match to more specific disease - refine using the new match
                            _logger.LogInformation("[DISEASE REFINEMENT] Direct match: {NewDisease} is more specific than {OldDisease} - will refine (no multi-marker refinement found)",
                                diseaseMatch.Disease.Name, currentCaseDisease.Name);

                            diseaseMatch.ShouldRefineDiseaseOnExistingCase = true;
                            diseaseMatch.RefinedDisease = diseaseMatch.Disease;
                            diseaseMatch.ReinfectionReason += $" | Disease will be refined: {currentCaseDisease.Name} → {diseaseMatch.Disease.Name}";
                        }
                    }
                }
            }
            else
            {
                // Outside reinfection window - create new case
                diseaseMatch.ReinfectionDecision = ReinfectionDecision.NewCase;
                diseaseMatch.ReinfectionReason = $"Outside reinfection window ({Math.Abs(daysSinceCase):F0} days from existing case)";

                _logger.LogInformation("[STAGING] Creating new case - outside reinfection window");

                // Evaluate disease hierarchy for new case
                await EvaluateDiseaseHierarchyAsync(diseaseMatch, stagedLabResult, cancellationToken);

                // Set case creation flags
                diseaseMatch.ShouldCreateNewCase = true;
                diseaseMatch.FinalDiseaseForCase = diseaseMatch.Disease;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error evaluating reinfection for disease {Disease}", diseaseMatch.Disease.Name);
            diseaseMatch.ReinfectionDecision = ReinfectionDecision.ManualReview;
            diseaseMatch.ReinfectionReason = $"Error during evaluation: {ex.Message}";
        }
    }

    /// <summary>
    /// STEP 3b: Evaluate disease hierarchy - find most specific matching disease
    /// Start at LOWEST (most specific) disease in family, work UP until case definition matches
    /// </summary>
    private async Task EvaluateDiseaseHierarchyAsync(
        DiseaseMatch diseaseMatch,
        StagedLabResult stagedLabResult,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[STAGING] Evaluating disease hierarchy for {Disease}", diseaseMatch.Disease.Name);

            // Load disease family (children and descendants)
            var diseaseFamily = await LoadDiseaseFamilyAsync(diseaseMatch.Disease.Id, cancellationToken);

            if (!diseaseFamily.Any())
            {
                // No children - this is already the most specific disease
                _logger.LogInformation("[STAGING] No child diseases found - using {Disease}", diseaseMatch.Disease.Name);
                return;
            }

            // Sort by level descending (deepest first)
            var sortedDiseases = diseaseFamily.OrderByDescending(d => d.Level).ToList();

            // Start with deepest child and work up
            foreach (var childDisease in sortedDiseases)
            {
                _logger.LogDebug("[STAGING] Checking child disease: {Disease} (Level {Level})", 
                    childDisease.Name, childDisease.Level);

                // Load case definitions for this disease
                // NOTE: CaseDefinition doesn't have query filters, but included for consistency
                var caseDefinitions = await _context.CaseDefinitions
                    .Where(cd => cd.DiseaseId == childDisease.Id && 
                                cd.Status == CaseDefinitionStatus.Current &&
                                cd.DateActiveFrom <= DateTime.UtcNow &&
                                (cd.DateActiveTo == null || cd.DateActiveTo >= DateTime.UtcNow))
                    .Include(cd => cd.Criteria)
                    .ToListAsync(cancellationToken);

                if (!caseDefinitions.Any())
                {
                    _logger.LogDebug("[STAGING] No active case definitions for {Disease}", childDisease.Name);
                    continue;
                }

                // Evaluate each marker against child disease case definitions
                // A child disease must ACTUALLY MATCH the lab result, not just have a case definition
                bool childMatches = false;
                CaseDefinitionMatchResult? childMatchResult = null;

                
                foreach (var marker in diseaseMatch.MatchedMarkers)
                {
                    // Create a MarkerResolutionResult from the staged marker
                    var markerResolution = new MarkerResolutionResult
                    {
                        PathogenId = marker.ResolvedPathogenId,
                        PathogenStatus = marker.PathogenStatus,
                        TestMethodId = marker.ResolvedTestMethodId,
                        TestMethodStatus = marker.TestMethodStatus,
                        SpecimenTypeId = marker.ResolvedSpecimenTypeId,
                        SpecimenTypeStatus = marker.SpecimenTypeStatus,
                        TestResultId = marker.ResolvedTestResultId,
                        TestResultStatus = marker.TestResultStatus,
                        QuantitativeValue = marker.QuantitativeValue
                    };

                    // Evaluate child disease case definitions
                    var matchResult = await _caseDefinitionMatchingService.MatchCaseDefinitionAsync(
                        markerResolution,
                        cancellationToken);

                    // Check if this match is for the child disease we're evaluating
                    if (matchResult != null && matchResult.DiseaseId == childDisease.Id)
                    {
                        childMatches = true;
                        childMatchResult = matchResult;
                        _logger.LogInformation("[STAGING] Child disease {Disease} matches case definition", childDisease.Name);
                        break;
                    }
                }

                if (childMatches && childMatchResult != null)
                {
                    // This child disease actually matches the lab result - use it
                    _logger.LogInformation("[STAGING] Using child disease: {Disease} (validated match)", childDisease.Name);
                    diseaseMatch.Disease = childDisease;
                    diseaseMatch.MatchedCaseDefinition = childMatchResult.CaseDefinition;
                    diseaseMatch.ConfirmationStatus = childMatchResult.ConfirmationStatus;
                    diseaseMatch.ConfirmationStatusId = childMatchResult.ConfirmationStatusId;
                    diseaseMatch.IsPartialMatch = childMatchResult.IsPartialMatch;
                    diseaseMatch.MissingFields = childMatchResult.MissingFields;
                    return;
                }
                else
                {
                    _logger.LogDebug("[STAGING] Child disease {Disease} has case definitions but does not match lab result", childDisease.Name);
                }
            }

            // No child matched - keep original disease
            _logger.LogInformation("[STAGING] No child disease matched - keeping original: {Disease}", diseaseMatch.Disease.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STAGING] Error evaluating disease hierarchy");
        }
    }

    /// <summary>
    /// STEP 3c: Check if disease should be refined for an existing case
    /// </summary>
    private async Task EvaluateDiseaseRefinementForExistingCaseAsync(
        DiseaseMatch diseaseMatch,
        StagedLabResult stagedLabResult,
        CancellationToken cancellationToken)
    {
        try
        {
            if (diseaseMatch.ExistingCase == null)
                return;

            _logger.LogInformation("[DISEASE REFINEMENT] Evaluating refinement for existing case {CaseId}", 
                diseaseMatch.ExistingCase.FriendlyId);

            var currentCaseDiseaseId = diseaseMatch.ExistingCase.DiseaseId;

            if (!currentCaseDiseaseId.HasValue)
            {
                _logger.LogWarning("[DISEASE REFINEMENT] Existing case has no disease ID");
                return;
            }

            var currentCaseDisease = await _context.Diseases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == currentCaseDiseaseId.Value, cancellationToken);

            if (currentCaseDisease == null)
            {
                _logger.LogWarning("[DISEASE REFINEMENT] Could not load current case disease {DiseaseId}", currentCaseDiseaseId);
                return;
            }

            _logger.LogInformation("[DISEASE REFINEMENT] Current case disease: {Disease} (Level: {Level})", 
                currentCaseDisease.Name, currentCaseDisease.Level);

            // STEP 1: Load ALL existing lab result markers from the case
            var existingLabResults = await _context.LabResults
                .IgnoreQueryFilters()
                .Include(lr => lr.Markers)
                .Where(lr => lr.CaseId == diseaseMatch.ExistingCase.Id && !lr.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingMarkers = existingLabResults
                .SelectMany(lr => lr.Markers)
                .Where(m => m.PathogenId.HasValue)
                .ToList();

            _logger.LogInformation("[DISEASE REFINEMENT] Existing case has {Count} lab result(s) with {MarkerCount} pathogen marker(s)", 
                existingLabResults.Count, existingMarkers.Count);

            // STEP 2: Combine existing markers with new incoming markers
            // Build a temporary LabResult that contains ALL markers for case-definition evaluation
            var combinedLabResult = new LabResult
            {
                SpecimenTypeId = stagedLabResult.ResolvedSpecimenTypeId ?? existingLabResults.FirstOrDefault()?.SpecimenTypeId,
                Markers = new List<LabResultMarker>()
            };

            // Load all active TestResults once for resolution (to avoid translation issues)
            var allActiveTestResults = await _context.Set<TestResult>()
                .IgnoreQueryFilters()
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);

            // Add existing markers with result resolution
            foreach (var existingMarker in existingMarkers)
            {
                // Resolve TestResultId from qualitative text if not set
                int? resolvedTestResultId = existingMarker.TestResultId;
                if (!resolvedTestResultId.HasValue && !string.IsNullOrWhiteSpace(existingMarker.QualitativeResultText))
                {
                    var qualTextUpper = existingMarker.QualitativeResultText.ToUpperInvariant();
                    var testResult = allActiveTestResults
                        .FirstOrDefault(r => r.Name.ToUpperInvariant() == qualTextUpper);
                    resolvedTestResultId = testResult?.Id;

                    if (resolvedTestResultId.HasValue)
                    {
                        _logger.LogInformation("[DISEASE REFINEMENT] Resolved existing marker result text '{Text}' → TestResultId={Id}", 
                            existingMarker.QualitativeResultText, resolvedTestResultId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("[DISEASE REFINEMENT] Could not resolve existing marker result text '{Text}' to TestResult ID", 
                            existingMarker.QualitativeResultText);
                    }
                }

                combinedLabResult.Markers.Add(new LabResultMarker
                {
                    PathogenId = existingMarker.PathogenId.Value,
                    TestMethodId = existingMarker.TestMethodId,
                    TestResultId = resolvedTestResultId,
                    QualitativeResultText = existingMarker.QualitativeResultText
                });
            }

            // Add new markers from incoming HL7
            foreach (var newMarker in stagedLabResult.Markers.Where(m => m.ResolvedPathogenId.HasValue))
            {
                combinedLabResult.Markers.Add(new LabResultMarker
                {
                    PathogenId = newMarker.ResolvedPathogenId.Value,
                    TestMethodId = newMarker.ResolvedTestMethodId,
                    TestResultId = newMarker.ResolvedTestResultId,
                    QualitativeResultText = newMarker.NormalizedResultValue ?? newMarker.QualitativeResult
                });
            }

            _logger.LogInformation("[DISEASE REFINEMENT] Combined marker set: {Count} total markers ({ExistingCount} existing + {NewCount} new)", 
                combinedLabResult.Markers.Count, existingMarkers.Count, stagedLabResult.Markers.Count(m => m.ResolvedPathogenId.HasValue));

            if (!combinedLabResult.Markers.Any())
            {
                _logger.LogWarning("[DISEASE REFINEMENT] No markers available for evaluation");
                return;
            }

            // STEP 3: Re-evaluate case definitions with combined marker set
            // This will identify if the combined markers match a more specific disease
            _logger.LogInformation("[DISEASE REFINEMENT] Re-evaluating case definitions with combined markers...");

            var combinedMatches = await _caseDefinitionMatchingService.MatchCaseDefinitionsForLabResultAsync(
                combinedLabResult,
                cancellationToken);

            if (!combinedMatches.Any())
            {
                _logger.LogInformation("[DISEASE REFINEMENT] No case definition matches with combined markers");
                return;
            }

            _logger.LogInformation("[DISEASE REFINEMENT] Combined markers matched {Count} case definition(s):", combinedMatches.Count);
            foreach (var match in combinedMatches)
            {
                _logger.LogInformation("[DISEASE REFINEMENT]   • {Disease} via '{CaseDefinition}'", 
                    match.Disease?.Name ?? "Unknown", match.CaseDefinition.Name);
            }

            // STEP 4: Find the most specific matching disease
            var matchedDiseases = combinedMatches
                .Where(m => m.Disease != null)
                .Select(m => m.Disease!)
                .ToList();

            if (!matchedDiseases.Any())
            {
                _logger.LogWarning("[DISEASE REFINEMENT] Matches found but no diseases resolved");
                return;
            }

            // Filter to most specific disease(s) in the matched set
            // Prioritize by:
            // 1. Disease specificity (Level - higher = more specific)
            // 2. Number of criteria (more criteria = more comprehensive definition)
            // 3. Confirmation status name (alphabetical as final tiebreaker)
            var mostSpecificMatches = combinedMatches
                .Where(m => m.Disease != null)
                .OrderByDescending(m => m.Disease!.Level)  // Most specific disease first
                .ThenByDescending(m => m.CaseDefinition?.Criteria?.Count ?? 0)  // More criteria = better match
                .ThenBy(m => m.ConfirmationStatus?.Name ?? string.Empty)  // Alphabetical tiebreaker
                .ToList();

            var mostSpecificMatch = mostSpecificMatches.FirstOrDefault();

            _logger.LogInformation("[DISEASE REFINEMENT] Selected case definition: '{CaseDefName}' with {CriteriaCount} criteria (from {TotalMatches} matches)",
                mostSpecificMatch?.CaseDefinition?.Name ?? "N/A",
                mostSpecificMatch?.CaseDefinition?.Criteria?.Count ?? 0,
                combinedMatches.Count);

            if (mostSpecificMatch?.Disease == null)
            {
                _logger.LogWarning("[DISEASE REFINEMENT] Could not determine most specific disease");
                return;
            }

            var refinedDisease = mostSpecificMatch.Disease;

            _logger.LogInformation("[DISEASE REFINEMENT] Most specific disease from combined evaluation: {Disease} (Level: {Level})", 
                refinedDisease.Name, refinedDisease.Level);

            // STEP 5: Check if refined disease is more specific than current case disease
            if (refinedDisease.Id != currentCaseDisease.Id)
            {
                // Different disease - check if it's a descendant (more specific)
                if (refinedDisease.Level > currentCaseDisease.Level &&
                    refinedDisease.PathIds.Contains(currentCaseDisease.Id.ToString()))
                {
                    // More specific disease found via combined marker evaluation!
                    diseaseMatch.ShouldRefineDiseaseOnExistingCase = true;
                    diseaseMatch.RefinedDisease = refinedDisease;
                    diseaseMatch.MatchedCaseDefinition = mostSpecificMatch.CaseDefinition;
                    diseaseMatch.ConfirmationStatusId = mostSpecificMatch.ConfirmationStatusId;
                    diseaseMatch.ConfirmationStatus = mostSpecificMatch.ConfirmationStatus;
                    diseaseMatch.ReinfectionReason += $" | Disease refined via combined marker evaluation: {currentCaseDisease.Name} → {refinedDisease.Name}";

                    _logger.LogInformation("[DISEASE REFINEMENT] ✅ REFINEMENT RECOMMENDED: {Old} → {New} (via {CaseDefinition})", 
                        currentCaseDisease.Name, refinedDisease.Name, mostSpecificMatch.CaseDefinition.Name);
                }
                else if (refinedDisease.Level <= currentCaseDisease.Level)
                {
                    _logger.LogInformation("[DISEASE REFINEMENT] Combined markers matched {Disease} but it's not more specific than current {Current} - no refinement", 
                        refinedDisease.Name, currentCaseDisease.Name);
                }
                else
                {
                    _logger.LogInformation("[DISEASE REFINEMENT] Refined disease {Refined} is not a descendant of current disease {Current} - no refinement", 
                        refinedDisease.Name, currentCaseDisease.Name);
                }
            }
            else
            {
                _logger.LogInformation("[DISEASE REFINEMENT] Combined markers still match current disease {Disease} - no refinement needed", 
                    currentCaseDisease.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DISEASE REFINEMENT] Error evaluating disease refinement");
        }
    }

    /// <summary>
    /// Resolve specimen type with exact or text matching
    /// </summary>
    private async Task ResolveSpecimenTypeAsync(
        StagedLabResult stagedLabResult,
        bool enableTextSearch,
        CancellationToken cancellationToken)
    {
        if (stagedLabResult == null) return;

        _logger.LogDebug("[CASE DEF MATCH] Resolving specimen type: Code='{Code}', Text='{Text}', TextSearchEnabled={Enabled}", 
            stagedLabResult.SpecimenTypeCode, stagedLabResult.SpecimenTypeText, enableTextSearch);

        // Try exact code match first
        if (!string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeCode))
        {
            var exactMatch = await _context.SpecimenTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => 
                    s.SnomedCode == stagedLabResult.SpecimenTypeCode || 
                    s.Hl7Code == stagedLabResult.SpecimenTypeCode ||
                    s.LoincSystemCode == stagedLabResult.SpecimenTypeCode, 
                    cancellationToken);

            if (exactMatch != null)
            {
                stagedLabResult.ResolvedSpecimenTypeId = exactMatch.Id;
                stagedLabResult.SpecimenMatchMethod = MatchMethod.Exact;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Specimen type matched by EXACT code: '{Code}' → '{Name}' (ID={Id})", 
                    stagedLabResult.SpecimenTypeCode, exactMatch.Name, exactMatch.Id);
                return;
            }

            _logger.LogDebug("[CASE DEF MATCH] No exact match for specimen code '{Code}'", stagedLabResult.SpecimenTypeCode);
        }

        // Text search fallback if enabled
        if (enableTextSearch && !string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeText))
        {
            const double textMatchThreshold = 0.80; // Default threshold for text matching
            _logger.LogDebug("[CASE DEF MATCH] Attempting TEXT search for specimen: '{Text}', Threshold={Threshold}", 
                stagedLabResult.SpecimenTypeText, textMatchThreshold);

            var allSpecimens = await _context.SpecimenTypes
                .IgnoreQueryFilters()
                .Where(s => s.IsActive)
                .ToListAsync(cancellationToken);

            var bestMatch = allSpecimens
                .Select(s => new
                {
                    Specimen = s,
                    Score = CalculateSimilarity(stagedLabResult.SpecimenTypeText, s.Name)
                })
                .Where(x => x.Score >= textMatchThreshold)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                stagedLabResult.ResolvedSpecimenTypeId = bestMatch.Specimen.Id;
                stagedLabResult.SpecimenMatchMethod = MatchMethod.Text;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Specimen type matched by TEXT: '{Text}' → '{Name}' (ID={Id}, Score={Score:F2})", 
                    stagedLabResult.SpecimenTypeText, bestMatch.Specimen.Name, bestMatch.Specimen.Id, bestMatch.Score);
                return;
            }

            _logger.LogWarning("[CASE DEF MATCH] ❌ No text match found for specimen '{Text}' above threshold {Threshold}", 
                stagedLabResult.SpecimenTypeText, textMatchThreshold);
        }
        else if (!enableTextSearch)
        {
            _logger.LogDebug("[CASE DEF MATCH] Text search disabled for specimen type");
        }
    }

    /// <summary>
    /// Resolve pathogen for a marker with exact or text matching
    /// </summary>
    private async Task ResolvePathogenAsync(
        StagedMarker marker,
        bool enableTextSearch,
        CancellationToken cancellationToken)
    {
        if (marker == null) return;

        _logger.LogInformation("[CASE DEF MATCH] Resolving pathogen: LOINC='{LOINC}', Name='{Name}', TextSearchEnabled={Enabled}", 
            marker.TestCode, marker.TestName, enableTextSearch);

        // Try exact LOINC → Pathogen match first
        if (!string.IsNullOrWhiteSpace(marker.TestCode))
        {
            var exactMatch = await _context.Pathogens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.LOINCCode == marker.TestCode, cancellationToken);

            if (exactMatch != null)
            {
                marker.ResolvedPathogenId = exactMatch.Id;
                marker.PathogenMatchMethod = MatchMethod.Exact;
                marker.PathogenName = exactMatch.Name;
                marker.PathogenStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Pathogen matched by EXACT LOINC: '{LOINC}' → '{Name}' (ID={Id})", 
                    marker.TestCode, exactMatch.Name, exactMatch.Id);
                return;
            }

            _logger.LogInformation("[CASE DEF MATCH] No exact LOINC match for pathogen '{LOINC}'", marker.TestCode);
        }
        else
        {
            _logger.LogInformation("[CASE DEF MATCH] Marker has no LOINC code (TestCode is empty/null)");
            marker.PathogenStatus = FieldResolutionStatus.NotPresent;
        }

        // Text search fallback if enabled
        if (enableTextSearch && !string.IsNullOrWhiteSpace(marker.TestName))
        {
            const double textMatchThreshold = 0.80; // Default threshold for text matching
            _logger.LogInformation("[CASE DEF MATCH] Attempting TEXT search for pathogen: '{Name}', Threshold={Threshold}", 
                marker.TestName, textMatchThreshold);

            var allPathogens = await _context.Pathogens
                .IgnoreQueryFilters()
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("[CASE DEF MATCH] Found {Count} active pathogens for text matching", allPathogens.Count);

            // Log first few pathogens for diagnostics
            if (allPathogens.Any())
            {
                var samples = allPathogens.Take(5);
                _logger.LogInformation("[CASE DEF MATCH] Sample pathogens: {Names}", 
                    string.Join(", ", samples.Select(p => $"'{p.Name}'")));
            }

            var matchScores = allPathogens
                .Select(p => new
                {
                    Pathogen = p,
                    Score = CalculateSimilarity(marker.TestName, p.Name)
                })
                .OrderByDescending(x => x.Score)
                .Take(5)
                .ToList();

            // Log top 5 matches for diagnostics
            _logger.LogInformation("[CASE DEF MATCH] Top matching pathogens for '{TestName}':", marker.TestName);
            foreach (var match in matchScores)
            {
                _logger.LogInformation("[CASE DEF MATCH]   - '{Name}' (Score: {Score:F2})", 
                    match.Pathogen.Name, match.Score);
            }

            var bestMatch = matchScores.FirstOrDefault(x => x.Score >= textMatchThreshold);

            if (bestMatch != null)
            {
                marker.ResolvedPathogenId = bestMatch.Pathogen.Id;
                marker.PathogenMatchMethod = MatchMethod.Text;
                marker.PathogenName = bestMatch.Pathogen.Name;
                marker.PathogenStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Pathogen matched by TEXT: '{Name}' → '{MatchName}' (ID={Id}, Score={Score:F2})", 
                    marker.TestName, bestMatch.Pathogen.Name, bestMatch.Pathogen.Id, bestMatch.Score);
                return;
            }

            _logger.LogWarning("[CASE DEF MATCH] ❌ No text match found for pathogen '{Name}' above threshold {Threshold}", 
                marker.TestName, textMatchThreshold);
            marker.PathogenStatus = FieldResolutionStatus.ParseFailed;
        }
        else if (!enableTextSearch)
        {
            _logger.LogInformation("[CASE DEF MATCH] Text search disabled for pathogen");
        }
        else if (string.IsNullOrWhiteSpace(marker.TestName))
        {
            _logger.LogInformation("[CASE DEF MATCH] Marker has no TestName - cannot attempt text search");
        }
    }

    /// <summary>
    /// Resolve test method for a marker with exact or text matching
    /// </summary>
    private async Task ResolveTestMethodAsync(
        StagedMarker marker,
        bool enableTextSearch,
        CancellationToken cancellationToken)
    {
        if (marker == null) return;

        _logger.LogDebug("[CASE DEF MATCH] Resolving test method: Code='{Code}', Text='{Text}', TextSearchEnabled={Enabled}", 
            marker.TestMethodCode, marker.TestMethodText, enableTextSearch);

        // Try exact code match first
        if (!string.IsNullOrWhiteSpace(marker.TestMethodCode))
        {
            var exactMatch = await _context.TestMethods
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => 
                    t.SnomedCode == marker.TestMethodCode ||
                    t.LoincMethodCode == marker.TestMethodCode ||
                    t.ExportCode == marker.TestMethodCode, 
                    cancellationToken);

            if (exactMatch != null)
            {
                marker.ResolvedTestMethodId = exactMatch.Id;
                marker.TestMethodMatchMethod = MatchMethod.Exact;
                marker.TestMethodName = exactMatch.Name;
                marker.TestMethodStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Test method matched by EXACT code: '{Code}' → '{Name}' (ID={Id})", 
                    marker.TestMethodCode, exactMatch.Name, exactMatch.Id);
                return;
            }

            _logger.LogDebug("[CASE DEF MATCH] No exact code match for test method '{Code}'", marker.TestMethodCode);
            marker.TestMethodStatus = FieldResolutionStatus.ParseFailed;
        }
        else
        {
            marker.TestMethodStatus = FieldResolutionStatus.NotPresent;
        }

        // Text search fallback if enabled
        if (enableTextSearch && !string.IsNullOrWhiteSpace(marker.TestMethodText))
        {
            const double textMatchThreshold = 0.80; // Threshold for fuzzy matching
            _logger.LogDebug("[CASE DEF MATCH] Attempting TEXT search for test method: '{Text}', Threshold={Threshold}", 
                marker.TestMethodText, textMatchThreshold);

            var allTestMethods = await _context.TestMethods
                .IgnoreQueryFilters()
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            var testMethodTextUpper = marker.TestMethodText.ToUpperInvariant();

            // STRATEGY 1: Try exact substring match first (e.g., "PCR" in "Influenza A RNA PCR")
            var substringMatch = allTestMethods
                .Where(t => testMethodTextUpper.Contains(t.Name.ToUpperInvariant()) || 
                           t.Name.ToUpperInvariant().Contains(testMethodTextUpper))
                .OrderByDescending(t => t.Name.Length) // Prefer longer/more specific matches
                .FirstOrDefault();

            if (substringMatch != null)
            {
                marker.ResolvedTestMethodId = substringMatch.Id;
                marker.TestMethodMatchMethod = MatchMethod.Text;
                marker.TestMethodName = substringMatch.Name;
                marker.TestMethodStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Test method matched by SUBSTRING: '{Text}' → '{Name}' (ID={Id})", 
                    marker.TestMethodText, substringMatch.Name, substringMatch.Id);
                return;
            }

            // STRATEGY 2: Fuzzy match if no substring found
            var bestMatch = allTestMethods
                .Select(t => new
                {
                    TestMethod = t,
                    Score = CalculateSimilarity(marker.TestMethodText, t.Name)
                })
                .Where(x => x.Score >= textMatchThreshold)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                marker.ResolvedTestMethodId = bestMatch.TestMethod.Id;
                marker.TestMethodMatchMethod = MatchMethod.Text;
                marker.TestMethodName = bestMatch.TestMethod.Name;
                marker.TestMethodStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[CASE DEF MATCH] ✅ Test method matched by FUZZY TEXT: '{Text}' → '{Name}' (ID={Id}, Score={Score:F2})", 
                    marker.TestMethodText, bestMatch.TestMethod.Name, bestMatch.TestMethod.Id, bestMatch.Score);
                return;
            }

            _logger.LogWarning("[CASE DEF MATCH] ❌ No text match found for test method '{Text}' above threshold {Threshold}", 
                marker.TestMethodText, textMatchThreshold);
            marker.TestMethodStatus = FieldResolutionStatus.ParseFailed;
        }
        else if (!enableTextSearch)
        {
            _logger.LogDebug("[CASE DEF MATCH] Text search disabled for test method");
        }
    }

    /// <summary>
    /// Normalize result value to standard terms (Positive, Negative, etc.)
    /// </summary>
    private void NormalizeResultValue(StagedMarker marker)
    {
        if (marker == null) 
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(marker.QualitativeResult))
        {
            marker.TestResultStatus = FieldResolutionStatus.NotPresent;
            return;
        }

        var originalValue = marker.QualitativeResult;
        var normalizedValue = originalValue.Trim().ToUpperInvariant();

        // Map common variants to standard terms
        if (new[] { "POS", "POSITIVE", "DETECTED", "REACTIVE", "ABNORMAL" }.Contains(normalizedValue))
        {
            marker.NormalizedResultValue = "Positive";
            marker.TestResultStatus = FieldResolutionStatus.Resolved;
        }
        else if (new[] { "NEG", "NEGATIVE", "NOT DETECTED", "NON-REACTIVE", "NORMAL" }.Contains(normalizedValue))
        {
            marker.NormalizedResultValue = "Negative";
            marker.TestResultStatus = FieldResolutionStatus.Resolved;
        }
        else if (new[] { "EQUIVOCAL", "INDETERMINATE", "INCONCLUSIVE" }.Contains(normalizedValue))
        {
            marker.NormalizedResultValue = "Indeterminate";
            marker.TestResultStatus = FieldResolutionStatus.Resolved;
        }
        else
        {
            marker.NormalizedResultValue = marker.QualitativeResult; // Keep original if no mapping
            marker.TestResultStatus = FieldResolutionStatus.Resolved; // Still resolved, just not normalized
        }

        if (marker.NormalizedResultValue != originalValue)
        {
            _logger.LogDebug("[CASE DEF MATCH] Normalized result: '{Original}' → '{Normalized}'", 
                originalValue, marker.NormalizedResultValue);
        }
    }

    /// <summary>
    /// Resolve test result to database ID
    /// </summary>
    private async Task ResolveTestResultAsync(
        StagedMarker marker,
        bool enableTextSearch,
        CancellationToken cancellationToken)
    {
        if (marker == null) return;

        _logger.LogError("[🔍 TEST RESULT ENTRY] Marker: TestCode='{TC}', TestName='{TN}', QualitativeResult='{QR}'",
            marker.TestCode ?? "NULL", marker.TestName ?? "NULL", marker.QualitativeResult ?? "NULL");

        // If no qualitative result, nothing to resolve
        if (string.IsNullOrWhiteSpace(marker.QualitativeResult))
        {
            _logger.LogError("[🔍 TEST RESULT] QualitativeResult is NULL or empty - cannot resolve");
            marker.TestResultStatus = FieldResolutionStatus.NotPresent;
            return;
        }

        _logger.LogDebug("[TEST RESULT] Resolving test result: Qualitative='{Qual}', TextSearchEnabled={Enabled}", 
            marker.QualitativeResult, enableTextSearch);

        // Get all active test results
        var testResults = await _context.Set<TestResult>()
            .IgnoreQueryFilters()
            .Where(tr => tr.IsActive)
            .ToListAsync(cancellationToken);

        _logger.LogError("[🔍 TEST RESULT] Found {Count} active TestResults: {Names}",
            testResults.Count, string.Join(", ", testResults.Select(tr => $"'{tr.Name}'")));

        if (!testResults.Any())
        {
            _logger.LogWarning("[TEST RESULT] No active TestResult entries found in database");
            marker.TestResultStatus = FieldResolutionStatus.ParseFailed;
            return;
        }

        var qualitativeResult = marker.QualitativeResult;

        // STRATEGY 1: Match by HL7 code
        _logger.LogError("[🔍 TEST RESULT] Trying STRATEGY 1: HL7 code match");
        var hl7Match = testResults.FirstOrDefault(tr =>
            !string.IsNullOrWhiteSpace(tr.Hl7Code) &&
            tr.Hl7Code.Equals(qualitativeResult, StringComparison.OrdinalIgnoreCase));

        if (hl7Match != null)
        {
            marker.ResolvedTestResultId = hl7Match.Id;
            marker.TestResultStatus = FieldResolutionStatus.Resolved;
            _logger.LogError("[🔍 TEST RESULT] ✅ HL7 code match SUCCESS: '{Result}' → '{Name}' (ID={Id})", 
                qualitativeResult, hl7Match.Name, hl7Match.Id);
            return;
        }
        _logger.LogError("[🔍 TEST RESULT] ❌ HL7 code match FAILED");

        // STRATEGY 2: Match by SNOMED code
        _logger.LogError("[🔍 TEST RESULT] Trying STRATEGY 2: SNOMED code match");
        var snomedMatch = testResults.FirstOrDefault(tr =>
            !string.IsNullOrWhiteSpace(tr.SnomedCode) &&
            tr.SnomedCode.Equals(qualitativeResult, StringComparison.OrdinalIgnoreCase));

        if (snomedMatch != null)
        {
            marker.ResolvedTestResultId = snomedMatch.Id;
            marker.TestResultStatus = FieldResolutionStatus.Resolved;
            _logger.LogError("[🔍 TEST RESULT] ✅ SNOMED code match SUCCESS: '{Result}' → '{Name}' (ID={Id})", 
                qualitativeResult, snomedMatch.Name, snomedMatch.Id);
            return;
        }
        _logger.LogError("[🔍 TEST RESULT] ❌ SNOMED code match FAILED");

        // STRATEGY 3: Text-based matching (if enabled)
        _logger.LogError("[🔍 TEST RESULT] Trying STRATEGY 3: Text search (enabled={Enabled})", enableTextSearch);
        if (enableTextSearch)
        {
            var normalizedResult = NormalizeText(qualitativeResult);
            _logger.LogError("[🔍 TEST RESULT] Normalized result: '{Original}' → '{Normalized}'", qualitativeResult, normalizedResult);

            _logger.LogError("[🔍 TEST RESULT] Comparing against database names:");
            foreach (var tr in testResults)
            {
                var normalizedName = NormalizeText(tr.Name);
                var matches = normalizedName == normalizedResult;
                _logger.LogError("[🔍 TEST RESULT]   '{DbName}' → '{NormName}' | Match={Match}", tr.Name, normalizedName, matches);
            }

            // Try exact name match
            var exactMatch = testResults.FirstOrDefault(tr =>
                NormalizeText(tr.Name) == normalizedResult);

            if (exactMatch != null)
            {
                marker.ResolvedTestResultId = exactMatch.Id;
                marker.TestResultStatus = FieldResolutionStatus.Resolved;
                _logger.LogError("[🔍 TEST RESULT] ✅ Exact text match SUCCESS: '{Result}' → '{Name}' (ID={Id})", 
                    qualitativeResult, exactMatch.Name, exactMatch.Id);
                return;
            }
            _logger.LogError("[🔍 TEST RESULT] ❌ Exact text match FAILED");

            // Try SNOMED display match
            var displayMatch = testResults.FirstOrDefault(tr =>
                !string.IsNullOrWhiteSpace(tr.SnomedDisplay) &&
                NormalizeText(tr.SnomedDisplay) == normalizedResult);

            if (displayMatch != null)
            {
                marker.ResolvedTestResultId = displayMatch.Id;
                marker.TestResultStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[TEST RESULT] ✅ SNOMED display match: '{Result}' → '{Name}' (ID={Id})", 
                    qualitativeResult, displayMatch.Name, displayMatch.Id);
                return;
            }

            // Try fuzzy match
            var fuzzyMatch = testResults.FirstOrDefault(tr =>
            {
                var normalizedName = NormalizeText(tr.Name);
                return normalizedName.Contains(normalizedResult) || normalizedResult.Contains(normalizedName);
            });

            if (fuzzyMatch != null)
            {
                marker.ResolvedTestResultId = fuzzyMatch.Id;
                marker.TestResultStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[TEST RESULT] ✅ Fuzzy text match: '{Result}' → '{Name}' (ID={Id})", 
                    qualitativeResult, fuzzyMatch.Name, fuzzyMatch.Id);
                return;
            }
        }

        // STRATEGY 4: Fallback to abnormal flag interpretation (if available)
        if (!string.IsNullOrWhiteSpace(marker.InterpretationFlag))
        {
            var flagMatch = marker.InterpretationFlag.ToUpper() switch
            {
                "A" or "H" or "HH" or ">" => testResults.FirstOrDefault(tr =>
                    tr.Name.Equals("Positive", StringComparison.OrdinalIgnoreCase) ||
                    tr.Name.Equals("Abnormal", StringComparison.OrdinalIgnoreCase)),
                "N" or "NORMAL" => testResults.FirstOrDefault(tr =>
                    tr.Name.Equals("Negative", StringComparison.OrdinalIgnoreCase) ||
                    tr.Name.Equals("Normal", StringComparison.OrdinalIgnoreCase)),
                _ => null
            };

            if (flagMatch != null)
            {
                marker.ResolvedTestResultId = flagMatch.Id;
                marker.TestResultStatus = FieldResolutionStatus.Resolved;
                _logger.LogInformation("[TEST RESULT] ✅ Abnormal flag match: '{Flag}' → '{Name}' (ID={Id})", 
                    marker.InterpretationFlag, flagMatch.Name, flagMatch.Id);
                return;
            }
        }

        _logger.LogDebug("[TEST RESULT] No match found for QualitativeResult='{Qual}', Flag='{Flag}'",
            qualitativeResult ?? "NULL", marker.InterpretationFlag ?? "NULL");
        marker.TestResultStatus = FieldResolutionStatus.ParseFailed;
    }

    /// <summary>
    /// Normalize text for comparison (remove special chars, uppercase, trim)
    /// </summary>
    private string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = new string(text
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .ToUpperInvariant()
            .Trim();

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    /// <summary>
    /// Determine field resolution status based on presence and resolution outcome
    /// </summary>
    private FieldResolutionStatus DetermineFieldStatus(bool fieldPresent, bool resolvedSuccessfully)
    {
        if (!fieldPresent)
            return FieldResolutionStatus.NotPresent;

        return resolvedSuccessfully ? FieldResolutionStatus.Resolved : FieldResolutionStatus.ParseFailed;
    }

    /// <summary>
    /// Evaluate case definitions for a disease
    /// </summary>
    private async Task<CaseDefinition?> EvaluateCaseDefinitionsForDiseaseAsync(
        Disease disease,
        StagedLabResult stagedLabResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CASE DEF] Evaluating case definitions for disease: {DiseaseName} (ID={DiseaseId})", 
            disease.Name, disease.Id);

        // Load active case definitions for this disease with laboratory criteria
        var caseDefinitions = await _context.CaseDefinitions
            .IgnoreQueryFilters()
            .Include(cd => cd.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory))
            .Where(cd => cd.DiseaseId == disease.Id && 
                         cd.Status == CaseDefinitionStatus.Current &&
                         cd.DateActiveFrom <= DateTime.UtcNow &&
                         (cd.DateActiveTo == null || cd.DateActiveTo >= DateTime.UtcNow))
            .OrderBy(cd => cd.Id)
            .ToListAsync(cancellationToken);

        if (!caseDefinitions.Any())
        {
            _logger.LogInformation("[CASE DEF] ℹ️ No active case definitions found for disease {DiseaseName}", disease.Name);
            return null;
        }

        _logger.LogInformation("[CASE DEF] Found {Count} active case definition(s) for disease {DiseaseName}", 
            caseDefinitions.Count, disease.Name);

        // Evaluate each case definition
        foreach (var caseDefinition in caseDefinitions)
        {
            var labCriteriaCount = caseDefinition.Criteria.Count(c => c.CriterionType == CriterionType.Laboratory);
            _logger.LogDebug("[CASE DEF] Evaluating definition: '{Name}' (ID={Id}, LabCriteria={Count})", 
                caseDefinition.Name, caseDefinition.Id, labCriteriaCount);

            if (await EvaluateSingleCaseDefinitionAsync(caseDefinition, disease, stagedLabResult, cancellationToken))
            {
                _logger.LogInformation("[CASE DEF] ✅ MATCHED case definition: '{Name}' for disease {Disease}", 
                    caseDefinition.Name, disease.Name);
                return caseDefinition;
            }
            else
            {
                _logger.LogDebug("[CASE DEF] ❌ Did not match definition: '{Name}'", caseDefinition.Name);
            }
        }

        _logger.LogInformation("[CASE DEF] ℹ️ No matching case definitions for disease {DiseaseName}", disease.Name);
        return null;
    }

    /// <summary>
    /// Evaluate a single case definition against staged lab result
    /// </summary>
    private async Task<bool> EvaluateSingleCaseDefinitionAsync(
        CaseDefinition caseDefinition,
        Disease disease,
        StagedLabResult stagedLabResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CASE DEF] ▶️ START Evaluating case definition: '{DefName}' for disease '{DiseaseName}' with {MarkerCount} markers", 
            caseDefinition.Name, disease.Name, stagedLabResult.Markers.Count);

        var labCriteria = caseDefinition.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory).ToList();

        if (!labCriteria.Any())
        {
            _logger.LogDebug("[CASE DEF] Definition '{Name}' has no lab criteria", caseDefinition.Name);
            return false;
        }

        _logger.LogDebug("[CASE DEF] Evaluating '{DefName}': {CriteriaCount} lab criteria", 
            caseDefinition.Name, labCriteria.Count);

        // Get text-search configuration for this disease (with inheritance)
        var diseaseConfig = await GetDiseaseHL7MatchingConfigAsync(disease, cancellationToken);

        _logger.LogInformation("[CASE DEF] Evaluating '{DefName}' - Text search: Specimen={Specimen}, Pathogen={Pathogen}, TestMethod={TestMethod}", 
            caseDefinition.Name,
            diseaseConfig?.SpecimenType_UseTextFallback ?? false, 
            diseaseConfig?.Pathogen_UseTextFallback ?? false, 
            diseaseConfig?.TestMethod_UseTextFallback ?? false);

        _logger.LogInformation("[CASE DEF] Disease Config Status - HasConfig: {HasConfig}, OverrideParent: {Override}", 
            diseaseConfig != null, 
            diseaseConfig?.OverrideParentRules ?? false);

        // First, resolve all HL7 values with proper matching
        await ResolveHL7ValuesAsync(stagedLabResult, diseaseConfig, cancellationToken);

        // Group criteria by group number and evaluate
        var criteriaGroups = labCriteria
            .GroupBy(lc => lc.GroupNumber)
            .OrderBy(g => g.Key);

        var groupNumber = 0;
        foreach (var group in criteriaGroups)
        {
            groupNumber++;
            var groupMatches = new List<bool>();
            var logicalOp = group.First().LogicalOperator;
            var isRequired = group.First().IsRequired ?? true;

            _logger.LogDebug("[CASE DEF] Evaluating group #{GroupNum}: {Count} criteria, Operator={Operator}, Required={Required}", 
                groupNumber, group.Count(), logicalOp, isRequired);

            foreach (var criteria in group)
            {
                var criteriaMatch = await EvaluateSingleLabCriteriaAsync(criteria, stagedLabResult, diseaseConfig, cancellationToken);
                groupMatches.Add(criteriaMatch);
                _logger.LogDebug("[CASE DEF]   Criteria result: {Result}", criteriaMatch ? "MATCH" : "NO MATCH");
            }

            // Apply logical operator within group
            var groupResult = logicalOp == LogicalOperator.AND
                ? groupMatches.All(m => m)
                : groupMatches.Any(m => m);

            _logger.LogDebug("[CASE DEF] Group #{GroupNum} result: {Result} (Required={Required})", 
                groupNumber, groupResult ? "PASS" : "FAIL", isRequired);

            if (isRequired && !groupResult)
            {
                _logger.LogDebug("[CASE DEF] ❌ Required group failed - definition does not match");
                return false; // Required group failed
            }
        }

        _logger.LogDebug("[CASE DEF] ✅ All criteria groups passed");
        return true;
    }

    /// <summary>
    /// Resolve all HL7 values in staged lab result with text-search fallback
    /// </summary>
    private async Task ResolveHL7ValuesAsync(
        StagedLabResult stagedLabResult,
        Sentinel.Models.HL7.DiseaseHL7MatchingConfig? config,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CASE DEF] Resolving HL7 values for {MarkerCount} markers - Pathogen text search enabled: {Enabled}", 
            stagedLabResult.Markers.Count, 
            config?.Pathogen_UseTextFallback ?? false);

        // Resolve specimen type
        await ResolveSpecimenTypeAsync(
            stagedLabResult,
            config?.SpecimenType_UseTextFallback ?? false,
            cancellationToken);

        // Populate specimen info and status on all markers (specimen is at lab result level)
        SpecimenType? specimenType = null;
        if (stagedLabResult.ResolvedSpecimenTypeId.HasValue)
        {
            specimenType = await _context.SpecimenTypes
                .FirstOrDefaultAsync(s => s.Id == stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken);
        }

        // Resolve each marker's pathogen, test method, and result
        foreach (var marker in stagedLabResult.Markers)
        {
            _logger.LogInformation("[CASE DEF] Processing marker - TestCode: '{TestCode}', TestName: '{TestName}', QualResult: '{QualResult}'", 
                marker.TestCode ?? "(empty)", 
                marker.TestName ?? "(empty)", 
                marker.QualitativeResult ?? "(empty)");

            // Copy specimen info from lab result level to marker
            marker.ResolvedSpecimenTypeId = stagedLabResult.ResolvedSpecimenTypeId;
            marker.SpecimenTypeName = specimenType?.Name;
            marker.SpecimenTypeStatus = DetermineFieldStatus(
                !string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeCode) || 
                !string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeText),
                stagedLabResult.ResolvedSpecimenTypeId.HasValue);

            await ResolvePathogenAsync(marker, config?.Pathogen_UseTextFallback ?? false, cancellationToken);
            await ResolveTestMethodAsync(marker, config?.TestMethod_UseTextFallback ?? false, cancellationToken);
            NormalizeResultValue(marker);
        }
    }

    /// <summary>
    /// Evaluate a single lab criteria against staged lab result
    /// </summary>
    private async Task<bool> EvaluateSingleLabCriteriaAsync(
        CaseDefinitionCriteria criteria,
        StagedLabResult stagedLabResult,
        Sentinel.Models.HL7.DiseaseHL7MatchingConfig? diseaseConfig,
        CancellationToken cancellationToken)
    {
        // Parse acceptable values from JSON
        var acceptableSpecimens = ParseJsonArray<int>(criteria.AcceptableSpecimenTypesJson ?? "[]");
        var acceptablePathogens = ParseJsonArray<Guid>(criteria.AcceptablePathogensJson ?? "[]");
        var acceptableTestMethods = ParseJsonArray<int>(criteria.AcceptableTestMethodsJson ?? "[]");
        var acceptableResults = ParseJsonArray<int>(criteria.AcceptableResultsJson ?? "[]");

        // CRITICAL FIX: Check if criteria is completely empty
        // Empty criteria should NOT match everything - it should fail validation
        bool hasCriteria = acceptableSpecimens.Any() || 
                          acceptablePathogens.Any() || 
                          acceptableTestMethods.Any() || 
                          acceptableResults.Any();

        if (!hasCriteria)
        {
            _logger.LogDebug("[CASE DEF] Criteria has no acceptable values specified - cannot match");
            return false; // Empty criteria = no match
        }

        // Check specimen match - only TRUE if criteria specified AND matched
        bool specimenMatch = false;
        if (acceptableSpecimens.Any())
        {
            specimenMatch = stagedLabResult.ResolvedSpecimenTypeId.HasValue &&
                           acceptableSpecimens.Contains(stagedLabResult.ResolvedSpecimenTypeId.Value);

            // Reject if text-matched but text search not enabled for this disease
            if (specimenMatch && 
                stagedLabResult.SpecimenMatchMethod == MatchMethod.Text && 
                diseaseConfig?.SpecimenType_UseTextFallback != true)
            {
                _logger.LogDebug("[CASE DEF] Specimen matched by text but text search not enabled for disease");
                return false;
            }
        }

        // Find markers that match this criteria
        var matchingMarkers = new List<StagedMarker>();

        foreach (var marker in stagedLabResult.Markers)
        {
            // CRITICAL FIX: Default to FALSE, only TRUE if criteria exists AND matches
            bool pathogenMatch = !acceptablePathogens.Any();  // TRUE only if no pathogen criteria (neutral)
            bool testMethodMatch = !acceptableTestMethods.Any();  // TRUE only if no test method criteria (neutral)
            bool resultMatch = !acceptableResults.Any();  // TRUE only if no result criteria (neutral)

            // Check pathogen - if criteria specified, must match
            if (acceptablePathogens.Any())
            {
                pathogenMatch = marker.ResolvedPathogenId.HasValue &&
                               acceptablePathogens.Contains(marker.ResolvedPathogenId.Value);

                if (pathogenMatch && 
                    marker.PathogenMatchMethod == MatchMethod.Text && 
                    diseaseConfig?.Pathogen_UseTextFallback != true)
                {
                    pathogenMatch = false;
                }
            }

            // Check test method - if criteria specified, must match
            if (acceptableTestMethods.Any())
            {
                testMethodMatch = marker.ResolvedTestMethodId.HasValue &&
                                 acceptableTestMethods.Contains(marker.ResolvedTestMethodId.Value);

                if (testMethodMatch && 
                    marker.TestMethodMatchMethod == MatchMethod.Text && 
                    diseaseConfig?.TestMethod_UseTextFallback != true)
                {
                    testMethodMatch = false;
                }
            }

            // Check result - if criteria specified, must match
            if (acceptableResults.Any())
            {
                // Load acceptable result names
                var acceptableResultNames = await _context.TestResults
                    .IgnoreQueryFilters()
                    .Where(tr => acceptableResults.Contains(tr.Id))
                    .Select(tr => tr.Name)
                    .ToListAsync(cancellationToken);

                resultMatch = !string.IsNullOrWhiteSpace(marker.NormalizedResultValue) &&
                             acceptableResultNames.Any(name => 
                                 string.Equals(name, marker.NormalizedResultValue, StringComparison.OrdinalIgnoreCase));
            }

            if (pathogenMatch && testMethodMatch && resultMatch)
            {
                matchingMarkers.Add(marker);
            }
        }

        // Apply RequireAllElementsMatch logic
        if (criteria.RequireAllElementsMatch ?? false)
        {
            // All specified elements must match
            bool hasAllElements = 
                (!acceptableSpecimens.Any() || specimenMatch) &&
                (!acceptablePathogens.Any() || matchingMarkers.Any()) &&
                (!acceptableTestMethods.Any() || matchingMarkers.Any()) &&
                (!acceptableResults.Any() || matchingMarkers.Any());

            return hasAllElements;
        }
        else
        {
            // At least one specified element must match
            return specimenMatch || matchingMarkers.Any();
        }
    }

    /// <summary>
    /// Get DiseaseHL7MatchingConfig for a disease with parent inheritance
    /// </summary>
    private async Task<Sentinel.Models.HL7.DiseaseHL7MatchingConfig?> GetDiseaseHL7MatchingConfigAsync(
        Disease disease,
        CancellationToken cancellationToken)
    {
        // Try to get config for this disease
        var config = await _context.DiseaseHL7MatchingConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.DiseaseId == disease.Id, cancellationToken);

        // If no config or not overriding parent, try parent
        if ((config == null || !config.OverrideParentRules) && disease.ParentDiseaseId.HasValue)
        {
            var parent = await _context.Diseases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == disease.ParentDiseaseId.Value, cancellationToken);

            if (parent != null)
            {
                var parentConfig = await GetDiseaseHL7MatchingConfigAsync(parent, cancellationToken);

                // If current has no config, use parent's completely
                if (config == null)
                {
                    config = parentConfig;
                }
            }
        }

        return config;
    }

    /// <summary>
    /// Parse JSON array from string
    /// </summary>
    private List<T> ParseJsonArray<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    /// <summary>
    /// Build a full address string from components
    /// </summary>
    private string BuildFullAddress(string? addressLine, string? city, string? state, string? postalCode)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(addressLine))
            parts.Add(addressLine.Trim());

        if (!string.IsNullOrWhiteSpace(city))
            parts.Add(city.Trim());

        if (!string.IsNullOrWhiteSpace(state))
            parts.Add(state.Trim());

        if (!string.IsNullOrWhiteSpace(postalCode))
            parts.Add(postalCode.Trim());

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Calculate text similarity (simple Levenshtein-based)
    /// </summary>
    private double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return 0.0;

        source = source.ToLowerInvariant().Trim();
        target = target.ToLowerInvariant().Trim();

        if (source == target) return 1.0;

        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Levenshtein distance implementation
    /// </summary>
    private int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            distance[i, 0] = i;
        for (int j = 0; j <= target.Length; j++)
            distance[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }

    /// <summary>
    /// Helper class for text-search configuration
    /// </summary>
    /// <summary>
    /// Apply storage preferences from matched case definition to staged lab result
    /// </summary>
    private void ApplyStoragePreferences(
        StagedLabResult stagedLabResult,
        CaseDefinitionCriteria? matchedCriteria)
    {
        if (matchedCriteria == null) return;

        // Apply specimen storage preference
        if (matchedCriteria.SpecimenStoragePreference == DataStoragePreference.StoreAsCanonical &&
            matchedCriteria.CanonicalSpecimenTypeId.HasValue)
        {
            stagedLabResult.ResolvedSpecimenTypeId = matchedCriteria.CanonicalSpecimenTypeId.Value;
        }

        // Apply marker storage preferences
        foreach (var marker in stagedLabResult.Markers)
        {
            // Apply pathogen storage preference
            if (matchedCriteria.BiomarkerStoragePreference == DataStoragePreference.StoreAsCanonical &&
                matchedCriteria.CanonicalPathogenId.HasValue)
            {
                marker.ResolvedPathogenId = matchedCriteria.CanonicalPathogenId.Value;
            }

            // Apply test method storage preference
            if (matchedCriteria.TestMethodStoragePreference == DataStoragePreference.StoreAsCanonical &&
                matchedCriteria.CanonicalTestMethodId.HasValue)
            {
                marker.ResolvedTestMethodId = matchedCriteria.CanonicalTestMethodId.Value;
            }

            // Apply result storage preference (normalize result text)
            if (matchedCriteria.ResultStoragePreference == DataStoragePreference.StoreAsCanonical &&
                matchedCriteria.CanonicalTestResultId.HasValue)
            {
                // We'll apply this when creating the marker entity
            }
        }
    }

    /// <summary>
    /// Get the canonical result text for storage from matched case definition
    /// </summary>
    private async Task<string?> GetCanonicalResultTextAsync(
        CaseDefinitionCriteria? matchedCriteria,
        CancellationToken cancellationToken)
    {
        if (matchedCriteria?.ResultStoragePreference != DataStoragePreference.StoreAsCanonical ||
            !matchedCriteria.CanonicalTestResultId.HasValue)
        {
            return null;
        }

        var canonicalResult = await _context.TestResults
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(tr => tr.Id == matchedCriteria.CanonicalTestResultId.Value, cancellationToken);

        return canonicalResult?.Name;
    }

    /// <summary>
    /// Identify diseases via case definition matching (supports partial matching)
    /// FIXED: Now evaluates multi-marker case definitions first, then single-marker definitions
    /// </summary>
    private async Task<List<DiseaseMatch>> IdentifyDiseasesViaCaseDefinitionsAsync(
        StagedLabResult stagedLabResult,
        List<string> stagingLog,
        CancellationToken cancellationToken)
    {
        var matches = new List<DiseaseMatch>();

        stagingLog.Add($"========================================");
        stagingLog.Add($"DISEASE IDENTIFICATION VIA CASE DEFINITIONS");
        stagingLog.Add($"========================================");
        stagingLog.Add($"Evaluating case definitions for {stagedLabResult.Markers.Count} marker(s)");
        stagingLog.Add("");

        // STEP 1: Try multi-marker evaluation for ALL markers together
        // This is essential for diseases requiring multiple markers (e.g., Salmonella Typhimurium needs PCR + Culture + Serotype)
        if (stagedLabResult.Markers.Count > 1)
        {
            stagingLog.Add($"🔍 STEP 1: Multi-marker evaluation ({stagedLabResult.Markers.Count} markers together)");
            stagingLog.Add($"   This evaluates case definitions requiring combined evidence from multiple markers");

            // Create a temporary LabResult for multi-marker evaluation
            var tempLabResult = new LabResult
            {
                SpecimenTypeId = stagedLabResult.ResolvedSpecimenTypeId,
                Markers = stagedLabResult.Markers.Select(m => new LabResultMarker
                {
                    PathogenId = m.ResolvedPathogenId ?? Guid.Empty,
                    TestMethodId = m.ResolvedTestMethodId,
                    TestResultId = m.ResolvedTestResultId,
                    QualitativeResultText = m.NormalizedResultValue ?? m.QualitativeResult
                }).ToList()
            };

            try
            {
                var multiMarkerResults = await _caseDefinitionMatchingService.MatchCaseDefinitionsForLabResultAsync(
                    tempLabResult,
                    cancellationToken);

                // SPECIFICITY FILTERING: When multiple case definitions match the same disease,
                // keep only the most specific one (most criteria satisfied)
                if (multiMarkerResults.Count > 1)
                {
                    stagingLog.Add($"🔍 SPECIFICITY FILTER: {multiMarkerResults.Count} case definitions matched");

                    // Group by disease
                    var groupedByDisease = multiMarkerResults.GroupBy(m => m.Disease?.Id).ToList();

                    var filteredResults = new List<CaseDefinitionMatchResult>();
                    foreach (var diseaseGroup in groupedByDisease)
                    {
                        var matchesForDisease = diseaseGroup.ToList();

                        if (matchesForDisease.Count == 1)
                        {
                            stagingLog.Add($"   • Disease {matchesForDisease[0].Disease?.Name}: 1 definition (keeping)");
                            filteredResults.Add(matchesForDisease[0]);
                        }
                        else
                        {
                            stagingLog.Add($"   • Disease {matchesForDisease[0].Disease?.Name}: {matchesForDisease.Count} definitions");
                            stagingLog.Add($"     (Scoring by unique matched pathogens)");

                            var scored = new List<(CaseDefinitionMatchResult match, int score)>();
                            foreach (var match in matchesForDisease)
                            {
                                var caseDefId = match.CaseDefinition?.Id ?? 0;

                                // Get all laboratory criteria for this definition
                                var laboratoryCriteria = await _context.CaseDefinitionCriteria
                                    .Where(c => c.CaseDefinitionId == caseDefId && c.CriterionType == CriterionType.Laboratory)
                                    .ToListAsync(cancellationToken);

                                // Count how many criteria were satisfied by the lab markers
                                var satisfiedCriteriaCount = 0;
                                foreach (var criterion in laboratoryCriteria)
                                {
                                    // Check if any lab marker satisfies this criterion
                                    bool criterionSatisfied = false;

                                    foreach (var marker in stagedLabResult.Markers.Where(m => m.ResolvedPathogenId.HasValue))
                                    {
                                        if (string.IsNullOrEmpty(criterion.AcceptablePathogensJson))
                                            continue;

                                        try
                                        {
                                            var acceptablePathogens = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(criterion.AcceptablePathogensJson);
                                            if (acceptablePathogens?.Contains(marker.ResolvedPathogenId!.Value) != true)
                                                continue;

                                            // Pathogen matches, now check other constraints
                                            bool markerMatches = true;

                                            // Check test method if specified
                                            if (!string.IsNullOrEmpty(criterion.AcceptableTestMethodsJson))
                                            {
                                                var acceptableMethods = System.Text.Json.JsonSerializer.Deserialize<List<int>>(criterion.AcceptableTestMethodsJson);
                                                if (acceptableMethods?.Any() == true && marker.ResolvedTestMethodId.HasValue)
                                                {
                                                    if (!acceptableMethods.Contains(marker.ResolvedTestMethodId.Value))
                                                        markerMatches = false;
                                                }
                                            }

                                            // Check specimen type if specified
                                            if (markerMatches && !string.IsNullOrEmpty(criterion.AcceptableSpecimenTypesJson))
                                            {
                                                var acceptableSpecimens = System.Text.Json.JsonSerializer.Deserialize<List<int>>(criterion.AcceptableSpecimenTypesJson);
                                                if (acceptableSpecimens?.Any() == true && marker.ResolvedSpecimenTypeId.HasValue)
                                                {
                                                    if (!acceptableSpecimens.Contains(marker.ResolvedSpecimenTypeId.Value))
                                                        markerMatches = false;
                                                }
                                            }

                                            // Check result if specified
                                            if (markerMatches && !string.IsNullOrEmpty(criterion.AcceptableResultsJson))
                                            {
                                                var acceptableResults = System.Text.Json.JsonSerializer.Deserialize<List<int>>(criterion.AcceptableResultsJson);
                                                if (acceptableResults?.Any() == true && marker.ResolvedTestResultId.HasValue)
                                                {
                                                    if (!acceptableResults.Contains(marker.ResolvedTestResultId.Value))
                                                        markerMatches = false;
                                                }
                                            }

                                            if (markerMatches)
                                            {
                                                criterionSatisfied = true;
                                                break; // This criterion is satisfied, no need to check more markers
                                            }
                                        }
                                        catch { /* Ignore JSON errors */ }
                                    }

                                    if (criterionSatisfied)
                                        satisfiedCriteriaCount++;
                                }

                                scored.Add((match: match, score: satisfiedCriteriaCount));
                                stagingLog.Add($"     - {match.CaseDefinition?.Name}: {satisfiedCriteriaCount} satisfied criteria");
                            }

                            var mostSpecific = scored.OrderByDescending(s => s.score).First();
                            stagingLog.Add($"     ✅ Keeping most specific: {mostSpecific.match.CaseDefinition?.Name} ({mostSpecific.score} satisfied criteria)");
                            filteredResults.Add(mostSpecific.match);
                                            }
                                        }

                    multiMarkerResults = filteredResults;
                    stagingLog.Add($"   After filtering: {multiMarkerResults.Count} case definition(s)");
                    stagingLog.Add("");
                }

                if (multiMarkerResults.Any())
                {
                    stagingLog.Add($"   ✅ Multi-marker evaluation found {multiMarkerResults.Count} matching case definition(s)");

                    foreach (var matchResult in multiMarkerResults)
                    {
                        if (matchResult.Disease == null) continue;

                        stagingLog.Add($"   ✅ MULTI-MARKER MATCH: {matchResult.Disease.Name}");
                        stagingLog.Add($"       Case Definition: {matchResult.CaseDefinition.Name}");
                        stagingLog.Add($"       Confirmation Status: {matchResult.ConfirmationStatus?.Name ?? "Default"}");

                        // Create disease match with ALL markers
                        var diseaseMatch = new DiseaseMatch
                        {
                            Disease = matchResult.Disease,
                            MatchedCaseDefinition = matchResult.CaseDefinition,
                            MatchedCaseDefinitions = new List<CaseDefinition> { matchResult.CaseDefinition },
                            Source = MatchSource.CaseDefinitionMatch,
                            IsPositiveResult = stagedLabResult.Markers.Any(m => IsPositiveResult(m.NormalizedResultValue)),
                            MatchedMarkers = stagedLabResult.Markers.ToList(),  // All markers contributed
                            IsPartialMatch = matchResult.IsPartialMatch,
                            MissingFields = matchResult.MissingFields,
                            ConfirmationStatusId = matchResult.ConfirmationStatusId,
                            ConfirmationStatus = matchResult.ConfirmationStatus
                        };

                        matches.Add(diseaseMatch);
                    }
                }
                else
                {
                    stagingLog.Add($"   ℹ️ No multi-marker case definitions matched");
                }
            }
            catch (Exception ex)
            {
                stagingLog.Add($"   ⚠️ Multi-marker evaluation error: {ex.Message}");
                _logger.LogWarning(ex, "[DISEASE ID] Multi-marker evaluation failed");
            }

            stagingLog.Add("");
        }

        // STEP 2: Single-marker evaluation for each marker individually
        // This catches simpler case definitions that only require one marker
        stagingLog.Add($"🔍 STEP 2: Single-marker evaluation (each marker independently)");

        foreach (var marker in stagedLabResult.Markers)
        {
            // Determine specimen type status from lab result level
            var specimenTypeStatus = stagedLabResult.ResolvedSpecimenTypeId.HasValue 
                ? FieldResolutionStatus.Resolved 
                : (!string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeCode) || !string.IsNullOrWhiteSpace(stagedLabResult.SpecimenTypeText)
                    ? FieldResolutionStatus.ParseFailed 
                    : FieldResolutionStatus.NotPresent);

            // Get specimen type name for logging
            var specimenTypeName = "N/A";
            if (stagedLabResult.ResolvedSpecimenTypeId.HasValue)
            {
                var specimenType = await _context.SpecimenTypes
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == stagedLabResult.ResolvedSpecimenTypeId.Value, cancellationToken);
                specimenTypeName = specimenType?.Name ?? "N/A";
            }

            stagingLog.Add($"   🔍 Marker: {marker.PathogenName ?? "Unknown pathogen"}");
            stagingLog.Add($"       - Test Method: {marker.TestMethodName ?? "N/A"}");
            stagingLog.Add($"       - Specimen Type: {specimenTypeName}");
            stagingLog.Add($"       - Result: {marker.NormalizedResultValue ?? "N/A"}");

            // Convert staged marker to resolution result for case matching
            // NOTE: Specimen type is a lab-result-level property, not marker-level
            var resolution = new MarkerResolutionResult
            {
                PathogenId = marker.ResolvedPathogenId,
                TestMethodId = marker.ResolvedTestMethodId,
                SpecimenTypeId = stagedLabResult.ResolvedSpecimenTypeId,
                TestResultId = marker.ResolvedTestResultId,
                PathogenStatus = marker.PathogenStatus,
                TestMethodStatus = marker.TestMethodStatus,
                SpecimenTypeStatus = specimenTypeStatus,
                TestResultStatus = marker.TestResultStatus
            };

            // Find matching case definition
            var matchResult = await _caseDefinitionMatchingService.MatchCaseDefinitionAsync(
                resolution,
                cancellationToken);

            if (matchResult == null)
            {
                stagingLog.Add($"       ❌ No single-marker case definition matched");
                stagingLog.Add("");
                continue;
            }

            stagingLog.Add($"       ✅ Single-marker match found");

            // Create disease match
            var disease = matchResult.Disease;
            if (disease == null)
            {
                stagingLog.Add($"       ⚠️ Disease not found for case definition");
                continue;
            }

            // Check if we already have a match for this disease (from multi-marker or previous single-marker)
            var existingMatch = matches.FirstOrDefault(m => m.Disease.Id == disease.Id);

            if (existingMatch != null)
            {
                // Add this marker to the existing disease match
                if (!existingMatch.MatchedMarkers.Any(m => m == marker))
                {
                    existingMatch.MatchedMarkers.Add(marker);
                }

                // If the new match has a different case definition, add it
                if (!existingMatch.MatchedCaseDefinitions.Any(cd => cd.Id == matchResult.CaseDefinition.Id))
                {
                    existingMatch.MatchedCaseDefinitions.Add(matchResult.CaseDefinition);
                }

                stagingLog.Add($"       ℹ️ Added marker to existing disease match: {disease.Name}");
            }
            else
            {
                // Create new disease match
                var diseaseMatch = new DiseaseMatch
                {
                    Disease = disease,
                    MatchedCaseDefinition = matchResult.CaseDefinition,
                    MatchedCaseDefinitions = new List<CaseDefinition> { matchResult.CaseDefinition },
                    Source = MatchSource.CaseDefinitionMatch,
                    IsPositiveResult = IsPositiveResult(marker.NormalizedResultValue),
                    MatchedMarkers = new List<StagedMarker> { marker },
                    IsPartialMatch = matchResult.IsPartialMatch,
                    MissingFields = matchResult.MissingFields,
                    ConfirmationStatusId = matchResult.ConfirmationStatusId,
                    ConfirmationStatus = matchResult.ConfirmationStatus
                };

                matches.Add(diseaseMatch);
                stagingLog.Add($"       ✅ SINGLE-MARKER MATCH: {disease.Name} - {matchResult.CaseDefinition.Name}");
            }

            stagingLog.Add("");
        }

        stagingLog.Add("");
        stagingLog.Add($"========================================");
        stagingLog.Add($"BEFORE FILTERING: {matches.Count} disease match(es)");
        stagingLog.Add($"========================================");
        foreach (var match in matches)
        {
            stagingLog.Add($"   • {match.Disease.Name} ({match.MatchedMarkers.Count} marker(s))");
        }
        stagingLog.Add($"========================================");
        stagingLog.Add("");

        // STEP 3: Filter to most specific diseases when parent/child both match
        // If both generic Salmonella and Salmonella Typhimurium match, keep only Typhimurium
        if (matches.Count > 1)
        {
            stagingLog.Add($"🔍 STEP 3: Disease hierarchy filtering (Parent/Child)");
            stagingLog.Add($"   Checking for parent/child disease relationships...");

            var filteredMatches = new List<DiseaseMatch>();

            foreach (var match in matches)
            {
                // Load full disease with hierarchy
                var disease = await _context.Diseases
                    .IgnoreQueryFilters()
                    .Include(d => d.ParentDisease)
                    .FirstOrDefaultAsync(d => d.Id == match.Disease.Id, cancellationToken);

                if (disease == null) continue;

                // Check if this is a parent disease
                if (disease.ParentDiseaseId == null)
                {
                    // This is a top-level disease - check if any child diseases also matched
                    var hasMatchingChild = matches.Any(m => 
                        m.Disease.Id != disease.Id && 
                        _context.Diseases.IgnoreQueryFilters().Any(d => d.Id == m.Disease.Id && d.ParentDiseaseId == disease.Id));

                    if (hasMatchingChild)
                    {
                        stagingLog.Add($"   ⚠️ Filtering out parent disease: {disease.Name} (child disease(s) matched)");
                        continue;  // Skip this parent disease
                    }
                }

                filteredMatches.Add(match);
            }

            if (filteredMatches.Count < matches.Count)
            {
                stagingLog.Add($"   ✅ Filtered from {matches.Count} to {filteredMatches.Count} disease(s)");
                matches = filteredMatches;
            }
            else
            {
                stagingLog.Add($"   ℹ️ No parent/child filtering needed");
            }

            stagingLog.Add("");

            // STEP 3b: Filter sibling diseases (same parent)
            // When multiple sibling diseases match (e.g., STM9, Salmonella 135, Saint Paul, Hessarek),
            // keep only the one with the most specific/most markers matched
            if (matches.Count > 1)
            {
                stagingLog.Add($"🔍 STEP 3b: Sibling disease filtering");
                stagingLog.Add($"   Checking for sibling diseases (same parent)...");
                _logger.LogInformation("[SIBLING FILTER] Starting sibling disease filtering for {Count} matches", matches.Count);

                // Load full disease records with parent information
                var diseaseIds = matches.Select(m => m.Disease.Id).ToList();
                var diseasesWithParents = await _context.Diseases
                    .IgnoreQueryFilters()
                    .Where(d => diseaseIds.Contains(d.Id))
                    .Select(d => new { d.Id, d.ParentDiseaseId, d.Name })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("[SIBLING FILTER] Loaded {Count} diseases with parent info: {Diseases}", 
                    diseasesWithParents.Count,
                    string.Join(", ", diseasesWithParents.Select(d => $"{d.Name} (Parent: {d.ParentDiseaseId})")));

                var diseaseParentMap = diseasesWithParents.ToDictionary(d => d.Id, d => d.ParentDiseaseId);

                // Group by parent disease ID
                var siblingGroups = matches
                    .Where(m => diseaseParentMap.ContainsKey(m.Disease.Id))
                    .GroupBy(m => diseaseParentMap[m.Disease.Id])
                    .Where(g => g.Key.HasValue && g.Count() > 1) // Only groups with siblings
                    .ToList();

                _logger.LogInformation("[SIBLING FILTER] Found {Count} sibling group(s)", siblingGroups.Count);

                if (siblingGroups.Any())
                {
                    stagingLog.Add($"   Found {siblingGroups.Count} sibling group(s):");

                    var filteredSiblingMatches = new List<DiseaseMatch>();

                    foreach (var siblingGroup in siblingGroups)
                    {
                        var siblings = siblingGroup.ToList();
                        stagingLog.Add($"   Group with {siblings.Count} siblings:");
                        _logger.LogInformation("[SIBLING FILTER] Processing sibling group with {Count} diseases", siblings.Count);

                        foreach (var sibling in siblings)
                        {
                            stagingLog.Add($"     • {sibling.Disease.Name} ({sibling.MatchedMarkers.Count} marker(s))");
                            _logger.LogInformation("[SIBLING FILTER]   - {Disease}: {MarkerCount} marker(s)", 
                                sibling.Disease.Name, sibling.MatchedMarkers.Count);
                        }

                        // IMPROVED LOGIC: Prioritize by specificity, not just count
                        // 1. First, find the disease(s) with the most markers
                        var maxMarkers = siblings.Max(s => s.MatchedMarkers.Count);
                        var bestByCount = siblings.Where(s => s.MatchedMarkers.Count == maxMarkers).ToList();

                        if (bestByCount.Count == 1)
                        {
                            // Clear winner by marker count
                            stagingLog.Add($"   ✅ Selected: {bestByCount[0].Disease.Name} ({maxMarkers} marker(s))");
                            _logger.LogInformation("[SIBLING FILTER] ✅ SELECTED: {Disease} with {Count} markers", 
                                bestByCount[0].Disease.Name, maxMarkers);
                            filteredSiblingMatches.Add(bestByCount[0]);
                        }
                        else
                        {
                            // Tie by marker count - check marker specificity
                            // Prefer diseases with more specific pathogens (e.g., serovar markers)
                            stagingLog.Add($"   ⚠️ Tie: {bestByCount.Count} siblings have {maxMarkers} marker(s)");
                            stagingLog.Add($"   🔍 Breaking tie by checking marker specificity...");
                            _logger.LogInformation("[SIBLING FILTER] Tie detected - checking marker specificity");

                            // Score each disease by counting markers that contain the disease name
                            // (indicating a disease-specific marker like "Salmonella Typhimurium 9" serovar)
                            var scoredSiblings = new List<(DiseaseMatch match, int specificityScore)>();

                            foreach (var sibling in bestByCount)
                            {
                                // Count how many markers have names that contain the disease name
                                var diseaseName = sibling.Disease.Name.ToLower();
                                var specificMarkerCount = sibling.MatchedMarkers
                                    .Count(m => !string.IsNullOrWhiteSpace(m.TestName) && 
                                               m.TestName.ToLower().Contains(diseaseName));

                                scoredSiblings.Add((sibling, specificMarkerCount));
                                stagingLog.Add($"     • {sibling.Disease.Name}: {specificMarkerCount} disease-specific marker(s)");
                                _logger.LogInformation("[SIBLING FILTER]   {Disease} has {Count} specific markers", 
                                    sibling.Disease.Name, specificMarkerCount);
                            }

                            // Select the disease(s) with the highest specificity score
                            var maxSpecificity = scoredSiblings.Max(s => s.specificityScore);
                            var bestBySpecificity = scoredSiblings
                                .Where(s => s.specificityScore == maxSpecificity)
                                .Select(s => s.match)
                                .ToList();

                            if (bestBySpecificity.Count == 1 && maxSpecificity > 0)
                            {
                                // Winner by specificity
                                stagingLog.Add($"   ✅ Selected by specificity: {bestBySpecificity[0].Disease.Name} ({maxSpecificity} specific marker(s))");
                                _logger.LogInformation("[SIBLING FILTER] ✅ SELECTED by specificity: {Disease}", 
                                    bestBySpecificity[0].Disease.Name);
                                filteredSiblingMatches.Add(bestBySpecificity[0]);
                            }
                            else
                            {
                                // Still a tie or all have 0 specificity - keep all (shouldn't happen often)
                                stagingLog.Add($"   ⚠️ Still tied or no specific markers - keeping all {bestByCount.Count} siblings");
                                _logger.LogWarning("[SIBLING FILTER] ⚠️ Unresolved tie - keeping all {Count} siblings", 
                                    bestByCount.Count);
                                filteredSiblingMatches.AddRange(bestByCount);
                            }
                        }

                        // Remove the non-selected siblings from matches
                        var removedSiblings = siblings.Except(filteredSiblingMatches).ToList();
                        foreach (var removed in removedSiblings)
                        {
                            stagingLog.Add($"   🗑️ Removed: {removed.Disease.Name} ({removed.MatchedMarkers.Count} marker(s))");
                            _logger.LogInformation("[SIBLING FILTER] 🗑️ REMOVED: {Disease} with {Count} markers", 
                                removed.Disease.Name, removed.MatchedMarkers.Count);
                        }
                    }

                    // Keep non-sibling matches and the selected siblings
                    var nonSiblingMatches = matches
                        .Where(m => !siblingGroups.Any(g => g.Contains(m)))
                        .ToList();

                    matches = nonSiblingMatches.Concat(filteredSiblingMatches).ToList();
                    stagingLog.Add($"   ✅ After sibling filtering: {matches.Count} disease(s)");
                    _logger.LogInformation("[SIBLING FILTER] ✅ FINAL: {Count} disease(s) after sibling filtering", matches.Count);
                }
                else
                {
                    stagingLog.Add($"   ℹ️ No sibling filtering needed");
                    _logger.LogInformation("[SIBLING FILTER] ℹ️ No sibling filtering needed");
                }

                stagingLog.Add("");
            }
        }

        stagingLog.Add($"========================================");
        stagingLog.Add($"✅ FINAL DISEASE MATCHES: {matches.Count}");
        stagingLog.Add($"========================================");
        foreach (var match in matches)
        {
            stagingLog.Add($"   • {match.Disease.Name} ({match.MatchedMarkers.Count} marker(s), Definition: {match.MatchedCaseDefinition?.Name ?? "N/A"})");
        }
        stagingLog.Add($"========================================");
        stagingLog.Add("");

        return matches;
    }

    /// <summary>
    /// Helper: Load disease family (all children and descendants)
    /// </summary>
    private async Task<List<Disease>> LoadDiseaseFamilyAsync(Guid diseaseId, CancellationToken cancellationToken)
    {
        // Use PathIds for efficient querying (contains parent IDs in hierarchy)
        // IgnoreQueryFilters() bypasses disease access control filter (requires HTTP context)
        var parentDisease = await _context.Diseases
            .IgnoreQueryFilters()  // Background service - bypass disease access control filter
            .FirstOrDefaultAsync(d => d.Id == diseaseId, cancellationToken);

        if (parentDisease == null)
            return new List<Disease>();

        // Find all diseases where PathIds contains this disease's ID
        // IgnoreQueryFilters() bypasses disease access control filter (requires HTTP context)
        var children = await _context.Diseases
            .IgnoreQueryFilters()  // Background service - bypass disease access control filter
            .Where(d => d.PathIds.Contains(diseaseId.ToString()) && 
                       d.Id != diseaseId &&
                       d.IsActive)
            .ToListAsync(cancellationToken);

        return children;
    }

    /// <summary>
    /// STEP 6: Commit staged entities to database within a transaction
    /// </summary>
    private async Task<DataExtractionResult> CommitStagedEntitiesToDatabaseAsync(
        HL7ProcessingStage stage,
        HL7Message message,
        CancellationToken cancellationToken)
    {
        var result = new DataExtractionResult();
        var commitLog = new List<string>();

        try
        {
            commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== DATABASE COMMIT START ==========");
            commitLog.Add($"Message Control ID: {message.MessageControlId}");
            commitLog.Add($"Staging Decision: {stage.Decision}");
            commitLog.Add("");

            _logger.LogInformation("[STAGING COMMIT] Beginning transaction commit for message {MessageControlId}", 
                message.MessageControlId);

            // Use the execution strategy to handle transactions with retry logic
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Use transaction for atomic commit
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Transaction started");
                commitLog.Add("");

                try
                {
                    // 1. Commit Patient
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 1: PATIENT COMMIT ---");

                if (stage.StagedPatient != null)
                {
                    if (stage.StagedPatient.IsNew)
                    {
                        commitLog.Add($"Creating NEW patient: {stage.StagedPatient.FirstName} {stage.StagedPatient.LastName}");

                        // Build new Patient entity from staged data
                        var newPatient = new Patient
                        {
                            GivenName = stage.StagedPatient.FirstName ?? string.Empty,
                            FamilyName = stage.StagedPatient.LastName ?? string.Empty,
                            DateOfBirth = stage.StagedPatient.DateOfBirth,
                            HomePhone = stage.StagedPatient.Phone,
                            EmailAddress = stage.StagedPatient.Email,
                            AddressLine = stage.StagedPatient.Address,
                            City = stage.StagedPatient.City,
                            PostalCode = stage.StagedPatient.Zip
                        };

                        // Map Sex to SexAtBirthId lookup
                        if (!string.IsNullOrEmpty(stage.StagedPatient.Sex))
                        {
                            var sexCode = stage.StagedPatient.Sex.Trim().ToUpper();
                            commitLog.Add($"  Mapping sex from HL7: '{stage.StagedPatient.Sex}' (normalized: '{sexCode}')");

                            // Map HL7 codes to full names
                            var sexName = sexCode switch
                            {
                                "M" => "Male",
                                "F" => "Female",
                                "O" => "Other",
                                "U" => "Unknown",
                                "A" => "Ambiguous",
                                "N" => "Not Applicable",
                                _ => sexCode // Use original if not a standard code
                            };

                            var sexAtBirth = await _context.SexAtBirths
                                .FirstOrDefaultAsync(s => s.Name.ToLower() == sexName.ToLower(), cancellationToken);

                            if (sexAtBirth != null)
                            {
                                newPatient.SexAtBirthId = sexAtBirth.Id;
                                commitLog.Add($"  ✅ Mapped sex '{stage.StagedPatient.Sex}' to '{sexAtBirth.Name}' (ID: {sexAtBirth.Id})");
                            }
                            else
                            {
                                commitLog.Add($"  ⚠️ Could not find sex '{sexName}' in database");
                                _logger.LogWarning("[STAGING COMMIT] Failed to map sex '{Sex}' to database lookup", stage.StagedPatient.Sex);
                            }
                        }

                        // Map State to StateId lookup
                        if (!string.IsNullOrEmpty(stage.StagedPatient.State))
                        {
                            var state = await _context.States
                                .FirstOrDefaultAsync(s => s.Code == stage.StagedPatient.State || s.Name == stage.StagedPatient.State, cancellationToken);

                            if (state != null)
                            {
                                newPatient.StateId = state.Id;
                                commitLog.Add($"  ✅ Mapped state '{stage.StagedPatient.State}' to '{state.Name}' (ID: {state.Id})");
                            }
                        }

                        // RETRY LOOP for FriendlyId collisions
                        // The FriendlyId is auto-generated by a database trigger on INSERT
                        // If multiple messages process simultaneously, collisions can occur
                        const int maxRetries = 5;
                        int retryCount = 0;
                        bool patientCreated = false;

                        while (!patientCreated && retryCount < maxRetries)
                        {
                            try
                            {
                                _context.Patients.Add(newPatient);
                                await _context.SaveChangesAsync(cancellationToken);
                                patientCreated = true;
                                result.Patient = newPatient;
                                commitLog.Add($"✅ Patient created successfully: ID={result.Patient.Id}, FriendlyId={result.Patient.FriendlyId}");
                                result.Warnings.Add($"[PATIENT] ✅ Created new patient: {result.Patient.GivenName} {result.Patient.FamilyName}");
                                _logger.LogInformation("[STAGING COMMIT] Created patient {PatientId}", result.Patient.Id);

                                // Queue patient address for background geocoding
                                if (_geocodingQueue != null && !string.IsNullOrWhiteSpace(newPatient.AddressLine))
                                {
                                    var fullAddress = BuildFullAddress(
                                        newPatient.AddressLine,
                                        newPatient.City,
                                        newPatient.StateId.HasValue ? await _context.States.Where(s => s.Id == newPatient.StateId).Select(s => s.Code).FirstOrDefaultAsync(cancellationToken) : null,
                                        newPatient.PostalCode);

                                    _geocodingQueue.Enqueue(newPatient.Id, fullAddress);
                                    commitLog.Add($"  📍 Queued address for background geocoding: {fullAddress}");
                                    _logger.LogInformation("[GEOCODING] Queued patient {PatientId} for background geocoding", newPatient.Id);
                                }
                            }
                            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) 
                                when (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                                      sqlEx.Message.Contains("IX_Patients_FriendlyId"))
                            {
                                retryCount++;
                                commitLog.Add($"  ⚠️ FriendlyId collision detected (attempt {retryCount}/{maxRetries})");
                                _logger.LogWarning("[STAGING COMMIT] FriendlyId collision on attempt {Attempt} for patient {Name}", 
                                    retryCount, $"{stage.StagedPatient.FirstName} {stage.StagedPatient.LastName}");

                                // Clear the failed entity from change tracker
                                _context.Entry(newPatient).State = EntityState.Detached;

                                if (retryCount >= maxRetries)
                                {
                                    commitLog.Add($"  ❌ FAILED: Max retries exceeded for FriendlyId collision");
                                    _logger.LogError("[STAGING COMMIT] Max retries exceeded for FriendlyId collision");
                                    throw new InvalidOperationException(
                                        $"Failed to create patient after {maxRetries} attempts due to FriendlyId collisions. " +
                                        "This indicates high concurrency or database trigger issues.", dbEx);
                                }

                                // Wait briefly before retry (exponential backoff)
                                await Task.Delay(50 * retryCount, cancellationToken);

                                // Create a fresh patient entity for the next attempt
                                newPatient = new Patient
                                {
                                    GivenName = stage.StagedPatient.FirstName ?? string.Empty,
                                    FamilyName = stage.StagedPatient.LastName ?? string.Empty,
                                    DateOfBirth = stage.StagedPatient.DateOfBirth,
                                    HomePhone = stage.StagedPatient.Phone,
                                    EmailAddress = stage.StagedPatient.Email,
                                    AddressLine = stage.StagedPatient.Address,
                                    City = stage.StagedPatient.City,
                                    PostalCode = stage.StagedPatient.Zip,
                                    SexAtBirthId = result.Patient?.SexAtBirthId, // Preserve mapped values
                                    StateId = result.Patient?.StateId
                                };
                            }
                        }
                    }
                    else
                    {
                        result.Patient = stage.StagedPatient.ExistingPatient;
                        commitLog.Add($"✅ Using EXISTING patient: ID={result.Patient!.Id}, FriendlyId={result.Patient.FriendlyId}");
                        commitLog.Add($"   Name: {result.Patient.GivenName} {result.Patient.FamilyName}");

                        // Update sex if missing and HL7 provides it
                        if (!result.Patient.SexAtBirthId.HasValue && !string.IsNullOrEmpty(stage.StagedPatient.Sex))
                        {
                            var sexCode = stage.StagedPatient.Sex.Trim().ToUpper();
                            commitLog.Add($"  Existing patient missing sex, attempting to populate from HL7: '{stage.StagedPatient.Sex}'");

                            var sexName = sexCode switch
                            {
                                "M" => "Male",
                                "F" => "Female",
                                "O" => "Other",
                                "U" => "Unknown",
                                "A" => "Ambiguous",
                                "N" => "Not Applicable",
                                _ => sexCode
                            };

                            var sexAtBirth = await _context.SexAtBirths
                                .FirstOrDefaultAsync(s => s.Name.ToLower() == sexName.ToLower(), cancellationToken);

                            if (sexAtBirth != null)
                            {
                                result.Patient.SexAtBirthId = sexAtBirth.Id;
                                await _context.SaveChangesAsync(cancellationToken);
                                commitLog.Add($"  ✅ Updated existing patient's sex to '{sexAtBirth.Name}' (ID: {sexAtBirth.Id})");
                            }
                            else
                            {
                                commitLog.Add($"  ⚠️ Could not find sex '{sexName}' in database");
                            }
                        }

                        result.Warnings.Add($"[PATIENT] ✅ Matched existing patient: {result.Patient!.GivenName} {result.Patient.FamilyName}");
                        _logger.LogInformation("[STAGING COMMIT] Using existing patient {PatientId}", result.Patient.Id);
                    }
                }
                commitLog.Add("");

                // 2. Commit Organizations
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 2: ORGANIZATION COMMIT ---");

                if (stage.StagedLaboratory != null)
                {
                    if (stage.StagedLaboratory.IsNew)
                    {
                        commitLog.Add($"Creating NEW laboratory: {stage.StagedLaboratory.Name}");

                        // Look up organization type by name
                        var labOrgType = await _context.OrganizationTypes
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ot => ot.Name == stage.StagedLaboratory.OrganizationTypeName, cancellationToken);

                        if (labOrgType == null)
                        {
                            var errorMsg = $"Organization type '{stage.StagedLaboratory.OrganizationTypeName}' not found in database";
                            commitLog.Add($"❌ ERROR: {errorMsg}");
                            _logger.LogError("[STAGING COMMIT] {Error}", errorMsg);
                            throw new InvalidOperationException(errorMsg);
                        }

                        commitLog.Add($"  OrganizationType: '{labOrgType.Name}' (ID={labOrgType.Id})");

                        // Build new Organization entity from staged data
                        var newLab = new Organization
                        {
                            Name = stage.StagedLaboratory.Name,
                            OrganizationTypeId = labOrgType.Id,
                            IsActive = true
                        };

                        _context.Organizations.Add(newLab);
                        await _context.SaveChangesAsync(cancellationToken);
                        result.Laboratory = newLab;
                        commitLog.Add($"✅ Laboratory created successfully: ID={result.Laboratory.Id}, Type={labOrgType.Name}");
                        result.Warnings.Add($"[LABORATORY] ✅ Created laboratory: {result.Laboratory.Name} (Type: {labOrgType.Name})");
                        _logger.LogInformation("[STAGING COMMIT] Created laboratory {OrgId} with type {TypeId}", result.Laboratory.Id, labOrgType.Id);
                    }
                    else
                    {
                        result.Laboratory = stage.StagedLaboratory.ExistingOrganization;
                        commitLog.Add($"✅ Using EXISTING laboratory: ID={result.Laboratory!.Id}, Name={result.Laboratory.Name}");
                        result.Warnings.Add($"[LABORATORY] ✅ Matched existing laboratory: {result.Laboratory!.Name}");
                    }
                }

                if (stage.StagedOrderingProvider != null)
                {
                    if (stage.StagedOrderingProvider.IsNew)
                    {
                        commitLog.Add($"Creating NEW ordering provider: {stage.StagedOrderingProvider.Name}");

                        // Look up organization type by name
                        var providerOrgType = await _context.OrganizationTypes
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(ot => ot.Name == stage.StagedOrderingProvider.OrganizationTypeName, cancellationToken);

                        if (providerOrgType == null)
                        {
                            var errorMsg = $"Organization type '{stage.StagedOrderingProvider.OrganizationTypeName}' not found in database";
                            commitLog.Add($"❌ ERROR: {errorMsg}");
                            _logger.LogError("[STAGING COMMIT] {Error}", errorMsg);
                            throw new InvalidOperationException(errorMsg);
                        }

                        commitLog.Add($"  OrganizationType: '{providerOrgType.Name}' (ID={providerOrgType.Id})");

                        // Build new Organization entity from staged data
                        var newProvider = new Organization
                        {
                            Name = stage.StagedOrderingProvider.Name,
                            OrganizationTypeId = providerOrgType.Id,
                            IsActive = true
                        };

                        _context.Organizations.Add(newProvider);
                        await _context.SaveChangesAsync(cancellationToken);
                        result.OrderingProvider = newProvider;
                        commitLog.Add($"✅ Ordering provider created successfully: ID={result.OrderingProvider.Id}, Type={providerOrgType.Name}");
                        result.Warnings.Add($"[ORDERING PROVIDER] ✅ Created provider: {result.OrderingProvider.Name} (Type: {providerOrgType.Name})");
                        _logger.LogInformation("[STAGING COMMIT] Created ordering provider {OrgId} with type {TypeId}", result.OrderingProvider.Id, providerOrgType.Id);
                    }
                    else
                    {
                        result.OrderingProvider = stage.StagedOrderingProvider.ExistingOrganization;
                        commitLog.Add($"✅ Using EXISTING ordering provider: ID={result.OrderingProvider!.Id}, Name={result.OrderingProvider.Name}");
                        result.Warnings.Add($"[ORDERING PROVIDER] ✅ Matched existing provider: {result.OrderingProvider!.Name}");
                    }
                }
                else
                {
                    commitLog.Add($"ℹ️ No ordering provider staged");
                }

                commitLog.Add("");

                // 3. Handle duplicate lab results
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 3: LAB RESULT COMMIT ---");

                // Apply storage preferences from matched case definitions before committing
                foreach (var diseaseMatch in stage.DiseaseMatches.Where(dm => dm.MatchedCaseDefinition != null))
                {
                    var labCriteriaForStorage = diseaseMatch.MatchedCaseDefinition!.Criteria
                        .Where(c => c.CriterionType == CriterionType.Laboratory)
                        .ToList();

                    if (labCriteriaForStorage.Any())
                        {
                            // Apply first matching criteria storage preferences
                            var matchedCriteria = labCriteriaForStorage.First();
                        ApplyStoragePreferences(stage.StagedLabResult!, matchedCriteria);
                        commitLog.Add($"Applied storage preferences from case definition: {diseaseMatch.MatchedCaseDefinition.Name}");
                        break; // Only apply from first matched definition
                    }
                }

                if (stage.DuplicateCheck.IsDuplicate)
                {
                    commitLog.Add($"Duplicate check result: IsDuplicate=true");

                    if (stage.DuplicateCheck.IsPatientMismatch)
                    {
                        commitLog.Add($"❌ CRITICAL: Patient mismatch detected - ROLLING BACK");
                        commitLog.Add($"Accession {stage.StagedLabResult!.AccessionNumber} exists for different patient");

                        result.RequiresManualReview = true;
                        result.ManualReviewReason = $"Patient mismatch: Same accession {stage.StagedLabResult!.AccessionNumber} for different patients";
                        result.Errors.Add($"[LAB RESULT] ⚠️ PATIENT MISMATCH: Accession {stage.StagedLabResult.AccessionNumber} exists for different patient");
                        result.Warnings.Add("[LAB RESULT] ❌ Lab result NOT created due to patient mismatch - requires manual review");
                        result.Warnings.AddRange(stage.Warnings);
                        result.Warnings.AddRange(commitLog);

                        await transaction.RollbackAsync(cancellationToken);
                        commitLog.Add($"Transaction rolled back");

                        // Throw to exit the transaction - will be caught by outer catch
                        throw new InvalidOperationException($"Patient mismatch: Same accession {stage.StagedLabResult!.AccessionNumber} for different patients");
                    }

                    if (stage.DuplicateCheck.IsIdentical)
                    {
                        result.LabResult = stage.DuplicateCheck.ExistingLabResult;
                        commitLog.Add($"✅ Identical duplicate - using existing lab result: ID={result.LabResult!.Id}");
                        result.Warnings.Add($"[LAB RESULT] ⏭️ Duplicate detected - using existing lab result {result.LabResult!.Id}");
                        _logger.LogInformation("[STAGING COMMIT] Duplicate lab result found, using existing {LabResultId}", result.LabResult.Id);
                    }
                    else
                    {
                        // Same accession but different results → Create NEW lab result
                        // This preserves the audit trail for lab amendments/corrections
                        commitLog.Add($"Same accession, different results detected");
                        commitLog.Add($"Creating NEW lab result to preserve amendment/correction history");
                        commitLog.Add($"Accession: {stage.StagedLabResult!.AccessionNumber} (same as existing)");
                        commitLog.Add($"Markers in this report: {stage.StagedLabResult.Markers.Count}");

                        // Match specimen type
                        int? specimenTypeId = await MatchSpecimenTypeAsync(
                            stage.StagedLabResult.SpecimenTypeCode,
                            stage.StagedLabResult.SpecimenTypeText,
                            stage.StagedLabResult.SpecimenTypeCodingSystem,
                            cancellationToken);

                        if (specimenTypeId.HasValue)
                        {
                            commitLog.Add($"  ✅ SpecimenTypeId={specimenTypeId} matched");
                        }
                        else if (!string.IsNullOrWhiteSpace(stage.StagedLabResult.SpecimenTypeText))
                        {
                            commitLog.Add($"  ⚠️ Specimen type '{stage.StagedLabResult.SpecimenTypeText}' not matched");
                        }

                        var labResult = new LabResult
                        {
                            PatientId = result.Patient!.Id,
                            LaboratoryId = result.Laboratory!.Id,
                            OrderingProviderId = result.OrderingProvider?.Id,
                            AccessionNumber = stage.StagedLabResult!.AccessionNumber,
                            SpecimenCollectionDate = stage.StagedLabResult.SpecimenCollectionDate,
                            ResultDate = stage.StagedLabResult.ResultDate,
                            SpecimenTypeId = specimenTypeId,
                            Notes = $"Amended/corrected report for accession {stage.StagedLabResult.AccessionNumber}",
                            Markers = new List<LabResultMarker>()
                        };

                        foreach (var marker in stage.StagedLabResult.Markers)
                        {
                            commitLog.Add($"  • Processing marker: {marker.TestCode} = {marker.QualitativeResult ?? marker.QuantitativeValue?.ToString() ?? "N/A"}");

                            // Use resolved PathogenId from staging (already resolved with text matching)
                            Guid? pathogenId = marker.ResolvedPathogenId;

                            // ONLY use fallback if marker actually resolved a pathogen during field resolution
                            // Do NOT assign disease pathogen to non-pathogen markers (e.g., Patient Age, Sodium)
                            if (!pathogenId.HasValue)
                            {
                                // CRITICAL FIX: Do NOT fall back to disease pathogen - marker must have its own resolved pathogen
                                // This prevents incorrectly assigning COVID-19 pathogen to Influenza A markers
                                commitLog.Add($"    ℹ️ No resolved pathogen for marker - will skip (not a surveillance marker)");
                            }
                            else
                            {
                                commitLog.Add($"    ✅ PathogenId={pathogenId} (resolved from case definition matching)");
                            }

                            // Skip markers without a pathogen match - these are ancillary data (age, lab values, notes)
                            if (!pathogenId.HasValue)
                            {
                                commitLog.Add($"    ℹ️ SKIPPED: No pathogen for marker '{marker.TestName ?? marker.TestCode}' (ancillary data)");
                                _logger.LogDebug("[STAGING COMMIT] Skipped non-pathogen marker {TestCode} - ancillary data", marker.TestCode);
                                continue;
                            }

                            // Use resolved TestMethodId from staging or match
                            int? testMethodId = marker.ResolvedTestMethodId;
                            if (!testMethodId.HasValue)
                            {
                                testMethodId = await MatchTestMethodAsync(
                                    marker.TestMethodCode,
                                    marker.TestMethodText,
                                    marker.TestMethodCodingSystem,
                                    cancellationToken);
                            }

                            if (testMethodId.HasValue)
                            {
                                commitLog.Add($"    ✅ TestMethodId={testMethodId} matched");
                            }

                            // Get canonical result text if storage preference is set
                            string? resultText = marker.QualitativeResult;
                            var firstMatchedCriteria = stage.DiseaseMatches
                                .FirstOrDefault(dm => dm.MatchedCaseDefinition != null)?
                                .MatchedCaseDefinition?.Criteria
                                .FirstOrDefault(c => c.CriterionType == CriterionType.Laboratory);

                            if (firstMatchedCriteria?.ResultStoragePreference == DataStoragePreference.StoreAsCanonical)
                            {
                                var canonicalText = await GetCanonicalResultTextAsync(firstMatchedCriteria, cancellationToken);
                                if (canonicalText != null)
                                {
                                    resultText = canonicalText;
                                    commitLog.Add($"    ✅ Applied canonical result text: '{resultText}'");
                                }
                            }

                            labResult.Markers.Add(new LabResultMarker
                            {
                                TestCode = marker.TestCode,
                                LOINCCode = marker.TestCode,
                                PathogenId = pathogenId.Value,
                                TestMethodId = testMethodId,
                                QualitativeResultText = resultText,
                                QuantitativeValue = marker.QuantitativeValue,
                                QuantitativeUnit = marker.Units,
                                InterpretationFlag = marker.InterpretationFlag,
                                ResultStatus = marker.ResultStatus
                            });
                        }

                        _context.LabResults.Add(labResult);
                        await _context.SaveChangesAsync(cancellationToken);
                        result.LabResult = labResult;
                        commitLog.Add($"✅ New lab result created: ID={labResult.Id}, FriendlyId={labResult.FriendlyId}");
                        result.Warnings.Add($"[LAB RESULT] ✅ Created new lab result (amended/corrected report) with {labResult.Markers.Count} markers");
                        _logger.LogInformation("[STAGING COMMIT] Created new lab result {LabResultId} for amended report with accession {Accession}", 
                            labResult.Id, stage.StagedLabResult.AccessionNumber);
                    }
                }
                else
                {
                    // 4. Create new lab result
                    commitLog.Add($"Creating NEW lab result");
                    commitLog.Add($"Accession: {stage.StagedLabResult!.AccessionNumber}");
                    commitLog.Add($"Markers to add: {stage.StagedLabResult.Markers.Count}");

                    // Match specimen type (use resolved ID from case definition matching)
                    int? specimenTypeId = stage.StagedLabResult.ResolvedSpecimenTypeId;

                    if (!specimenTypeId.HasValue)
                    {
                        specimenTypeId = await MatchSpecimenTypeAsync(
                            stage.StagedLabResult.SpecimenTypeCode,
                            stage.StagedLabResult.SpecimenTypeText,
                            stage.StagedLabResult.SpecimenTypeCodingSystem,
                            cancellationToken);
                    }

                    if (specimenTypeId.HasValue)
                    {
                        commitLog.Add($"  ✅ SpecimenTypeId={specimenTypeId} matched");
                    }
                    else if (!string.IsNullOrWhiteSpace(stage.StagedLabResult.SpecimenTypeText))
                    {
                        commitLog.Add($"  ⚠️ Specimen type '{stage.StagedLabResult.SpecimenTypeText}' not matched");
                    }

                    var labResult = new LabResult
                    {
                        PatientId = result.Patient!.Id,
                        LaboratoryId = result.Laboratory!.Id,
                        OrderingProviderId = result.OrderingProvider?.Id,
                        AccessionNumber = stage.StagedLabResult!.AccessionNumber,
                        SpecimenCollectionDate = stage.StagedLabResult.SpecimenCollectionDate,
                        ResultDate = stage.StagedLabResult.ResultDate,
                        SpecimenTypeId = specimenTypeId,
                        Notes = stage.StagedLabResult.Notes,
                        Markers = new List<LabResultMarker>()
                    };

                    foreach (var marker in stage.StagedLabResult.Markers)
                    {
                        commitLog.Add($"  • Processing marker: {marker.TestCode} = {marker.QualitativeResult ?? marker.QuantitativeValue?.ToString() ?? "N/A"}");

                        // Use resolved PathogenId from staging (already resolved with text matching)
                        Guid? pathogenId = marker.ResolvedPathogenId;

                        // ONLY use fallback if marker actually resolved a pathogen during field resolution
                        // Do NOT assign disease pathogen to non-pathogen markers (e.g., Patient Age, Sodium)
                        if (!pathogenId.HasValue)
                        {
                            // CRITICAL FIX: Do NOT fall back to disease pathogen - marker must have its own resolved pathogen
                            // This prevents incorrectly assigning COVID-19 pathogen to Influenza A markers
                            commitLog.Add($"    ℹ️ No resolved pathogen for marker - will skip (not a surveillance marker)");
                        }
                        else
                        {
                            commitLog.Add($"    ✅ PathogenId={pathogenId} (resolved from case definition matching)");
                        }

                        // Skip markers without a pathogen match - these are ancillary data (age, lab values, notes)
                        if (!pathogenId.HasValue)
                        {
                            commitLog.Add($"    ℹ️ SKIPPED: No pathogen for marker '{marker.TestName ?? marker.TestCode}' (ancillary data)");
                            _logger.LogDebug("[STAGING COMMIT] Skipped non-pathogen marker {TestCode} - ancillary data", marker.TestCode);
                            continue;
                        }

                        // Use resolved TestMethodId from staging or match
                        int? testMethodId = marker.ResolvedTestMethodId;
                        if (!testMethodId.HasValue)
                        {
                            testMethodId = await MatchTestMethodAsync(
                                marker.TestMethodCode,
                                marker.TestMethodText,
                                marker.TestMethodCodingSystem,
                                cancellationToken);
                        }

                        if (testMethodId.HasValue)
                        {
                            commitLog.Add($"    ✅ TestMethodId={testMethodId} matched");
                        }

                        // Get canonical result text if storage preference is set
                        string? resultText = marker.QualitativeResult;
                        var firstMatchedCriteria = stage.DiseaseMatches
                            .FirstOrDefault(dm => dm.MatchedCaseDefinition != null)?
                            .MatchedCaseDefinition?.Criteria
                            .FirstOrDefault(c => c.CriterionType == CriterionType.Laboratory);

                        if (firstMatchedCriteria?.ResultStoragePreference == DataStoragePreference.StoreAsCanonical)
                        {
                            var canonicalText = await GetCanonicalResultTextAsync(firstMatchedCriteria, cancellationToken);
                            if (canonicalText != null)
                            {
                                resultText = canonicalText;
                                commitLog.Add($"    ✅ Applied canonical result text: '{resultText}'");
                            }
                        }

                        labResult.Markers.Add(new LabResultMarker
                        {
                            TestCode = marker.TestCode,
                            LOINCCode = marker.TestCode,
                            PathogenId = pathogenId.Value,
                            TestMethodId = testMethodId,
                            QualitativeResultText = resultText,
                            QuantitativeValue = marker.QuantitativeValue,
                            QuantitativeUnit = marker.Units,
                            InterpretationFlag = marker.InterpretationFlag,
                            ResultStatus = marker.ResultStatus
                            // Note: ReferenceRange is split into ReferenceRangeLow/High in model
                            // TODO: Parse marker.ReferenceRange if needed
                        });
                    }

                    _context.LabResults.Add(labResult);
                    await _context.SaveChangesAsync(cancellationToken);
                    result.LabResult = labResult;
                    commitLog.Add($"✅ Lab result created successfully: ID={labResult.Id}, FriendlyId={labResult.FriendlyId}");
                    result.Warnings.Add($"[LAB RESULT] ✅ Created lab result with {labResult.Markers.Count} markers");
                    _logger.LogInformation("[STAGING COMMIT] Created lab result {LabResultId}", labResult.Id);
                }

                commitLog.Add("");

                // 5. Process case creation/linking based on disease matches
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- STEP 4: CASE PROCESSING ---");
                commitLog.Add($"Disease matches to process: {stage.DiseaseMatches.Count}");

                // CRITICAL SAFEGUARD (Phase 1 Fix): Filter to only diseases with markers from THIS lab result
                // This prevents creating cases for sibling diseases pulled in by family-based case matching
                // but not actually detected in the current lab result

                // First, log what markers we have in the staged result for debugging
                var stagedMarkerKeys = stage.StagedLabResult!.Markers
                    .Select(m => $"{m.ResolvedPathogenId}|{m.ResolvedTestMethodId}|{m.ResolvedTestResultId}|{m.ResolvedSpecimenTypeId}")
                    .ToHashSet();

                commitLog.Add($"Staged lab result markers ({stage.StagedLabResult.Markers.Count}):");
                foreach (var marker in stage.StagedLabResult.Markers)
                {
                    commitLog.Add($"  • {marker.TestName} = {marker.QualitativeResult} (P:{marker.ResolvedPathogenId}, TM:{marker.ResolvedTestMethodId}, TR:{marker.ResolvedTestResultId}, ST:{marker.ResolvedSpecimenTypeId})");
                }

                var relevantDiseaseMatches = stage.DiseaseMatches
                    .Where(dm =>
                    {
                        // Check if any of the disease match's markers are present in the staged result
                        // Compare by marker identity (PathogenId, TestMethodId, TestResultId, SpecimenTypeId)
                        var hasMatchingMarker = dm.MatchedMarkers.Any(m =>
                        {
                            var markerKey = $"{m.ResolvedPathogenId}|{m.ResolvedTestMethodId}|{m.ResolvedTestResultId}|{m.ResolvedSpecimenTypeId}";
                            return stagedMarkerKeys.Contains(markerKey);
                        });

                        // Log disease match evaluation
                        if (!hasMatchingMarker)
                        {
                            commitLog.Add($"⚠️ Disease '{dm.Disease.Name}' has {dm.MatchedMarkers.Count} marker(s) but NONE match current lab result:");
                            foreach (var m in dm.MatchedMarkers)
                            {
                                var key = $"{m.ResolvedPathogenId}|{m.ResolvedTestMethodId}|{m.ResolvedTestResultId}|{m.ResolvedSpecimenTypeId}";
                                commitLog.Add($"     - {m.TestName} = {m.QualitativeResult} (key: {key})");
                            }
                        }

                        return hasMatchingMarker;
                    })
                    .ToList();

                if (relevantDiseaseMatches.Count < stage.DiseaseMatches.Count)
                {
                    var filteredCount = stage.DiseaseMatches.Count - relevantDiseaseMatches.Count;
                    commitLog.Add($"🛡️ SAFEGUARD: Filtered out {filteredCount} disease(es) with no markers from current lab result");
                    commitLog.Add($"   (Prevents creating unrelated sibling disease cases)");
                    _logger.LogInformation(
                        "[STAGING COMMIT] Filtered disease matches from {Total} to {Relevant} based on current lab result markers",
                        stage.DiseaseMatches.Count, relevantDiseaseMatches.Count);

                    // Log which diseases were filtered out for debugging
                    var filteredDiseases = stage.DiseaseMatches
                        .Where(dm => !relevantDiseaseMatches.Contains(dm))
                        .Select(dm => dm.Disease.Name)
                        .ToList();

                    if (filteredDiseases.Any())
                    {
                        commitLog.Add($"   Filtered diseases: {string.Join(", ", filteredDiseases)}");
                    }
                }

                commitLog.Add($"Processing {relevantDiseaseMatches.Count} relevant disease match(es)");

                var casesCreated = new List<Case>();
                var casesLinked = new List<Case>();

                foreach (var diseaseMatch in relevantDiseaseMatches)
                {
                    commitLog.Add($"");
                    commitLog.Add($"Processing disease: {diseaseMatch.Disease.Name}");
                    commitLog.Add($"  Is positive: {diseaseMatch.IsPositiveResult}");
                    commitLog.Add($"  Reinfection Decision: {diseaseMatch.ReinfectionDecision}");
                    commitLog.Add($"  Should Create New Case: {diseaseMatch.ShouldCreateNewCase}");

                    // Check for multiple active cases (requires manual review)
                    if (diseaseMatch.MultipleActiveCasesDetected)
                    {
                        commitLog.Add($"  ⚠️ Multiple active cases detected - flagging for manual review");
                        result.RequiresManualReview = true;
                        result.ManualReviewReason = $"Multiple active cases for {diseaseMatch.Disease.Name}";
                        result.Warnings.Add($"[CASE] ⚠️ Multiple active cases detected for {diseaseMatch.Disease.Name} - requires manual review");
                        continue;
                    }

                    // Create new case (honor reinfection decision - must check ShouldCreateNewCase first)
                    if (diseaseMatch.ShouldCreateNewCase)
                    {
                        commitLog.Add($"  Creating NEW case for disease: {diseaseMatch.FinalDiseaseForCase!.Name}");
                        if (diseaseMatch.ExistingCase != null)
                        {
                            commitLog.Add($"  (Previous case exists: {diseaseMatch.ExistingCase.FriendlyId}, but outside reinfection window)");
                        }

                        var newCase = new Case
                        {
                            PatientId = result.Patient!.Id,
                            DiseaseId = diseaseMatch.FinalDiseaseForCase!.Id,
                            Type = CaseType.Case,
                            DateOfNotification = DateTime.UtcNow,
                            ClinicalNotificationDate = result.LabResult!.SpecimenCollectionDate,
                            DateOfOnset = result.LabResult!.SpecimenCollectionDate,
                            // Copy address snapshot from patient
                            CaseAddressLine = result.Patient.AddressLine,
                            CaseCity = result.Patient.City,
                            CasePostalCode = result.Patient.PostalCode,
                            CaseStateId = result.Patient.StateId,
                            CaseAddressCapturedAt = DateTime.UtcNow
                        };

                        // Apply confirmation status from matched case definition
                        if (diseaseMatch.MatchedCaseDefinition != null)
                        {
                            newCase.ConfirmationStatusId = diseaseMatch.MatchedCaseDefinition.ConfirmationStatusId;
                            commitLog.Add($"  Applied confirmation status from case definition: {diseaseMatch.MatchedCaseDefinition.Name}");
                            commitLog.Add($"    Confirmation Status ID: {diseaseMatch.MatchedCaseDefinition.ConfirmationStatusId}");
                        }

                        _context.Cases.Add(newCase);
                        await _context.SaveChangesAsync(cancellationToken);

                        commitLog.Add($"  ✅ Case created: ID={newCase.Id}, FriendlyId={newCase.FriendlyId}");

                        // Link lab result to case
                        result.LabResult!.CaseId = newCase.Id;
                        await _context.SaveChangesAsync(cancellationToken);

                        commitLog.Add($"  ✅ Lab result linked to new case");

                        casesCreated.Add(newCase);
                        result.Warnings.Add($"[CASE] ✅ Created new case {newCase.FriendlyId} for {diseaseMatch.FinalDiseaseForCase.Name}");
                        _logger.LogInformation("[STAGING COMMIT] Created case {CaseId} for disease {DiseaseId}", 
                            newCase.Id, diseaseMatch.FinalDiseaseForCase.Id);
                    }
                    // Link to existing case
                    else if (diseaseMatch.ExistingCase != null && diseaseMatch.ReinfectionDecision == ReinfectionDecision.LinkToExisting)
                    {
                        commitLog.Add($"  Linking to EXISTING case: {diseaseMatch.ExistingCase.FriendlyId}");

                        // Check if disease should be refined
                        if (diseaseMatch.ShouldRefineDiseaseOnExistingCase && diseaseMatch.RefinedDisease != null)
                        {
                            commitLog.Add($"  🔄 Refining case disease from {diseaseMatch.ExistingCase.Disease?.Name ?? "N/A"} to {diseaseMatch.RefinedDisease.Name}");

                            diseaseMatch.ExistingCase.DiseaseId = diseaseMatch.RefinedDisease.Id;

                            // Update confirmation status if case definition provides one
                            if (diseaseMatch.ConfirmationStatusId.HasValue)
                            {
                                diseaseMatch.ExistingCase.ConfirmationStatusId = diseaseMatch.ConfirmationStatusId.Value;
                                commitLog.Add($"  🔄 Updated confirmation status to: {diseaseMatch.ConfirmationStatus?.Name ?? "N/A"}");
                            }

                            result.Warnings.Add($"[CASE] ✅ Refined case {diseaseMatch.ExistingCase.FriendlyId} disease to {diseaseMatch.RefinedDisease.Name}");
                            _logger.LogInformation("[STAGING COMMIT] Refined case {CaseId} disease to {DiseaseId}", 
                                diseaseMatch.ExistingCase.Id, diseaseMatch.RefinedDisease.Id);
                        }

                        // Link lab result to case using navigation property
                        result.LabResult!.CaseId = diseaseMatch.ExistingCase.Id;

                        await _context.SaveChangesAsync(cancellationToken);
                        casesLinked.Add(diseaseMatch.ExistingCase);
                        commitLog.Add($"  ✅ Lab result linked to case successfully");
                        result.Warnings.Add($"[CASE] ✅ Linked lab result to existing case {diseaseMatch.ExistingCase.FriendlyId} ({diseaseMatch.ReinfectionReason})");
                    }
                    else
                    {
                        commitLog.Add($"  ℹ️ No case action taken (Decision: {diseaseMatch.ReinfectionDecision})");
                    }
                }

                commitLog.Add("");

                // 6. Log summary
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] --- COMMIT SUMMARY ---");

                if (casesCreated.Any())
                {
                    commitLog.Add($"✅ Cases created: {casesCreated.Count}");
                    foreach (var c in casesCreated)
                    {
                        commitLog.Add($"  • {c.FriendlyId} (Disease: {c.Disease?.Name ?? "N/A"})");
                        result.Cases.Add(c); // Add to result
                    }
                    result.Warnings.Add($"[SUMMARY] Created {casesCreated.Count} case(s): {string.Join(", ", casesCreated.Select(c => c.FriendlyId))}");
                }
                if (casesLinked.Any())
                {
                    commitLog.Add($"✅ Cases linked: {casesLinked.Count}");
                    foreach (var c in casesLinked)
                    {
                        commitLog.Add($"  • {c.FriendlyId} (Disease: {c.Disease?.Name ?? "N/A"})");
                        result.Cases.Add(c); // Add to result
                    }
                    result.Warnings.Add($"[SUMMARY] Linked to {casesLinked.Count} existing case(s): {string.Join(", ", casesLinked.Select(c => c.FriendlyId))}");
                }

                commitLog.Add("");
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Committing transaction...");

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);
                result.Success = true;

                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== DATABASE COMMIT SUCCESS ==========");

                // Add staging logs FIRST, then commit logs
                result.Warnings.AddRange(stage.Warnings);
                result.Warnings.AddRange(commitLog);

                _logger.LogInformation("[STAGING COMMIT] ✅ Transaction committed successfully for message {MessageControlId}", 
                    message.MessageControlId);
            }
            catch (Exception ex)
            {
                commitLog.Add("");
                commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== TRANSACTION ERROR ==========");
                commitLog.Add($"❌ EXCEPTION during transaction: {ex.Message}");
                commitLog.Add($"Stack trace: {ex.StackTrace}");
                commitLog.Add($"Rolling back transaction...");

                result.Warnings.AddRange(stage.Warnings);
                result.Warnings.AddRange(commitLog);

                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "[STAGING COMMIT] Transaction rolled back due to error");
                throw;
            }
            }); // End of execution strategy lambda

            return result;
        }
        catch (Exception ex)
        {
            commitLog.Add("");
            commitLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ========== COMMIT FAILED ==========");
            commitLog.Add($"❌ EXCEPTION: {ex.Message}");
            commitLog.Add($"Stack trace: {ex.StackTrace}");

            result.Warnings.AddRange(stage.Warnings);
            result.Warnings.AddRange(commitLog);

            _logger.LogError(ex, "[STAGING COMMIT] Error committing staged entities");
            result.Success = false;
            result.Errors.Add($"Failed to commit staged entities: {ex.Message}");
            return result;
        }
    }

    private object? BuildPartialMatchDetails(CaseMatchingResult caseMatchingResult)
    {
        if (caseMatchingResult?.DiseaseIdentifications == null || !caseMatchingResult.DiseaseIdentifications.Any())
            return null;

        var partialMatches = caseMatchingResult.DiseaseIdentifications
            .Where(id => id.IsPartialMatch && id.MissingFields.Any())
            .ToList();

        if (!partialMatches.Any())
            return null;

        // Build consolidated partial match details
        return new
        {
            MissingFields = partialMatches
                .SelectMany(pm => pm.MissingFields)
                .Distinct()
                .OrderBy(f => f)
                .ToList(),
            Diseases = partialMatches.Select(pm => new
            {
                DiseaseName = pm.Disease?.Name ?? "Unknown",
                DiseaseId = pm.Disease?.Id,
                MissingFields = pm.MissingFields,
                ConfirmationStatus = pm.ConfirmationStatus?.Name,
                OriginalConfirmationStatusId = pm.OriginalConfirmationStatusId
            }).ToList(),
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Check if a disease is an ancestor (parent, grandparent, etc.) of another disease
    /// Used to filter out parent diseases when child diseases are present
    /// </summary>
    private bool IsAncestorOfDisease(Guid ancestorId, Disease disease)
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

    #endregion
}




