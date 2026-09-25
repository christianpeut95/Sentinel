using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace Sentinel.Services;

/// <summary>
/// Implementation of collection mapping service
/// Fully dynamic - no hardcoded field names or entity types
/// </summary>
public class CollectionMappingService : ICollectionMappingService
{
    private const int MaximumDuplicateCandidates = 250;
    private const int MaximumDuplicateMatches = 25;
    private readonly ApplicationDbContext _context;
    private readonly IReportFieldMetadataService _fieldMetadataService;
    private readonly ILogger<CollectionMappingService> _logger;
    private readonly IPatientIdGeneratorService _patientIdGenerator;
    private readonly ICaseIdGeneratorService _caseIdGenerator;

    private sealed record DuplicateCandidateField(
        string ConfiguredFieldPath,
        string PropertyName,
        Type PropertyType,
        object Value);

    // Related entities are created from an administrator-controlled collection-mapping
    // configuration. These fields are deliberately not supplied by the reporting
    // metadata service: that service hides foreign keys from report and mapper UIs.
    // Keep server-managed/security fields out of this narrow write path while allowing
    // real relationship keys such as Case.DiseaseId and ExposureEvent.SourceCaseId.
    private static readonly HashSet<string> RelatedEntitySystemManagedPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "FriendlyId",
        "CreatedDate",
        "CreatedByUserId",
        "LastModified",
        "LastModifiedByUserId",
        "IsDeleted",
        "DeletedAt",
        "DeletedByUserId",
        "RowVersion",
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "AccessFailedCount",
        "LockoutEnd",
        "LockoutEnabled",
        "TwoFactorEnabled"
    };
    
    public CollectionMappingService(
        ApplicationDbContext context,
        IReportFieldMetadataService fieldMetadataService,
        ILogger<CollectionMappingService> logger,
        IPatientIdGeneratorService patientIdGenerator,
        ICaseIdGeneratorService caseIdGenerator)
    {
        _context = context;
        _fieldMetadataService = fieldMetadataService;
        _logger = logger;
        _patientIdGenerator = patientIdGenerator;
        _caseIdGenerator = caseIdGenerator;
    }
    
    public async Task<List<ReportFieldMetadata>> GetEntityFieldsAsync(string entityType)
    {
        _logger.LogCritical("========== GetEntityFieldsAsync CALLED ==========");
        _logger.LogCritical("Entity Type: '{EntityType}'", entityType);
        
        try
        {
            var fields = await _fieldMetadataService.GetFieldsForEntityAsync(entityType);
            
            _logger.LogCritical("? Loaded {Count} fields for {EntityType}", fields?.Count ?? 0, entityType);
            if (fields != null)
            {
                foreach (var field in fields.Take(10))
                {
                    _logger.LogCritical("   - {FieldPath} ({DisplayName})", field.FieldPath, field.DisplayName);
                }
                if (fields.Count > 10)
                {
                    _logger.LogCritical("   ... and {More} more fields", fields.Count - 10);
                }
            }
            _logger.LogCritical("==================================================");
            
            return fields ?? new List<ReportFieldMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? ERROR loading fields for {EntityType}: {Message}", entityType, ex.Message);
            _logger.LogCritical("==================================================");
            return new List<ReportFieldMetadata>();
        }
    }
    
    public async Task<Dictionary<string, List<ReportFieldMetadata>>> GetEntityFieldsByCategoryAsync(string entityType)
    {
        // ? Use existing service - NO HARDCODING
        return await _fieldMetadataService.GetFieldsByCategoryAsync(entityType);
    }
    
    public async Task<List<string>> GetAvailableTargetEntityTypesAsync()
    {
        // ? Dynamically discover from EF Core model
        return await Task.FromResult(
            _context.Model.GetEntityTypes()
                .Where(e => !e.IsOwned() && e.ClrType != null)
                .Select(e => e.ClrType.Name)
                .OrderBy(n => n)
                .ToList()
        );
    }

    public Task<List<ReportFieldMetadata>> GetRelatedEntityTargetFieldsAsync(string entityType)
    {
        var persistenceEntityType = GetPersistenceEntityType(entityType);
        return Task.FromResult(GetRelatedEntityWritableFields(persistenceEntityType));
    }

    // "Contact" is the user-facing name for a contact Case. Contacts are persisted
    // in the Cases table with Type = Contact, rather than in a separate table.
    private static bool IsContactCaseAlias(string entityType) =>
        entityType.Equals("Contact", StringComparison.OrdinalIgnoreCase);

    private static string GetPersistenceEntityType(string entityType) =>
        IsContactCaseAlias(entityType) ? nameof(Case) : entityType;

    private static string NormalizeEntityFieldPath(
        string fieldPath,
        string configuredEntityType,
        string persistenceEntityType)
    {
        foreach (var entityType in new[] { configuredEntityType, persistenceEntityType }
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var prefix = $"{entityType}.";
            if (fieldPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return fieldPath[prefix.Length..];
            }
        }

        return fieldPath;
    }

    /// <summary>
    /// Gets direct, writable EF scalar properties for a related entity. This is kept
    /// separate from report field discovery so required relationship keys can be
    /// mapped without exposing those keys in reporting or configuration pickers.
    /// </summary>
    private List<ReportFieldMetadata> GetRelatedEntityWritableFields(string persistenceEntityType)
    {
        var entityMetadata = _context.Model.GetEntityTypes()
            .FirstOrDefault(entity => !entity.IsOwned() &&
                                      entity.ClrType.Name.Equals(persistenceEntityType, StringComparison.OrdinalIgnoreCase));

        if (entityMetadata == null)
        {
            return new List<ReportFieldMetadata>();
        }

        return entityMetadata.GetProperties()
            .Where(property =>
                !property.IsShadowProperty() &&
                !property.IsPrimaryKey() &&
                !IsSystemManagedRelatedEntityProperty(property.Name))
            .Select(property => new ReportFieldMetadata
            {
                EntityType = persistenceEntityType,
                FieldPath = property.Name,
                DisplayName = property.Name,
                DataType = (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).Name,
                IsNullable = property.IsNullable,
                IsForeignKey = property.IsForeignKey(),
                IsPrimaryKey = false,
                IsNavigationProperty = false,
                IsCollection = false,
                IsCustomField = false,
                IsEnum = (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum
            })
            .ToList();
    }

    private static bool IsSystemManagedRelatedEntityProperty(string propertyName) =>
        RelatedEntitySystemManagedPropertyNames.Contains(propertyName) ||
        propertyName.EndsWith("ByUserId", StringComparison.OrdinalIgnoreCase) ||
        propertyName.EndsWith("ModifiedBy", StringComparison.OrdinalIgnoreCase);

    private static bool EntityTypesReferToSameCaseRow(string requestedEntityType, string createdEntityType) =>
        requestedEntityType.Equals(createdEntityType, StringComparison.OrdinalIgnoreCase) ||
        (IsContactCaseAlias(requestedEntityType) && createdEntityType.Equals(nameof(Case), StringComparison.OrdinalIgnoreCase)) ||
        (requestedEntityType.Equals(nameof(Case), StringComparison.OrdinalIgnoreCase) && IsContactCaseAlias(createdEntityType));
    
    public async Task<ValidationResult> ValidateMappingConfigAsync(CollectionMappingConfig config)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate target entity type exists
        var entityTypes = await GetAvailableTargetEntityTypesAsync();
        if (!entityTypes.Contains(config.TargetEntityType))
        {
            result.IsValid = false;
            result.Errors.Add($"Entity type '{config.TargetEntityType}' does not exist");
            return result;
        }
        
        // Get fields for entity type
        var fields = await GetEntityFieldsAsync(config.TargetEntityType);
        
        // ? Validate each target field path using field metadata service
        foreach (var mapping in config.RowMappings)
        {
            // ? Normalize: Remove entity prefix if it matches target entity
            // e.g., "Patient.GivenName" ? "GivenName" when TargetEntityType = "Patient"
            var normalizedFieldPath = mapping.TargetFieldPath;
            if (normalizedFieldPath.StartsWith($"{config.TargetEntityType}.", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldPath = normalizedFieldPath.Substring(config.TargetEntityType.Length + 1);
            }
            
            var fieldExists = fields.Any(f => f.FieldPath == normalizedFieldPath);
            
            if (!fieldExists)
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Field path '{mapping.TargetFieldPath}' does not exist on entity '{config.TargetEntityType}'"
                );
            }
            
            // Check required fields
            if (mapping.Required && string.IsNullOrEmpty(mapping.DefaultValue))
            {
                result.Warnings.Add(
                    $"Required field '{mapping.TargetFieldPath}' has no default value - will fail if source is empty"
                );
            }
        }
        
        // Validate matching configuration
        if (config.MatchingConfig != null)
        {
            var matchEntityTypes = await GetAvailableTargetEntityTypesAsync();
            if (!matchEntityTypes.Contains(config.MatchingConfig.EntityType))
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Match entity type '{config.MatchingConfig.EntityType}' does not exist"
                );
            }
            else
            {
                var matchFields = await GetEntityFieldsAsync(config.MatchingConfig.EntityType);
                
                foreach (var matchField in config.MatchingConfig.MatchOnFields)
                {
                    // ? Normalize: Remove entity prefix if it matches match entity type
                    var normalizedMatchField = matchField;
                    if (normalizedMatchField.StartsWith($"{config.MatchingConfig.EntityType}.", StringComparison.OrdinalIgnoreCase))
                    {
                        normalizedMatchField = normalizedMatchField.Substring(config.MatchingConfig.EntityType.Length + 1);
                    }
                    
                    var fieldExists = matchFields.Any(f => f.FieldPath == normalizedMatchField);
                    
                    if (!fieldExists)
                    {
                        result.IsValid = false;
                        result.Errors.Add(
                            $"Match field '{matchField}' does not exist on entity '{config.MatchingConfig.EntityType}'"
                        );
                    }
                }
            }
        }
        
        // ? CRITICAL FIX: Validate RelatedEntity mappings (prevent "Id" as target)
        foreach (var relatedEntity in config.RelatedEntities)
        {
            // "Contact" is a logical label in collection mappings. It is persisted
            // as a Case row, so validate its fields against the actual persistence
            // entity just as CreateRelatedEntityAsync does below.
            var persistenceEntityType = GetPersistenceEntityType(relatedEntity.EntityType);
            var relatedFields = GetRelatedEntityWritableFields(persistenceEntityType);
            
            foreach (var mapping in relatedEntity.Mappings)
            {
                // Normalize field path
                var normalizedFieldPath = NormalizeEntityFieldPath(
                    mapping.TargetFieldPath,
                    relatedEntity.EntityType,
                    persistenceEntityType);
                
                // ? CRITICAL: Reject "Id" as target field path (primary keys are auto-generated)
                if (normalizedFieldPath.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add(
                        $"? INVALID: Cannot map to primary key 'Id' on {relatedEntity.EntityType}. " +
                        $"Primary keys are auto-generated by EF Core. " +
                        $"If you need to reference the case, use SourceCaseId or ExposedCaseId instead."
                    );
                    continue;
                }
                
                // Validate field exists
                var fieldExists = relatedFields.Any(f => f.FieldPath == normalizedFieldPath);
                if (!fieldExists)
                {
                    result.IsValid = false;
                    result.Errors.Add(
                        $"Field path '{mapping.TargetFieldPath}' does not exist on entity '{relatedEntity.EntityType}'"
                    );
                }
            }
        }
        
        return result;
    }
    
    public async Task<CollectionMappingResult> ProcessCollectionAsync(
        Guid surveyResponseId,
        string questionName,
        JArray rowData,
        Guid contextCaseId,
        CollectionMappingConfig config)
    {
        var result = new CollectionMappingResult();
        
        _logger.LogInformation(
            "Processing collection mapping for question '{QuestionName}' with {RowCount} rows",
            questionName,
            rowData.Count
        );
        
        // Validate configuration
        var validation = await ValidateMappingConfigAsync(config);
        if (!validation.IsValid)
        {
            result.Success = false;
            result.Errors.AddRange(validation.Errors);
            return result;
        }
        
        // Get field metadata for target entity
        var targetFields = await GetEntityFieldsAsync(config.TargetEntityType);
        
        // Process each row
        foreach (var row in rowData.Cast<JObject>())
        {
            try
            {
                await ProcessSingleRowAsync(
                    row,
                    config,
                    targetFields,
                    contextCaseId,
                    surveyResponseId,
                    questionName,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing collection row");
                result.Errors.Add("A collection row could not be processed. Check the mapping configuration and application logs.");
            }
        }
        
        _logger.LogInformation(
            "Collection processing complete: {Created} entities created, {ReviewItems} items require review, {Errors} errors",
            result.EntitiesCreated.Count,
            result.ItemsRequiringReview,
            result.Errors.Count
        );
        
        return result;
    }
    
    /// <summary>
    /// Process collection with full context support (Phase 3)
    /// Supports multi-entity creation: Patient + Contact + ExposureEvent
    /// </summary>
    public async Task<CollectionMappingResult> ProcessCollectionWithContextAsync(
        Guid surveyResponseId,
        string questionName,
        JArray rowData,
        CollectionMappingConfig config,
        SurveySubmissionContext context)
    {
        var result = new CollectionMappingResult();
        
        _logger.LogInformation(
            "Processing collection with context for question '{QuestionName}' with {RowCount} rows (CaseId: {CaseId})",
            questionName,
            rowData.Count,
            context.CaseId
        );
        
        // Validate configuration
        var validation = await ValidateMappingConfigAsync(config);
        if (!validation.IsValid)
        {
            result.Success = false;
            result.Errors.AddRange(validation.Errors);
            return result;
        }
        
        // Get field metadata for primary entity
        var targetFields = await GetEntityFieldsAsync(config.TargetEntityType);
        
        // Process each row with multi-entity creation
        foreach (var row in rowData.Cast<JObject>())
        {
            try
            {
                await ProcessSingleRowWithContextAsync(
                    row,
                    config,
                    targetFields,
                    context,
                    surveyResponseId,
                    questionName,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing collection row with context");
                result.Errors.Add("A collection row could not be processed. Check the mapping configuration and application logs.");
            }
        }
        
        // ? Save all related entities (Contacts, Exposures, etc.)
        // Primary entities were already saved individually
        try
        {
            var relatedEntityCount = result.EntitiesCreated.Count(e => !e.IsPrimaryEntity);
            
            if (relatedEntityCount > 0)
            {
                _logger.LogInformation(
                    "Saving {Count} related entities (Contacts, Exposures, etc.)",
                    relatedEntityCount
                );
                
                // Log each related entity before saving
                foreach (var entity in result.EntitiesCreated.Where(e => !e.IsPrimaryEntity))
                {
                    _logger.LogInformation(
                        "Related entity to save: {EntityType} with ID {EntityId}, Fields: {Fields}",
                        entity.EntityType,
                        entity.EntityId,
                        string.Join(", ", entity.FieldValues.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                    );
                }
                
                // Check what's actually in the EF change tracker
                var trackedEntities = _context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();
                    
                _logger.LogInformation(
                    "EF Change Tracker has {Count} entities pending save: {Entities}",
                    trackedEntities.Count,
                    string.Join(", ", trackedEntities.Select(e => $"{e.Entity.GetType().Name} ({e.State})"))
                );
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "? Successfully saved {Count} related entities",
                    relatedEntityCount
                );
            }
            else
            {
                _logger.LogWarning(
                    "No related entities to save (expected at least Contact and Exposure if configured)"
                );
            }
        }
        catch (Exception saveEx)
        {
            // Generate helpful error message for FK violations
            var helpfulMessage = GenerateHelpfulErrorMessage(saveEx);
            
            _logger.LogError(saveEx, "Failed to save related entities: {Message}\n\n{HelpfulMessage}", 
                saveEx.Message, helpfulMessage);
            
            result.Errors.Add("Related entities could not be saved. Check the mapping configuration and application logs.");
            if (!string.IsNullOrWhiteSpace(helpfulMessage))
            {
                result.Errors.Add(helpfulMessage);
            }
            result.Success = false;
        }
        
        _logger.LogInformation(
            "Collection processing complete: {Created} primary entities, {RelatedCount} related entities, {ReviewItems} items require review",
            result.EntitiesCreated.Count(e => e.IsPrimaryEntity),
            result.EntitiesCreated.Count(e => !e.IsPrimaryEntity),
            result.ItemsRequiringReview
        );
        
        return result;
    }
    
    /// <summary>
    /// Process a single row with multi-entity creation support
    /// </summary>
    private async Task ProcessSingleRowWithContextAsync(
        JObject row,
        CollectionMappingConfig config,
        List<ReportFieldMetadata> targetFields,
        SurveySubmissionContext context,
        Guid surveyResponseId,
        string questionName,
        CollectionMappingResult result)
    {
        // ???????????????????????????????????????????????????????????
        // STAGE 1: Create Primary Entity (e.g., Patient)
        // ???????????????????????????????????????????????????????????
        
        // Extract data for primary entity
        var entityData = new Dictionary<string, object>();
        
        foreach (var colMapping in config.RowMappings)
        {
            var sourceValue = row[colMapping.SourceColumn]?.ToObject<object>();
            
            // ? Normalize field path before lookup
            var normalizedFieldPath = colMapping.TargetFieldPath;
            if (normalizedFieldPath.StartsWith($"{config.TargetEntityType}.", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldPath = normalizedFieldPath.Substring(config.TargetEntityType.Length + 1);
            }
            
            if (sourceValue != null)
            {
                var fieldMetadata = targetFields.FirstOrDefault(f => f.FieldPath == normalizedFieldPath);
                if (fieldMetadata != null)
                {
                    var convertedValue = ConvertValueToFieldType(sourceValue, fieldMetadata.DataType);
                    entityData[normalizedFieldPath] = convertedValue;
                }
            }
            else if (!string.IsNullOrEmpty(colMapping.DefaultValue))
            {
                entityData[normalizedFieldPath] = colMapping.DefaultValue;
            }
            else if (colMapping.Required)
            {
                result.Errors.Add($"Required field '{colMapping.TargetFieldPath}' is missing");
                return;
            }
        }
        
        // Check for duplicates
        List<EntityMatch> matches = new();
        
        // ? CRITICAL FIX: Skip duplicate detection if this is a reprocessing scenario
        // The user already resolved the duplicate, so don't check again!
        _logger.LogCritical("========================================");
        _logger.LogCritical("??? DUPLICATE DETECTION CHECK:");
        _logger.LogCritical("   - context.AdditionalData is null: {IsNull}", context.AdditionalData == null);
        if (context.AdditionalData != null)
        {
            _logger.LogCritical("   - AdditionalData.Count: {Count}", context.AdditionalData.Count);
            _logger.LogCritical("   - AdditionalData.Keys: {Keys}", string.Join(", ", context.AdditionalData.Keys));
            foreach (var kvp in context.AdditionalData)
            {
                _logger.LogCritical("   - Key: {Key}, Value: {Value} (Type: {Type})", 
                    kvp.Key, kvp.Value, kvp.Value?.GetType().Name);
            }
            _logger.LogCritical("   - Contains ResolvedFromDuplicate: {Contains}", 
                context.AdditionalData.ContainsKey("ResolvedFromDuplicate"));
            if (context.AdditionalData.ContainsKey("ResolvedFromDuplicate"))
            {
                _logger.LogCritical("   - ResolvedFromDuplicate value: {Value}", 
                    context.AdditionalData["ResolvedFromDuplicate"]);
            }
        }
        
        var isReprocessing = context.AdditionalData?.ContainsKey("ResolvedFromDuplicate") == true &&
                           context.AdditionalData["ResolvedFromDuplicate"] as bool? == true;
        
        _logger.LogCritical("   - isReprocessing result: {IsReprocessing}", isReprocessing);
        _logger.LogCritical("========================================");
        
        if (isReprocessing)
        {
            _logger.LogInformation("?? Skipping duplicate detection - this is a reprocessing scenario (user already resolved duplicate)");
        }
        else if (config.MatchingConfig != null)
        {
            _logger.LogInformation("?? Checking for duplicates using matching config...");
            
            matches = await FindDuplicatesAsync(
                config.MatchingConfig.EntityType,
                entityData,
                config.MatchingConfig
            );
            
            _logger.LogInformation("?? Duplicate check complete: Found {Count} matches", matches.Count);
            
            if (matches.Any())
            {
                foreach (var match in matches)
                {
                    _logger.LogInformation("  - Match: EntityId={EntityId}, Score={Score}, Fields={Fields}",
                        match.ExistingEntityId, match.ConfidenceScore, string.Join(", ", match.MatchedFields));
                }
            }
        }
        else
        {
            _logger.LogInformation("?? No matching config - skipping duplicate detection");
        }
        
        // Handle based on duplicate handling strategy
        _logger.LogInformation("========================================");
        _logger.LogInformation("?? DUPLICATE HANDLING DECISION:");
        _logger.LogInformation("   - matches.Count: {Count}", matches.Count);
        _logger.LogInformation("   - matches.Any(): {Any}", matches.Any());
        _logger.LogInformation("   - config.OnDuplicateFound: {Strategy} (enum value: {Value})", 
            config.OnDuplicateFound, (int)config.OnDuplicateFound);
        _logger.LogInformation("   - context.MappingAction: {MappingAction}", context.MappingAction);
        _logger.LogInformation("   - DuplicateHandling.RequireReview: {Value}", (int)DuplicateHandling.RequireReview);
        _logger.LogInformation("   - isReprocessing: {IsReprocessing}", isReprocessing);
        _logger.LogInformation("========================================");
        
        // ?? CRITICAL FIX: Parent SurveyFieldMapping.MappingAction overrides config.OnDuplicateFound!
        // This allows users to set "AutoSave" on the mapping even if duplicate detection is enabled
        var shouldQueueForReview = context.MappingAction == MappingAction.QueueForReview || 
                                   context.MappingAction == MappingAction.RequireApproval;
        
        _logger.LogInformation("   - shouldQueueForReview (from MappingAction): {ShouldQueue}", shouldQueueForReview);
        
        // ?? CRITICAL FIX: Only create ReviewQueue if MappingAction says to queue for review!
        // During reprocessing, we're already past the review stage - proceed to entity creation
        if (!isReprocessing && shouldQueueForReview)
        {
            var reviewReason = matches.Any() 
                ? $"Duplicate detected ({matches.Count} potential matches)" 
                : $"Manual review required (MappingAction={context.MappingAction})";
            
            _logger.LogInformation("?? CREATING REVIEWQUEUE - SKIPPING PATIENT CREATION!");
            _logger.LogInformation("   Reason: {Reason}", reviewReason);
            _logger.LogInformation("   MappingAction={Action} requires review - Patient should NOT be created during initial submission.", 
                context.MappingAction);
            
            // Create review item for primary entity + related entities
            await CreateReviewItemForCollectionRowAsync(
                surveyResponseId,
                questionName,
                entityData,
                matches,  // Can be empty if no duplicates found
                context.CaseId,
                config,
                context.TaskId,
                sourceRow: row
            );
            
            result.ItemsRequiringReview++;
            _logger.LogInformation("?? ReviewQueue created. RETURNING EARLY - Patient will NOT be created.");
            return; // Skip all entity creation - wait for user review
        }
        else if (isReprocessing && shouldQueueForReview)
        {
            _logger.LogInformation("?? REPROCESSING: Bypassing QueueForReview check - user already reviewed");
            _logger.LogInformation("   Proceeding to entity creation/selection...");
        }
        else
        {
            _logger.LogInformation("?? MappingAction={Action} - proceeding to automatic Patient creation", 
                context.MappingAction ?? MappingAction.AutoSave);
        }
        
        if (matches.Any())
        {
            _logger.LogInformation("?? Duplicates found but strategy is {Strategy}, proceeding with creation/linking",
                config.OnDuplicateFound);
        }
        
        CreatedEntityInfo primaryEntity;
        
        // ? CRITICAL FIX: During reprocessing, decide whether to skip or create patient
        // If patient already exists (duplicate scenario): Skip creation
        // If patient doesn't exist (always-review scenario): Create it
        var patientAlreadyExists = context.AdditionalData?.ContainsKey("PatientAlreadyExists") == true &&
                                  context.AdditionalData["PatientAlreadyExists"] as bool? == true;
        
        _logger.LogCritical("==================== PATIENT ATTACH CHECK ====================");
        _logger.LogCritical("?? PRIMARY ENTITY DECISION:");
        _logger.LogCritical("   - isReprocessing: {IsReprocessing}", isReprocessing);
        _logger.LogCritical("   - patientAlreadyExists: {AlreadyExists}", patientAlreadyExists);
        _logger.LogCritical("   - context.PatientId: {PatientId}", context.PatientId);
        _logger.LogCritical("   - context.PatientId != Guid.Empty: {IsNotEmpty}", context.PatientId != Guid.Empty);
        _logger.LogCritical("   - ALL THREE CONDITIONS MET: {Result}", isReprocessing && patientAlreadyExists && context.PatientId != Guid.Empty);
        _logger.LogCritical("   - context.AdditionalData exists: {HasData}", context.AdditionalData != null);
        if (context.AdditionalData != null)
        {
            _logger.LogCritical("   - AdditionalData.Keys: {Keys}", string.Join(", ", context.AdditionalData.Keys));
            if (context.AdditionalData.ContainsKey("ResolvedFromDuplicate"))
            {
                _logger.LogCritical("   - ResolvedFromDuplicate value: {Value} (type: {Type})", 
                    context.AdditionalData["ResolvedFromDuplicate"],
                    context.AdditionalData["ResolvedFromDuplicate"]?.GetType().Name);
            }
            if (context.AdditionalData.ContainsKey("PatientAlreadyExists"))
            {
                _logger.LogCritical("   - PatientAlreadyExists value: {Value} (type: {Type})", 
                    context.AdditionalData["PatientAlreadyExists"],
                    context.AdditionalData["PatientAlreadyExists"]?.GetType().Name);
            }
        }
        _logger.LogCritical("==============================================================");
        
        if (isReprocessing && patientAlreadyExists && context.PatientId != Guid.Empty)
        {
            _logger.LogInformation("? REPROCESSING MODE (DUPLICATE): Skipping primary entity creation, using resolved PatientId {PatientId}", 
                context.PatientId);
            
            // ? CRITICAL FIX: Attach existing patient to EF Core context
            // Without this, Case.PatientId will cause FK violation because Patient is not tracked
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == context.PatientId);
            
            if (existingPatient == null)
            {
                var errorMsg = $"Patient {context.PatientId} not found in database during reprocessing";
                _logger.LogError(errorMsg);
                result.Errors.Add(errorMsg);
                return;
            }
            
            // Patient is now tracked by EF Core (no AsNoTracking means it's automatically attached)
            _logger.LogInformation(
                "? Loaded and tracked existing Patient {PatientId} ({Name}) in EF Core context",
                existingPatient.Id,
                $"{existingPatient.GivenName} {existingPatient.FamilyName}"
            );
            
            // Create a "fake" primary entity reference for the existing patient
            primaryEntity = new CreatedEntityInfo
            {
                EntityType = config.TargetEntityType,
                EntityId = context.PatientId,
                CreatedAt = DateTime.UtcNow,
                IsPrimaryEntity = true,
                FieldValues = entityData // Use the data we would have created
            };
            
            _logger.LogInformation("? Using existing Patient {PatientId} as primary entity, proceeding to related entities", 
                context.PatientId);
        }
        else if (isReprocessing && !patientAlreadyExists)
        {
            _logger.LogInformation("? REPROCESSING MODE (ALWAYS-REVIEW): Patient does NOT exist yet - creating now");
            _logger.LogInformation("   This is an always-review scenario (not duplicate). Patient must be created.");
            
            // Create patient from entityData (normal creation path)
            primaryEntity = await CreateEntityFromDataAsync(
                config.TargetEntityType,
                entityData,
                targetFields,
                context.CaseId
            );
            
            primaryEntity.IsPrimaryEntity = true;
            
            // Save primary entity immediately
            try
            {
                _logger.LogInformation(
                    "Saving primary entity {EntityType} immediately (always-review scenario)",
                    config.TargetEntityType
                );
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("? Patient created successfully with ID {PatientId}", primaryEntity.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Patient during reprocessing");
                result.Errors.Add($"Failed to save {config.TargetEntityType}. Check the mapping configuration and application logs.");
                return;
            }
        }
        else if (!isReprocessing && (!matches.Any() || config.OnDuplicateFound == DuplicateHandling.CreateNew))
        {
            // Create primary entity (ONLY during initial submission, not reprocessing)
            _logger.LogInformation("? INITIAL SUBMISSION: Creating new patient (no duplicates or CreateNew strategy)");
            
            primaryEntity = await CreateEntityFromDataAsync(
                config.TargetEntityType,
                entityData,
                targetFields,
                context.CaseId
            );
            
            primaryEntity.IsPrimaryEntity = true;
            
            // ? CRITICAL FIX: Save primary entity IMMEDIATELY before creating related entities
            // This prevents FK violations when related entities reference the primary entity
            try
            {
                _logger.LogInformation(
                    "Saving primary entity {EntityType} immediately to avoid FK violations",
                    config.TargetEntityType
                );
                
                // Get the entity from the context to ensure we have the correct reference
                var entityEntry = _context.ChangeTracker.Entries()
                    .FirstOrDefault(e => e.Entity.GetType().Name == config.TargetEntityType && 
                                        e.State == EntityState.Added);
                
                if (entityEntry == null)
                {
                    _logger.LogError("Entity {EntityType} not found in change tracker!", config.TargetEntityType);
                    result.Errors.Add($"Failed to track {config.TargetEntityType} in EF context");
                    return;
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("? SaveChangesAsync completed for Patient");
                
                // ?? CRITICAL: Update the EntityId with the ACTUAL ID from database after save
                var idProperty = entityEntry.Entity.GetType().GetProperty("Id");
                var actualId = idProperty?.GetValue(entityEntry.Entity) as Guid?;
                
                _logger.LogInformation("Retrieved actualId from entity: {ActualId}", actualId);
                
                if (actualId.HasValue && actualId.Value != Guid.Empty)
                {
                    var oldId = primaryEntity.EntityId;
                    primaryEntity.EntityId = actualId.Value;
                    
                    _logger.LogInformation(
                        "? Successfully saved primary entity {EntityType} with ACTUAL ID {EntityId} (was: {OldId})",
                        config.TargetEntityType,
                        actualId.Value,
                        oldId
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Could not retrieve actual ID for {EntityType} after save (actualId: {ActualId})",
                        config.TargetEntityType,
                        actualId
                    );
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, 
                    "Failed to save primary entity {EntityType}. Related entities will not be created.",
                    config.TargetEntityType
                );
                result.Errors.Add($"Failed to save {config.TargetEntityType}. Check the mapping configuration and application logs.");
                return; // Don't create related entities if primary save failed
            }
            
            
            result.EntitiesCreated.Add(primaryEntity);
        }
        else if (isReprocessing)
        {
            // ?? REPROCESSING ERROR: Patient should already exist but wasn't handled properly
            var errorMsg = $"REPROCESSING ERROR: Patient should exist but PatientId is invalid or missing. " +
                          $"PatientId={context.PatientId}, PatientAlreadyExists={patientAlreadyExists}";
            _logger.LogError(errorMsg);
            result.Errors.Add(errorMsg);
            return; // Don't create duplicate patient or related entities
        }
        else // SkipAndLink (during initial submission)
        {
            // Use existing entity (duplicate found, link to first match)
            _logger.LogInformation("? Linking to existing {EntityType} (duplicate found)", config.TargetEntityType);
            
            primaryEntity = new CreatedEntityInfo
            {
                EntityType = config.TargetEntityType,
                EntityId = matches.First().ExistingEntityId,
                CreatedAt = DateTime.UtcNow,
                IsPrimaryEntity = true,
                FieldValues = entityData
            };
            
            _logger.LogInformation(
                "Linked to existing {EntityType} {EntityId}",
                config.TargetEntityType,
                primaryEntity.EntityId
            );
        }
        
        // ???????????????????????????????????????????????????????????
        // STAGE 2-N: Create Related Entities
        // ???????????????????????????????????????????????????????????
        
        // Track all created entities for this row so later entities can reference earlier ones
        var createdEntitiesForRow = new List<CreatedEntityInfo> { primaryEntity };
        
        if (config.RelatedEntities != null && config.RelatedEntities.Any())
        {
            foreach (var relatedConfig in config.RelatedEntities.OrderBy(e => e.CreationOrder))
            {
                try
                {
                    // Check condition if specified
                    if (!string.IsNullOrEmpty(relatedConfig.Condition))
                    {
                        if (!EvaluateCondition(relatedConfig.Condition, row))
                        {
                            _logger.LogInformation(
                                "Skipping {EntityType} - condition not met: {Condition}",
                                relatedConfig.EntityType,
                                relatedConfig.Condition
                            );
                            continue;
                        }
                    }
                    
                    var relatedEntity = await CreateRelatedEntityAsync(
                        row,
                        relatedConfig,
                        context,
                        primaryEntity.EntityId,
                        primaryEntity,
                        createdEntitiesForRow  // Pass list of previously created entities
                    );
                    
                    relatedEntity.IsPrimaryEntity = false;
                    result.EntitiesCreated.Add(relatedEntity);
                    createdEntitiesForRow.Add(relatedEntity);  // Add to list for next entities
                    
                    _logger.LogInformation(
                        "Created related entity {EntityType} with ID {EntityId}",
                        relatedConfig.EntityType,
                        relatedEntity.EntityId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating related entity {EntityType}", relatedConfig.EntityType);
                    result.Errors.Add($"Failed to create {relatedConfig.EntityType}. Check the mapping configuration and application logs.");

                    // Later related entities can depend on this entity's ID. Continuing
                    // would create incomplete dependants (for example an ExposureEvent
                    // without the contact Case ID), so fail this row immediately.
                    result.Success = false;
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// Create a related entity (Contact, ExposureEvent, etc.)
    /// </summary>
    private async Task<CreatedEntityInfo> CreateRelatedEntityAsync(
        JObject rowData,
        RelatedEntityConfig config,
        SurveySubmissionContext context,
        Guid primaryEntityId,
        CreatedEntityInfo primaryEntity,
        List<CreatedEntityInfo> previouslyCreatedEntities)
    {
        // Resolve logical Contact mappings to the persisted Case entity while retaining
        // the logical name in CreatedEntityInfo for related-entity references.
        var persistenceEntityType = GetPersistenceEntityType(config.EntityType);
        var fields = GetRelatedEntityWritableFields(persistenceEntityType);
        var entityData = new Dictionary<string, object>();
        
        // Process mappings
        foreach (var mapping in config.Mappings)
        {
            var value = await ResolveFieldValueAsync(
                mapping,
                rowData,
                context,
                primaryEntityId,
                primaryEntity,
                previouslyCreatedEntities  // Pass the list
            );
            
            if (value != null)
            {
                var normalizedFieldPath = NormalizeEntityFieldPath(
                    mapping.TargetFieldPath,
                    config.EntityType,
                    persistenceEntityType);
                entityData[normalizedFieldPath] = value;
            }
            else if (mapping.Required)
            {
                throw new InvalidOperationException(
                    $"Required field '{mapping.TargetFieldPath}' could not be resolved"
                );
            }
        }

        if (IsContactCaseAlias(config.EntityType))
        {
            // A contact Case belongs to the Patient created from the same survey row.
            // Keep this implicit relationship safe even if an older mapping omitted it.
            entityData.TryAdd(nameof(Case.PatientId), primaryEntityId);
            entityData.TryAdd(nameof(Case.Type), CaseType.Contact);
        }

        if (persistenceEntityType == nameof(ExposureEvent) &&
            (!entityData.TryGetValue(nameof(ExposureEvent.ExposedCaseId), out var exposedCaseId) ||
             exposedCaseId is not Guid caseId || caseId == Guid.Empty))
        {
            throw new InvalidOperationException(
                "ExposureEvent.ExposedCaseId must resolve to the newly created contact Case before an exposure can be created.");
        }
        
        // Create the entity
        var relatedEntity = await CreateEntityFromDataAsync(
            persistenceEntityType,
            entityData,
            fields,
            context.CaseId
        );

        // Preserve the configured logical entity name so both
        // {RelatedEntity.Contact.Id} and {RelatedEntity.Case.Id} can refer to it.
        relatedEntity.EntityType = config.EntityType;
        relatedEntity.PrimaryEntityId = primaryEntityId;
        return relatedEntity;
    }
    
    /// <summary>
    /// Resolve field value based on source type
    /// </summary>
    private async Task<object?> ResolveFieldValueAsync(
        RelatedEntityMapping mapping,
        JObject rowData,
        SurveySubmissionContext context,
        Guid primaryEntityId,
        CreatedEntityInfo primaryEntity,
        List<CreatedEntityInfo> previouslyCreatedEntities)
    {
        switch (mapping.SourceType.ToLower())
        {
            case "column":
                return rowData[mapping.Source]?.ToObject<object>();
            
            case "context":
                return mapping.Source switch
                {
                    "{{Context.CaseId}}" => context.CaseId,
                    "{{Context.PatientId}}" => context.PatientId,
                    "{{Context.TaskId}}" => context.TaskId,
                    "{{Context.DiseaseId}}" => context.DiseaseId,
                    "{{Context.JurisdictionId}}" => context.JurisdictionId,
                    "{{Context.SubmittedBy}}" => context.SubmittedBy,
                    "{{Context.SubmittedDate}}" => context.SubmittedDate,
                    var s when s.StartsWith("{{Context.") => 
                        await GetContextAdditionalDataAsync(s, context),
                    _ => throw new InvalidOperationException($"Unknown context variable: {mapping.Source}")
                };
            
            case "primary":
                return mapping.Source switch
                {
                    "{Primary.Id}" => primaryEntityId,
                    var s when s.StartsWith("{Primary.") => 
                        GetPrimaryEntityFieldValue(s, primaryEntity),
                    _ => throw new InvalidOperationException($"Unknown primary reference: {mapping.Source}")
                };
            
            case "relatedentity":
                return ResolveRelatedEntityReference(mapping.Source, previouslyCreatedEntities);
            
            case "constant":
                return mapping.Source;
            
            default:
                throw new InvalidOperationException($"Unknown source type: {mapping.SourceType}");
        }
    }
    
    /// <summary>
    /// Get value from additional context data
    /// </summary>
    private async Task<object?> GetContextAdditionalDataAsync(string contextVar, SurveySubmissionContext context)
    {
        // Extract key from {{Context.KeyName}}
        var key = contextVar.Replace("{{Context.", "").Replace("}}", "");
        
        if (context.AdditionalData.TryGetValue(key, out var value))
        {
            return value;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get field value from created primary entity
    /// </summary>
    private object? GetPrimaryEntityFieldValue(string reference, CreatedEntityInfo primaryEntity)
    {
        // Extract field name from {Primary.FieldName}
        var fieldName = reference.Replace("{Primary.", "").Replace("}", "");
        
        if (primaryEntity.FieldValues.TryGetValue(fieldName, out var value))
        {
            return value;
        }
        
        return null;
    }
    
    /// <summary>
    /// Resolve reference to a previously created related entity
    /// Format: {RelatedEntity.EntityType.FieldName}
    /// Example: {RelatedEntity.Case.Id}
    /// </summary>
    private object? ResolveRelatedEntityReference(string reference, List<CreatedEntityInfo> createdEntities)
    {
        // Parse reference: {RelatedEntity.Case.Id}
        var parts = reference.Replace("{RelatedEntity.", "").Replace("}", "").Split('.');
        
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid RelatedEntity reference format: {Reference}. Expected: {{RelatedEntity.EntityType.FieldName}}", reference);
            return null;
        }
        
        var entityType = parts[0];  // e.g., "Case"
        var fieldName = parts[1];   // e.g., "Id"
        
        // Find the entity in previously created entities
        var entity = createdEntities.FirstOrDefault(e =>
            EntityTypesReferToSameCaseRow(entityType, e.EntityType));
        
        if (entity == null)
        {
            _logger.LogWarning(
                "RelatedEntity reference '{Reference}' - entity type '{EntityType}' not found in created entities. Available: {Available}",
                reference,
                entityType,
                string.Join(", ", createdEntities.Select(e => e.EntityType))
            );
            return null;
        }
        
        // Special case: "Id" always refers to EntityId
        if (fieldName.Equals("Id", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Resolved {Reference} to {Value}",
                reference,
                entity.EntityId
            );
            return entity.EntityId;
        }
        
        // Look up field value
        if (entity.FieldValues.TryGetValue(fieldName, out var value))
        {
            _logger.LogInformation(
                "Resolved {Reference} to {Value}",
                reference,
                value
            );
            return value;
        }
        
        _logger.LogWarning(
            "RelatedEntity reference '{Reference}' - field '{FieldName}' not found. Available fields: {Available}",
            reference,
            fieldName,
            string.Join(", ", entity.FieldValues.Keys)
        );
        return null;
    }
    
    /// <summary>
    /// Evaluate a simple condition
    /// Example: "symptomatic == 'Yes'"
    /// </summary>
    private bool EvaluateCondition(string condition, JObject rowData)
    {
        try
        {
            // Simple equality check (can be expanded later)
            var parts = condition.Split(new[] { "==", "!=" }, StringSplitOptions.None);
            
            if (parts.Length == 2)
            {
                var fieldName = parts[0].Trim();
                var expectedValue = parts[1].Trim().Trim('\'', '"');
                var isNegation = condition.Contains("!=");
                
                var actualValue = rowData[fieldName]?.ToString();
                
                if (isNegation)
                {
                    return actualValue != expectedValue;
                }
                else
                {
                    return actualValue == expectedValue;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate condition: {Condition}", condition);
            return false;
        }
    }
    
    private async Task ProcessSingleRowAsync(
        JObject row,
        CollectionMappingConfig config,
        List<ReportFieldMetadata> targetFields,
        Guid contextCaseId,
        Guid surveyResponseId,
        string questionName,
        CollectionMappingResult result)
    {
        // Extract data using column mappings
        var entityData = new Dictionary<string, object>();
        
        foreach (var colMapping in config.RowMappings)
        {
            var sourceValue = row[colMapping.SourceColumn]?.ToObject<object>();
            
            // ? Normalize field path before lookup
            var normalizedFieldPath = colMapping.TargetFieldPath;
            if (normalizedFieldPath.StartsWith($"{config.TargetEntityType}.", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldPath = normalizedFieldPath.Substring(config.TargetEntityType.Length + 1);
            }
            
            if (sourceValue != null)
            {
                // ? Get field metadata to know the correct data type
                var fieldMetadata = targetFields.FirstOrDefault(
                    f => f.FieldPath == normalizedFieldPath
                );
                
                if (fieldMetadata != null)
                {
                    // Convert to correct type based on metadata
                    var convertedValue = ConvertValueToFieldType(
                        sourceValue,
                        fieldMetadata.DataType
                    );
                    
                    entityData[normalizedFieldPath] = convertedValue;
                }
            }
            else if (!string.IsNullOrEmpty(colMapping.DefaultValue))
            {
                entityData[normalizedFieldPath] = colMapping.DefaultValue;
            }
            else if (colMapping.Required)
            {
                result.Errors.Add(
                    $"Required field '{colMapping.TargetFieldPath}' is missing and has no default value"
                );
                return;
            }
        }
        
        // Check for duplicates if configured
        List<EntityMatch> matches = new();
        if (config.MatchingConfig != null)
        {
            matches = await FindDuplicatesAsync(
                config.MatchingConfig.EntityType,
                entityData,
                config.MatchingConfig
            );
        }
        
        // Handle based on duplicate handling strategy
        if (matches.Any() && config.OnDuplicateFound == DuplicateHandling.RequireReview)
        {
            // Create review item
            await CreateReviewItemForCollectionRowAsync(
                surveyResponseId,
                questionName,
                entityData,
                matches,
                contextCaseId,
                config,
                sourceRow: row
            );
            
            result.ItemsRequiringReview++;
        }
        else if (!matches.Any() || config.OnDuplicateFound == DuplicateHandling.CreateNew)
        {
            // Auto-create entity
            var createdEntity = await CreateEntityFromDataAsync(
                config.TargetEntityType,
                entityData,
                targetFields,
                contextCaseId
            );
            
            result.EntitiesCreated.Add(createdEntity);
            
            // TODO: Create related entities if configured
            // await CreateRelatedEntitiesAsync(createdEntity.EntityId, config, row, contextCaseId);
        }
        else if (matches.Any() && config.OnDuplicateFound == DuplicateHandling.SkipAndLink)
        {
            // Link to existing without creating new
            _logger.LogInformation(
                "Skipping entity creation - linking to existing {EntityType} {EntityId}",
                config.TargetEntityType,
                matches.First().ExistingEntityId
            );
            
            result.Metadata["SkippedDuplicates"] = 
                (result.Metadata.ContainsKey("SkippedDuplicates") 
                    ? (int)result.Metadata["SkippedDuplicates"] + 1 
                    : 1);
        }
    }
    
    /// <summary>
    /// Dynamically create entity using field metadata
    /// NO HARDCODED FIELD NAMES
    /// </summary>
    private async Task<CreatedEntityInfo> CreateEntityFromDataAsync(
        string entityType,
        Dictionary<string, object> data,
        List<ReportFieldMetadata> fields,
        Guid contextCaseId)
    {
        // Get the CLR type
        var clrType = _context.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == entityType)
            ?.ClrType;
        
        if (clrType == null)
            throw new ArgumentException($"Entity type '{entityType}' not found");
        
        // Create instance
        var entity = Activator.CreateInstance(clrType);
        if (entity == null)
            throw new InvalidOperationException($"Could not create instance of {entityType}");
        
        // Track what we set for audit
        var fieldValues = new Dictionary<string, object>();
        
        // Set field values using reflection + field metadata
        foreach (var kvp in data)
        {
            var fieldPath = kvp.Key;
            var value = kvp.Value;
            
            // ? Use field metadata to determine how to set the value
            var fieldMetadata = fields.FirstOrDefault(f => f.FieldPath == fieldPath);
            if (fieldMetadata == null)
                continue;
            
            if (fieldMetadata.IsCustomField)
            {
                // TODO: Handle custom field (EAV pattern)
                _logger.LogWarning("Custom field handling not yet implemented: {FieldPath}", fieldPath);
            }
            else if (fieldMetadata.IsNavigationProperty)
            {
                // TODO: Handle navigation property
                _logger.LogWarning("Navigation property handling not yet implemented: {FieldPath}", fieldPath);
            }
            else
            {
                // Handle regular property
                SetPropertyValue(entity, fieldMetadata.FieldPath, value);
                fieldValues[fieldPath] = value;
            }
        }
        
        // Log what data we're about to save
        _logger.LogInformation(
            "About to create {EntityType}. Data contains {Count} fields: {Fields}",
            entityType,
            data.Count,
            string.Join(", ", data.Select(kvp => $"{kvp.Key}={kvp.Value}"))
        );
        
        // Special logging for ExposureEvent to debug FK issue
        if (entityType == "ExposureEvent")
        {
            var caseIdProp = clrType.GetProperty("CaseId");
            var actualCaseId = caseIdProp?.GetValue(entity);
            _logger.LogInformation(
                "ExposureEvent CaseId before save: {CaseId} (Context CaseId was: {ContextCaseId})",
                actualCaseId,
                contextCaseId
            );
        }
        
        // =====================================================
        // CRITICAL: Generate Friendly IDs BEFORE adding to context
        // Uses dedicated ID generator services with duplicate prevention
        // =====================================================
        if (entityType == "Patient" && entity is Patient patient)
        {
            if (string.IsNullOrEmpty(patient.FriendlyId))
            {
                patient.FriendlyId = await _patientIdGenerator.GenerateNextPatientIdAsync();
                fieldValues["FriendlyId"] = patient.FriendlyId;

                _logger.LogInformation(
                    "Generated FriendlyId {FriendlyId} for new Patient (prevents duplicates)",
                    patient.FriendlyId
                );
            }
        }
        else if (entityType == "Case" && entity is Case caseEntity)
        {
            if (string.IsNullOrEmpty(caseEntity.FriendlyId))
            {
                caseEntity.FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync();
                fieldValues["FriendlyId"] = caseEntity.FriendlyId;

                _logger.LogInformation(
                    "Generated FriendlyId {FriendlyId} for new Case (prevents duplicates)",
                    caseEntity.FriendlyId
                );
            }

            // CRITICAL: Set CaseType to Case if not explicitly set
            // Without this, Type defaults to 0 which is not a valid CaseType enum value
            // (CaseType.Case = 1, CaseType.Contact = 2)
            if (caseEntity.Type == 0 || (int)caseEntity.Type == 0)
            {
                caseEntity.Type = CaseType.Case;
                fieldValues["Type"] = CaseType.Case;

                _logger.LogInformation(
                    "Set CaseType to 'Case' for new Case entity (was not explicitly set)"
                );
            }
        }
        
        // Add to context (but don't save yet - let caller save all entities in one transaction)
        _context.Add(entity);
        
        // For entities being created, EF Core will generate a temporary ID
        // The actual ID will be assigned when SaveChangesAsync is called
        var idProperty = clrType.GetProperty("Id");
        var entityId = idProperty?.GetValue(entity) as Guid?;
        
        // If no ID yet, generate a temporary one that EF Core will use
        if (entityId == null || entityId == Guid.Empty)
        {
            entityId = Guid.NewGuid();
            idProperty?.SetValue(entity, entityId);
        }
        
        _logger.LogInformation(
            "Added {EntityType} to context with ID {EntityId} (will be saved in transaction)",
            entityType,
            entityId
        );
        
        return new CreatedEntityInfo
        {
            EntityType = entityType,
            EntityId = entityId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            FieldValues = fieldValues
        };
    }
    
    private void SetPropertyValue(object entity, string fieldPath, object value)
    {
        // Extract property name from field path (e.g., "Patient.GivenName" ? "GivenName")
        var parts = fieldPath.Split('.');
        var propertyName = parts.Length > 1 ? parts[1] : parts[0];
        
        var property = entity.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            try
            {
                // Handle null values
                if (value == null)
                {
                    property.SetValue(entity, null);
                    return;
                }
                
                // Get the target type (unwrap Nullable<T> if needed)
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                
                // Convert value to target type
                object convertedValue;
                
                if (targetType == typeof(Guid) && value is string guidString)
                {
                    // Handle Guid strings
                    convertedValue = Guid.Parse(guidString);
                }
                else if (targetType == typeof(Guid) && value is Guid guidValue)
                {
                    // Handle Guid directly
                    convertedValue = guidValue;
                }
                else if (targetType.IsEnum && value is string enumString)
                {
                    // Handle enums
                    convertedValue = Enum.Parse(targetType, enumString);
                }
                else
                {
                    // Handle other types
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                
                property.SetValue(entity, convertedValue);
                
                _logger.LogDebug(
                    "Successfully set property {PropertyName} to value {Value} (type: {Type})",
                    propertyName,
                    convertedValue,
                    targetType.Name
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to set property {PropertyName} to value {Value} (propertyType: {PropertyType}, valueType: {ValueType})",
                    propertyName,
                    value,
                    property.PropertyType.Name,
                    value?.GetType().Name ?? "null"
                );
            }
        }
    }
    
    private object ConvertValueToFieldType(object value, string dataType)
    {
        try
        {
            return dataType.ToLower() switch
            {
                "string" => value.ToString() ?? string.Empty,
                "int32" or "int" => Convert.ToInt32(value),
                "int64" or "long" => Convert.ToInt64(value),
                "decimal" => Convert.ToDecimal(value),
                "double" => Convert.ToDouble(value),
                "boolean" or "bool" => Convert.ToBoolean(value),
                "datetime" or "date" => Convert.ToDateTime(value),
                "guid" => Guid.Parse(value.ToString() ?? string.Empty),
                _ => value
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to convert value {Value} to type {DataType}",
                value,
                dataType
            );
            return value;
        }
    }
    
    public async Task<List<EntityMatch>> FindDuplicatesAsync(
        string entityType,
        Dictionary<string, object> searchData,
        MatchingConfig matchingConfig)
    {
        var persistenceEntityType = GetPersistenceEntityType(entityType);
        var entityMetadata = _context.Model.GetEntityTypes()
            .FirstOrDefault(e => !e.IsOwned() &&
                                 e.ClrType.Name.Equals(persistenceEntityType, StringComparison.OrdinalIgnoreCase));

        if (entityMetadata == null)
        {
            _logger.LogWarning("Skipping collection duplicate check for unknown entity type {EntityType}", entityType);
            return new List<EntityMatch>();
        }

        // A mapping configuration is administrator-supplied data, not executable
        // query text. Resolve it strictly to direct scalar EF properties before
        // using it to construct an expression tree. This prevents navigation
        // traversal and dynamic-query injection, while preserving EF Core query
        // parameterisation for every submitted value.
        var candidateFields = BuildDuplicateCandidateFields(entityMetadata, entityType, searchData, matchingConfig);
        if (candidateFields.Count == 0)
        {
            _logger.LogWarning(
                "Skipping collection duplicate check for {EntityType}: no configured direct scalar field had a usable value",
                entityType);
            return new List<EntityMatch>();
        }

        var candidates = await GetDuplicateCandidatesAsync(entityMetadata.ClrType, candidateFields);
        var matches = new List<EntityMatch>();

        foreach (var entity in candidates)
        {
            var score = CalculateMatchScore(entity, searchData, matchingConfig);
            
            if (score >= matchingConfig.ConfidenceThreshold)
            {
                var idProperty = entityMetadata.ClrType.GetProperty("Id");
                var entityId = idProperty?.GetValue(entity) is Guid id ? id : Guid.Empty;
                
                matches.Add(new EntityMatch
                {
                    ExistingEntityId = entityId,
                    EntityType = entityType,
                    ConfidenceScore = score,
                    MatchedFields = ExtractMatchedFields(entity, matchingConfig.MatchOnFields, entityType)
                });
            }
        }
        
        if (matches.Count > MaximumDuplicateMatches)
        {
            _logger.LogWarning(
                "Collection duplicate check for {EntityType} produced more than {MatchLimit} matches. Returning the highest-confidence matches only.",
                entityType,
                MaximumDuplicateMatches);
        }

        return matches
            .OrderByDescending(m => m.ConfidenceScore)
            .ThenBy(m => m.ExistingEntityId)
            .Take(MaximumDuplicateMatches)
            .ToList();
    }

    private List<DuplicateCandidateField> BuildDuplicateCandidateFields(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityMetadata,
        string entityType,
        IReadOnlyDictionary<string, object> searchData,
        MatchingConfig matchingConfig)
    {
        var fields = new List<DuplicateCandidateField>();

        foreach (var configuredFieldPath in matchingConfig.MatchOnFields.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!TryGetDirectScalarProperty(entityMetadata, entityType, configuredFieldPath, out var propertyName, out var propertyType))
            {
                _logger.LogWarning(
                    "Ignoring unsafe or unsupported collection duplicate field {FieldPath} for {EntityType}. Only direct mapped scalar properties are supported.",
                    configuredFieldPath,
                    entityType);
                continue;
            }

            if (!TryGetSearchValue(searchData, entityType, configuredFieldPath, out var searchValue) ||
                !TryConvertCandidateValue(searchValue, propertyType, out var typedValue))
            {
                continue;
            }

            fields.Add(new DuplicateCandidateField(configuredFieldPath, propertyName, propertyType, typedValue));
        }

        return fields;
    }

    private async Task<List<object>> GetDuplicateCandidatesAsync(
        Type clrType,
        IReadOnlyList<DuplicateCandidateField> candidateFields)
    {
        var genericMethod = typeof(CollectionMappingService)
            .GetMethod(nameof(GetDuplicateCandidatesForEntityAsync), BindingFlags.Instance | BindingFlags.NonPublic)
            ?.MakeGenericMethod(clrType)
            ?? throw new InvalidOperationException("Unable to build the collection duplicate candidate query.");

        var task = (Task<List<object>>)genericMethod.Invoke(this, new object[] { candidateFields })!;
        return await task;
    }

    private async Task<List<object>> GetDuplicateCandidatesForEntityAsync<TEntity>(
        IReadOnlyList<DuplicateCandidateField> candidateFields)
        where TEntity : class
    {
        var predicate = BuildDuplicateCandidatePredicate<TEntity>(candidateFields);
        IQueryable<TEntity> query = _context.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate);

        var idProperty = typeof(TEntity).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        if (idProperty != null && IsSupportedDuplicatePropertyType(idProperty.PropertyType))
        {
            query = OrderByProperty(query, idProperty);
        }

        var candidates = await query
            .Take(MaximumDuplicateCandidates + 1)
            .ToListAsync();

        if (candidates.Count > MaximumDuplicateCandidates)
        {
            _logger.LogWarning(
                "Collection duplicate candidate search reached the {CandidateLimit} record limit for entity {EntityType}. Only bounded candidates were scored.",
                MaximumDuplicateCandidates,
                typeof(TEntity).Name);
            candidates.RemoveAt(candidates.Count - 1);
        }

        return candidates.Cast<object>().ToList();
    }

    private static IQueryable<TEntity> OrderByProperty<TEntity>(IQueryable<TEntity> query, PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var propertyAccess = Expression.Property(parameter, property);
        var keySelector = Expression.Lambda(propertyAccess, parameter);
        var orderBy = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Queryable.OrderBy) && method.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), property.PropertyType);

        return (IQueryable<TEntity>)orderBy.Invoke(null, new object[] { query, keySelector })!;
    }

    private static Expression<Func<TEntity, bool>> BuildDuplicateCandidatePredicate<TEntity>(
        IReadOnlyList<DuplicateCandidateField> candidateFields)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        Expression? combined = null;

        foreach (var field in candidateFields)
        {
            var property = Expression.Property(parameter, field.PropertyName);
            var fieldPredicate = BuildCandidateFieldPredicate(property, field);
            combined = combined == null ? fieldPredicate : Expression.OrElse(combined, fieldPredicate);
        }

        return Expression.Lambda<Func<TEntity, bool>>(combined ?? Expression.Constant(false), parameter);
    }

    private static Expression BuildCandidateFieldPredicate(Expression property, DuplicateCandidateField field)
    {
        var underlyingType = Nullable.GetUnderlyingType(field.PropertyType) ?? field.PropertyType;

        if (underlyingType == typeof(string))
        {
            var value = (string)field.Value;
            var prefix = value[..Math.Min(value.Length, 3)];
            var isNotNull = Expression.NotEqual(property, Expression.Constant(null, property.Type));
            var startsWith = Expression.Call(
                property,
                nameof(string.StartsWith),
                Type.EmptyTypes,
                Expression.Constant(prefix));
            return Expression.AndAlso(isNotNull, startsWith);
        }

        Expression valueExpression = Expression.Constant(field.Value, underlyingType);
        if (Nullable.GetUnderlyingType(field.PropertyType) != null)
        {
            valueExpression = Expression.Convert(valueExpression, field.PropertyType);
        }

        return Expression.Equal(property, valueExpression);
    }

    private static bool TryGetDirectScalarProperty(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityMetadata,
        string entityType,
        string configuredFieldPath,
        out string propertyName,
        out Type propertyType)
    {
        propertyName = string.Empty;
        propertyType = typeof(object);

        if (!TryGetDirectPropertyName(entityType, configuredFieldPath, out var candidatePropertyName))
        {
            return false;
        }

        var property = entityMetadata.FindProperty(candidatePropertyName);
        if (property?.PropertyInfo == null || property.IsPrimaryKey() || !IsSupportedDuplicatePropertyType(property.ClrType))
        {
            return false;
        }

        propertyName = property.Name;
        propertyType = property.ClrType;
        return true;
    }

    private static bool TryGetDirectPropertyName(string entityType, string configuredFieldPath, out string propertyName)
    {
        propertyName = string.Empty;
        if (string.IsNullOrWhiteSpace(configuredFieldPath))
        {
            return false;
        }

        var parts = configuredFieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            propertyName = parts[0];
            return true;
        }

        if (parts.Length == 2 && parts[0].Equals(entityType, StringComparison.OrdinalIgnoreCase))
        {
            propertyName = parts[1];
            return true;
        }

        return false;
    }

    private static bool IsSupportedDuplicatePropertyType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(string) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(DateOnly) ||
               underlyingType == typeof(TimeOnly) ||
               underlyingType == typeof(decimal) ||
               underlyingType.IsEnum ||
               underlyingType.IsPrimitive;
    }

    private static bool TryGetSearchValue(
        IReadOnlyDictionary<string, object> searchData,
        string entityType,
        string configuredFieldPath,
        out object searchValue)
    {
        if (searchData.TryGetValue(configuredFieldPath, out searchValue!))
        {
            return true;
        }

        if (TryGetDirectPropertyName(entityType, configuredFieldPath, out var propertyName) &&
            searchData.TryGetValue(propertyName, out searchValue!))
        {
            return true;
        }

        searchValue = null!;
        return false;
    }

    private static bool TryConvertCandidateValue(object value, Type propertyType, out object typedValue)
    {
        typedValue = null!;
        value = value is JValue jValue ? jValue.Value! : value;
        if (value == null)
        {
            return false;
        }

        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (targetType == typeof(string))
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            typedValue = text;
            return true;
        }

        try
        {
            if (targetType == typeof(Guid))
            {
                if (!Guid.TryParse(text, out var parsedGuid)) return false;
                typedValue = parsedGuid;
            }
            else if (targetType == typeof(DateTime))
            {
                if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate)) return false;
                typedValue = parsedDate;
            }
            else if (targetType == typeof(DateTimeOffset))
            {
                if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDateTimeOffset)) return false;
                typedValue = parsedDateTimeOffset;
            }
            else if (targetType == typeof(DateOnly))
            {
                if (!DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDateOnly)) return false;
                typedValue = parsedDateOnly;
            }
            else if (targetType == typeof(TimeOnly))
            {
                if (!TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedTimeOnly)) return false;
                typedValue = parsedTimeOnly;
            }
            else if (targetType.IsEnum)
            {
                if (!Enum.TryParse(targetType, text, true, out var parsedEnum)) return false;
                typedValue = parsedEnum;
            }
            else
            {
                typedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
    
    private double CalculateMatchScore(
        object entity,
        Dictionary<string, object> searchData,
        MatchingConfig config)
    {
        if (config.MatchOnFields.Count == 0)
            return 0.0;
        
        int matchCount = 0;
        int totalFields = 0;
        
        foreach (var fieldPath in config.MatchOnFields)
        {
            if (!TryGetDirectPropertyName(config.EntityType, fieldPath, out var propertyName) ||
                !TryGetSearchValue(searchData, config.EntityType, fieldPath, out var searchValue))
            {
                _logger.LogDebug(
                    "Collection duplicate field '{FieldPath}' is not a direct configured value. Available keys: {Keys}",
                    fieldPath,
                    string.Join(", ", searchData.Keys)
                );
                continue;
            }

            var property = entity.GetType().GetProperty(propertyName);
            if (property == null || !IsSupportedDuplicatePropertyType(property.PropertyType))
                continue;

            totalFields++;
            var entityValue = property.GetValue(entity);
            
            if (ValuesMatch(entityValue, searchValue, config.Strategy))
            {
                matchCount++;
                _logger.LogDebug(
                    "? Match found on field '{FieldPath}': '{EntityValue}' == '{SearchValue}'",
                    fieldPath,
                    entityValue,
                    searchValue
                );
            }
            else
            {
                _logger.LogDebug(
                    "? No match on field '{FieldPath}': '{EntityValue}' != '{SearchValue}'",
                    fieldPath,
                    entityValue,
                    searchValue
                );
            }
        }
        
        if (totalFields == 0)
        {
            return 0.0;
        }

        var score = (double)matchCount / totalFields;
        
        _logger.LogInformation(
            "Duplicate detection score: {MatchCount}/{TotalFields} = {Score:F2} (threshold: {Threshold:F2})",
            matchCount,
            totalFields,
            score,
            config.ConfidenceThreshold
        );
        
        return score;
    }
    
    private bool ValuesMatch(object? entityValue, object searchValue, MatchingStrategy strategy)
    {
        if (entityValue == null || searchValue == null)
        {
            return false;
        }

        // SurveyJS serialises date-only answers as ISO strings (for example,
        // "1990-01-01"), while EF materialises Patient.DateOfBirth as a
        // DateTime. Comparing their ToString() representations makes an exact
        // date match fail merely because the display formats differ.
        if (TryGetCalendarDate(entityValue, out var entityDate) &&
            TryGetCalendarDate(searchValue, out var submittedDate))
        {
            return entityDate == submittedDate;
        }

        // Exact text matching should be insensitive to harmless input
        // formatting differences such as leading/trailing or repeated spaces.
        // It does not do fuzzy matching or substring matching.
        if (entityValue is string entityText && searchValue is string submittedText)
        {
            return string.Equals(
                NormalizeMatchText(entityText),
                NormalizeMatchText(submittedText),
                StringComparison.OrdinalIgnoreCase);
        }

        // TODO: Implement a separately-configured fuzzy strategy. Until then,
        // non-text scalar fields retain strict equality semantics.
        return entityValue.Equals(searchValue) ||
               string.Equals(
                   Convert.ToString(entityValue, CultureInfo.InvariantCulture),
                   Convert.ToString(searchValue, CultureInfo.InvariantCulture),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetCalendarDate(object value, out DateOnly date)
    {
        switch (value)
        {
            case DateOnly dateOnly:
                date = dateOnly;
                return true;
            case DateTime dateTime:
                date = DateOnly.FromDateTime(dateTime);
                return true;
            case DateTimeOffset dateTimeOffset:
                date = DateOnly.FromDateTime(dateTimeOffset.DateTime);
                return true;
            case JValue jValue:
                return TryGetCalendarDate(jValue.Value!, out date);
            default:
                return DateOnly.TryParse(
                    Convert.ToString(value, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out date);
        }
    }

    private static string NormalizeMatchText(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    
    private Dictionary<string, object> ExtractMatchedFields(object entity, List<string> fieldPaths, string entityType)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var fieldPath in fieldPaths)
        {
            if (!TryGetDirectPropertyName(entityType, fieldPath, out var propertyName))
            {
                continue;
            }

            var property = entity.GetType().GetProperty(propertyName);
            if (property != null && IsSupportedDuplicatePropertyType(property.PropertyType))
            {
                var value = property.GetValue(entity);
                if (value != null)
                {
                    result[fieldPath] = value;
                }
            }
        }
        
        return result;
    }
    
    private async Task CreateReviewItemForCollectionRowAsync(
        Guid surveyResponseId,
        string questionName,
        Dictionary<string, object> entityData,
        List<EntityMatch> matches,
        Guid contextCaseId,
        CollectionMappingConfig config,
        Guid? taskId = null,
        JObject? sourceRow = null)
    {
        // ? FIX: Determine EntityType and ChangeType based on whether duplicates found
        var entityType = matches.Any() 
            ? ReviewEntityTypes.DuplicatePatient 
            : ReviewEntityTypes.NewPatient;  // Or could be based on config.TargetEntityType
        
        var changeType = matches.Any() 
            ? ReviewChangeTypes.PotentialDuplicate 
            : ReviewChangeTypes.PendingCreation;  // Always-review (no duplicate)
        
        var reviewItem = new ReviewQueue
        {
            EntityType = entityType,
            EntityId = 0, // No entity exists yet
            CaseId = contextCaseId,
            TaskId = taskId, // ? CRITICAL FIX: Link to the Task for reprocessing
            ChangeType = changeType,
            Priority = matches.Any(m => m.ConfidenceScore > 0.95) 
                ? ReviewPriorities.High 
                : ReviewPriorities.Medium,
            ReviewStatus = ReviewStatuses.Pending,
            CreatedDate = DateTime.UtcNow,
            PotentialMatchesJson = JsonConvert.SerializeObject(matches),
            ProposedEntityDataJson = JsonConvert.SerializeObject(entityData),
            // Store the specific row alongside metadata so reprocessing only re-runs this exact row
            CollectionSourceDataJson = JsonConvert.SerializeObject(new
            {
                SurveyResponseId = surveyResponseId != Guid.Empty ? surveyResponseId : (Guid?)null,
                QuestionName = questionName,
                TargetEntityType = config.TargetEntityType,
                RowData = sourceRow?.ToString(Newtonsoft.Json.Formatting.None)
            })
        };
        
        _context.ReviewQueue.Add(reviewItem);
        // Note: Review item will be saved when caller saves the entire transaction
        
        _logger.LogInformation(
            "Added review item to context for potential duplicate {EntityType} with TaskId {TaskId} (will be saved in transaction)",
            config.TargetEntityType,
            taskId
        );
    }
    
    public async Task<CreatedEntityInfo> CreateEntitiesFromReviewAsync(
        int reviewItemId,
        Guid? selectedEntityId = null)
    {
        var reviewItem = await _context.ReviewQueue.FindAsync(reviewItemId);
        if (reviewItem == null)
            throw new ArgumentException($"Review item {reviewItemId} not found");
        
        if (selectedEntityId.HasValue)
        {
            // User chose to link to existing entity
            _logger.LogInformation(
                "Linking to existing entity {EntityId} instead of creating new",
                selectedEntityId.Value
            );
            
            // TODO: Create link/relationship
            return new CreatedEntityInfo
            {
                EntityId = selectedEntityId.Value,
                EntityType = reviewItem.EntityType,
                CreatedAt = DateTime.UtcNow
            };
        }
        
        // Create new entity from proposed data
        var proposedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            reviewItem.ProposedEntityDataJson ?? "{}"
        ) ?? new Dictionary<string, object>();
        
        var sourceData = JsonConvert.DeserializeObject<dynamic>(
            reviewItem.CollectionSourceDataJson ?? "{}"
        );
        
        string targetEntityType = sourceData?.TargetEntityType ?? reviewItem.EntityType;
        
        var fields = await GetEntityFieldsAsync(targetEntityType);
        
        return await CreateEntityFromDataAsync(
            targetEntityType,
            proposedData,
            fields,
            reviewItem.CaseId ?? Guid.Empty
        );
    }
    
    /// <summary>
    /// Generate helpful error messages for common FK violations
    /// </summary>
    private string GenerateHelpfulErrorMessage(Exception ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        var sb = new StringBuilder();

        if (message.Contains("FK_Cases_Patients_PatientId"))
        {
            sb.AppendLine("\n========================================");
            sb.AppendLine("? FOREIGN KEY VIOLATION: Case.PatientId");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("**Problem**: The Contact (Case) is missing a PatientId.");
            sb.AppendLine();
            sb.AppendLine("**Fix**: Add this mapping to the Case entity in your collection config:");
            sb.AppendLine("   � SourceType: 'Primary'");
            sb.AppendLine("   � Source: '{Primary.Id}'");
            sb.AppendLine("   � Target: 'Case.PatientId'");
            sb.AppendLine("   � Required: ?");
            sb.AppendLine();
            sb.AppendLine("**Quick SQL Fix**:");
            sb.AppendLine("See: fix_household_contacts_mapping.sql");
        }
        else if (message.Contains("FK_ExposureEvents_Cases_SourceCaseId") || message.Contains("FK_ExposureEvents_Cases_RelatedCaseId"))
        {
            sb.AppendLine("\n========================================");
            sb.AppendLine("? FOREIGN KEY VIOLATION: ExposureEvent.SourceCaseId");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("**Problem**: The Exposure is missing a SourceCaseId (source/transmitter case).");
            sb.AppendLine();
            sb.AppendLine("**Fix**: Add this mapping to the ExposureEvent entity:");
            sb.AppendLine("   � SourceType: 'Context'");
            sb.AppendLine("   � Source: '{{Context.CaseId}}'");
            sb.AppendLine("   � Target: 'ExposureEvent.SourceCaseId'");
            sb.AppendLine("   � Required: ? (yes for Contact exposures)");
        }
        else if (message.Contains("FK_Cases_Diseases_DiseaseId"))
        {
            sb.AppendLine("\n========================================");
            sb.AppendLine("? FOREIGN KEY VIOLATION: Case.DiseaseId");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("**Problem**: The Case is missing a DiseaseId.");
            sb.AppendLine();
            sb.AppendLine("**Fix**: Add this mapping to the Case entity:");
            sb.AppendLine("   � SourceType: 'Context'");
            sb.AppendLine("   � Source: '{{Context.DiseaseId}}'");
            sb.AppendLine("   � Target: 'Case.DiseaseId'");
            sb.AppendLine("   � Required: ?");
        }
        else if (message.Contains("FOREIGN KEY"))
        {
            sb.AppendLine("\n========================================");
            sb.AppendLine("? FOREIGN KEY VIOLATION");
            sb.AppendLine("========================================");
            sb.AppendLine();
            sb.AppendLine("**Problem**: An entity is missing a required FK field.");
            sb.AppendLine();
            sb.AppendLine("**Common Missing Fields**:");
            sb.AppendLine("   � Case.PatientId ? Use '{Primary.Id}' from Primary source");
            sb.AppendLine("   � Case.DiseaseId ? Use '{{Context.DiseaseId}}' from Context");
            sb.AppendLine("   � ExposureEvent.SourceCaseId ? Use '{{Context.CaseId}}' from Context");
            sb.AppendLine("   � ExposureEvent.ExposedCaseId ? Use '{RelatedEntity.Case.Id}'");
        }

        return sb.ToString();
    }
}

