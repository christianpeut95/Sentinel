using Sentinel.Models;
using Sentinel.Models.Reporting;
using Newtonsoft.Json.Linq;

namespace Sentinel.Services;

/// <summary>
/// Service for processing survey collection mappings (matrix/matrixdynamic questions)
/// Fully dynamic - uses ReportFieldMetadataService for zero-hardcoding
/// </summary>
public interface ICollectionMappingService
{
    /// <summary>
    /// Process a survey collection question and create entities
    /// Uses ReportFieldMetadataService to discover fields dynamically
    /// </summary>
    /// <param name="surveyResponseId">ID of the survey response</param>
    /// <param name="questionName">Name of the matrix/dynamic question</param>
    /// <param name="rowData">Array of row data from the survey</param>
    /// <param name="contextCaseId">Case ID for linking created entities</param>
    /// <param name="config">Collection mapping configuration</param>
    /// <returns>Result containing created entities and review items</returns>
    Task<CollectionMappingResult> ProcessCollectionAsync(
        Guid surveyResponseId,
        string questionName,
        JArray rowData,
        Guid contextCaseId,
        CollectionMappingConfig config
    );
    
    /// <summary>
    /// Process a survey collection question with full context support
    /// Supports multi-entity creation (Patient + Contact + Exposure)
    /// NEW in Phase 3 - includes survey submission context
    /// </summary>
    /// <param name="surveyResponseId">ID of the survey response</param>
    /// <param name="questionName">Name of the matrix/dynamic question</param>
    /// <param name="rowData">Array of row data from the survey</param>
    /// <param name="config">Collection mapping configuration</param>
    /// <param name="context">Survey submission context (case, patient, task info)</param>
    /// <returns>Result containing created entities and review items</returns>
    Task<CollectionMappingResult> ProcessCollectionWithContextAsync(
        Guid surveyResponseId,
        string questionName,
        JArray rowData,
        CollectionMappingConfig config,
        SurveySubmissionContext context
    );
    
    /// <summary>
    /// Find potential duplicate entities using field metadata service
    /// Supports exact and fuzzy matching
    /// </summary>
    /// <param name="entityType">Type of entity to search (e.g., "Patient", "Contact")</param>
    /// <param name="searchData">Field values to match against</param>
    /// <param name="matchingConfig">Matching rules and strategy</param>
    /// <returns>List of potential matches with confidence scores</returns>
    Task<List<EntityMatch>> FindDuplicatesAsync(
        string entityType,
        Dictionary<string, object> searchData,
        MatchingConfig matchingConfig
    );
    
    /// <summary>
    /// Get available entity types that can be targets for collection mapping
    /// Dynamically discovered from DbContext
    /// </summary>
    /// <returns>List of entity type names</returns>
    Task<List<string>> GetAvailableTargetEntityTypesAsync();
    
    /// <summary>
    /// Get available fields for an entity type
    /// Uses ReportFieldMetadataService - no hardcoding
    /// </summary>
    /// <param name="entityType">Entity type name</param>
    /// <returns>List of field metadata</returns>
    Task<List<ReportFieldMetadata>> GetEntityFieldsAsync(string entityType);
    
    /// <summary>
    /// Validate a collection mapping configuration
    /// Ensures all target field paths exist and are valid
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>Validation result with errors/warnings</returns>
    Task<ValidationResult> ValidateMappingConfigAsync(CollectionMappingConfig config);
    
    /// <summary>
    /// Create entities from approved review item
    /// Called by DataReviewService when user approves entity creation
    /// </summary>
    /// <param name="reviewItemId">Review queue item ID</param>
    /// <param name="selectedEntityId">ID of existing entity to link (if user chose existing)</param>
    /// <returns>Created entity information</returns>
    Task<CreatedEntityInfo> CreateEntitiesFromReviewAsync(int reviewItemId, Guid? selectedEntityId = null);
    
    /// <summary>
    /// Get fields grouped by category for a specific entity type
    /// Used by UI for organizing field dropdowns
    /// </summary>
    /// <param name="entityType">Entity type name</param>
    /// <returns>Dictionary of category name to field list</returns>
    Task<Dictionary<string, List<ReportFieldMetadata>>> GetEntityFieldsByCategoryAsync(string entityType);
}
