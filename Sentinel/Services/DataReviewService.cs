using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Services;

public class DataReviewService : IDataReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICollectionMappingService _collectionMappingService;
    private readonly ITaskService _taskService;

    public DataReviewService(
        ApplicationDbContext context, 
        IHttpContextAccessor httpContextAccessor,
        ICollectionMappingService collectionMappingService,
        ITaskService taskService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _collectionMappingService = collectionMappingService;
        _taskService = taskService;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public async Task<int> QueueForReviewAsync(
        string entityType,
        int entityId,
        Guid? diseaseId = null,
        Guid? caseId = null,
        Guid? patientId = null,
        string changeType = "New",
        string? triggerField = null,
        object? changeSnapshot = null,
        bool autoCreateTask = true)
    {
        // Get disease settings if disease is provided
        int priority = ReviewPriorities.Medium;
        int groupingWindowHours = 6;

        if (diseaseId.HasValue)
        {
            var settings = await GetEffectiveReviewSettingsAsync(diseaseId.Value);
            priority = settings.DefaultPriority;
            groupingWindowHours = settings.GroupingWindowHours;
        }

        // Generate group key for potential grouping
        var groupKey = GenerateGroupKey(entityType, triggerField, changeSnapshot, diseaseId);

        // Check if there's a recent group we can add to
        var cutoffTime = DateTime.UtcNow.AddHours(-groupingWindowHours);
        var existingGroup = await _context.ReviewQueue
            .Where(rq => rq.GroupKey == groupKey 
                      && rq.CreatedDate >= cutoffTime
                      && rq.ReviewStatus == ReviewStatuses.Pending)
            .OrderByDescending(rq => rq.CreatedDate)
            .FirstOrDefaultAsync();

        if (existingGroup != null)
        {
            // Add to existing group
            existingGroup.GroupCount++;
            await _context.SaveChangesAsync();
            return existingGroup.Id;
        }

        // Create new review queue entry
        var reviewEntry = new ReviewQueue
        {
            EntityType = entityType,
            EntityId = entityId,
            CaseId = caseId,
            PatientId = patientId,
            DiseaseId = diseaseId,
            ChangeType = changeType,
            TriggerField = triggerField,
            ChangeSnapshot = changeSnapshot != null ? JsonSerializer.Serialize(changeSnapshot) : null,
            Priority = priority,
            ReviewStatus = ReviewStatuses.Pending,
            GroupKey = groupKey,
            GroupCount = 1,
            CreatedByUserId = GetCurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        _context.ReviewQueue.Add(reviewEntry);
        await _context.SaveChangesAsync();

        // Auto-create task if requested
        if (autoCreateTask)
        {
            var taskId = await CreateTaskForReviewEntryAsync(reviewEntry);
            if (taskId.HasValue)
            {
                reviewEntry.TaskId = taskId;
                await _context.SaveChangesAsync();
            }
        }

        return reviewEntry.Id;
    }

    private async Task<Guid?> CreateTaskForReviewEntryAsync(ReviewQueue reviewEntry)
    {
        try
        {
            var taskTitle = $"Review {reviewEntry.EntityType}";
            
            if (reviewEntry.CaseId.HasValue)
            {
                var caseEntity = await _context.Cases
                    .Where(c => c.Id == reviewEntry.CaseId.Value)
                    .Select(c => c.FriendlyId)
                    .FirstOrDefaultAsync();
                
                if (!string.IsNullOrEmpty(caseEntity))
                {
                    taskTitle += $" for Case {caseEntity}";
                }
            }

            var task = new CaseTask
            {
                Id = Guid.NewGuid(),
                Title = taskTitle,
                Description = $"Review required for {reviewEntry.EntityType} (ID: {reviewEntry.EntityId})",
                CaseId = reviewEntry.CaseId ?? Guid.Empty,
                Status = CaseTaskStatus.Pending,
                Priority = reviewEntry.Priority switch
                {
                    ReviewPriorities.Urgent => TaskPriority.Urgent,
                    ReviewPriorities.High => TaskPriority.High,
                    ReviewPriorities.Medium => TaskPriority.Medium,
                    _ => TaskPriority.Low
                },
                DueDate = DateTime.UtcNow.AddDays(2),
                IsInterviewTask = false,
                CreatedAt = DateTime.UtcNow,
                TaskTypeId = await GetDefaultTaskTypeIdAsync()
            };

            _context.CaseTasks.Add(task);
            await _context.SaveChangesAsync();

            return task.Id;
        }
        catch
        {
            // Task creation failed, but don't fail the whole operation
            return null;
        }
    }

    private async Task<Guid> GetDefaultTaskTypeIdAsync()
    {
        var taskType = await _context.TaskTypes.FirstOrDefaultAsync();
        return taskType?.Id ?? Guid.Empty;
    }

    public async Task<DiseaseReviewSettings> GetEffectiveReviewSettingsAsync(Guid diseaseId)
    {
        var disease = await _context.Diseases
            .Where(d => d.Id == diseaseId)
            .Select(d => new
            {
                d.ParentDiseaseId,
                d.ReviewGroupingWindowHours,
                d.ReviewAutoQueueLabResults,
                d.ReviewAutoQueueExposures,
                d.ReviewAutoQueueContacts,
                d.ReviewAutoQueueConfirmationChanges,
                d.ReviewAutoQueueDiseaseChanges,
                d.ReviewAutoQueueClinicalNotifications,
                d.ReviewAutoQueueNewCases,
                d.ReviewDefaultPriority
            })
            .FirstOrDefaultAsync();

        if (disease == null)
        {
            return new DiseaseReviewSettings();
        }

        // If no parent, use this disease's settings
        if (!disease.ParentDiseaseId.HasValue)
        {
            return new DiseaseReviewSettings
            {
                GroupingWindowHours = disease.ReviewGroupingWindowHours,
                AutoQueueLabResults = disease.ReviewAutoQueueLabResults,
                AutoQueueExposures = disease.ReviewAutoQueueExposures,
                AutoQueueContacts = disease.ReviewAutoQueueContacts,
                AutoQueueConfirmationChanges = disease.ReviewAutoQueueConfirmationChanges,
                AutoQueueDiseaseChanges = disease.ReviewAutoQueueDiseaseChanges,
                AutoQueueClinicalNotifications = disease.ReviewAutoQueueClinicalNotifications,
                AutoQueueNewCases = disease.ReviewAutoQueueNewCases,
                DefaultPriority = disease.ReviewDefaultPriority
            };
        }

        // If parent exists, inherit settings
        var parentSettings = await GetEffectiveReviewSettingsAsync(disease.ParentDiseaseId.Value);

        return new DiseaseReviewSettings
        {
            GroupingWindowHours = disease.ReviewGroupingWindowHours > 0 
                ? disease.ReviewGroupingWindowHours 
                : parentSettings.GroupingWindowHours,
            AutoQueueLabResults = disease.ReviewAutoQueueLabResults,
            AutoQueueExposures = disease.ReviewAutoQueueExposures,
            AutoQueueContacts = disease.ReviewAutoQueueContacts,
            AutoQueueConfirmationChanges = disease.ReviewAutoQueueConfirmationChanges,
            AutoQueueDiseaseChanges = disease.ReviewAutoQueueDiseaseChanges,
            AutoQueueClinicalNotifications = disease.ReviewAutoQueueClinicalNotifications,
            DefaultPriority = disease.ReviewDefaultPriority > 0 
                ? disease.ReviewDefaultPriority 
                : parentSettings.DefaultPriority
        };
    }

    public string GenerateGroupKey(string entityType, string? triggerField, object? newValue, Guid? diseaseId)
    {
        var components = new List<string> { entityType };

        if (!string.IsNullOrEmpty(triggerField))
        {
            components.Add(triggerField);
        }

        if (newValue != null)
        {
            var valueString = newValue.ToString() ?? string.Empty;
            components.Add(valueString);
        }

        if (diseaseId.HasValue)
        {
            components.Add(diseaseId.Value.ToString());
        }

        return string.Join("|", components);
    }

    public async Task<bool> ConfirmReviewAsync(int reviewQueueId, string? notes = null)
    {
        var reviewItem = await _context.ReviewQueue
            .Include(r => r.Task)
                .ThenInclude(t => t!.Case)
                    .ThenInclude(c => c!.Patient)
            .FirstOrDefaultAsync(r => r.Id == reviewQueueId);
            
        if (reviewItem == null || reviewItem.ReviewStatus != ReviewStatuses.Pending)
        {
            return false;
        }

        // APPLY THE FIELD CHANGE for SurveyFieldChange entity type
        if (reviewItem.EntityType == "SurveyFieldChange" && reviewItem.ChangeSnapshot != null)
        {
            try
            {
                await ApplySurveyFieldChangeAsync(reviewItem);
            }
            catch (Exception ex)
            {
                // Log error but still mark as reviewed
                // TODO: Add ILogger to log the error
                reviewItem.ReviewNotes = $"Applied with errors: {ex.Message}\n{notes}";
            }
        }

        // ========================================
        // PHASE 2C: HANDLE COLLECTION MAPPING REVIEW ITEMS
        // ========================================
        
        CreatedEntityInfo? createdEntity = null;
        
        // Handle collection mapping entity creation approvals
        if (reviewItem.EntityType == ReviewEntityTypes.DuplicatePatient ||
            reviewItem.EntityType == ReviewEntityTypes.DuplicateContact ||
            reviewItem.EntityType == ReviewEntityTypes.DuplicateExposure ||
            reviewItem.EntityType == ReviewEntityTypes.BulkEntityCreation)
        {
            try
            {
                // User approved - create the entity
                createdEntity = await _collectionMappingService.CreateEntitiesFromReviewAsync(
                    reviewItem.Id,
                    reviewItem.SelectedExistingEntityId
                );
                
                reviewItem.ReviewNotes = reviewItem.SelectedExistingEntityId.HasValue
                    ? $"Linked to existing entity {reviewItem.SelectedExistingEntityId}\n{notes}"
                    : $"Created new entity {createdEntity.EntityId}\n{notes}";
            }
            catch (Exception ex)
            {
                reviewItem.ReviewNotes = $"Error creating entity: {ex.Message}\n{notes}";
            }
        }

        reviewItem.ReviewStatus = ReviewStatuses.Reviewed;
        reviewItem.ReviewAction = ReviewActions.Confirmed;
        reviewItem.ReviewedByUserId = GetCurrentUserId();
        reviewItem.ReviewedDate = DateTime.UtcNow;
        reviewItem.ReviewNotes = notes;

        // Complete associated task if it exists
        if (reviewItem.TaskId.HasValue)
        {
            var task = await _context.CaseTasks.FindAsync(reviewItem.TaskId.Value);
            if (task != null && task.Status != CaseTaskStatus.Completed)
            {
                task.Status = CaseTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = GetCurrentUserId();
            }
        }

        await _context.SaveChangesAsync();
        
        // ========================================
        // AUTO-CREATE TASKS FOR NEWLY CREATED CONTACTS
        // ========================================
        // When contacts are created through review queue approval, the CaseCreationInterceptor
        // doesn't trigger properly because the entity was added in a different scope.
        // Manually trigger task creation for contacts here.
        if (createdEntity != null && 
            !reviewItem.SelectedExistingEntityId.HasValue &&
            reviewItem.EntityType == ReviewEntityTypes.DuplicateContact)
        {
            try
            {
                var tasksCreated = await _taskService.CreateTasksForCase(
                    createdEntity.EntityId,
                    TaskTrigger.OnContactCreation
                );
                
                if (tasksCreated.Any())
                {
                    reviewItem.ReviewNotes = (reviewItem.ReviewNotes ?? "") + 
                        $"\nAuto-created {tasksCreated.Count} task(s) for contact";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - the entity was created successfully
                // Task creation failure shouldn't block the approval
            }
        }
        
        return true;
    }

    private async Task ApplySurveyFieldChangeAsync(ReviewQueue reviewItem)
    {
        if (reviewItem.ChangeSnapshot == null) return;

        // Parse the change snapshot
        var snapshot = JsonSerializer.Deserialize<SurveyFieldChangeSnapshot>(reviewItem.ChangeSnapshot);
        if (snapshot == null) return;

        // Load the case with patient
        var caseEntity = await _context.Cases
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == reviewItem.CaseId);

        if (caseEntity == null) return;

        // Load the mapping to get field type information
        var mapping = await _context.SurveyFieldMappings
            .FirstOrDefaultAsync(m => m.Id == snapshot.MappingId);

        if (mapping == null)
        {
            // Fallback: try to apply based on field path
            await ApplyFieldChangeWithoutMapping(caseEntity, snapshot.Field, snapshot.NewValue);
            return;
        }

        // Use the same SetFieldValueAsync logic from SurveyMappingService
        await SetFieldValueFromReviewAsync(mapping, caseEntity, snapshot.NewValue);
    }

    private async Task SetFieldValueFromReviewAsync(SurveyFieldMapping mapping, Case caseEntity, object? value)
    {
        // Check for CustomField FIRST (uses colon separator, not dot)
        if (mapping.TargetFieldPath.StartsWith("CustomField:"))
        {
            await SetCustomFieldValueFromReviewAsync(mapping, caseEntity, value);
            return;
        }

        // For standard fields, split by dot
        var parts = mapping.TargetFieldPath.Split('.');

        if (parts[0] == "Patient" && caseEntity.Patient != null)
        {
            SetPropertyValue(caseEntity.Patient, parts.Skip(1).ToArray(), value);
        }
        else if (parts[0] == "Case")
        {
            SetPropertyValue(caseEntity, parts.Skip(1).ToArray(), value);
        }
    }

    private void SetPropertyValue(object obj, string[] propertyPath, object? value)
    {
        for (int i = 0; i < propertyPath.Length - 1; i++)
        {
            var property = obj.GetType().GetProperty(propertyPath[i]);
            if (property == null) return;
            
            var nextObj = property.GetValue(obj);
            if (nextObj == null) return;
            
            obj = nextObj;
        }

        var finalProperty = obj.GetType().GetProperty(propertyPath[^1]);
        if (finalProperty != null && finalProperty.CanWrite)
        {
            var convertedValue = ConvertValueToPropertyType(value, finalProperty.PropertyType);
            finalProperty.SetValue(obj, convertedValue);
        }
    }

    private object? ConvertValueToPropertyType(object? value, Type targetType)
    {
        if (value == null) return null;
        
        // Handle JsonElement
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

        if (targetType.IsInstanceOfType(value))
            return value;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
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
                return Convert.ChangeType(value, underlyingType);
            }
        }
        catch
        {
            return Nullable.GetUnderlyingType(targetType) != null ? null : value;
        }

        return value;
    }

    private async Task SetCustomFieldValueFromReviewAsync(SurveyFieldMapping mapping, Case caseEntity, object? value)
    {
        var fieldName = mapping.TargetFieldPath.Replace("CustomField:", "");
        
        var fieldDef = await _context.CustomFieldDefinitions
            .FirstOrDefaultAsync(f => f.Name == fieldName);

        if (fieldDef == null) return;

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
                }
                else
                {
                    _context.CaseCustomFieldStrings.Add(new CaseCustomFieldString
                    {
                        CaseId = caseEntity.Id,
                        FieldDefinitionId = fieldDef.Id,
                        Value = value?.ToString()
                    });
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
                    }
                    else
                    {
                        _context.CaseCustomFieldNumbers.Add(new CaseCustomFieldNumber
                        {
                            CaseId = caseEntity.Id,
                            FieldDefinitionId = fieldDef.Id,
                            Value = numValue
                        });
                    }
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
                    }
                    else
                    {
                        _context.CaseCustomFieldDates.Add(new CaseCustomFieldDate
                        {
                            CaseId = caseEntity.Id,
                            FieldDefinitionId = fieldDef.Id,
                            Value = dateValue
                        });
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
                    }
                    else
                    {
                        _context.CaseCustomFieldBooleans.Add(new CaseCustomFieldBoolean
                        {
                            CaseId = caseEntity.Id,
                            FieldDefinitionId = fieldDef.Id,
                            Value = boolValue
                        });
                    }
                }
                break;

            case CustomFieldType.Dropdown:
                var existingLookup = await _context.CaseCustomFieldLookups
                    .FirstOrDefaultAsync(cf => cf.CaseId == caseEntity.Id && cf.FieldDefinitionId == fieldDef.Id);
                
                // Try to find lookup value by name or ID
                int? lookupValueId = null;
                
                if (int.TryParse(value?.ToString(), out var directId))
                {
                    lookupValueId = directId;
                }
                else if (fieldDef.LookupTableId.HasValue)
                {
                    var lookupValue = await _context.LookupValues
                        .FirstOrDefaultAsync(lv => lv.LookupTableId == fieldDef.LookupTableId.Value 
                                                && lv.Value == value.ToString());
                    
                    if (lookupValue != null)
                    {
                        lookupValueId = lookupValue.Id;
                    }
                }
                
                if (lookupValueId.HasValue)
                {
                    if (existingLookup != null)
                    {
                        existingLookup.LookupValueId = lookupValueId;
                    }
                    else
                    {
                        _context.CaseCustomFieldLookups.Add(new CaseCustomFieldLookup
                        {
                            CaseId = caseEntity.Id,
                            FieldDefinitionId = fieldDef.Id,
                            LookupValueId = lookupValueId
                        });
                    }
                }
                break;
        }
    }

    private async Task ApplyFieldChangeWithoutMapping(Case caseEntity, string fieldPath, object? value)
    {
        // Fallback for when mapping is not found
        // Basic implementation for common fields
        var parts = fieldPath.Split('.');

        if (parts[0] == "Case")
        {
            SetPropertyValue(caseEntity, parts.Skip(1).ToArray(), value);
        }
        else if (parts[0] == "Patient" && caseEntity.Patient != null)
        {
            SetPropertyValue(caseEntity.Patient, parts.Skip(1).ToArray(), value);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> DismissReviewAsync(int reviewQueueId, string? notes = null)
    {
        var reviewItem = await _context.ReviewQueue.FindAsync(reviewQueueId);
        if (reviewItem == null || reviewItem.ReviewStatus != ReviewStatuses.Pending)
        {
            return false;
        }

        reviewItem.ReviewStatus = ReviewStatuses.Dismissed;
        reviewItem.ReviewAction = ReviewActions.Dismissed;
        reviewItem.ReviewedByUserId = GetCurrentUserId();
        reviewItem.ReviewedDate = DateTime.UtcNow;
        reviewItem.ReviewNotes = notes;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> CreateTaskForReviewAsync(
        int reviewQueueId, 
        string taskTitle, 
        string? taskDescription = null, 
        DateTime? dueDate = null, 
        string? assignedToUserId = null)
    {
        var reviewItem = await _context.ReviewQueue
            .Include(r => r.Case)
            .FirstOrDefaultAsync(r => r.Id == reviewQueueId);

        if (reviewItem == null)
        {
            return null;
        }

        var task = new CaseTask
        {
            Id = Guid.NewGuid(),
            Title = taskTitle,
            Description = taskDescription ?? $"Review required for {reviewItem.EntityType}",
            CaseId = reviewItem.CaseId ?? Guid.Empty,
            Status = CaseTaskStatus.Pending,
            Priority = reviewItem.Priority switch
            {
                ReviewPriorities.Urgent => TaskPriority.Urgent,
                ReviewPriorities.High => TaskPriority.High,
                ReviewPriorities.Medium => TaskPriority.Medium,
                _ => TaskPriority.Low
            },
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(3),
            AssignedToUserId = assignedToUserId,
            IsInterviewTask = false,
            CreatedAt = DateTime.UtcNow,
            TaskTypeId = await GetDefaultTaskTypeIdAsync()
        };

        _context.CaseTasks.Add(task);

        // Mark review as completed with task created
        reviewItem.ReviewStatus = ReviewStatuses.Reviewed;
        reviewItem.ReviewAction = ReviewActions.TaskCreated;
        reviewItem.ReviewedByUserId = GetCurrentUserId();
        reviewItem.ReviewedDate = DateTime.UtcNow;
        reviewItem.TaskId = task.Id;

        await _context.SaveChangesAsync();

        return task.Id;
    }

    public async Task<ReviewQueueResult> GetReviewQueueAsync(
        string? entityType = null,
        List<Guid>? diseaseIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? reviewStatus = "Pending",
        int skip = 0,
        int take = 50)
    {
        var query = _context.ReviewQueue
            .Include(r => r.Case)
                .ThenInclude(c => c.Patient)  // ? Load Case's Patient!
                    .ThenInclude(p => p.Gender)  // ? And its lookups
            .Include(r => r.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)  // ? And SexAtBirth
            .Include(r => r.Case)
                .ThenInclude(c => c.Disease)  // ? Load Case's Disease!
            .Include(r => r.Patient)  // Also load direct ReviewQueue.Patient (for other scenarios)
                .ThenInclude(p => p.Gender)
            .Include(r => r.Patient)
                .ThenInclude(p => p.SexAtBirth)
            .Include(r => r.Disease)  // And direct ReviewQueue.Disease
            .Include(r => r.Task)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(r => r.EntityType == entityType);
        }

        if (diseaseIds != null && diseaseIds.Any())
        {
            query = query.Where(r => r.DiseaseId.HasValue && diseaseIds.Contains(r.DiseaseId.Value));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedDate <= toDate.Value);
        }

        if (!string.IsNullOrEmpty(reviewStatus))
        {
            query = query.Where(r => r.ReviewStatus == reviewStatus);
        }

        // Get total counts
        var totalCount = await query.CountAsync();
        var pendingCount = await _context.ReviewQueue
            .Where(r => r.ReviewStatus == ReviewStatuses.Pending)
            .CountAsync();

        // Get paged items
        var items = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(r => new ReviewQueueItem
            {
                Id = r.Id,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                ChangeType = r.ChangeType,
                TriggerField = r.TriggerField,
                ChangeSnapshot = r.ChangeSnapshot,
                Priority = r.Priority,
                ReviewStatus = r.ReviewStatus,
                GroupKey = r.GroupKey,
                GroupCount = r.GroupCount,
                CreatedDate = r.CreatedDate,
                CaseId = r.CaseId,
                CaseFriendlyId = r.Case != null ? r.Case.FriendlyId : null,
                PatientId = r.PatientId ?? (r.Case != null ? r.Case.PatientId : null),
                PatientName = r.Patient != null 
                    ? $"{r.Patient.GivenName} {r.Patient.FamilyName}" 
                    : (r.Case != null && r.Case.Patient != null ? $"{r.Case.Patient.GivenName} {r.Case.Patient.FamilyName}" : null),
                // ? Complete Patient Demographics - Use Case.Patient if ReviewQueue.Patient is null
                PatientFriendlyId = r.Patient != null 
                    ? r.Patient.FriendlyId 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.FriendlyId : null),
                PatientDateOfBirth = r.Patient != null 
                    ? r.Patient.DateOfBirth 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.DateOfBirth : null),
                PatientAge = (r.Patient != null && r.Patient.DateOfBirth.HasValue) 
                    ? (int)((DateTime.UtcNow - r.Patient.DateOfBirth.Value).TotalDays / 365.25)
                    : (r.Case != null && r.Case.Patient != null && r.Case.Patient.DateOfBirth.HasValue 
                        ? (int)((DateTime.UtcNow - r.Case.Patient.DateOfBirth.Value).TotalDays / 365.25) 
                        : (int?)null),
                PatientGender = r.Patient != null && r.Patient.Gender != null 
                    ? r.Patient.Gender.Name 
                    : (r.Case != null && r.Case.Patient != null && r.Case.Patient.Gender != null ? r.Case.Patient.Gender.Name : null),
                PatientSexAtBirth = r.Patient != null && r.Patient.SexAtBirth != null 
                    ? r.Patient.SexAtBirth.Name 
                    : (r.Case != null && r.Case.Patient != null && r.Case.Patient.SexAtBirth != null ? r.Case.Patient.SexAtBirth.Name : null),
                PatientPhone = r.Patient != null 
                    ? (r.Patient.MobilePhone ?? r.Patient.HomePhone) 
                    : (r.Case != null && r.Case.Patient != null ? (r.Case.Patient.MobilePhone ?? r.Case.Patient.HomePhone) : null),
                PatientEmail = r.Patient != null 
                    ? r.Patient.EmailAddress 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.EmailAddress : null),
                PatientAddressLine = r.Patient != null 
                    ? r.Patient.AddressLine 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.AddressLine : null),
                PatientCity = r.Patient != null 
                    ? r.Patient.City 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.City : null),
                PatientState = r.Patient != null 
                    ? r.Patient.State 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.State : null),
                PatientPostalCode = r.Patient != null 
                    ? r.Patient.PostalCode 
                    : (r.Case != null && r.Case.Patient != null ? r.Case.Patient.PostalCode : null),
                DiseaseId = r.DiseaseId ?? (r.Case != null ? r.Case.DiseaseId : null),
                DiseaseName = r.Disease != null ? r.Disease.Name : (r.Case != null && r.Case.Disease != null ? r.Case.Disease.Name : null),
                HasTask = r.TaskId.HasValue,
                TaskStatus = r.Task != null ? r.Task.Status.ToString() : null
            })
            .ToListAsync();

        // ? For PendingCreation reviews, extract proposed patient data from JSON
        var pendingCreationItems = items.Where(i => i.ChangeType == "PendingCreation" && string.IsNullOrEmpty(i.PatientName)).ToList();
        
        foreach (var item in pendingCreationItems)
        {
            try
            {
                var reviewQueue = await _context.ReviewQueue
                    .Where(r => r.Id == item.Id)
                    .Select(r => r.ProposedEntityDataJson)
                    .FirstOrDefaultAsync();
                
                if (!string.IsNullOrEmpty(reviewQueue))
                {
                    var proposedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(reviewQueue);
                    
                    if (proposedData != null)
                    {
                        // Extract patient name
                        var givenName = proposedData.ContainsKey("GivenName") ? proposedData["GivenName"].GetString() : "";
                        var familyName = proposedData.ContainsKey("FamilyName") ? proposedData["FamilyName"].GetString() : "";
                        if (!string.IsNullOrEmpty(givenName) && !string.IsNullOrEmpty(familyName))
                        {
                            item.PatientName = $"{givenName} {familyName}";
                        }
                        
                        // Extract phone
                        if (proposedData.ContainsKey("MobilePhone"))
                            item.PatientPhone = proposedData["MobilePhone"].GetString();
                        
                        // Extract email
                        if (proposedData.ContainsKey("EmailAddress"))
                            item.PatientEmail = proposedData["EmailAddress"].GetString();
                        
                        // Extract address
                        if (proposedData.ContainsKey("AddressLine"))
                            item.PatientAddressLine = proposedData["AddressLine"].GetString();
                        
                        // Extract city
                        if (proposedData.ContainsKey("City"))
                            item.PatientCity = proposedData["City"].GetString();
                        
                        // Extract state
                        if (proposedData.ContainsKey("State"))
                            item.PatientState = proposedData["State"].GetString();
                        
                        // Extract postal code
                        if (proposedData.ContainsKey("PostalCode"))
                            item.PatientPostalCode = proposedData["PostalCode"].GetString();
                        
                        // Extract DOB and calculate age
                        if (proposedData.ContainsKey("DateOfBirth"))
                        {
                            if (DateTime.TryParse(proposedData["DateOfBirth"].GetString(), out var dob))
                            {
                                item.PatientDateOfBirth = dob;
                                item.PatientAge = (int)((DateTime.UtcNow - dob).TotalDays / 365.25);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, just show without demographics
            }
        }

        return new ReviewQueueResult
        {
            Items = items,
            TotalCount = totalCount,
            PendingCount = pendingCount,
            HasMore = (skip + take) < totalCount
        };
    }

    public async Task<ReviewQueueDetail?> GetReviewItemDetailAsync(int reviewQueueId)
    {
        var reviewItem = await _context.ReviewQueue
            .Include(r => r.Case)
                .ThenInclude(c => c!.Disease)
            .Include(r => r.Case)
                .ThenInclude(c => c!.ConfirmationStatus)
            .Include(r => r.Patient)
            .Include(r => r.Disease)
            .Include(r => r.Task)
            .FirstOrDefaultAsync(r => r.Id == reviewQueueId);

        if (reviewItem == null)
        {
            return null;
        }

        var detail = new ReviewQueueDetail
        {
            ReviewItem = new ReviewQueueItem
            {
                Id = reviewItem.Id,
                EntityType = reviewItem.EntityType,
                EntityId = reviewItem.EntityId,
                ChangeType = reviewItem.ChangeType,
                TriggerField = reviewItem.TriggerField,
                Priority = reviewItem.Priority,
                ReviewStatus = reviewItem.ReviewStatus,
                GroupKey = reviewItem.GroupKey,
                GroupCount = reviewItem.GroupCount,
                CreatedDate = reviewItem.CreatedDate,
                CaseId = reviewItem.CaseId,
                CaseFriendlyId = reviewItem.Case?.FriendlyId,
                PatientId = reviewItem.PatientId,
                PatientName = reviewItem.Patient != null ? $"{reviewItem.Patient.GivenName} {reviewItem.Patient.FamilyName}" : null,
                DiseaseId = reviewItem.DiseaseId,
                DiseaseName = reviewItem.Disease?.Name,
                HasTask = reviewItem.TaskId.HasValue,
                TaskStatus = reviewItem.Task?.Status.ToString()
            }
        };

        // Parse change snapshot
        if (!string.IsNullOrEmpty(reviewItem.ChangeSnapshot))
        {
            try
            {
                detail.ChangeSnapshot = JsonSerializer.Deserialize<Dictionary<string, object?>>(reviewItem.ChangeSnapshot) ?? new();
            }
            catch
            {
                detail.ChangeSnapshot = new();
            }
        }

        // Load entity data based on type
        detail.EntityData = await LoadEntityDataAsync(reviewItem.EntityType, reviewItem.EntityId);

        // Load patient context
        if (reviewItem.PatientId.HasValue)
        {
            var patient = await _context.Patients
                .Where(p => p.Id == reviewItem.PatientId.Value)
                .FirstOrDefaultAsync();

            if (patient != null)
            {
                detail.Patient = new PatientContext
                {
                    Id = patient.Id,
                    FriendlyId = patient.FriendlyId,
                    GivenName = patient.GivenName,
                    FamilyName = patient.FamilyName,
                    DateOfBirth = patient.DateOfBirth,
                    EmailAddress = patient.EmailAddress,
                    MobilePhone = patient.MobilePhone,
                    Address = patient.AddressLine,
                    City = patient.City,
                    State = patient.State,
                    Postcode = patient.PostalCode
                };
            }
        }
        // ? CRITICAL FIX: Parse ProposedEntityDataJson for new entities (from surveys)
        else if (reviewItem.EntityType == ReviewEntityTypes.DuplicatePatient ||
                 reviewItem.EntityType == ReviewEntityTypes.NewPatient ||
                 reviewItem.EntityType == ReviewEntityTypes.BulkEntityCreation)
        {
            if (!string.IsNullOrEmpty(reviewItem.ProposedEntityDataJson))
            {
                try
                {
                    var proposedData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(reviewItem.ProposedEntityDataJson);
                    
                    if (proposedData != null)
                    {
                        detail.Patient = new PatientContext
                        {
                            Id = Guid.Empty, // Not created yet
                            FriendlyId = null,
                            GivenName = proposedData.ContainsKey("GivenName") ? proposedData["GivenName"].GetString() : null,
                            FamilyName = proposedData.ContainsKey("FamilyName") ? proposedData["FamilyName"].GetString() : null,
                            DateOfBirth = proposedData.ContainsKey("DateOfBirth") && DateTime.TryParse(proposedData["DateOfBirth"].GetString(), out var dob) ? dob : null,
                            EmailAddress = proposedData.ContainsKey("EmailAddress") ? proposedData["EmailAddress"].GetString() : null,
                            MobilePhone = proposedData.ContainsKey("MobilePhone") ? proposedData["MobilePhone"].GetString() : null,
                            Address = proposedData.ContainsKey("AddressLine") ? proposedData["AddressLine"].GetString() : null,
                            City = proposedData.ContainsKey("City") ? proposedData["City"].GetString() : null,
                            State = proposedData.ContainsKey("State") ? proposedData["State"].GetString() : null,
                            Postcode = proposedData.ContainsKey("PostalCode") ? proposedData["PostalCode"].GetString() : null
                        };
                    }
                }
                catch
                {
                    // If parsing fails, leave Patient as null
                }
            }
        }

        // Load case context
        if (reviewItem.CaseId.HasValue && reviewItem.Case != null)
        {
            detail.Case = new CaseContext
            {
                Id = reviewItem.Case.Id,
                FriendlyId = reviewItem.Case.FriendlyId,
                DiseaseName = reviewItem.Case.Disease?.Name ?? "Unknown",
                ConfirmationStatus = reviewItem.Case.ConfirmationStatus?.Name ?? "Unknown",
                DateOfOnset = reviewItem.Case.DateOfOnset,
                DateOfNotification = reviewItem.Case.DateOfNotification,
                Status = "Active" // Case doesn't have Status field, defaulting
            };
        }
        
        // ? CRITICAL FIX: Parse PotentialMatchesJson for duplicate detection
        if (!string.IsNullOrEmpty(reviewItem.PotentialMatchesJson))
        {
            try
            {
                detail.PotentialMatches = JsonSerializer.Deserialize<List<EntityMatch>>(reviewItem.PotentialMatchesJson) ?? new();
            }
            catch
            {
                detail.PotentialMatches = new();
            }
        }

        // Load related items (labs, exposures, etc.) for context
        if (reviewItem.CaseId.HasValue)
        {
            detail.RelatedItems = await LoadRelatedItemsAsync(reviewItem.CaseId.Value);
        }

        return detail;
    }

    private async Task<Dictionary<string, object?>> LoadEntityDataAsync(string entityType, int entityId)
    {
        var data = new Dictionary<string, object?>();

        try
        {
            switch (entityType)
            {
                case ReviewEntityTypes.LabResult:
                    var lab = await _context.LabResults
                        .Include(l => l.TestType)
                        .Include(l => l.SpecimenType)
                        .Include(l => l.TestResult)
                        .Include(l => l.Laboratory)
                        .Where(l => l.Id.ToString() == entityId.ToString())
                        .FirstOrDefaultAsync();

                    if (lab != null)
                    {
                        data["TestType"] = lab.TestType?.Name;
                        data["Result"] = lab.TestResult?.Name;
                        data["SpecimenType"] = lab.SpecimenType?.Name;
                        data["CollectionDate"] = lab.SpecimenCollectionDate;
                        data["Laboratory"] = lab.Laboratory?.Name;
                        data["Notes"] = lab.Notes;
                    }
                    break;

                case ReviewEntityTypes.Exposure:
                    var exposure = await _context.ExposureEvents
                        .Include(e => e.Location)
                        .Include(e => e.Event)
                        .Where(e => e.Id.ToString() == entityId.ToString())
                        .FirstOrDefaultAsync();

                    if (exposure != null)
                    {
                        data["LocationName"] = exposure.Location?.Name;
                        data["EventName"] = exposure.Event?.Name;
                        data["ExposureStartDate"] = exposure.ExposureStartDate;
                        data["ExposureEndDate"] = exposure.ExposureEndDate;
                        data["Description"] = exposure.Description;
                    }
                    break;

                case ReviewEntityTypes.Contact:
                    var contact = await _context.Cases
                        .Include(c => c.Patient)
                        .Where(c => c.Id.ToString() == entityId.ToString() && c.Type == CaseType.Contact)
                        .FirstOrDefaultAsync();

                    if (contact != null)
                    {
                        data["ContactName"] = $"{contact.Patient?.GivenName} {contact.Patient?.FamilyName}";
                        data["RelatedCase"] = "See case relationships"; // Contact cases may have relationships
                        data["DateOfNotification"] = contact.DateOfNotification;
                    }
                    break;
            }
        }
        catch
        {
            // Silently fail if entity loading has issues
        }

        return data;
    }

    private async Task<List<RelatedItem>> LoadRelatedItemsAsync(Guid caseId)
    {
        var relatedItems = new List<RelatedItem>();

        try
        {
            // Load recent lab results for this case
            var labs = await _context.LabResults
                .Where(l => l.CaseId == caseId)
                .Include(l => l.TestType)
                .Include(l => l.TestResult)
                .OrderByDescending(l => l.SpecimenCollectionDate ?? l.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var lab in labs)
            {
                relatedItems.Add(new RelatedItem
                {
                    Type = "Lab Result",
                    Id = lab.Id.ToString(),
                    Description = $"{lab.TestType?.Name ?? "Unknown Test"} - {lab.TestResult?.Name ?? "Pending"}",
                    Date = lab.SpecimenCollectionDate ?? lab.CreatedAt
                });
            }

            // Load exposures for this case
            var exposures = await _context.ExposureEvents
                .Where(e => e.ExposedCaseId == caseId)
                .Include(e => e.Location)
                .Include(e => e.Event)
                .OrderByDescending(e => e.ExposureStartDate)
                .Take(5)
                .ToListAsync();

            foreach (var exposure in exposures)
            {
                var desc = exposure.Location != null 
                    ? $"{exposure.Location.Name}" 
                    : exposure.Event != null 
                        ? $"Event: {exposure.Event.Name}"
                        : "Exposure";
                        
                relatedItems.Add(new RelatedItem
                {
                    Type = "Exposure",
                    Id = exposure.Id.ToString(),
                    Description = desc,
                    Date = exposure.ExposureStartDate
                });
            }

            // Load other review items for this case
            var otherReviews = await _context.ReviewQueue
                .Where(r => r.CaseId == caseId && r.ReviewStatus == "Pending")
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .ToListAsync();

            foreach (var review in otherReviews)
            {
                var desc = review.EntityType;
                if (!string.IsNullOrEmpty(review.TriggerField))
                {
                    desc += $" - {review.TriggerField} changed";
                }
                
                relatedItems.Add(new RelatedItem
                {
                    Type = "Pending Review",
                    Id = review.Id.ToString(),
                    Description = desc,
                    Date = review.CreatedDate
                });
            }

            // Sort all items by date (most recent first)
            relatedItems = relatedItems
                .OrderByDescending(r => r.Date)
                .Take(10) // Limit to top 10 most recent
                .ToList();
        }
        catch
        {
            // Silently fail if there are issues loading related items
        }


        return relatedItems;
    }

    // Continue in next part...
}


