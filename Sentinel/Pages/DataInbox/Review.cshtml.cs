using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Sentinel.Services;
using Sentinel.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Sentinel.Models;
using Newtonsoft.Json.Linq;

namespace Sentinel.Pages.DataInbox;

[Authorize(Policy = "Permission.Case.Create")]
public class ReviewModel : PageModel
{
    private readonly IDataReviewService _reviewService;
    private readonly ApplicationDbContext _context;
    private readonly ICollectionMappingService _collectionMappingService;
    private readonly ISurveyMappingService _surveyMappingService;
    private readonly ILogger<ReviewModel> _logger;

    public ReviewModel(
        IDataReviewService reviewService,
        ApplicationDbContext context,
        ICollectionMappingService collectionMappingService,
        ISurveyMappingService surveyMappingService,
        ILogger<ReviewModel> logger)
    {
        _reviewService = reviewService;
        _context = context;
        _collectionMappingService = collectionMappingService;
        _surveyMappingService = surveyMappingService;
        _logger = logger;
    }

    public ReviewQueueDetail? ReviewDetail { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }
    
    // Helper method to get full ReviewQueue entity with all properties
    public async Task<ReviewQueue?> GetFullReviewQueueEntityAsync(int id)
    {
        return await _context.ReviewQueue.FindAsync(id);
    }
    
    // Helper method to get patient by ID for duplicate display
    public async Task<Patient?> GetPatientByIdAsync(Guid id)
    {
        return await _context.Patients.FindAsync(id);
    }

    [BindProperty]
    public string? TaskTitle { get; set; }

    [BindProperty]
    public string? TaskDescription { get; set; }

    [BindProperty]
    public string? SelectedPatientId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ReviewDetail = await _reviewService.GetReviewItemDetailAsync(id);

        if (ReviewDetail == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var result = await _reviewService.ConfirmReviewAsync(id, ReviewNotes);

        if (result)
        {
            TempData["SuccessMessage"] = "Review confirmed successfully.";
            return RedirectToPage("./Index");
        }

        TempData["ErrorMessage"] = "Failed to confirm review.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        var result = await _reviewService.DismissReviewAsync(id, ReviewNotes);

        if (result)
        {
            TempData["SuccessMessage"] = "Review dismissed.";
            return RedirectToPage("./Index");
        }

        TempData["ErrorMessage"] = "Failed to dismiss review.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(TaskTitle))
        {
            TempData["ErrorMessage"] = "Task title is required.";
            return RedirectToPage(new { id });
        }

        var taskId = await _reviewService.CreateTaskForReviewAsync(
            id,
            TaskTitle,
            TaskDescription
        );

        if (taskId.HasValue)
        {
            TempData["SuccessMessage"] = $"Task created successfully and review marked as complete.";
            return RedirectToPage("./Index");
        }

        TempData["ErrorMessage"] = "Failed to create task.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveDuplicateAsync(int id)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("? OnPostResolveDuplicateAsync CALLED!");
        _logger.LogInformation("   Review ID: {ReviewId}", id);
        _logger.LogInformation("   SelectedPatientId: {SelectedPatientId}", SelectedPatientId ?? "(NULL)");
        _logger.LogInformation("   ReviewNotes: {ReviewNotes}", ReviewNotes ?? "(NULL)");
        _logger.LogInformation("========================================");
        
        if (string.IsNullOrEmpty(SelectedPatientId))
        {
            _logger.LogWarning("?? SelectedPatientId is null or empty - returning error");
            TempData["ErrorMessage"] = "Please select a patient or choose to create a new one.";
            return RedirectToPage(new { id });
        }

        try
        {
            // Get the full ReviewQueue entity to access all JSON fields
            var reviewQueue = await _context.ReviewQueue
                .Include(r => r.Task)
                    .ThenInclude(t => t!.Case)  // Load Task.Case
                .Include(r => r.Task)
                    .ThenInclude(t => t!.TaskTemplate)  // Load Task.TaskTemplate
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (reviewQueue == null)
            {
                TempData["ErrorMessage"] = "Review item not found.";
                return RedirectToPage("./Index");
            }

            _logger.LogInformation("?? Loaded ReviewQueue {ReviewId}:", id);
            _logger.LogInformation("   - Has Task: {HasTask}", reviewQueue.Task != null);
            _logger.LogInformation("   - TaskId: {TaskId}", reviewQueue.TaskId);
            if (reviewQueue.Task != null)
            {
                _logger.LogInformation("   - Has Case: {HasCase}", reviewQueue.Task.Case != null);
                _logger.LogInformation("   - CaseId: {CaseId}", reviewQueue.Task.CaseId);
            }

            Guid patientId;
            
            // STEP 1: Create new patient OR use existing
            if (SelectedPatientId == "CREATE_NEW")
            {
                _logger.LogInformation("Creating new patient from duplicate review {ReviewId}", id);
                
                // Extract patient data from ProposedEntityDataJson
                var newPatient = await ExtractPatientFromProposedData(reviewQueue.ProposedEntityDataJson);
                if (newPatient == null)
                {
                    TempData["ErrorMessage"] = "Could not extract patient data from survey.";
                    return RedirectToPage(new { id });
                }
                
                // Save new patient
                _context.Patients.Add(newPatient);
                await _context.SaveChangesAsync();
                patientId = newPatient.Id;
                
                _logger.LogInformation("Created new patient {PatientId} ({FriendlyId}) from duplicate review", 
                    patientId, newPatient.FriendlyId);
            }
            else
            {
                _logger.LogInformation("Using existing patient {PatientId} for duplicate review {ReviewId}", 
                    SelectedPatientId, id);
                    
                // Use selected existing patient
                if (!Guid.TryParse(SelectedPatientId, out patientId))
                {
                    TempData["ErrorMessage"] = "Invalid patient ID selected.";
                    return RedirectToPage(new { id });
                }
            }

            // STEP 2: Continue processing survey collection mapping with the selected PatientId
            _logger.LogInformation("?? DIAGNOSTIC: About to check Task and Case for reprocessing");
            _logger.LogInformation("   - reviewQueue.Task is null: {IsNull}", reviewQueue.Task == null);
            _logger.LogInformation("   - reviewQueue.TaskId: {TaskId}", reviewQueue.TaskId);
            
            if (reviewQueue.Task != null)
            {
                _logger.LogInformation("?? Task found! Processing survey collection mapping for Task {TaskId} with Patient {PatientId}", 
                    reviewQueue.TaskId, patientId);
                
                _logger.LogInformation("   - reviewQueue.Task.Case is null: {IsNull}", reviewQueue.Task.Case == null);
                _logger.LogInformation("   - reviewQueue.Task.CaseId: {CaseId}", reviewQueue.Task.CaseId);
                
                // Update the task's case to link to the selected/created patient
                if (reviewQueue.Task.Case != null)
                {
                    _logger.LogInformation("?? Case found! Case {CaseId} current PatientId: {CurrentPatientId}", 
                        reviewQueue.Task.CaseId, reviewQueue.Task.Case.PatientId);
                    
                    // Only update PatientId if it's NULL (not already set)
                    // DO NOT overwrite an existing patient association!
                    if (reviewQueue.Task.Case.PatientId == null || reviewQueue.Task.Case.PatientId == Guid.Empty)
                    {
                        _logger.LogInformation("?? Case has no patient - linking to {PatientId}", patientId);
                        reviewQueue.Task.Case.PatientId = patientId;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("?? Successfully linked Case {CaseId} to Patient {PatientId}", 
                            reviewQueue.Task.CaseId, patientId);
                    }
                    else
                    {
                        _logger.LogInformation("?? Case already has patient {ExistingPatientId} - NOT changing it!", 
                            reviewQueue.Task.Case.PatientId);
                        _logger.LogInformation("?? Related entities will be linked to resolved patient {ResolvedPatientId}", 
                            patientId);
                    }
                    
                    // STEP 2B: Re-process collection mappings now that patient is resolved
                    _logger.LogInformation("========================================");
                    _logger.LogInformation("?? STARTING REPROCESSING OF COLLECTION MAPPINGS");
                    _logger.LogInformation("   Patient ID: {PatientId}", patientId);
                    _logger.LogInformation("   Case ID: {CaseId}", reviewQueue.Task.CaseId);
                    _logger.LogInformation("   Task ID: {TaskId}", reviewQueue.TaskId);
                    _logger.LogInformation("   Patient Already Created/Selected: TRUE");
                    _logger.LogInformation("========================================");
                    
                    try
                    {
                        // ? CRITICAL: Patient was either created above (CREATE_NEW) or selected (existing)
                        // In BOTH cases, the patient already exists - don't create it again!
                        await ReprocessCollectionMappingsAsync(reviewQueue, patientId, patientAlreadyExists: true);
                        _logger.LogInformation("?? Reprocessing completed successfully");
                    }
                    catch (Exception reprocessEx)
                    {
                        _logger.LogError(reprocessEx, "?? Reprocessing failed: {Message}", reprocessEx.Message);
                        TempData["WarningMessage"] = "Patient resolved but failed to create related entities. Check logs.";
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("?? PROBLEM: Task.Case is NULL! Cannot reprocess collection mappings.");
                    _logger.LogWarning("   This means the Case navigation property wasn't loaded from the database.");
                    _logger.LogWarning("   Check the Include() statements in the query at line 137.");
                }
            }
            else
            {
                _logger.LogWarning("?? PROBLEM: Task is NULL! Cannot reprocess collection mappings.");
                _logger.LogWarning("   This means either:");
                _logger.LogWarning("   1. The ReviewQueue record has no TaskId");
                _logger.LogWarning("   2. The Task navigation property wasn't loaded (check Include() at line 138)");
                _logger.LogWarning("   3. The Task was deleted from the database");
            }

            // STEP 3: Mark review as confirmed
            var result = await _reviewService.ConfirmReviewAsync(id, 
                $"Resolved duplicate: {(SelectedPatientId == "CREATE_NEW" ? "Created new patient" : $"Linked to patient {SelectedPatientId}")}. {ReviewNotes}");

            if (result)
            {
                TempData["SuccessMessage"] = SelectedPatientId == "CREATE_NEW" 
                    ? "? New patient created and survey processed successfully." 
                    : "? Survey linked to existing patient successfully.";
                return RedirectToPage("./Index");
            }

            TempData["ErrorMessage"] = "Failed to resolve duplicate.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving duplicate for review {ReviewId}", id);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
            return RedirectToPage(new { id });
        }
    }
    
    /// <summary>
    /// Handler for always-review scenarios (PendingCreation) where no duplicates were detected
    /// Creates the patient and processes related entities
    /// </summary>
    public async Task<IActionResult> OnPostResolveAlwaysReviewAsync(int id)
    {
        _logger.LogCritical("========================================");
        _logger.LogCritical("??? OnPostResolveAlwaysReviewAsync CALLED!");
        _logger.LogCritical("   - Review ID: {Id}", id);
        _logger.LogCritical("   - This is the ALWAYS-REVIEW handler (PendingCreation)");
        _logger.LogCritical("========================================");
        
        try
        {
            // Get the full ReviewQueue entity
            var reviewQueue = await _context.ReviewQueue
                .Include(r => r.Task)
                    .ThenInclude(t => t!.Case)
                .Include(r => r.Task)
                    .ThenInclude(t => t!.TaskTemplate)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (reviewQueue == null)
            {
                TempData["ErrorMessage"] = "Review item not found.";
                return RedirectToPage("./Index");
            }
            
            _logger.LogInformation("? Loaded ReviewQueue {ReviewId} (ChangeType: {ChangeType})", 
                id, reviewQueue.ChangeType);
            
            // Extract patient data from ProposedEntityDataJson
            var newPatient = await ExtractPatientFromProposedData(reviewQueue.ProposedEntityDataJson);
            if (newPatient == null)
            {
                TempData["ErrorMessage"] = "Could not extract patient data from review item.";
                return RedirectToPage(new { id });
            }
            
            // Create patient
            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("? Created new patient {PatientId} ({FriendlyId}) from always-review", 
                newPatient.Id, newPatient.FriendlyId);
            
            // Link case to patient if needed
            if (reviewQueue.Task?.Case != null && 
                (reviewQueue.Task.Case.PatientId == null || reviewQueue.Task.Case.PatientId == Guid.Empty))
            {
                reviewQueue.Task.Case.PatientId = newPatient.Id;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("? Linked Case {CaseId} to new Patient {PatientId}", 
                    reviewQueue.Task.CaseId, newPatient.Id);
            }
            
            // Reprocess collection mappings to create related entities
            // ? CRITICAL: Patient already created above, so mark it as existing!
            _logger.LogInformation("?? Starting reprocessing for always-review scenario");
            _logger.LogInformation("   Patient ALREADY CREATED in handler - will be treated as existing during reprocessing");
            
            try
            {
                // Call with existing patient (same as duplicate scenario)
                await ReprocessCollectionMappingsAsync(reviewQueue, newPatient.Id, patientAlreadyExists: true);
                _logger.LogInformation("? Reprocessing completed successfully");
            }
            catch (Exception reprocessEx)
            {
                _logger.LogError(reprocessEx, "? Reprocessing failed: {Message}", reprocessEx.Message);
                TempData["WarningMessage"] = "Patient created but failed to create related entities. Check logs.";
            }
            
            // Mark review as confirmed
            var result = await _reviewService.ConfirmReviewAsync(id, 
                $"Approved always-review: Created patient {newPatient.FriendlyId}");
            
            if (result)
            {
                TempData["SuccessMessage"] = "? Patient created and related entities processed successfully.";
                return RedirectToPage("./Index");
            }
            
            TempData["ErrorMessage"] = "Failed to confirm review.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving always-review for review {ReviewId}", id);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
            return RedirectToPage(new { id });
        }
    }
    
    private async Task<Patient?> ExtractPatientFromProposedData(string? proposedDataJson)
    {
        if (string.IsNullOrEmpty(proposedDataJson))
            return null;
            
        try
        {
            var proposedData = JsonSerializer.Deserialize<Dictionary<string, object>>(proposedDataJson);
            if (proposedData == null)
                return null;
            
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                GivenName = GetValueFromDict(proposedData, "GivenName") ?? "",
                FamilyName = GetValueFromDict(proposedData, "FamilyName") ?? "",
                DateOfBirth = GetDateFromDict(proposedData, "DateOfBirth"),
                MobilePhone = GetValueFromDict(proposedData, "MobilePhone"),
                EmailAddress = GetValueFromDict(proposedData, "EmailAddress"),
                AddressLine = GetValueFromDict(proposedData, "AddressLine"),
                City = GetValueFromDict(proposedData, "City"),
                StateId = await GetStateIdFromStringAsync(GetValueFromDict(proposedData, "State")),
                PostalCode = GetValueFromDict(proposedData, "PostalCode"),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.Identity?.Name
            };
            
            // Generate FriendlyId
            var year = DateTime.UtcNow.Year;
            var prefix = $"P-{year}-";
            var maxId = _context.Patients
                .Where(p => p.FriendlyId.StartsWith(prefix))
                .Select(p => p.FriendlyId)
                .AsEnumerable()
                .Select(id => int.TryParse(id.Split('-').Last(), out var num) ? num : 0)
                .DefaultIfEmpty(0)
                .Max();
            
            patient.FriendlyId = $"{prefix}{(maxId + 1):D4}";
            
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting patient from proposed data");
            return null;
        }
    }
    
    private string? GetValueFromDict(Dictionary<string, object> dict, string key)
    {
        return dict.ContainsKey(key) ? dict[key]?.ToString() : null;
    }
    
    private DateTime? GetDateFromDict(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out var date))
            return date;
        return null;
    }

    private async Task<Patient?> ExtractPatientFromEntityData(Dictionary<string, object?> entityData)
    {
        try
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                GivenName = entityData.ContainsKey("GivenName") ? entityData["GivenName"]?.ToString() ?? "" : "",
                FamilyName = entityData.ContainsKey("FamilyName") ? entityData["FamilyName"]?.ToString() ?? "" : "",
                DateOfBirth = entityData.ContainsKey("DateOfBirth") && DateTime.TryParse(entityData["DateOfBirth"]?.ToString(), out var dob) ? dob : null,
                MobilePhone = entityData.ContainsKey("MobilePhone") ? entityData["MobilePhone"]?.ToString() : null,
                EmailAddress = entityData.ContainsKey("EmailAddress") ? entityData["EmailAddress"]?.ToString() : null,
                AddressLine = entityData.ContainsKey("AddressLine") ? entityData["AddressLine"]?.ToString() : 
                             entityData.ContainsKey("StreetAddress") ? entityData["StreetAddress"]?.ToString() : null,
                City = entityData.ContainsKey("City") ? entityData["City"]?.ToString() : null,
                StateId = await GetStateIdFromStringAsync(
                    entityData.ContainsKey("State") ? entityData["State"]?.ToString() : 
                    entityData.ContainsKey("Province") ? entityData["Province"]?.ToString() : null),
                PostalCode = entityData.ContainsKey("PostalCode") ? entityData["PostalCode"]?.ToString() : null,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.Identity?.Name
            };
            
            // Generate FriendlyId
            var year = DateTime.UtcNow.Year;
            var maxId = _context.Patients
                .Where(p => p.FriendlyId.StartsWith($"PAT-{year}-"))
                .Select(p => p.FriendlyId)
                .AsEnumerable()
                .Select(id => int.TryParse(id.Split('-').Last(), out var num) ? num : 0)
                .DefaultIfEmpty(0)
                .Max();
            
            patient.FriendlyId = $"PAT-{year}-{(maxId + 1):D4}";
            
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting patient from entity data");
            return null;
        }
    }

    /// <summary>
    /// Re-process collection mappings after patient duplicate is resolved
    /// This creates related entities (Contacts, Exposures, etc.) from the survey data
    /// </summary>
    private async Task ReprocessCollectionMappingsAsync(
        ReviewQueue reviewQueue, 
        Guid resolvedPatientId,
        bool patientAlreadyExists = true)  // ? Default TRUE for duplicate scenarios
    {
        _logger.LogInformation("=== REPROCESS COLLECTION MAPPINGS START ===");
        _logger.LogInformation("ReviewQueue Id: {ReviewId}, ResolvedPatientId: {PatientId}, PatientAlreadyExists: {AlreadyExists}", 
            reviewQueue.Id, resolvedPatientId, patientAlreadyExists);
        
        try
        {
            if (reviewQueue.Task == null)
            {
                _logger.LogWarning("? No task associated with review queue {ReviewId}", reviewQueue.Id);
                return;
            }
            
            _logger.LogInformation("Task Id: {TaskId}, CaseId: {CaseId}", reviewQueue.Task.Id, reviewQueue.Task.CaseId);
            
            // Extract survey responses from CollectionSourceDataJson
            if (string.IsNullOrEmpty(reviewQueue.CollectionSourceDataJson))
            {
                _logger.LogWarning("? No CollectionSourceDataJson - skipping collection mapping");
                return;
            }
            
            _logger.LogInformation("? CollectionSourceDataJson exists ({Length} chars)", reviewQueue.CollectionSourceDataJson.Length);

            // Parse collection source data
            Dictionary<string, object>? sourceData;
            try
            {
                sourceData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    reviewQueue.CollectionSourceDataJson
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse collection source data");
                return;
            }

            if (sourceData == null || !sourceData.ContainsKey("QuestionName"))
            {
                _logger.LogWarning("Invalid collection source data format - missing QuestionName");
                return;
            }

            // ? FIX: SurveyResponseId might be null (we use Task.SurveyResponseJson anyway)
            Guid surveyResponseId = Guid.Empty;
            if (sourceData.ContainsKey("SurveyResponseId") && sourceData["SurveyResponseId"] != null)
            {
                var surveyResponseElement = sourceData["SurveyResponseId"] as JsonElement?;
                if (surveyResponseElement.HasValue && 
                    surveyResponseElement.Value.ValueKind == JsonValueKind.String)
                {
                    Guid.TryParse(surveyResponseElement.Value.GetString(), out surveyResponseId);
                }
            }
            
            var questionName = sourceData["QuestionName"]?.ToString() ?? "unknown";
            
            _logger.LogInformation(
                "? Parsed source data: SurveyResponseId={SurveyResponseId}, QuestionName={QuestionName}",
                surveyResponseId != Guid.Empty ? surveyResponseId.ToString() : "(not tracked - using Task.SurveyResponseJson)", 
                questionName
            );

            // Get the collection mapping config for this question
            _logger.LogInformation("Fetching active mappings for question {QuestionName}", questionName);
            
            // ? FIX: Get DiseaseId from Task.Case (reviewQueue.DiseaseId is often NULL)
            var diseaseId = reviewQueue.Task.Case?.DiseaseId ?? reviewQueue.DiseaseId;
            
            _logger.LogInformation("Using DiseaseId: {DiseaseId} (from Case: {FromCase})", 
                diseaseId, reviewQueue.Task.Case?.DiseaseId);
            
            var mappings = await _surveyMappingService.GetActiveMappingsAsync(
                surveyTemplateId: reviewQueue.Task.TaskTemplate?.SurveyTemplateId,
                taskTemplateId: reviewQueue.Task.TaskTemplateId,
                diseaseId: diseaseId
            );
            
            _logger.LogInformation("Found {Count} active mappings", mappings.Count);

            var collectionMapping = mappings.FirstOrDefault(m => 
                m.SurveyQuestionName == questionName && 
                !string.IsNullOrEmpty(m.CollectionConfigJson)
            );

            if (collectionMapping == null)
            {
                _logger.LogWarning("? No collection mapping found for question {QuestionName}", questionName);
                return;
            }
            
            _logger.LogInformation("? Found collection mapping with config length {Length}", collectionMapping.CollectionConfigJson.Length);

            // Parse collection config
            var config = JsonSerializer.Deserialize<CollectionMappingConfig>(
                collectionMapping.CollectionConfigJson
            );

            if (config == null)
            {
                _logger.LogWarning("? Invalid collection mapping config");
                return;
            }
            
            _logger.LogInformation("? Config parsed: TargetEntity={Target}, RelatedEntities={Count}", 
                config.TargetEntityType, config.RelatedEntities?.Count ?? 0);

            JArray rowData;
            
            // ? CRITICAL FIX: Check if specific row was stored in CollectionSourceDataJson
            // This prevents reprocessing ALL rows when only ONE was queued for review
            if (sourceData.ContainsKey("RowData") && sourceData["RowData"] != null)
            {
                var rowDataElement = sourceData["RowData"] as JsonElement?;
                if (rowDataElement.HasValue && rowDataElement.Value.ValueKind == JsonValueKind.String)
                {
                    var rowJson = rowDataElement.Value.GetString();
                    if (!string.IsNullOrEmpty(rowJson))
                    {
                        _logger.LogInformation("?? Using SPECIFIC row from CollectionSourceDataJson (prevents duplicate processing)");
                        var singleRow = JObject.Parse(rowJson);
                        rowData = new JArray { singleRow };
                        _logger.LogInformation("? Loaded 1 specific row from stored RowData");
                    }
                    else
                    {
                        _logger.LogWarning("RowData field exists but is empty - falling back to full survey data");
                        rowData = await GetAllRowsFromSurveyResponseAsync(reviewQueue.Task, questionName);
                    }
                }
                else
                {
                    _logger.LogWarning("RowData field exists but wrong type - falling back to full survey data");
                    rowData = await GetAllRowsFromSurveyResponseAsync(reviewQueue.Task, questionName);
                }
            }
            else
            {
                // Legacy path: No RowData stored - use all rows from survey (old review items)
                _logger.LogWarning("?? No RowData in CollectionSourceDataJson - using ALL survey rows (may cause duplicates!)");
                rowData = await GetAllRowsFromSurveyResponseAsync(reviewQueue.Task, questionName);
            }
            
            if (rowData == null || rowData.Count == 0)
            {
                _logger.LogWarning("No row data available for reprocessing");
                return;
            }

            // Build context with resolved patient
            // ? FIX: Mark whether this had duplicates or not (affects reprocessing logic)
            var hadDuplicates = reviewQueue.ChangeType == "PotentialDuplicate" || 
                              reviewQueue.ChangeType == "DuplicateDetected";
            
            var isPendingCreation = reviewQueue.ChangeType == "PendingCreation";
            
            // ? CRITICAL: Use the patientAlreadyExists parameter!
            // For always-review, patient was ALREADY created in the handler, so treat as existing
            var context = new SurveySubmissionContext
            {
                CaseId = reviewQueue.Task.CaseId,
                PatientId = resolvedPatientId,
                TaskId = reviewQueue.Task.Id,
                DiseaseId = reviewQueue.Task.Case?.DiseaseId ?? Guid.Empty,
                JurisdictionId = null,
                SubmittedBy = User.Identity?.Name,
                SubmittedDate = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["ResolvedFromDuplicate"] = hadDuplicates || isPendingCreation,
                    ["PatientAlreadyExists"] = patientAlreadyExists,  // ? Use parameter!
                    ["OriginalReviewId"] = reviewQueue.Id,
                    ["Jurisdiction1Id"] = reviewQueue.Task.Case?.Jurisdiction1Id ?? 0
                }
            };
            
            _logger.LogCritical("============ CONTEXT CREATED - IMMEDIATE CHECK ============");
            _logger.LogCritical("? Built context: CaseId={CaseId}, PatientId={PatientId}, DiseaseId={DiseaseId}", 
                context.CaseId, context.PatientId, context.DiseaseId);
            _logger.LogCritical("? AdditionalData is null: {IsNull}", context.AdditionalData == null);
            _logger.LogCritical("? AdditionalData.Count: {Count}", context.AdditionalData?.Count ?? -1);
            if (context.AdditionalData != null)
            {
                _logger.LogCritical("? AdditionalData.Keys: {Keys}", string.Join(", ", context.AdditionalData.Keys));
                foreach (var kvp in context.AdditionalData)
                {
                    _logger.LogCritical("   - {Key} = {Value} (type: {Type})", 
                        kvp.Key, kvp.Value, kvp.Value?.GetType().Name);
                }
            }
            _logger.LogCritical("===========================================================");

            // Process the collection with context
            _logger.LogInformation("?? Calling ProcessCollectionWithContextAsync...");
            
            var result = await _collectionMappingService.ProcessCollectionWithContextAsync(
                surveyResponseId: surveyResponseId,
                questionName: questionName,
                rowData: rowData,
                config: config,
                context: context
            );

            _logger.LogInformation(
                "? Collection reprocessing result: {Created} entities created, {Review} need review, {Errors} errors",
                result.EntitiesCreated.Count,
                result.ItemsRequiringReview,
                result.Errors.Count
            );
            
            foreach (var entity in result.EntitiesCreated)
            {
                _logger.LogInformation("  - Created {EntityType} with ID {EntityId}", entity.EntityType, entity.EntityId);
            }

            if (result.Errors.Any())
            {
                _logger.LogWarning("Errors during collection reprocessing: {Errors}", 
                    string.Join("; ", result.Errors));
            }

            // Save all entities created
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully reprocessed collection mappings for Patient {PatientId}", 
                resolvedPatientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing collection mappings");
            throw;
        }
    }
    
    /// <summary>
    /// Helper to extract all rows from survey response (fallback for legacy review items)
    /// </summary>
    private async Task<JArray?> GetAllRowsFromSurveyResponseAsync(CaseTask task, string questionName)
    {
        if (string.IsNullOrEmpty(task.SurveyResponseJson))
        {
            _logger.LogWarning("? Task {TaskId} has no survey response JSON", task.Id);
            return null;
        }
        
        _logger.LogInformation("? Task has SurveyResponseJson ({Length} chars)", task.SurveyResponseJson.Length);

        var surveyData = JsonSerializer.Deserialize<Dictionary<string, object>>(task.SurveyResponseJson);
        if (surveyData == null || !surveyData.ContainsKey(questionName))
        {
            var keys = surveyData?.Keys != null ? string.Join(", ", surveyData.Keys) : "none";
            _logger.LogWarning("? Survey data does not contain question {QuestionName}. Keys: {Keys}", 
                questionName, keys);
            return null;
        }
        
        _logger.LogInformation("? Survey data contains question {QuestionName}", questionName);

        var rowDataElement = surveyData[questionName] as JsonElement?;
        if (rowDataElement == null)
        {
            _logger.LogWarning("? Question {QuestionName} data is null", questionName);
            return null;
        }

        // ? Handle both Object (single row) and Array (multiple rows) formats
        if (rowDataElement.Value.ValueKind == JsonValueKind.Array)
        {
            var rows = JArray.Parse(rowDataElement.Value.GetRawText());
            _logger.LogInformation("? Parsed {RowCount} rows from array data", rows.Count);
            return rows;
        }
        else if (rowDataElement.Value.ValueKind == JsonValueKind.Object)
        {
            var singleRow = JObject.Parse(rowDataElement.Value.GetRawText());
            var rows = new JArray { singleRow };
            _logger.LogInformation("? Parsed 1 row from object data (wrapped in array)");
            return rows;
        }
        else
        {
            var elementType = rowDataElement.Value.ValueKind.ToString();
            _logger.LogWarning("? Question {QuestionName} data is unexpected type: {Type}", 
                questionName, elementType);
            return null;
        }
    }

    private async Task<int?> GetStateIdFromStringAsync(string? stateString)
    {
        if (string.IsNullOrWhiteSpace(stateString))
        {
            return null;
        }

        // Try to find by code (e.g., "NSW") or name (e.g., "New South Wales")
        var state = await _context.States
            .FirstOrDefaultAsync(s => s.Code == stateString || s.Name == stateString);

        return state?.Id;
    }
}
