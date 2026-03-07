using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurveyService> _logger;
        private readonly ISurveyMappingService _mappingService;

        public SurveyService(
            ApplicationDbContext context, 
            ILogger<SurveyService> logger,
            ISurveyMappingService mappingService)
        {
            _context = context;
            _logger = logger;
            _mappingService = mappingService;
        }

        public async Task<SurveyDefinitionWithData> GetSurveyForTaskAsync(Guid taskId)
        {
            var task = await _context.CaseTasks
                .Include(t => t.TaskTemplate)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.SexAtBirth)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.Gender)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.AtsiStatus)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.Occupation)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.CountryOfBirth)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.LanguageSpokenAtHome)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                        .ThenInclude(p => p.Ancestry)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Disease)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new ArgumentException($"Task {taskId} not found");

            string? surveyJson = null;
            string? defaultInputMappings = null;
            string? defaultOutputMappings = null;

            // 1. Check if TaskTemplate uses Survey Library
            if (task.TaskTemplate?.SurveyTemplateId != null)
            {
                // First, get the survey template the task originally pointed to (may be archived)
                var originalTemplate = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId);
                
                SurveyTemplate? surveyTemplate = null;
                
                if (originalTemplate != null)
                {
                    // Determine the root parent of this survey family
                    var rootParentId = originalTemplate.ParentSurveyTemplateId ?? originalTemplate.Id;
                    
                    // Always use the ACTIVE version from this survey family
                    surveyTemplate = await _context.SurveyTemplates
                        .AsNoTracking()
                        .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                        .Where(st => st.VersionStatus == SurveyVersionStatus.Active)
                        .FirstOrDefaultAsync();
                    
                    if (surveyTemplate == null)
                    {
                        // Fallback: if no active version, use the original template even if archived
                        _logger.LogWarning("No active version found for survey family {RootParentId}, using original template {OriginalId}", 
                            rootParentId, originalTemplate.Id);
                        surveyTemplate = originalTemplate;
                    }
                    else if (surveyTemplate.Id != originalTemplate.Id)
                    {
                        _logger.LogInformation("Task {TaskId} originally linked to version {OriginalVersion}, now using active version {ActiveVersion}", 
                            taskId, originalTemplate.VersionNumber, surveyTemplate.VersionNumber);
                    }
                }
                
                if (surveyTemplate != null)
                {
                    surveyJson = surveyTemplate.SurveyDefinitionJson;
                    defaultInputMappings = surveyTemplate.DefaultInputMappingJson;
                    defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;

                    _logger.LogInformation("Using Survey Library template {TemplateId} (Version {VersionNumber}) for Task {TaskId}", 
                        surveyTemplate.Id, surveyTemplate.VersionNumber, taskId);

                    // Update usage tracking on the version being used
                    var templateToUpdate = await _context.SurveyTemplates.FindAsync(surveyTemplate.Id);
                    if (templateToUpdate != null)
                    {
                        templateToUpdate.UsageCount++;
                        templateToUpdate.LastUsedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            
            // 2. Fall back to embedded survey (backwards compatible)
            if (surveyJson == null && !string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson))
            {
                surveyJson = task.TaskTemplate.SurveyDefinitionJson;
                defaultInputMappings = task.TaskTemplate.DefaultInputMappingJson;
                defaultOutputMappings = task.TaskTemplate.DefaultOutputMappingJson;

                _logger.LogInformation("Using embedded survey from TaskTemplate {TaskTemplateId} for Task {TaskId}", 
                    task.TaskTemplateId, taskId);
            }

            var result = new SurveyDefinitionWithData
            {
                HasSurvey = !string.IsNullOrEmpty(surveyJson)
            };

            if (!result.HasSurvey)
                return result;

            result.SurveyDefinitionJson = surveyJson!;

            // 3. Get disease task template for field mappings (disease-specific overrides)
            var diseaseTaskTemplate = await _context.DiseaseTaskTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => 
                    dt.DiseaseId == task.Case.DiseaseId && 
                    dt.TaskTemplateId == task.TaskTemplateId);

            // AUTO-POPULATE ALL CASE/PATIENT/TASK DATA
            // Every survey gets access to all context data by default - no manual mapping needed!
            _logger.LogInformation("Auto-populating all case/patient/task data for Task {TaskId}", taskId);
            result.PrePopulatedData = await BuildAllAvailableDataAsync(task);

            // OPTIONAL: Apply custom input mappings as overrides/additions
            // This allows power users to add calculated fields or custom transformations
            string? inputMappingJson = diseaseTaskTemplate?.InputMappingJson;
            if (string.IsNullOrWhiteSpace(inputMappingJson))
            {
                inputMappingJson = defaultInputMappings;
            }

            if (!string.IsNullOrWhiteSpace(inputMappingJson))
            {
                _logger.LogInformation("Applying additional custom input mappings for Task {TaskId}", taskId);
                
                var context = await BuildSurveyDataContextAsync(task);
                var inputMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    inputMappingJson) ?? new();

                // Apply custom mappings (can override auto-populated values)
                foreach (var mapping in inputMappings)
                {
                    var surveyFieldName = mapping.Key;
                    var sourceFieldPath = mapping.Value;

                    try
                    {
                        var value = ResolveFieldPath(sourceFieldPath, context);
                        if (value != null)
                        {
                            result.PrePopulatedData[surveyFieldName] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, 
                            "Failed to resolve field path '{FieldPath}' for survey field '{SurveyField}'",
                            sourceFieldPath, surveyFieldName);
                    }
                }
            }

            return result;
        }

        public async Task SaveSurveyResponseAsync(Guid taskId, Dictionary<string, object> responses)
        {
            var task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                .Include(t => t.TaskTemplate)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new ArgumentException($"Task {taskId} not found");

            // =====================================================
            // CRITICAL: ALWAYS save response JSON first (even if mappings fail later)
            // This ensures user's work is never lost
            // =====================================================
            task.SurveyResponseJson = JsonSerializer.Serialize(responses);

            // ========================================
            // NEW UNIFIED MAPPING SYSTEM WITH ERROR HANDLING
            // ========================================
            try
            {
                // Get active mappings (respects Survey > Task > Disease priority)
                var surveyTemplateId = task.TaskTemplate?.SurveyTemplateId;
                var taskTemplateId = task.TaskTemplateId;
                var diseaseId = task.Case?.DiseaseId;
                
                _logger.LogInformation(
                    "Looking for mappings: SurveyTemplateId={SurveyTemplateId}, TaskTemplateId={TaskTemplateId}, DiseaseId={DiseaseId}",
                    surveyTemplateId, taskTemplateId, diseaseId);

                var mappings = await _mappingService.GetActiveMappingsAsync(
                    surveyTemplateId: surveyTemplateId,
                    taskTemplateId: taskTemplateId,
                    diseaseId: diseaseId
                );

                _logger.LogInformation("Found {Count} active mappings for Task {TaskId}", 
                    mappings.Count, taskId);
                
                // Debug: Check database directly
                if (mappings.Count == 0)
                {
                    var allMappingsCount = await _context.SurveyFieldMappings.CountAsync();
                    var activeMappingsCount = await _context.SurveyFieldMappings.CountAsync(m => m.IsActive);
                    
                    _logger.LogWarning(
                        "No mappings found! Total mappings in DB: {Total}, Active: {Active}. " +
                        "Searched for: Survey={Survey}, Task={Task}, Disease={Disease}",
                        allMappingsCount, activeMappingsCount, surveyTemplateId, taskTemplateId, diseaseId);
                    
                    // Log what mappings DO exist
                    var existingMappings = await _context.SurveyFieldMappings
                        .Select(m => new { m.ConfigurationType, m.ConfigurationId, m.IsActive })
                        .Take(10)
                        .ToListAsync();
                    
                    _logger.LogWarning("Sample of existing mappings: {Mappings}", 
                        string.Join(", ", existingMappings.Select(m => $"{m.ConfigurationType}:{m.ConfigurationId}(Active={m.IsActive})")));
                }

                if (mappings.Any())
                {
                    _logger.LogInformation("Executing {Count} field mappings for Task {TaskId}", 
                        mappings.Count, taskId);

                    // Execute mappings (applies business rules, handles review queue)
                    var result = await _mappingService.ExecuteMappingsAsync(
                        taskId: taskId,
                        surveyResponses: responses,
                        mappings: mappings
                    );

                    // Log results
                    _logger.LogInformation(
                        "Mapping execution complete for Task {TaskId}: " +
                        "{AutoSaved} auto-saved, {Queued} queued for review, " +
                        "{Approval} require approval, {Skipped} skipped, {Errors} errors",
                        taskId, result.AutoSavedCount, result.QueuedForReviewCount,
                        result.RequireApprovalCount, result.SkippedCount, result.ErrorCount);

                    // Log any errors
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Mapping error for Task {TaskId}: {Error}", taskId, error);
                    }

                    // Update task description if approval required
                    if (result.RequireApprovalCount > 0)
                    {
                        _logger.LogInformation(
                            "Task {TaskId} has {Count} fields requiring approval - changes queued for review", 
                            taskId, result.RequireApprovalCount);
                    }
                    
                    // Check if there were errors during mapping execution
                    if (result.ErrorCount > 0)
                    {
                        throw new InvalidOperationException(
                            $"Survey mapping completed with {result.ErrorCount} error(s): {string.Join("; ", result.Errors)}"
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("No field mappings configured for Task {TaskId} - survey data will not be auto-saved", taskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing unified field mappings for Task {TaskId}", taskId);
                
                // =====================================================
                // CRITICAL ERROR RECOVERY: Create review item for manual intervention
                // Survey JSON is already saved above, so user's work is preserved
                // =====================================================
                try
                {
                    // CRITICAL: Clear the change tracker to remove any unsaved entities from the failed mapping
                    // This prevents the FK violation error when saving the review item
                    _context.ChangeTracker.Clear();
                    
                    _logger.LogInformation("Change tracker cleared. Creating review item for failed survey mapping.");
                    
                    var reviewItem = new ReviewQueue
                    {
                        EntityType = "SurveyResponse",
                        EntityId = 0,
                        CaseId = task.CaseId,
                        ChangeType = "SurveyMappingError",
                        Priority = ReviewPriorities.High,
                        ReviewStatus = ReviewStatuses.Pending,
                        CreatedDate = DateTime.UtcNow,
                        ChangeSnapshot = $"Survey submission failed for Task {task.Id}: {ex.Message}",
                        ProposedEntityDataJson = JsonSerializer.Serialize(new
                        {
                            TaskId = task.Id,
                            TaskName = task.TaskTemplate?.Name ?? "Unknown",
                            ErrorMessage = ex.Message,
                            StackTrace = ex.StackTrace,
                            SurveyResponses = responses,
                            ErrorTimestamp = DateTime.UtcNow
                        }),
                        TaskId = task.Id
                    };
                    
                    _context.ReviewQueue.Add(reviewItem);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "Created review item {ReviewId} for failed survey submission on Task {TaskId}. Survey JSON was saved.",
                        reviewItem.Id,
                        taskId
                    );
                    
                    // Throw custom exception to inform user that data was saved but needs review
                    throw new InvalidOperationException(
                        $"Survey data was saved, but automatic mapping failed. " +
                        $"A review item has been created for manual processing. " +
                        $"Error: {ex.Message}",
                        ex
                    );
                }
                catch (InvalidOperationException)
                {
                    // Re-throw our custom exception
                    throw;
                }
                catch (Exception innerEx)
                {
                    _logger.LogCritical(innerEx, 
                        "CRITICAL: Failed to save review item after mapping error for Task {TaskId}. Survey JSON was saved to task.",
                        taskId);
                    
                    // Even if review item creation fails, survey JSON is saved
                    throw new InvalidOperationException(
                        $"Survey data was saved, but automatic mapping failed and review item creation also failed. " +
                        $"Please contact support. Original error: {ex.Message}",
                        ex
                    );
                }
            }

            // ========================================
            // LEGACY JSON MAPPING SYSTEM (Fallback)
            // ========================================
            // Keep old system as fallback for backward compatibility
            // This will be removed in future version after migration complete
            
            string? defaultOutputMappings = null;

            // 1. Check if TaskTemplate uses Survey Library
            if (task.TaskTemplate?.SurveyTemplateId != null)
            {
                var surveyTemplate = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId);
                
                if (surveyTemplate != null)
                {
                    defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;
                }
            }
            
            // 2. Fall back to embedded mappings (backwards compatible)
            if (defaultOutputMappings == null)
            {
                defaultOutputMappings = task.TaskTemplate?.DefaultOutputMappingJson;
            }

            // 3. Get disease task template for output mappings (disease-specific overrides)
            var diseaseTaskTemplate = await _context.DiseaseTaskTemplates
                .Include(dt => dt.TaskTemplate)
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => 
                    dt.DiseaseId == task.Case.DiseaseId && 
                    dt.TaskTemplateId == task.TaskTemplateId);

            // Determine which output mappings to use: disease-specific or default
            string? outputMappingJson = diseaseTaskTemplate?.OutputMappingJson;
            if (string.IsNullOrWhiteSpace(outputMappingJson))
            {
                // Fall back to defaults (from library or embedded)
                outputMappingJson = defaultOutputMappings;
            }

            // Only execute legacy mappings if they exist AND no new mappings were configured
            var hasNewMappings = await _context.SurveyFieldMappings
                .AnyAsync(m => m.IsActive && (
                    (m.ConfigurationType == MappingConfigurationType.Survey && 
                     m.ConfigurationId == task.TaskTemplate.SurveyTemplateId) ||
                    (m.ConfigurationType == MappingConfigurationType.Task && 
                     m.ConfigurationId == task.TaskTemplateId) ||
                    (m.ConfigurationType == MappingConfigurationType.Disease && 
                     m.ConfigurationId == task.Case.DiseaseId)
                ));

            if (!hasNewMappings && outputMappingJson != null)
            {
                _logger.LogInformation("Executing legacy JSON mappings for Task {TaskId} (no new mappings configured)", taskId);
                
                var context = await BuildSurveyDataContextAsync(task);

                // Parse output mappings
                var outputMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    outputMappingJson) ?? new();

                // Apply mappings
                foreach (var mapping in outputMappings)
                {
                    var surveyFieldName = mapping.Key;
                    var targetFieldPath = mapping.Value;

                    if (responses.TryGetValue(surveyFieldName, out var value))
                    {
                        try
                        {
                            await SetFieldValueAsync(targetFieldPath, value, context);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, 
                                "Failed to set field path '{FieldPath}' from survey field '{SurveyField}'",
                                targetFieldPath, surveyFieldName);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public bool ValidateSurveyDefinition(string surveyJson)
        {
            if (string.IsNullOrWhiteSpace(surveyJson))
                return false;

            try
            {
                using var document = JsonDocument.Parse(surveyJson);
                var root = document.RootElement;

                // Basic SurveyJS validation - must have pages or elements
                return root.TryGetProperty("pages", out _) || 
                       root.TryGetProperty("elements", out _);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>?> GetSurveyResponseAsync(Guid taskId)
        {
            var task = await _context.CaseTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task?.SurveyResponseJson == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(task.SurveyResponseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse survey response for task {TaskId}", taskId);
                return null;
            }
        }

        public object? ResolveFieldPath(string fieldPath, SurveyDataContext context)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                return null;

            var parts = fieldPath.Split('.');
            if (parts.Length < 2)
                return null;

            var rootObject = parts[0];
            var propertyPath = parts.Skip(1).ToArray();

            object? current = rootObject.ToLower() switch
            {
                "patient" => context.Patient,
                "case" => context.Case,
                "task" => context.Task,
                "exposures" => context.Exposures,
                "labresults" => context.LabResults,
                "customfields" => context.CustomFields,
                _ => null
            };

            if (current == null)
                return null;

            // Navigate property path
            foreach (var prop in propertyPath)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(prop, out current))
                        return null;
                }
                else
                {
                    var property = current.GetType().GetProperty(prop);
                    if (property == null)
                        return null;
                    current = property.GetValue(current);
                }

                if (current == null)
                    return null;
            }

            return current;
        }

        public async Task SetFieldValueAsync(string fieldPath, object? value, SurveyDataContext context)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                return;

            var parts = fieldPath.Split('.');
            if (parts.Length < 2)
                return;

            var rootObject = parts[0].ToLower();
            var propertyPath = parts.Skip(1).ToArray();

            // Navigate to the parent object
            object? current = rootObject switch
            {
                "patient" => context.Patient,
                "case" => context.Case,
                _ => null
            };

            if (current == null)
            {
                _logger.LogWarning("Cannot set field on unknown root object: {RootObject}", rootObject);
                return;
            }

            // Navigate to parent
            for (int i = 0; i < propertyPath.Length - 1; i++)
            {
                var property = current.GetType().GetProperty(propertyPath[i]);
                if (property == null)
                {
                    _logger.LogWarning("Property {Property} not found on {Type}", 
                        propertyPath[i], current.GetType().Name);
                    return;
                }
                current = property.GetValue(current);
                if (current == null)
                    return;
            }

            // Set the final property
            var targetProperty = current.GetType().GetProperty(propertyPath.Last());
            if (targetProperty == null || !targetProperty.CanWrite)
            {
                _logger.LogWarning("Property {Property} not found or not writable on {Type}", 
                    propertyPath.Last(), current.GetType().Name);
                return;
            }

            // Convert value to target type
            try
            {
                var convertedValue = ConvertValue(value, targetProperty.PropertyType);
                targetProperty.SetValue(current, convertedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert and set value for {FieldPath}", fieldPath);
            }
        }

        private async Task<SurveyDataContext> BuildSurveyDataContextAsync(CaseTask task)
        {
            // Load exposures
            var exposures = await _context.ExposureEvents
                .Where(e => e.ExposedCaseId == task.CaseId)
                .Include(e => e.Location)
                .Include(e => e.Event)
                .ToListAsync();

            // Load lab results
            var labResults = await _context.LabResults
                .Where(l => l.CaseId == task.CaseId)
                .Include(l => l.TestType)
                .Include(l => l.TestResult)
                .ToListAsync();

            return new SurveyDataContext
            {
                Task = task,
                Case = task.Case,
                Patient = task.Case.Patient,
                Exposures = exposures,
                LabResults = labResults
            };
        }

        /// <summary>
        /// Automatically builds a comprehensive dictionary of ALL available case/patient/task data
        /// Eliminates need for manual input mapping configuration - every survey gets full context
        /// </summary>
        private async Task<Dictionary<string, object>> BuildAllAvailableDataAsync(CaseTask task)
        {
            var data = new Dictionary<string, object>();

            try
            {
                // TASK DATA
                if (task != null)
                {
                    AddIfNotNull(data, "task_id", task.Id);
                    AddIfNotNull(data, "task_title", task.Title);
                    AddIfNotNull(data, "task_description", task.Description);
                    AddIfNotNull(data, "task_status", task.Status.ToString());
                    AddIfNotNull(data, "task_priority", task.Priority.ToString());
                    AddIfNotNull(data, "task_due_date", task.DueDate);
                    AddIfNotNull(data, "task_created_at", task.CreatedAt);
                }

                // CASE DATA
                if (task?.Case != null)
                {
                    var c = task.Case;
                    AddIfNotNull(data, "case_id", c.Id);
                    AddIfNotNull(data, "case_friendly_id", c.FriendlyId);
                    AddIfNotNull(data, "case_type", c.Type.ToString());
                    AddIfNotNull(data, "case_date_of_onset", c.DateOfOnset);
                    AddIfNotNull(data, "case_date_of_notification", c.DateOfNotification);
                    AddIfNotNull(data, "case_clinical_notification_date", c.ClinicalNotificationDate);
                    AddIfNotNull(data, "case_clinical_notifier_organisation", c.ClinicalNotifierOrganisation);
                    AddIfNotNull(data, "case_hospitalised", c.Hospitalised?.ToString());
                    AddIfNotNull(data, "case_date_of_admission", c.DateOfAdmission);
                    AddIfNotNull(data, "case_date_of_discharge", c.DateOfDischarge);
                    AddIfNotNull(data, "case_died_due_to_disease", c.DiedDueToDisease?.ToString());
                    
                    // Case lookups (eager load names)
                    if (c.Disease != null)
                    {
                        AddIfNotNull(data, "case_disease_name", c.Disease.Name);
                        AddIfNotNull(data, "case_disease_id", c.DiseaseId);
                    }
                    
                    if (c.ConfirmationStatus != null)
                    {
                        AddIfNotNull(data, "case_confirmation_status", c.ConfirmationStatus.Name);
                    }
                }

                // PATIENT DATA
                if (task?.Case?.Patient != null)
                {
                    var p = task.Case.Patient;
                    AddIfNotNull(data, "patient_id", p.Id);
                    AddIfNotNull(data, "patient_friendly_id", p.FriendlyId);
                    AddIfNotNull(data, "patient_given_name", p.GivenName);
                    AddIfNotNull(data, "patient_family_name", p.FamilyName);
                    AddIfNotNull(data, "patient_full_name", $"{p.GivenName} {p.FamilyName}");
                    AddIfNotNull(data, "patient_date_of_birth", p.DateOfBirth);
                    AddIfNotNull(data, "patient_mobile_phone", p.MobilePhone);
                    AddIfNotNull(data, "patient_home_phone", p.HomePhone);
                    AddIfNotNull(data, "patient_email", p.EmailAddress);
                    AddIfNotNull(data, "patient_address_line", p.AddressLine);
                    AddIfNotNull(data, "patient_city", p.City);
                    AddIfNotNull(data, "patient_state", p.State);
                    AddIfNotNull(data, "patient_postal_code", p.PostalCode);
                    AddIfNotNull(data, "patient_is_deceased", p.IsDeceased);
                    AddIfNotNull(data, "patient_date_of_death", p.DateOfDeath);
                    
                    // Demographics and lookup fields
                    if (p.SexAtBirth != null)
                    {
                        AddIfNotNull(data, "patient_sex_at_birth", p.SexAtBirth.Name);
                        AddIfNotNull(data, "patient_sex_at_birth_id", p.SexAtBirthId);
                    }
                    
                    if (p.Gender != null)
                    {
                        AddIfNotNull(data, "patient_gender", p.Gender.Name);
                        AddIfNotNull(data, "patient_gender_id", p.GenderId);
                    }
                    
                    if (p.AtsiStatus != null)
                    {
                        AddIfNotNull(data, "patient_atsi_status", p.AtsiStatus.Name);
                        AddIfNotNull(data, "patient_atsi_status_id", p.AtsiStatusId);
                    }
                    
                    if (p.Occupation != null)
                    {
                        AddIfNotNull(data, "patient_occupation", p.Occupation.Name);
                        AddIfNotNull(data, "patient_occupation_id", p.OccupationId);
                    }
                    
                    if (p.CountryOfBirth != null)
                    {
                        AddIfNotNull(data, "patient_country_of_birth", p.CountryOfBirth.Name);
                        AddIfNotNull(data, "patient_country_of_birth_id", p.CountryOfBirthId);
                    }
                    
                    if (p.LanguageSpokenAtHome != null)
                    {
                        AddIfNotNull(data, "patient_language_spoken_at_home", p.LanguageSpokenAtHome.Name);
                        AddIfNotNull(data, "patient_language_spoken_at_home_id", p.LanguageSpokenAtHomeId);
                    }
                    
                    if (p.Ancestry != null)
                    {
                        AddIfNotNull(data, "patient_ancestry", p.Ancestry.Name);
                        AddIfNotNull(data, "patient_ancestry_id", p.AncestryId);
                    }
                    
                    // Calculate age if DOB exists
                    if (p.DateOfBirth.HasValue)
                    {
                        var age = DateTime.Today.Year - p.DateOfBirth.Value.Year;
                        if (p.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                            age--;
                        AddIfNotNull(data, "patient_age", age);
                    }
                }

                _logger.LogInformation("Auto-populated {Count} data fields for Task {TaskId}", 
                    data.Count, task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building auto-populated data for Task {TaskId}", task?.Id);
            }

            return data;
        }

        /// <summary>
        /// Helper to add non-null values to dictionary
        /// Formats DateTime values for SurveyJS compatibility
        /// </summary>
        private void AddIfNotNull(Dictionary<string, object> dict, string key, object? value)
        {
            if (value != null)
            {
                // Format DateTime values for SurveyJS date inputs (YYYY-MM-DD format)
                if (value is DateTime dt)
                {
                    dict[key] = dt.ToString("yyyy-MM-dd");
                }
                else
                {
                    dict[key] = value;
                }
            }
        }

        private object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement)
            {
                value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.TryGetInt32(out var i) ? i : jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.ToString()
                };
            }

            if (value == null)
                return null;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
                return value.ToString();

            if (underlyingType == typeof(DateTime))
            {
                if (value is string dateStr && DateTime.TryParse(dateStr, out var date))
                    return date;
                if (value is DateTime dt)
                    return dt;
            }

            if (underlyingType == typeof(int))
            {
                if (value is int i)
                    return i;
                if (value is string intStr && int.TryParse(intStr, out var intVal))
                    return intVal;
                if (value is double d)
                    return (int)d;
            }

            if (underlyingType == typeof(bool))
            {
                if (value is bool b)
                    return b;
                if (value is string boolStr && bool.TryParse(boolStr, out var boolVal))
                    return boolVal;
            }

            if (underlyingType == typeof(decimal))
            {
                if (value is decimal dec)
                    return dec;
                if (value is string decStr && decimal.TryParse(decStr, out var decVal))
                    return decVal;
                if (value is double dbl)
                    return (decimal)dbl;
            }

            if (underlyingType == typeof(Guid))
            {
                if (value is Guid guid)
                    return guid;
                if (value is string guidStr && Guid.TryParse(guidStr, out var guidVal))
                    return guidVal;
            }

            return Convert.ChangeType(value, underlyingType);
        }
    }
}
