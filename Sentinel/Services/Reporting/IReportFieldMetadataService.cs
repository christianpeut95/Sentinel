using Sentinel.Models.Reporting;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for discovering and providing metadata about fields available for reporting
/// Ensures zero-hallucination by using actual database schema
/// </summary>
public interface IReportFieldMetadataService
{
    /// <summary>
    /// Gets all available fields for a specific entity type
    /// Includes both regular fields and custom fields
    /// </summary>
    /// <param name="entityType">Case, Outbreak, Patient, etc.</param>
    /// <param name="excludeNavigationFields">If true, excludes all fields from related entities (for survey mappings)</param>
    /// <param name="context">Usage context to apply appropriate field filtering (Mapper hides more fields than Report)</param>
    /// <returns>List of field metadata</returns>
    Task<List<ReportFieldMetadata>> GetFieldsForEntityAsync(
        string entityType, 
        bool excludeNavigationFields = false,
        FieldUsageContext context = FieldUsageContext.General);

    /// <summary>
    /// Gets fields grouped by category for UI display
    /// </summary>
    /// <param name="entityType">Case, Outbreak, Patient, etc.</param>
    /// <param name="context">Usage context to apply appropriate field filtering</param>
    /// <returns>Dictionary of category name to list of fields</returns>
    Task<Dictionary<string, List<ReportFieldMetadata>>> GetFieldsByCategoryAsync(
        string entityType,
        FieldUsageContext context = FieldUsageContext.General);

    /// <summary>
    /// Gets recommended (core/essential) fields for an entity type
    /// These are the most commonly used fields that should appear first in the UI
    /// </summary>
    /// <param name="entityType">Case, Outbreak, Patient, etc.</param>
    /// <returns>List of recommended field metadata</returns>
    Task<List<ReportFieldMetadata>> GetRecommendedFieldsAsync(string entityType);

    /// <summary>
    /// Gets only custom fields for an entity
    /// </summary>
    /// <param name="entityType">Case, Outbreak, Patient, etc.</param>
    /// <returns>List of custom field metadata</returns>
    Task<List<ReportFieldMetadata>> GetCustomFieldsForEntityAsync(string entityType);

    /// <summary>
    /// Gets metadata for a specific field path
    /// </summary>
    /// <param name="entityType">Case, Outbreak, Patient, etc.</param>
    /// <param name="fieldPath">Field path like "Patient.Age" or "CustomField_RiskLevel"</param>
    /// <returns>Field metadata or null if not found</returns>
    Task<ReportFieldMetadata?> GetFieldMetadataAsync(string entityType, string fieldPath);

    /// <summary>
    /// Validates that a field path exists and is accessible
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="fieldPath">Field path to validate</param>
    /// <returns>True if field exists and is queryable</returns>
    Task<bool> ValidateFieldPathAsync(string entityType, string fieldPath);

    /// <summary>
    /// Gets suggested aggregations for a field based on its data type
    /// </summary>
    /// <param name="dataType">Data type of the field</param>
    /// <returns>List of suggested aggregation types</returns>
    List<string> GetSuggestedAggregations(string dataType);
}
