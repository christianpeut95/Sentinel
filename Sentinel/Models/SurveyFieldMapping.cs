using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentinel.Models
{
    public class SurveyFieldMapping : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        public MappingConfigurationType ConfigurationType { get; set; }

        [Required]
        public Guid ConfigurationId { get; set; }

        public int Priority { get; set; }

        [Required]
        [MaxLength(500)]
        public string SurveyQuestionName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string TargetFieldPath { get; set; } = string.Empty;

        [Required]
        public MappingFieldType TargetFieldType { get; set; }

        [Required]
        public MappingFieldCategory FieldCategory { get; set; }

        [Required]
        public MappingAction MappingAction { get; set; }

        [Required]
        public MappingBusinessRule BusinessRule { get; set; }

        public bool TriggerReviewQueue { get; set; }

        public int ReviewPriority { get; set; } = 1;

        public int GroupingWindowHours { get; set; } = 6;

        [MaxLength(2000)]
        public string? ValidationRules { get; set; }

        [MaxLength(2000)]
        public string? TransformationScript { get; set; }

        [MaxLength(500)]
        public string? DisplayName { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        [Display(Name = "Target Symptom")]
        public int? TargetSymptomId { get; set; }
        public Lookups.Symptom? TargetSymptom { get; set; }

        // ========================================
        // COLLECTION MAPPING FIELDS (Phase 1)
        // ========================================
        
        /// <summary>
        /// Indicates whether this is a simple or complex mapping
        /// Default: Simple (backward compatible)
        /// </summary>
        public MappingComplexity Complexity { get; set; } = MappingComplexity.Simple;
        
        /// <summary>
        /// JSON configuration for collection mappings
        /// Format: CollectionMappingConfig serialized
        /// Only used when Complexity = Collection
        /// </summary>
        public string? CollectionConfigJson { get; set; }
        
        /// <summary>
        /// JSON configuration for duplicate detection/matching
        /// Format: MatchingConfig serialized
        /// </summary>
        public string? MatchingRulesJson { get; set; }
        
        /// <summary>
        /// What to do when potential duplicates are found
        /// </summary>
        public DuplicateHandling? OnDuplicateFound { get; set; }
        
        /// <summary>
        /// Execution order for complex mappings
        /// Lower numbers execute first
        /// </summary>
        public int ExecutionOrder { get; set; } = 100;

        public string? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedByUserId { get; set; }
        public DateTime? LastModified { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedBy { get; set; }

        [ForeignKey(nameof(LastModifiedByUserId))]
        public virtual ApplicationUser? LastModifiedBy { get; set; }
    }

    public enum MappingConfigurationType
    {
        Disease = 1,
        Task = 2,
        Survey = 3
    }

    public enum MappingFieldType
    {
        StandardField = 1,
        CustomField = 2
    }

    public enum MappingFieldCategory
    {
        Patient = 1,
        Case = 2,
        Symptom = 3,
        Exposure = 4,
        LabResult = 5,
        Task = 6
    }

    public enum MappingAction
    {
        AutoSave = 1,
        QueueForReview = 2,
        RequireApproval = 3,
        Skip = 4
    }

    public enum MappingBusinessRule
    {
        AlwaysOverwrite = 1,
        OnlyIfNull = 2,
        TakeEarliest = 3,
        TakeLatest = 4,
        TakeHighest = 5,
        TakeLowest = 6,
        Append = 7,
        RequireMatch = 8,
        ConditionalOverwrite = 9
    }

    /// <summary>
    /// Indicates the complexity level of the mapping
    /// </summary>
    public enum MappingComplexity
    {
        /// <summary>
        /// Simple field-to-field mapping (default/current behavior)
        /// </summary>
        Simple = 1,
        
        /// <summary>
        /// Collection/array mapping (matrix, matrixdynamic questions)
        /// Each row can create one or more related entities
        /// </summary>
        Collection = 2,
        
        /// <summary>
        /// Creates related child entities (Contact, Exposure, etc.)
        /// </summary>
        RelatedEntity = 3,
        
        /// <summary>
        /// Conditional mapping based on rules
        /// </summary>
        Conditional = 4
    }

    /// <summary>
    /// What to do when a potential duplicate entity is found
    /// </summary>
    public enum DuplicateHandling
    {
        /// <summary>
        /// Always create a new entity regardless of matches
        /// </summary>
        CreateNew = 1,
        
        /// <summary>
        /// Update the existing matched entity
        /// </summary>
        UpdateExisting = 2,
        
        /// <summary>
        /// Don't create new, just link to existing entity
        /// </summary>
        SkipAndLink = 3,
        
        /// <summary>
        /// Send to review queue for human decision
        /// </summary>
        RequireReview = 4,
        
        /// <summary>
        /// Merge new data into existing record
        /// </summary>
        MergeData = 5
    }
}
