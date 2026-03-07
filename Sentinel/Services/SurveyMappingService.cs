using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sentinel.Services
{
    public class SurveyMappingService : ISurveyMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportFieldMetadataService _fieldMetadataService;
        private readonly ICollectionMappingService _collectionMappingService;
        private readonly ILogger<SurveyMappingService> _logger;

        public SurveyMappingService(
            ApplicationDbContext context,
            IReportFieldMetadataService fieldMetadataService,
            ICollectionMappingService collectionMappingService,
            ILogger<SurveyMappingService> logger)
        {
            _context = context;
            _fieldMetadataService = fieldMetadataService;
            _collectionMappingService = collectionMappingService;
            _logger = logger;
        }

        public async Task<List<SurveyFieldMapping>> GetActiveMappingsAsync(
            Guid? surveyTemplateId,
            Guid? taskTemplateId,
            Guid? diseaseId)
        {
            var mappings = new List<SurveyFieldMapping>();

            _logger.LogInformation("GetActiveMappingsAsync called with: Survey={Survey}, Task={Task}, Disease={Disease}",
                surveyTemplateId, taskTemplateId, diseaseId);

            // Priority order: Survey (1) > Task (2) > Disease (3)
            // Lower priority number = higher precedence

            if (surveyTemplateId.HasValue)
            {
                // HANDLE SURVEY VERSIONING - look for mappings in the survey family
                // Get the survey template to find its version family
                var surveyTemplate = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == surveyTemplateId.Value);

                if (surveyTemplate != null)
                {
                    // Determine the root parent of this survey family
                    var rootParentId = surveyTemplate.ParentSurveyTemplateId ?? surveyTemplate.Id;
                    
                    _logger.LogInformation("Survey {SurveyId} belongs to family with root {RootId}", 
                        surveyTemplateId.Value, rootParentId);

                    // Get all survey IDs in this version family
                    var surveyFamilyIds = await _context.SurveyTemplates
                        .AsNoTracking()
                        .Where(st => st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId)
                        .Select(st => st.Id)
                        .ToListAsync();

                    _logger.LogInformation("Survey family contains {Count} versions: {Ids}", 
                        surveyFamilyIds.Count, string.Join(", ", surveyFamilyIds));

                    // Look for mappings linked to ANY version in this family
                    var surveyMappings = await _context.SurveyFieldMappings
                        .Where(m => m.ConfigurationType == MappingConfigurationType.Survey
                                 && surveyFamilyIds.Contains(m.ConfigurationId)
                                 && m.IsActive)
                        .OrderBy(m => m.DisplayOrder)
                        .ToListAsync();
                    
                    _logger.LogInformation("Found {Count} survey mappings for survey family (searched {FamilyCount} versions)", 
                        surveyMappings.Count, surveyFamilyIds.Count);
                    
                    mappings.AddRange(surveyMappings);
                }
                else
                {
                    _logger.LogWarning("SurveyTemplate {Id} not found", surveyTemplateId.Value);
                }
            }

            if (taskTemplateId.HasValue)
            {
                var taskMappings = await _context.SurveyFieldMappings
                    .Where(m => m.ConfigurationType == MappingConfigurationType.Task
                             && m.ConfigurationId == taskTemplateId.Value
                             && m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();
                _logger.LogInformation("Found {Count} task mappings for TaskTemplateId={Id}", 
                    taskMappings.Count, taskTemplateId.Value);
                mappings.AddRange(taskMappings);
            }

            if (diseaseId.HasValue)
            {
                var diseaseMappings = await _context.SurveyFieldMappings
                    .Where(m => m.ConfigurationType == MappingConfigurationType.Disease
                             && m.ConfigurationId == diseaseId.Value
                             && m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();
                _logger.LogInformation("Found {Count} disease mappings for DiseaseId={Id}", 
                    diseaseMappings.Count, diseaseId.Value);
                mappings.AddRange(diseaseMappings);
            }

            // Deduplicate by SurveyQuestionName, keeping highest priority (lowest Priority value)
            var deduplicatedMappings = mappings
                .GroupBy(m => m.SurveyQuestionName)
                .Select(g => g.OrderBy(m => m.Priority).First())
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            _logger.LogInformation("After deduplication: {Count} mappings", deduplicatedMappings.Count);

            return deduplicatedMappings;
        }

        public async Task<List<ReportFieldMetadata>> GetAvailableFieldsAsync(string entityType)
        {
            // Leverage existing field discovery service from reporting system
            return await _fieldMetadataService.GetFieldsForEntityAsync(entityType);
        }

        public async Task<List<SurveyQuestion>> GetSurveyQuestionsAsync(string surveyDefinitionJson)
        {
            var questions = new List<SurveyQuestion>();

            try
            {
                var surveyDef = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(surveyDefinitionJson);
                
                if (surveyDef.TryGetProperty("pages", out var pages))
                {
                    foreach (var page in pages.EnumerateArray())
                    {
                        if (page.TryGetProperty("elements", out var elements))
                        {
                            foreach (var element in elements.EnumerateArray())
                            {
                                var question = new SurveyQuestion();
                                
                                if (element.TryGetProperty("name", out var name))
                                    question.Name = name.GetString() ?? "";
                                
                                if (element.TryGetProperty("title", out var title))
                                    question.Title = title.GetString() ?? question.Name;
                                else
                                    question.Title = question.Name;
                                
                                if (element.TryGetProperty("type", out var type))
                                    question.Type = type.GetString() ?? "";
                                
                                if (element.TryGetProperty("isRequired", out var isRequired))
                                    question.IsRequired = isRequired.GetBoolean();
                                
                                if (element.TryGetProperty("choices", out var choices))
                                {
                                    question.Choices = choices.EnumerateArray()
                                        .Select(c => GetChoiceText(c))
                                        .Where(text => !string.IsNullOrEmpty(text))
                                        .ToList();
                                }
                                
                                questions.Add(question);
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Invalid JSON - return empty list
            }

            return await Task.FromResult(questions);
        }

        /// <summary>
        /// Extract choice text from various SurveyJS choice formats:
        /// - Simple string: "Option 1"
        /// - Object: {value: 1, text: "Option 1"}
        /// - Number: 1
        /// </summary>
        private string GetChoiceText(JsonElement choice)
        {
            try
            {
                return choice.ValueKind switch
                {
                    JsonValueKind.String => choice.GetString() ?? "",
                    JsonValueKind.Number => choice.ToString(),
                    JsonValueKind.Object => choice.TryGetProperty("text", out var text) 
                        ? text.GetString() ?? ""
                        : (choice.TryGetProperty("value", out var val) ? val.ToString() : ""),
                    _ => ""
                };
            }
            catch
            {
                return "";
            }
        }

        public async Task<MappingExecutionResult> ExecuteMappingsAsync(
            Guid taskId,
            Dictionary<string, object> surveyResponses,
            List<SurveyFieldMapping> mappings)
        {
            var result = new MappingExecutionResult { Success = true };

            _logger.LogInformation("ExecuteMappingsAsync started for Task {TaskId} with {MappingCount} mappings and {ResponseCount} survey responses",
                taskId, mappings.Count, surveyResponses.Count);

            var task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.CaseSymptoms)
                        .ThenInclude(cs => cs.Symptom)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task?.Case == null)
            {
                result.Success = false;
                result.Errors.Add("Task or associated case not found");
                _logger.LogError("Task {TaskId} or associated case not found", taskId);
                return result;
            }

            _logger.LogInformation("Processing mappings for Task {TaskId}, Case {CaseId}, Survey Response Keys: {Keys}",
                taskId, task.CaseId, string.Join(", ", surveyResponses.Keys));

            // ========================================
            // PHASE 2B: COLLECTION MAPPING INTEGRATION
            // ========================================
            
            // Separate simple and collection mappings
            var simpleMappings = mappings.Where(m => m.Complexity == MappingComplexity.Simple).ToList();
            var collectionMappings = mappings.Where(m => m.Complexity == MappingComplexity.Collection).ToList();
            
            _logger.LogInformation("Found {SimpleCount} simple mappings and {CollectionCount} collection mappings",
                simpleMappings.Count, collectionMappings.Count);
            
            // Process collection mappings first (they may create related entities)
            if (collectionMappings.Any())
            {
                await ProcessCollectionMappingsAsync(
                    collectionMappings,
                    surveyResponses,
                    task,
                    result
                );
            }

            foreach (var mapping in simpleMappings)
            {
                try
                {
                    var detail = new MappingExecutionDetail
                    {
                        SurveyQuestion = mapping.SurveyQuestionName,
                        TargetField = mapping.TargetFieldPath,
                        Action = mapping.MappingAction,
                        BusinessRuleApplied = mapping.BusinessRule.ToString()
                    };

                    // Get survey value
                    if (!surveyResponses.TryGetValue(mapping.SurveyQuestionName, out var surveyValue))
                    {
                        _logger.LogWarning("Survey response for question '{QuestionName}' not found. Available keys: {Keys}",
                            mapping.SurveyQuestionName, string.Join(", ", surveyResponses.Keys));
                        result.SkippedCount++;
                        continue;
                    }

                    detail.SurveyValue = surveyValue;
                    _logger.LogDebug("Mapping '{QuestionName}' -> '{TargetField}': Survey Value = {Value}, Action = {Action}",
                        mapping.SurveyQuestionName, mapping.TargetFieldPath, surveyValue, mapping.MappingAction);

                    // Get current database value
                    var currentValue = await GetFieldValueAsync(mapping, task.Case);
                    detail.DatabaseValue = currentValue;

                    // Apply business rule to determine if we should save
                    var shouldSave = ApplyBusinessRule(
                        mapping.BusinessRule,
                        currentValue,
                        surveyValue,
                        out var valueToSave);

                    detail.ResultingValue = valueToSave;
                    detail.WasModified = shouldSave;

                    _logger.LogDebug("Business rule '{Rule}' applied: shouldSave={ShouldSave}, currentValue={Current}, newValue={New}, resultValue={Result}",
                        mapping.BusinessRule, shouldSave, currentValue, surveyValue, valueToSave);

                    if (!shouldSave)
                    {
                        _logger.LogDebug("Skipping mapping for '{QuestionName}' - business rule determined no save needed",
                            mapping.SurveyQuestionName);
                        result.SkippedCount++;
                        result.Details.Add(detail);
                        continue;
                    }

                    // Handle based on mapping action
                    switch (mapping.MappingAction)
                    {
                        case MappingAction.AutoSave:
                            _logger.LogInformation("AutoSave: Setting '{TargetField}' = {Value}", 
                                mapping.TargetFieldPath, valueToSave);
                            await SetFieldValueAsync(mapping, task.Case, valueToSave);
                            result.AutoSavedCount++;
                            break;

                        case MappingAction.QueueForReview:
                        case MappingAction.RequireApproval:
                            _logger.LogInformation("QueueForReview: Adding review item for '{TargetField}' = {Value}",
                                mapping.TargetFieldPath, valueToSave);
                            await QueueFieldChangeForReviewAsync(
                                mapping,
                                task,
                                currentValue,
                                valueToSave);
                            
                            if (mapping.MappingAction == MappingAction.QueueForReview)
                                result.QueuedForReviewCount++;
                            else
                                result.RequireApprovalCount++;
                            break;

                        case MappingAction.Skip:
                            result.SkippedCount++;
                            break;
                    }

                    result.Details.Add(detail);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var errorMsg = $"Error processing mapping for {mapping.SurveyQuestionName}: {ex.Message}";
                    result.Errors.Add(errorMsg);
                    _logger.LogError(ex, "Error processing mapping for question '{QuestionName}' -> '{TargetField}'",
                        mapping.SurveyQuestionName, mapping.TargetFieldPath);
                }
            }

            // Save all changes (auto-saved field updates AND review queue items)
            if (result.AutoSavedCount > 0 || result.QueuedForReviewCount > 0 || result.RequireApprovalCount > 0)
            {
                _logger.LogInformation("Saving changes: {AutoSaved} auto-saved, {Queued} queued for review, {Approval} require approval",
                    result.AutoSavedCount, result.QueuedForReviewCount, result.RequireApprovalCount);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("No changes to save - all mappings were skipped or had errors");
            }

            return result;
        }

        public async Task<MappingPreviewResult> PreviewMappingsAsync(
            Guid? caseId,
            Dictionary<string, object> surveyResponses,
            List<SurveyFieldMapping> mappings)
        {
            var result = new MappingPreviewResult
            {
                TotalMappings = mappings.Count
            };

            Case? caseEntity = null;
            if (caseId.HasValue)
            {
                caseEntity = await _context.Cases
                    .Include(c => c.Patient)
                    .FirstOrDefaultAsync(c => c.Id == caseId.Value);
            }

            foreach (var mapping in mappings)
            {
                var item = new MappingPreviewItem
                {
                    SurveyQuestion = mapping.DisplayName ?? mapping.SurveyQuestionName,
                    TargetField = mapping.TargetFieldPath,
                    Action = mapping.MappingAction,
                    BusinessRule = mapping.BusinessRule.ToString()
                };

                // Get survey value
                if (surveyResponses.TryGetValue(mapping.SurveyQuestionName, out var surveyValue))
                {
                    item.SurveyValue = surveyValue;
                }

                // Get current database value if case exists
                if (caseEntity != null)
                {
                    item.CurrentDatabaseValue = await GetFieldValueAsync(mapping, caseEntity);
                }

                // Apply business rule to determine projected value
                var shouldSave = ApplyBusinessRule(
                    mapping.BusinessRule,
                    item.CurrentDatabaseValue,
                    item.SurveyValue,
                    out var projectedValue);

                item.ProjectedValue = projectedValue;
                item.WillModify = shouldSave;

                // Set display properties
                item.ActionDisplay = mapping.MappingAction switch
                {
                    MappingAction.AutoSave => "? Auto-save",
                    MappingAction.QueueForReview => "? Queue for review",
                    MappingAction.RequireApproval => "?? Require approval",
                    MappingAction.Skip => "? Skip",
                    _ => "Unknown"
                };

                item.Icon = mapping.MappingAction switch
                {
                    MappingAction.AutoSave => "check-circle",
                    MappingAction.QueueForReview => "clock",
                    MappingAction.RequireApproval => "shield-exclamation",
                    MappingAction.Skip => "circle",
                    _ => "question"
                };

                item.CssClass = mapping.MappingAction switch
                {
                    MappingAction.AutoSave => "text-success",
                    MappingAction.QueueForReview => "text-warning",
                    MappingAction.RequireApproval => "text-danger",
                    MappingAction.Skip => "text-muted",
                    _ => ""
                };

                // Update counts
                switch (mapping.MappingAction)
                {
                    case MappingAction.AutoSave:
                        result.WillAutoSave++;
                        break;
                    case MappingAction.QueueForReview:
                        result.WillQueueForReview++;
                        break;
                    case MappingAction.RequireApproval:
                        result.WillRequireApproval++;
                        break;
                    case MappingAction.Skip:
                        result.WillSkip++;
                        break;
                }

                result.Items.Add(item);
            }

            return result;
        }

        public async Task<MappingValidationResult> ValidateMappingAsync(SurveyFieldMapping mapping)
        {
            var result = new MappingValidationResult { IsValid = true };

            // Validate field path exists
            var entityType = mapping.FieldCategory switch
            {
                MappingFieldCategory.Patient => "Patient",
                MappingFieldCategory.Case => "Case",
                MappingFieldCategory.Symptom => "CaseSymptom",
                MappingFieldCategory.Exposure => "ExposureEvent",
                MappingFieldCategory.LabResult => "LabResult",
                MappingFieldCategory.Task => "CaseTask",
                _ => "Case"
            };

            var availableFields = await GetAvailableFieldsAsync(entityType);
            var fieldExists = availableFields.Any(f => f.FieldPath == mapping.TargetFieldPath);

            if (!fieldExists && mapping.TargetFieldType == MappingFieldType.StandardField)
            {
                result.IsValid = false;
                result.Errors.Add($"Field path '{mapping.TargetFieldPath}' not found in {entityType}");
            }

            // Validate business rule compatibility
            if (mapping.BusinessRule == MappingBusinessRule.TakeEarliest ||
                mapping.BusinessRule == MappingBusinessRule.TakeLatest)
            {
                var field = availableFields.FirstOrDefault(f => f.FieldPath == mapping.TargetFieldPath);
                if (field?.DataType != "DateTime" && field?.DataType != "DateOnly")
                {
                    result.Warnings.Add($"Business rule '{mapping.BusinessRule}' typically used with date fields");
                }
            }

            if (mapping.BusinessRule == MappingBusinessRule.TakeHighest ||
                mapping.BusinessRule == MappingBusinessRule.TakeLowest)
            {
                var field = availableFields.FirstOrDefault(f => f.FieldPath == mapping.TargetFieldPath);
                if (field?.DataType != "Int32" && field?.DataType != "Decimal" && field?.DataType != "Double")
                {
                    result.Warnings.Add($"Business rule '{mapping.BusinessRule}' typically used with numeric fields");
                }
            }

            // Validate action compatibility
            // Note: TriggerReviewQueue is now redundant for QueueForReview/RequireApproval actions
            // Those actions always create review items by definition
            if (mapping.MappingAction == MappingAction.Skip && mapping.TriggerReviewQueue)
            {
                result.Warnings.Add("Mapping action is 'Skip' but 'Trigger Review Queue' is enabled - review will not be created");
            }

            return result;
        }

        public async Task<int> CopyMappingsAsync(
            MappingConfigurationType sourceType,
            Guid sourceId,
            MappingConfigurationType targetType,
            Guid targetId)
        {
            var sourceMappings = await _context.SurveyFieldMappings
                .Where(m => m.ConfigurationType == sourceType && m.ConfigurationId == sourceId)
                .ToListAsync();

            var newMappings = sourceMappings.Select(m => new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = targetType,
                ConfigurationId = targetId,
                Priority = (int)targetType,
                SurveyQuestionName = m.SurveyQuestionName,
                TargetFieldPath = m.TargetFieldPath,
                TargetFieldType = m.TargetFieldType,
                FieldCategory = m.FieldCategory,
                MappingAction = m.MappingAction,
                BusinessRule = m.BusinessRule,
                TriggerReviewQueue = m.TriggerReviewQueue,
                ReviewPriority = m.ReviewPriority,
                GroupingWindowHours = m.GroupingWindowHours,
                ValidationRules = m.ValidationRules,
                TransformationScript = m.TransformationScript,
                DisplayName = m.DisplayName,
                Description = m.Description,
                IsActive = m.IsActive,
                DisplayOrder = m.DisplayOrder
            }).ToList();

            await _context.SurveyFieldMappings.AddRangeAsync(newMappings);
            await _context.SaveChangesAsync();

            return newMappings.Count;
        }

        public async Task<List<SurveyFieldMapping>> GetSuggestedMappingsAsync(
            string surveyDefinitionJson,
            MappingConfigurationType configurationType,
            Guid configurationId,
            Guid? diseaseId = null)
        {
            var suggestions = new List<SurveyFieldMapping>();
            var questions = await GetSurveyQuestionsAsync(surveyDefinitionJson);
            var availableFields = await GetAvailableFieldsAsync("Case");
            var patientFields = await GetAvailableFieldsAsync("Patient");
            availableFields.AddRange(patientFields);

            var displayOrder = 1;

            foreach (var question in questions)
            {
                // Find best matching field based on name similarity
                var bestMatch = FindBestFieldMatch(question.Name, question.Title, availableFields);

                if (bestMatch != null)
                {
                    var mapping = new SurveyFieldMapping
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationType = configurationType,
                        ConfigurationId = configurationId,
                        Priority = (int)configurationType,
                        SurveyQuestionName = question.Name,
                        TargetFieldPath = bestMatch.FieldPath,
                        TargetFieldType = bestMatch.FieldPath.StartsWith("CustomField:")
                            ? MappingFieldType.CustomField
                            : MappingFieldType.StandardField,
                        FieldCategory = DetermineFieldCategory(bestMatch.FieldPath),
                        MappingAction = MappingAction.AutoSave, // Default - user can change
                        BusinessRule = DetermineDefaultBusinessRule(bestMatch.DataType),
                        TriggerReviewQueue = false,
                        ReviewPriority = 1,
                        DisplayName = question.Title,
                        IsActive = false, // Suggestions are inactive until user activates
                        DisplayOrder = displayOrder++
                    };

                    suggestions.Add(mapping);
                }
            }

            return suggestions;
        }

        private ReportFieldMetadata? FindBestFieldMatch(
            string questionName,
            string questionTitle,
            List<ReportFieldMetadata> availableFields)
        {
            // Normalize for comparison
            var normalizedQuestion = NormalizeFieldName(questionName);
            var normalizedTitle = NormalizeFieldName(questionTitle);

            // Try exact match first
            var exactMatch = availableFields.FirstOrDefault(f =>
                NormalizeFieldName(f.DisplayName) == normalizedQuestion ||
                NormalizeFieldName(f.DisplayName) == normalizedTitle);

            if (exactMatch != null) return exactMatch;

            // Try partial match
            var partialMatch = availableFields.FirstOrDefault(f =>
                NormalizeFieldName(f.DisplayName).Contains(normalizedQuestion) ||
                normalizedQuestion.Contains(NormalizeFieldName(f.DisplayName)) ||
                NormalizeFieldName(f.DisplayName).Contains(normalizedTitle) ||
                normalizedTitle.Contains(NormalizeFieldName(f.DisplayName)));

            return partialMatch;
        }

        private string NormalizeFieldName(string name)
        {
            return Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]", "");
        }

        private MappingFieldCategory DetermineFieldCategory(string fieldPath)
        {
            if (fieldPath.StartsWith("Patient.")) return MappingFieldCategory.Patient;
            if (fieldPath.StartsWith("Case.")) return MappingFieldCategory.Case;
            if (fieldPath.StartsWith("CaseSymptom.")) return MappingFieldCategory.Symptom;
            if (fieldPath.StartsWith("ExposureEvent.")) return MappingFieldCategory.Exposure;
            if (fieldPath.StartsWith("LabResult.")) return MappingFieldCategory.LabResult;
            if (fieldPath.StartsWith("CaseTask.")) return MappingFieldCategory.Task;
            return MappingFieldCategory.Case;
        }

        private MappingBusinessRule DetermineDefaultBusinessRule(string dataType)
        {
            return dataType switch
            {
                "DateTime" or "DateOnly" => MappingBusinessRule.TakeEarliest,
                "Int32" or "Decimal" or "Double" => MappingBusinessRule.AlwaysOverwrite,
                "String" => MappingBusinessRule.OnlyIfNull,
                "Boolean" => MappingBusinessRule.AlwaysOverwrite,
                _ => MappingBusinessRule.AlwaysOverwrite
            };
        }

        private bool ApplyBusinessRule(
            MappingBusinessRule rule,
            object? currentValue,
            object? newValue,
            out object? valueToSave)
        {
            valueToSave = newValue;

            switch (rule)
            {
                case MappingBusinessRule.AlwaysOverwrite:
                    return true;

                case MappingBusinessRule.OnlyIfNull:
                    return currentValue == null || 
                           (currentValue is string str && string.IsNullOrWhiteSpace(str));

                case MappingBusinessRule.TakeEarliest:
                    if (currentValue is DateTime currentDate && newValue is DateTime newDate)
                    {
                        valueToSave = newDate < currentDate ? newDate : currentDate;
                        return newDate < currentDate;
                    }
                    return currentValue == null;

                case MappingBusinessRule.TakeLatest:
                    if (currentValue is DateTime currentDateLatest && newValue is DateTime newDateLatest)
                    {
                        valueToSave = newDateLatest > currentDateLatest ? newDateLatest : currentDateLatest;
                        return newDateLatest > currentDateLatest;
                    }
                    return currentValue == null;

                case MappingBusinessRule.TakeHighest:
                    if (TryGetNumericValue(currentValue, out var currentNum) &&
                        TryGetNumericValue(newValue, out var newNum))
                    {
                        valueToSave = newNum > currentNum ? newValue : currentValue;
                        return newNum > currentNum;
                    }
                    return currentValue == null;

                case MappingBusinessRule.TakeLowest:
                    if (TryGetNumericValue(currentValue, out var currentNumLow) &&
                        TryGetNumericValue(newValue, out var newNumLow))
                    {
                        valueToSave = newNumLow < currentNumLow ? newValue : currentValue;
                        return newNumLow < currentNumLow;
                    }
                    return currentValue == null;

                case MappingBusinessRule.Append:
                    if (currentValue is string currentStr && newValue is string newStr)
                    {
                        valueToSave = string.IsNullOrWhiteSpace(currentStr)
                            ? newStr
                            : $"{currentStr}\n[Survey {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {newStr}";
                        return !string.IsNullOrWhiteSpace(newStr);
                    }
                    return currentValue == null;

                case MappingBusinessRule.RequireMatch:
                    return Equals(currentValue, newValue);

                default:
                    return true;
            }
        }

        private bool TryGetNumericValue(object? value, out decimal numericValue)
        {
            numericValue = 0;
            if (value == null) return false;

            if (value is int intVal) { numericValue = intVal; return true; }
            if (value is decimal decVal) { numericValue = decVal; return true; }
            if (value is double dblVal) { numericValue = (decimal)dblVal; return true; }
            if (value is float fltVal) { numericValue = (decimal)fltVal; return true; }

            if (decimal.TryParse(value.ToString(), out var parsed))
            {
                numericValue = parsed;
                return true;
            }

            return false;
        }

        private async Task<object?> GetFieldValueAsync(SurveyFieldMapping mapping, Case caseEntity)
        {
            // Check for CustomField FIRST (uses colon separator, not dot)
            if (mapping.TargetFieldPath.StartsWith("CustomField:"))
            {
                return await GetCustomFieldValueAsync(mapping, caseEntity);
            }

            // For standard fields, split by dot
            var parts = mapping.TargetFieldPath.Split('.');
            
            if (parts[0] == "Patient" && caseEntity.Patient != null)
            {
                return GetPropertyValue(caseEntity.Patient, parts.Skip(1).ToArray());
            }
            else if (parts[0] == "Case")
            {
                return GetPropertyValue(caseEntity, parts.Skip(1).ToArray());
            }
            else if ((parts[0] == "CaseSymptom" || parts[0] == "Symptom") && mapping.TargetSymptomId.HasValue)
            {
                return await GetSymptomFieldAsync(caseEntity, mapping.TargetSymptomId.Value, parts.Skip(1).ToArray());
            }

            return null;
        }

        private async Task SetFieldValueAsync(SurveyFieldMapping mapping, Case caseEntity, object? value)
        {
            _logger.LogDebug("SetFieldValueAsync: path={Path}, value={Value}, valueType={ValueType}",
                mapping.TargetFieldPath, value, value?.GetType().Name);

            // Check for CustomField FIRST (uses colon separator, not dot)
            if (mapping.TargetFieldPath.StartsWith("CustomField:"))
            {
                _logger.LogDebug("Setting CustomField: {Path}", mapping.TargetFieldPath);
                await SetCustomFieldValueAsync(mapping, caseEntity, value);
                return;
            }

            // For standard fields, split by dot
            var parts = mapping.TargetFieldPath.Split('.');

            if (parts[0] == "Patient")
            {
                if (caseEntity.Patient == null)
                {
                    _logger.LogWarning("Cannot set Patient field - Patient is null for Case {CaseId}", caseEntity.Id);
                    return;
                }
                _logger.LogDebug("Setting Patient field: {Field}", string.Join(".", parts.Skip(1)));
                SetPropertyValue(caseEntity.Patient, parts.Skip(1).ToArray(), value);
            }
            else if (parts[0] == "Case")
            {
                _logger.LogDebug("Setting Case field: {Field}", string.Join(".", parts.Skip(1)));
                SetPropertyValue(caseEntity, parts.Skip(1).ToArray(), value);
            }
            else if (parts[0] == "CaseSymptom" || parts[0] == "Symptom")
            {
                if (!mapping.TargetSymptomId.HasValue)
                {
                    _logger.LogWarning("Cannot set Symptom field - TargetSymptomId is null for mapping {MappingId}", mapping.Id);
                    return;
                }
                
                _logger.LogDebug("Setting Symptom field: SymptomId={SymptomId}, Field={Field}", 
                    mapping.TargetSymptomId.Value, string.Join(".", parts.Skip(1)));
                await SetSymptomFieldAsync(caseEntity, mapping.TargetSymptomId.Value, parts.Skip(1).ToArray(), value);
            }
            else
            {
                _logger.LogWarning("Unknown field path prefix: {Prefix}", parts[0]);
            }
        }

        private object? GetPropertyValue(object obj, string[] propertyPath)
        {
            foreach (var propertyName in propertyPath)
            {
                if (obj == null) return null;
                
                var property = obj.GetType().GetProperty(propertyName);
                if (property == null) return null;
                
                obj = property.GetValue(obj)!;
            }
            return obj;
        }

        private void SetPropertyValue(object obj, string[] propertyPath, object? value)
        {
            _logger.LogDebug("SetPropertyValue: propertyPath={Path}, value={Value}, objType={Type}",
                string.Join(".", propertyPath), value, obj.GetType().Name);

            for (int i = 0; i < propertyPath.Length - 1; i++)
            {
                var property = obj.GetType().GetProperty(propertyPath[i]);
                if (property == null)
                {
                    _logger.LogWarning("Property '{PropertyName}' not found on type {Type}",
                        propertyPath[i], obj.GetType().Name);
                    return;
                }
                
                var nextObj = property.GetValue(obj);
                if (nextObj == null)
                {
                    _logger.LogWarning("Property '{PropertyName}' returned null", propertyPath[i]);
                    return;
                }
                
                obj = nextObj;
            }

            var finalProperty = obj.GetType().GetProperty(propertyPath[^1]);
            if (finalProperty != null && finalProperty.CanWrite)
            {
                // Convert value to target property type
                var convertedValue = ConvertValueToPropertyType(value, finalProperty.PropertyType);
                _logger.LogDebug("Setting property '{PropertyName}' from {OldValue} to {NewValue} (type: {Type})",
                    propertyPath[^1], finalProperty.GetValue(obj), convertedValue, finalProperty.PropertyType.Name);
                finalProperty.SetValue(obj, convertedValue);
                _logger.LogInformation("Successfully set {Path} = {Value}", 
                    string.Join(".", propertyPath), convertedValue);
            }
            else
            {
                _logger.LogWarning("Property '{PropertyName}' not found or not writable on type {Type}",
                    propertyPath[^1], obj.GetType().Name);
            }
        }

        private object? ConvertValueToPropertyType(object? value, Type targetType)
        {
            if (value == null) return null;
            
            // Handle JsonElement (from survey responses)
            if (value is JsonElement jsonElement)
            {
                value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.TryGetInt32(out var i) ? i : jsonElement.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.ToString()
                };
            }

            if (value == null) return null;

            // If types already match, return as-is
            if (targetType.IsInstanceOfType(value))
                return value;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                // Handle common conversions
                if (underlyingType == typeof(DateTime))
                {
                    if (DateTime.TryParse(value.ToString(), out var dateTime))
                        return dateTime;
                }
                else if (underlyingType == typeof(DateOnly))
                {
                    if (DateOnly.TryParse(value.ToString(), out var dateOnly))
                        return dateOnly;
                }
                else if (underlyingType == typeof(TimeOnly))
                {
                    if (TimeOnly.TryParse(value.ToString(), out var timeOnly))
                        return timeOnly;
                }
                else if (underlyingType == typeof(int))
                {
                    if (int.TryParse(value.ToString(), out var intValue))
                        return intValue;
                }
                else if (underlyingType == typeof(decimal))
                {
                    if (decimal.TryParse(value.ToString(), out var decimalValue))
                        return decimalValue;
                }
                else if (underlyingType == typeof(bool))
                {
                    if (bool.TryParse(value.ToString(), out var boolValue))
                        return boolValue;
                }
                else if (underlyingType == typeof(Guid))
                {
                    if (Guid.TryParse(value.ToString(), out var guidValue))
                        return guidValue;
                }
                else if (underlyingType.IsEnum)
                {
                    if (Enum.TryParse(underlyingType, value.ToString(), true, out var enumValue))
                        return enumValue;
                }
                else
                {
                    // Fallback to Convert.ChangeType
                    return Convert.ChangeType(value, underlyingType);
                }
            }
            catch
            {
                // If conversion fails, return null for nullable types, or the original value
                return Nullable.GetUnderlyingType(targetType) != null ? null : value;
            }

            return value;
        }

        private async Task<object?> GetCustomFieldValueAsync(SurveyFieldMapping mapping, Case caseEntity)
        {
            // Extract custom field name from path (CustomField:FieldName)
            var fieldName = mapping.TargetFieldPath.Replace("CustomField:", "");
            
            var fieldDef = await _context.CustomFieldDefinitions
                .FirstOrDefaultAsync(f => f.Name == fieldName);

            if (fieldDef == null) return null;

            return fieldDef.FieldType switch
            {
                CustomFieldType.Text or CustomFieldType.TextArea or CustomFieldType.Email or CustomFieldType.Phone => 
                    await _context.CaseCustomFieldStrings
                        .Where(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id)
                        .Select(cf => cf.Value)
                        .FirstOrDefaultAsync(),
                
                CustomFieldType.Number => await _context.CaseCustomFieldNumbers
                    .Where(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id)
                    .Select(cf => (object?)cf.Value)
                    .FirstOrDefaultAsync(),
                
                CustomFieldType.Date => await _context.CaseCustomFieldDates
                    .Where(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id)
                    .Select(cf => (object?)cf.Value)
                    .FirstOrDefaultAsync(),
                
                CustomFieldType.Dropdown => await _context.CaseCustomFieldLookups
                    .Where(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id)
                    .Select(cf => (object?)cf.LookupValueId)
                    .FirstOrDefaultAsync(),
                
                CustomFieldType.Checkbox => await _context.CaseCustomFieldBooleans
                    .Where(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id)
                    .Select(cf => (object?)cf.Value)
                    .FirstOrDefaultAsync(),
                
                _ => null
            };
        }

        private async Task SetCustomFieldValueAsync(SurveyFieldMapping mapping, Case caseEntity, object? value)
        {
            var fieldName = mapping.TargetFieldPath.Replace("CustomField:", "");
            
            _logger.LogDebug("SetCustomFieldValueAsync: fieldName={FieldName}, value={Value}, caseId={CaseId}",
                fieldName, value, caseEntity.Id);
            
            var fieldDef = await _context.CustomFieldDefinitions
                .FirstOrDefaultAsync(f => f.Name == fieldName);

            if (fieldDef == null)
            {
                _logger.LogWarning("Custom field definition not found for name: {FieldName}", fieldName);
                return;
            }

            _logger.LogDebug("Custom field found: Id={Id}, Type={Type}, LookupTableId={LookupTableId}",
                fieldDef.Id, fieldDef.FieldType, fieldDef.LookupTableId);

            switch (fieldDef.FieldType)
            {
                case CustomFieldType.Text:
                case CustomFieldType.TextArea:
                case CustomFieldType.Email:
                case CustomFieldType.Phone:
                    var existingString = await _context.CaseCustomFieldStrings
                        .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                    
                    if (existingString != null)
                    {
                        existingString.Value = value?.ToString();
                        _logger.LogInformation("Updated string custom field {FieldName} = {Value}", fieldName, value);
                    }
                    else
                    {
                        _context.CaseCustomFieldStrings.Add(new CaseCustomFieldString
                        {
                            CaseId = caseEntity.Id,
                            FieldDefinitionId = fieldDef.Id,
                            Value = value?.ToString()
                        });
                        _logger.LogInformation("Created string custom field {FieldName} = {Value}", fieldName, value);
                    }
                    break;

                case CustomFieldType.Number:
                    var existingNumber = await _context.CaseCustomFieldNumbers
                        .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                    
                    if (decimal.TryParse(value?.ToString(), out var numValue))
                    {
                        if (existingNumber != null)
                        {
                            existingNumber.Value = numValue;
                            _logger.LogInformation("Updated number custom field {FieldName} = {Value}", fieldName, numValue);
                        }
                        else
                        {
                            _context.CaseCustomFieldNumbers.Add(new CaseCustomFieldNumber
                            {
                                CaseId = caseEntity.Id,
                                FieldDefinitionId = fieldDef.Id,
                                Value = numValue
                            });
                            _logger.LogInformation("Created number custom field {FieldName} = {Value}", fieldName, numValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse number value: {Value}", value);
                    }
                    break;

                case CustomFieldType.Date:
                    var existingDate = await _context.CaseCustomFieldDates
                        .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                    
                    if (DateTime.TryParse(value?.ToString(), out var dateValue))
                    {
                        if (existingDate != null)
                        {
                            existingDate.Value = dateValue;
                            _logger.LogInformation("Updated date custom field {FieldName} = {Value}", fieldName, dateValue);
                        }
                        else
                        {
                            _context.CaseCustomFieldDates.Add(new CaseCustomFieldDate
                            {
                                CaseId = caseEntity.Id,
                                FieldDefinitionId = fieldDef.Id,
                                Value = dateValue
                            });
                            _logger.LogInformation("Created date custom field {FieldName} = {Value}", fieldName, dateValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse date value: {Value}", value);
                    }
                    break;

                case CustomFieldType.Dropdown:
                    var existingLookup = await _context.CaseCustomFieldLookups
                        .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                    
                    // Try to find lookup value by name or ID
                    int? lookupValueId = null;
                    
                    if (int.TryParse(value?.ToString(), out var directId))
                    {
                        // Value is already an ID
                        lookupValueId = directId;
                        _logger.LogDebug("Using direct lookup value ID: {Id}", directId);
                    }
                    else if (fieldDef.LookupTableId.HasValue)
                    {
                        // Value is a string - look up by name
                        var lookupValue = await _context.LookupValues
                            .FirstOrDefaultAsync(lv => lv.LookupTableId == fieldDef.LookupTableId.Value 
                                                    && lv.Value == value.ToString());
                        
                        if (lookupValue != null)
                        {
                            lookupValueId = lookupValue.Id;
                            _logger.LogDebug("Found lookup value '{Name}' with ID: {Id}", value, lookupValueId);
                        }
                        else
                        {
                            _logger.LogWarning("Lookup value '{Value}' not found in LookupTable {TableId}", 
                                value, fieldDef.LookupTableId.Value);
                        }
                    }
                    
                    if (lookupValueId.HasValue)
                    {
                        if (existingLookup != null)
                        {
                            existingLookup.LookupValueId = lookupValueId;
                            _logger.LogInformation("Updated lookup custom field {FieldName} = {Value} (ID: {Id})", 
                                fieldName, value, lookupValueId);
                        }
                        else
                        {
                            _context.CaseCustomFieldLookups.Add(new CaseCustomFieldLookup
                            {
                                CaseId = caseEntity.Id,
                                FieldDefinitionId = fieldDef.Id,
                                LookupValueId = lookupValueId
                            });
                            _logger.LogInformation("Created lookup custom field {FieldName} = {Value} (ID: {Id})", 
                                fieldName, value, lookupValueId);
                        }
                    }
                    break;

                case CustomFieldType.Checkbox:
                    var existingBool = await _context.CaseCustomFieldBooleans
                        .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                    
                    if (bool.TryParse(value?.ToString(), out var boolValue))
                    {
                        if (existingBool != null)
                        {
                            existingBool.Value = boolValue;
                            _logger.LogInformation("Updated checkbox custom field {FieldName} = {Value}", fieldName, boolValue);
                        }
                        else
                        {
                            _context.CaseCustomFieldBooleans.Add(new CaseCustomFieldBoolean
                            {
                                CaseId = caseEntity.Id,
                                FieldDefinitionId = fieldDef.Id,
                                Value = boolValue
                            });
                            _logger.LogInformation("Created checkbox custom field {FieldName} = {Value}", fieldName, boolValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse boolean value: {Value}", value);
                    }
                    break;
                    
                default:
                    _logger.LogWarning("Unsupported custom field type: {Type} for field {FieldName}", 
                        fieldDef.FieldType, fieldName);
                    break;
            }
        }

        private async Task QueueFieldChangeForReviewAsync(
            SurveyFieldMapping mapping,
            CaseTask task,
            object? oldValue,
            object? newValue)
        {
            // MappingAction now fully controls behavior - no need for TriggerReviewQueue boolean
            var changeSnapshot = System.Text.Json.JsonSerializer.Serialize(new
            {
                field = mapping.TargetFieldPath,
                surveyQuestion = mapping.SurveyQuestionName,
                oldValue = oldValue,
                newValue = newValue,
                changedAt = DateTime.UtcNow,
                mappingId = mapping.Id
            });

            var reviewEntry = new ReviewQueue
            {
                EntityType = "SurveyFieldChange",
                EntityId = task.Id.GetHashCode(),
                CaseId = task.CaseId,
                PatientId = task.Case?.PatientId,
                DiseaseId = task.Case?.DiseaseId,
                TaskId = task.Id,
                ChangeType = "FieldChanged",
                TriggerField = mapping.TargetFieldPath,
                ChangeSnapshot = changeSnapshot,
                Priority = mapping.ReviewPriority,
                ReviewStatus = "Pending",
                GroupKey = GenerateGroupKey(mapping, task, newValue),
                GroupCount = 1,
                CreatedDate = DateTime.UtcNow
            };

            // Check for existing group within time window
            var cutoffTime = DateTime.UtcNow.AddHours(-mapping.GroupingWindowHours);
            var existingGroup = await _context.ReviewQueue
                .Where(rq => rq.GroupKey == reviewEntry.GroupKey
                          && rq.CreatedDate >= cutoffTime
                          && rq.ReviewStatus == "Pending")
                .OrderByDescending(rq => rq.CreatedDate)
                .FirstOrDefaultAsync();

            if (existingGroup != null)
            {
                existingGroup.GroupCount++;
            }
            else
            {
                _context.ReviewQueue.Add(reviewEntry);
            }
        }

        private string GenerateGroupKey(SurveyFieldMapping mapping, CaseTask task, object? newValue)
        {
            var components = new List<string>
            {
                "SurveyFieldChange",
                mapping.TargetFieldPath,
                task.CaseId.ToString(),
                newValue?.ToString() ?? "null",
                task.Case?.DiseaseId?.ToString() ?? "",
                DateTime.UtcNow.ToString("yyyyMMddHH")
            };

            return string.Join("|", components);
        }

        // CRUD Methods for UI Component
        
        public async Task<List<SurveyFieldMapping>> GetMappingsByTypeAsync(
            MappingConfigurationType configurationType,
            Guid configurationId)
        {
            return await _context.SurveyFieldMappings
                .Include(m => m.TargetSymptom)
                .Where(m => m.ConfigurationType == configurationType && m.ConfigurationId == configurationId)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
        }

        public async Task<SurveyFieldMapping?> GetMappingByIdAsync(Guid mappingId)
        {
            return await _context.SurveyFieldMappings
                .Include(m => m.TargetSymptom)
                .FirstOrDefaultAsync(m => m.Id == mappingId);
        }

        public async Task<List<SurveyQuestion>> GetSurveyQuestionsAsync(Guid surveyTemplateId)
        {
            var template = await _context.SurveyTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == surveyTemplateId);

            if (template == null || string.IsNullOrEmpty(template.SurveyDefinitionJson))
                return new List<SurveyQuestion>();

            return await GetSurveyQuestionsAsync(template.SurveyDefinitionJson);
        }

        public async Task<List<SurveyQuestion>> GetCollectionQuestionsAsync(Guid surveyTemplateId)
        {
            var allQuestions = await GetSurveyQuestionsAsync(surveyTemplateId);
            
            // Filter to only matrix/dynamic questions (collection-capable)
            return allQuestions
                .Where(q => q.Type == "matrix" || 
                           q.Type == "matrixdynamic" || 
                           q.Type == "matrixdropdown")
                .ToList();
        }

        public async Task<SurveyFieldMapping> CreateMappingAsync(SurveyFieldMapping mapping)
        {
            mapping.Id = Guid.NewGuid();
            mapping.CreatedDate = DateTime.UtcNow;
            
            // Auto-fix field path format if needed
            mapping.TargetFieldPath = NormalizeFieldPath(mapping.TargetFieldPath, mapping.FieldCategory);
            _logger.LogInformation("Creating mapping: {Question} -> {Path}", 
                mapping.SurveyQuestionName, mapping.TargetFieldPath);
            
            _context.SurveyFieldMappings.Add(mapping);
            await _context.SaveChangesAsync();
            
            return mapping;
        }

        public async Task UpdateMappingAsync(SurveyFieldMapping mapping)
        {
            var existing = await _context.SurveyFieldMappings.FindAsync(mapping.Id);
            if (existing == null)
                throw new ArgumentException($"Mapping {mapping.Id} not found");

            // Auto-fix field path format if needed
            mapping.TargetFieldPath = NormalizeFieldPath(mapping.TargetFieldPath, mapping.FieldCategory);
            _logger.LogInformation("Updating mapping: {Question} -> {Path}", 
                mapping.SurveyQuestionName, mapping.TargetFieldPath);

            // Update properties
            existing.SurveyQuestionName = mapping.SurveyQuestionName;
            existing.TargetFieldPath = mapping.TargetFieldPath;
            existing.TargetFieldType = mapping.TargetFieldType;
            existing.FieldCategory = mapping.FieldCategory;
            existing.TargetSymptomId = mapping.TargetSymptomId;
            existing.MappingAction = mapping.MappingAction;
            existing.BusinessRule = mapping.BusinessRule;
            existing.TriggerReviewQueue = mapping.TriggerReviewQueue;
            existing.ReviewPriority = mapping.ReviewPriority;
            existing.GroupingWindowHours = mapping.GroupingWindowHours;
            existing.ValidationRules = mapping.ValidationRules;
            existing.TransformationScript = mapping.TransformationScript;
            existing.DisplayName = mapping.DisplayName;
            existing.Description = mapping.Description;
            existing.IsActive = mapping.IsActive;
            existing.DisplayOrder = mapping.DisplayOrder;
            existing.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private string NormalizeFieldPath(string fieldPath, MappingFieldCategory category)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                return fieldPath;

            // Fix CustomField underscore ? colon
            if (fieldPath.StartsWith("CustomField_"))
            {
                fieldPath = fieldPath.Replace("CustomField_", "CustomField:");
                _logger.LogInformation("Normalized custom field path: {Path}", fieldPath);
            }

            // Add entity prefix if missing
            if (!fieldPath.Contains('.') && !fieldPath.StartsWith("CustomField:"))
            {
                var prefix = category switch
                {
                    MappingFieldCategory.Patient => "Patient",
                    MappingFieldCategory.Case => "Case",
                    MappingFieldCategory.Symptom => "CaseSymptom",
                    MappingFieldCategory.Exposure => "ExposureEvent",
                    MappingFieldCategory.LabResult => "LabResult",
                    MappingFieldCategory.Task => "CaseTask",
                    _ => "Case"
                };

                fieldPath = $"{prefix}.{fieldPath}";
                _logger.LogInformation("Added entity prefix to field path: {Path}", fieldPath);
            }

            return fieldPath;
        }

        public async Task DeleteMappingAsync(Guid mappingId)
        {
            var mapping = await _context.SurveyFieldMappings.FindAsync(mappingId);
            if (mapping != null)
            {
                _context.SurveyFieldMappings.Remove(mapping);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SuggestMappingsAsync(
            Guid surveyTemplateId,
            MappingConfigurationType configurationType,
            Guid configurationId,
            Guid? diseaseId = null)
        {
            var template = await _context.SurveyTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == surveyTemplateId);

            if (template == null || string.IsNullOrEmpty(template.SurveyDefinitionJson))
                return;

            // CRITICAL FIX: Remove any existing inactive suggestions first to prevent duplicates
            var existingSuggestions = await _context.SurveyFieldMappings
                .Where(m => m.ConfigurationType == configurationType 
                         && m.ConfigurationId == configurationId 
                         && !m.IsActive)
                .ToListAsync();

            if (existingSuggestions.Any())
            {
                _logger.LogInformation(
                    "Removing {Count} existing inactive suggestions for {Type}/{Id} before generating new ones",
                    existingSuggestions.Count,
                    configurationType,
                    configurationId);
                    
                _context.SurveyFieldMappings.RemoveRange(existingSuggestions);
                await _context.SaveChangesAsync();
            }

            var suggestions = await GetSuggestedMappingsAsync(
                template.SurveyDefinitionJson,
                configurationType,
                configurationId,
                diseaseId);

            // Add suggested mappings to database (inactive by default)
            foreach (var suggestion in suggestions)
            {
                suggestion.IsActive = false; // User must review and activate
                suggestion.CreatedDate = DateTime.UtcNow;
            }

            await _context.SurveyFieldMappings.AddRangeAsync(suggestions);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Created {Count} new mapping suggestions for {Type}/{Id}",
                suggestions.Count,
                configurationType,
                configurationId);
        }

        private async Task<object?> GetSymptomFieldAsync(Case caseEntity, int symptomId, string[] propertyPath)
        {
            _logger.LogDebug("GetSymptomFieldAsync: CaseId={CaseId}, SymptomId={SymptomId}, Path={Path}",
                caseEntity.Id, symptomId, string.Join(".", propertyPath));

            var caseSymptom = caseEntity.CaseSymptoms?
                .FirstOrDefault(cs => cs.SymptomId == symptomId);

            if (caseSymptom == null)
            {
                _logger.LogDebug("CaseSymptom not found for SymptomId={SymptomId}", symptomId);
                return null;
            }

            return GetPropertyValue(caseSymptom, propertyPath);
        }

        private async Task SetSymptomFieldAsync(Case caseEntity, int symptomId, string[] propertyPath, object? value)
        {
            _logger.LogDebug("SetSymptomFieldAsync: CaseId={CaseId}, SymptomId={SymptomId}, Path={Path}, Value={Value}",
                caseEntity.Id, symptomId, string.Join(".", propertyPath), value);

            var caseSymptom = caseEntity.CaseSymptoms?
                .FirstOrDefault(cs => cs.SymptomId == symptomId && !cs.IsDeleted);

            if (caseSymptom == null)
            {
                _logger.LogInformation("Creating new CaseSymptom for SymptomId={SymptomId}", symptomId);
                caseSymptom = new CaseSymptom
                {
                    CaseId = caseEntity.Id,
                    SymptomId = symptomId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CaseSymptoms.Add(caseSymptom);
                
                if (caseEntity.CaseSymptoms == null)
                    caseEntity.CaseSymptoms = new List<CaseSymptom>();
                caseEntity.CaseSymptoms.Add(caseSymptom);
            }

            SetPropertyValue(caseSymptom, propertyPath, value);
            _logger.LogInformation("Set CaseSymptom field: SymptomId={SymptomId}, {Path}={Value}", 
                symptomId, string.Join(".", propertyPath), value);
        }

        // ========================================
        // PHASE 2B: COLLECTION MAPPING PROCESSOR
        // ========================================
        
        /// <summary>
        /// Process collection mappings (matrix/matrixdynamic questions)
        /// Calls ICollectionMappingService for entity creation and duplicate detection
        /// </summary>
        private async Task ProcessCollectionMappingsAsync(
            List<SurveyFieldMapping> collectionMappings,
            Dictionary<string, object> surveyResponses,
            CaseTask task,
            MappingExecutionResult result)
        {
            _logger.LogInformation("Processing {Count} collection mappings", collectionMappings.Count);

            foreach (var mapping in collectionMappings)
            {
                try
                {
                    // Get survey response data for this question
                    if (!surveyResponses.TryGetValue(mapping.SurveyQuestionName, out var questionData))
                    {
                        _logger.LogWarning(
                            "Collection question '{Question}' not found in survey responses",
                            mapping.SurveyQuestionName
                        );
                        result.SkippedCount++;
                        continue;
                    }

                    // DEBUG: Log what type of data we actually received
                    _logger.LogInformation(
                        "Collection question '{Question}' found. Data type: {Type}, Value: {Value}",
                        mapping.SurveyQuestionName,
                        questionData?.GetType().Name ?? "null",
                        questionData?.ToString()?.Substring(0, Math.Min(200, questionData?.ToString()?.Length ?? 0)) ?? "null"
                    );

                    // Parse as JArray (matrix data)
                    // Support both matrixdynamic (array) and matrixdropdown (object with row keys)
                    // Also support System.Text.Json.JsonElement
                    JArray? rowData = null;
                    
                    // Handle System.Text.Json.JsonElement (from Dictionary<string, object>)
                    if (questionData is System.Text.Json.JsonElement jsonElement)
                    {
                        _logger.LogInformation(
                            "Collection question '{Question}' is JsonElement. ValueKind: {Kind}",
                            mapping.SurveyQuestionName,
                            jsonElement.ValueKind
                        );
                        
                        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            // matrixdynamic: Convert JsonElement array to JArray
                            rowData = JArray.Parse(jsonElement.GetRawText());
                            _logger.LogInformation(
                                "Converted JsonElement array to JArray with {Count} rows",
                                rowData.Count
                            );
                        }
                        else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            // matrixdropdown: Convert JsonElement object to JArray
                            var jObjectFromElement = JObject.Parse(jsonElement.GetRawText());
                            rowData = new JArray();
                            foreach (var prop in jObjectFromElement.Properties())
                            {
                                if (prop.Value is JObject rowObj)
                                {
                                    rowData.Add(rowObj);
                                }
                            }
                            _logger.LogInformation(
                                "Converted JsonElement object (matrixdropdown) to JArray with {Count} rows. Properties: {Props}",
                                rowData.Count,
                                string.Join(", ", jObjectFromElement.Properties().Select(p => p.Name))
                            );
                        }
                    }
                    else if (questionData is JArray jArray)
                    {
                        // matrixdynamic format: [{ "col1": "val1" }, { "col2": "val2" }]
                        rowData = jArray;
                        _logger.LogInformation(
                            "Collection question '{Question}' is array format (matrixdynamic) with {Count} rows",
                            mapping.SurveyQuestionName,
                            jArray.Count
                        );
                    }
                    else if (questionData is JObject jObject)
                    {
                        // matrixdropdown format: { "Row 1": { "col1": "val1" }, "Row 2": { "col2": "val2" } }
                        // Convert to array format
                        rowData = new JArray();
                        foreach (var prop in jObject.Properties())
                        {
                            if (prop.Value is JObject rowObj)
                            {
                                rowData.Add(rowObj);
                            }
                        }
                        _logger.LogInformation(
                            "Collection question '{Question}' is object format (matrixdropdown) with {Count} rows. Properties: {Props}",
                            mapping.SurveyQuestionName,
                            rowData.Count,
                            string.Join(", ", jObject.Properties().Select(p => p.Name))
                        );
                    }
                    else if (questionData is string jsonString)
                    {
                        _logger.LogInformation(
                            "Collection question '{Question}' is string format, attempting to parse...",
                            mapping.SurveyQuestionName
                        );
                        try
                        {
                            // Try parsing as array first
                            rowData = JArray.Parse(jsonString);
                            _logger.LogInformation("Parsed collection question '{Question}' from string as array with {Count} items", 
                                mapping.SurveyQuestionName, rowData.Count);
                        }
                        catch
                        {
                            try
                            {
                                // Try parsing as object (matrixdropdown)
                                var jObjectFromString = JObject.Parse(jsonString);
                                rowData = new JArray();
                                foreach (var prop in jObjectFromString.Properties())
                                {
                                    if (prop.Value is JObject rowObj)
                                    {
                                        rowData.Add(rowObj);
                                    }
                                }
                                _logger.LogInformation("Parsed collection question '{Question}' from string as object with {Count} rows", 
                                    mapping.SurveyQuestionName, rowData.Count);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to parse collection data as JSON array or object");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Collection question '{Question}' has unexpected data type: {Type}",
                            mapping.SurveyQuestionName,
                            questionData?.GetType().FullName ?? "null"
                        );
                    }

                    if (rowData == null || rowData.Count == 0)
                    {
                        _logger.LogInformation(
                            "Collection question '{Question}' has no rows - skipping",
                            mapping.SurveyQuestionName
                        );
                        result.SkippedCount++;
                        continue;
                    }

                    // Parse collection configuration
                    if (string.IsNullOrEmpty(mapping.CollectionConfigJson))
                    {
                        _logger.LogError(
                            "Collection mapping '{Question}' has no configuration JSON",
                            mapping.SurveyQuestionName
                        );
                        result.ErrorCount++;
                        result.Errors.Add($"Collection mapping '{mapping.SurveyQuestionName}' missing configuration");
                        continue;
                    }

                    var config = JsonConvert.DeserializeObject<CollectionMappingConfig>(
                        mapping.CollectionConfigJson
                    );

                    if (config == null)
                    {
                        _logger.LogError(
                            "Failed to deserialize collection configuration for '{Question}'",
                            mapping.SurveyQuestionName
                        );
                        result.ErrorCount++;
                        result.Errors.Add($"Invalid collection configuration for '{mapping.SurveyQuestionName}'");
                        continue;
                    }

                    // Process the collection via CollectionMappingService
                    _logger.LogInformation(
                        "Processing collection '{Question}' with {RowCount} rows, target entity: {EntityType}",
                        mapping.SurveyQuestionName,
                        rowData.Count,
                        config.TargetEntityType
                    );

                    // Build survey submission context for multi-entity creation
                    var submissionContext = new SurveySubmissionContext
                    {
                        CaseId = task.CaseId,
                        PatientId = task.Case!.PatientId,
                        TaskId = task.Id,
                        DiseaseId = task.Case.DiseaseId ?? Guid.Empty,
                        JurisdictionId = null, // Case doesn't have JurisdictionId yet
                        SubmittedBy = "System", // TODO: Pass user from caller
                        SubmittedDate = DateTime.UtcNow,
                        MappingAction = mapping.MappingAction // Pass through parent mapping action
                    };
                    
                    _logger.LogInformation(
                        "Collection context: CaseId={CaseId}, PatientId={PatientId}, TaskId={TaskId}, DiseaseId={DiseaseId}, MappingAction={MappingAction}",
                        submissionContext.CaseId,
                        submissionContext.PatientId,
                        submissionContext.TaskId,
                        submissionContext.DiseaseId,
                        submissionContext.MappingAction
                    );

                    // Use context-aware processing for multi-entity support (Patient + Contact + Exposure)
                    var collectionResult = await _collectionMappingService.ProcessCollectionWithContextAsync(
                        surveyResponseId: Guid.Empty, // TODO: Track survey responses
                        questionName: mapping.SurveyQuestionName,
                        rowData: rowData,
                        config: config,
                        context: submissionContext
                    );

                    // Update result with collection processing outcomes
                    result.CollectionEntitiesCreated += collectionResult.EntitiesCreated.Count(e => e.IsPrimaryEntity);
                    result.CollectionItemsForReview += collectionResult.ItemsRequiringReview;

                    if (!collectionResult.Success)
                    {
                        result.ErrorCount += collectionResult.Errors.Count;
                        result.Errors.AddRange(collectionResult.Errors);
                    }

                    _logger.LogInformation(
                        "Collection '{Question}' processed: {PrimaryCount} primary entities, {RelatedCount} related entities, {Review} items for review, {Errors} errors",
                        mapping.SurveyQuestionName,
                        collectionResult.EntitiesCreated.Count(e => e.IsPrimaryEntity),
                        collectionResult.EntitiesCreated.Count(e => !e.IsPrimaryEntity),
                        collectionResult.ItemsRequiringReview,
                        collectionResult.Errors.Count
                    );
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var errorMsg = $"Error processing collection mapping for {mapping.SurveyQuestionName}: {ex.Message}";
                    result.Errors.Add(errorMsg);
                    _logger.LogError(ex, "Error processing collection mapping '{Question}'", mapping.SurveyQuestionName);
                }
            }
        }


    }
}


