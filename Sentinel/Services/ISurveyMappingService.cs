using Sentinel.Models;
using Sentinel.Models.Reporting;

namespace Sentinel.Services
{
    public interface ISurveyMappingService
    {
        /// <summary>
        /// Get all active mappings for a specific configuration, respecting priority order
        /// </summary>
        Task<List<SurveyFieldMapping>> GetActiveMappingsAsync(
            Guid? surveyTemplateId,
            Guid? taskTemplateId,
            Guid? diseaseId);

        /// <summary>
        /// Get available database fields using the existing field discovery service
        /// </summary>
        Task<List<ReportFieldMetadata>> GetAvailableFieldsAsync(string entityType);

        /// <summary>
        /// Get survey questions from SurveyJS JSON definition
        /// </summary>
        Task<List<SurveyQuestion>> GetSurveyQuestionsAsync(string surveyDefinitionJson);

        /// <summary>
        /// Execute mappings for survey response, handling business rules
        /// </summary>
        Task<MappingExecutionResult> ExecuteMappingsAsync(
            Guid taskId,
            Dictionary<string, object> surveyResponses,
            List<SurveyFieldMapping> mappings);

        /// <summary>
        /// Preview what will happen when mappings are executed (for testing)
        /// </summary>
        Task<MappingPreviewResult> PreviewMappingsAsync(
            Guid? caseId,
            Dictionary<string, object> surveyResponses,
            List<SurveyFieldMapping> mappings);

        /// <summary>
        /// Validate mapping configuration
        /// </summary>
        Task<MappingValidationResult> ValidateMappingAsync(SurveyFieldMapping mapping);

        /// <summary>
        /// Copy mappings from one configuration to another
        /// </summary>
        Task<int> CopyMappingsAsync(
            MappingConfigurationType sourceType,
            Guid sourceId,
            MappingConfigurationType targetType,
            Guid targetId);

        /// <summary>
        /// Get or create default mappings based on field name similarity
        /// </summary>
        Task<List<SurveyFieldMapping>> GetSuggestedMappingsAsync(
            string surveyDefinitionJson,
            MappingConfigurationType configurationType,
            Guid configurationId,
            Guid? diseaseId = null);

        /// <summary>
        /// Get all mappings for a specific configuration type and ID
        /// </summary>
        Task<List<SurveyFieldMapping>> GetMappingsByTypeAsync(
            MappingConfigurationType configurationType,
            Guid configurationId);

        /// <summary>
        /// Get a specific mapping by ID
        /// </summary>
        Task<SurveyFieldMapping?> GetMappingByIdAsync(Guid mappingId);

        /// <summary>
        /// Get survey questions from a survey template ID
        /// </summary>
        Task<List<SurveyQuestion>> GetSurveyQuestionsAsync(Guid surveyTemplateId);

        /// <summary>
        /// Get only matrix/dynamic questions (collection-capable) from a survey template
        /// </summary>
        Task<List<SurveyQuestion>> GetCollectionQuestionsAsync(Guid surveyTemplateId);

        /// <summary>
        /// Create a new mapping
        /// </summary>
        Task<SurveyFieldMapping> CreateMappingAsync(SurveyFieldMapping mapping);

        /// <summary>
        /// Update an existing mapping
        /// </summary>
        Task UpdateMappingAsync(SurveyFieldMapping mapping);

        /// <summary>
        /// Delete a mapping
        /// </summary>
        Task DeleteMappingAsync(Guid mappingId);

        /// <summary>
        /// Suggest mappings based on survey template
        /// </summary>
        Task SuggestMappingsAsync(
            Guid surveyTemplateId,
            MappingConfigurationType configurationType,
            Guid configurationId,
            Guid? diseaseId = null);
    }

    public class SurveyQuestion
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<string>? Choices { get; set; }
    }

    public class MappingExecutionResult
    {
        public bool Success { get; set; }
        public int AutoSavedCount { get; set; }
        public int QueuedForReviewCount { get; set; }
        public int RequireApprovalCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<MappingExecutionDetail> Details { get; set; } = new();
        
        // Collection Mapping Results (Phase 2B)
        public int CollectionEntitiesCreated { get; set; }
        public int CollectionItemsForReview { get; set; }
    }

    public class MappingExecutionDetail
    {
        public string SurveyQuestion { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public object? SurveyValue { get; set; }
        public object? DatabaseValue { get; set; }
        public object? ResultingValue { get; set; }
        public MappingAction Action { get; set; }
        public string BusinessRuleApplied { get; set; } = string.Empty;
        public bool WasModified { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MappingPreviewResult
    {
        public List<MappingPreviewItem> Items { get; set; } = new();
        public int TotalMappings { get; set; }
        public int WillAutoSave { get; set; }
        public int WillQueueForReview { get; set; }
        public int WillRequireApproval { get; set; }
        public int WillSkip { get; set; }
    }

    public class MappingPreviewItem
    {
        public string SurveyQuestion { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public object? SurveyValue { get; set; }
        public object? CurrentDatabaseValue { get; set; }
        public object? ProjectedValue { get; set; }
        public MappingAction Action { get; set; }
        public string ActionDisplay { get; set; } = string.Empty;
        public string BusinessRule { get; set; } = string.Empty;
        public bool WillModify { get; set; }
        public string? Icon { get; set; }
        public string? CssClass { get; set; }
    }

    public class MappingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
