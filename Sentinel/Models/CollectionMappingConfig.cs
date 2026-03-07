using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models;

/// <summary>
/// Configuration for mapping a survey collection (matrix/dynamic) to entity creation
/// Fully dynamic - no hardcoded fields
/// Uses ReportFieldMetadataService for field discovery
/// </summary>
public class CollectionMappingConfig
{
    /// <summary>
    /// Source question name from survey (e.g., "householdContacts")
    /// </summary>
    [Required]
    public string SourceQuestionName { get; set; } = string.Empty;
    
    /// <summary>
    /// Target entity type to create (e.g., "Contact", "Exposure", "LabResult")
    /// Dynamically discovered from DbContext
    /// </summary>
    [Required]
    public string TargetEntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Column-to-field mappings
    /// Each item maps a survey column to a database field path
    /// </summary>
    public List<CollectionColumnMapping> RowMappings { get; set; } = new();
    
    /// <summary>
    /// Duplicate detection configuration
    /// </summary>
    public MatchingConfig? MatchingConfig { get; set; }
    
    /// <summary>
    /// What to do when potential duplicates are found
    /// </summary>
    public DuplicateHandling OnDuplicateFound { get; set; } = DuplicateHandling.RequireReview;
    
    /// <summary>
    /// Related entities to create automatically
    /// (e.g., create Patient + Contact + Exposure from one row)
    /// </summary>
    public List<RelatedEntityConfig> RelatedEntities { get; set; } = new();
    
    /// <summary>
    /// Execution order (when multiple collection mappings exist)
    /// </summary>
    public int ExecutionOrder { get; set; } = 100;
}

/// <summary>
/// Single column-to-field mapping within a collection
/// </summary>
public class CollectionColumnMapping
{
    /// <summary>
    /// Column name from survey matrix (e.g., "firstName")
    /// </summary>
    [Required]
    public string SourceColumn { get; set; } = string.Empty;
    
    /// <summary>
    /// Target field path using ReportFieldMetadata format
    /// (e.g., "Patient.GivenName", "CustomField:RiskLevel")
    /// </summary>
    [Required]
    public string TargetFieldPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this field required for entity creation?
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// Transformation function name (optional)
    /// e.g., "ToUpperCase", "ParseDate", "StripSpaces"
    /// </summary>
    public string? TransformFunction { get; set; }
    
    /// <summary>
    /// Default value if source is empty
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Configuration for finding duplicate entities
/// Uses ReportFieldMetadataService to validate field paths
/// </summary>
public class MatchingConfig
{
    /// <summary>
    /// Entity type to search for duplicates (e.g., "Patient")
    /// </summary>
    [Required]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// List of field paths to match on (from ReportFieldMetadata)
    /// e.g., ["Patient.GivenName", "Patient.FamilyName", "Patient.DateOfBirth"]
    /// </summary>
    public List<string> MatchOnFields { get; set; } = new();
    
    /// <summary>
    /// Fuzzy matching threshold (0.0 to 1.0)
    /// 1.0 = exact match required
    /// 0.8 = 80% similarity acceptable
    /// </summary>
    [Range(0.0, 1.0)]
    public double ConfidenceThreshold { get; set; } = 0.85;
    
    /// <summary>
    /// Matching strategy: Exact, Fuzzy, Phonetic, Combined
    /// </summary>
    public MatchingStrategy Strategy { get; set; } = MatchingStrategy.Exact;
}

/// <summary>
/// Related entity to create automatically as part of collection processing
/// Example: Create Contact + ExposureEvent when creating Patient
/// </summary>
public class RelatedEntityConfig
{
    /// <summary>
    /// Entity type to create (e.g., "Contact", "ExposureEvent")
    /// </summary>
    [Required]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Order in which to create this entity (after primary entity)
    /// 1 = Create first (e.g., Contact)
    /// 2 = Create second (e.g., ExposureEvent)
    /// </summary>
    public int CreationOrder { get; set; } = 1;
    
    /// <summary>
    /// Mappings for this related entity
    /// Includes column mappings, context variables, and computed fields
    /// </summary>
    public List<RelatedEntityMapping> Mappings { get; set; } = new();
    
    /// <summary>
    /// Conditional creation rule (optional)
    /// Example: "symptomatic == 'Yes'" - only create if condition met
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// Whether this entity requires the primary entity to be created successfully
    /// Default: true (skip if primary entity failed/queued)
    /// </summary>
    public bool RequiresPrimaryEntity { get; set; } = true;
}

/// <summary>
/// Mapping for a related entity field
/// Supports multiple source types: column data, context variables, computed values
/// </summary>
public class RelatedEntityMapping
{
    /// <summary>
    /// Source type: "Column", "Context", "Primary", "Constant"
    /// </summary>
    [Required]
    public string SourceType { get; set; } = "Column";
    
    /// <summary>
    /// Source value based on SourceType:
    /// - Column: "firstName" (column name from matrix)
    /// - Context: "{{Context.CaseId}}" (context variable)
    /// - Primary: "{Primary.Id}" (value from created primary entity)
    /// - Constant: "Household" (static value)
    /// </summary>
    [Required]
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Target field path (e.g., "Contact.PatientId", "ExposureEvent.ExposureDate")
    /// </summary>
    [Required]
    public string TargetFieldPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this field required?
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// Default value if source is empty/null
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Context information available during survey submission processing
/// Passed to collection mapping service for context variable resolution
/// </summary>
public class SurveySubmissionContext
{
    /// <summary>
    /// The case being investigated
    /// </summary>
    public Guid CaseId { get; set; }
    
    /// <summary>
    /// The patient who is the index case
    /// </summary>
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// The task being completed
    /// </summary>
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Disease being investigated
    /// </summary>
    public Guid DiseaseId { get; set; }
    
    /// <summary>
    /// Jurisdiction
    /// </summary>
    public Guid? JurisdictionId { get; set; }
    
    /// <summary>
    /// Who submitted the survey
    /// </summary>
    public string SubmittedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// When the survey was submitted
    /// </summary>
    public DateTime SubmittedDate { get; set; }
    
    /// <summary>
    /// Mapping action from parent SurveyFieldMapping (AutoSave, QueueForReview, etc.)
    /// Controls whether collection entities are auto-created or queued for review
    /// </summary>
    public MappingAction? MappingAction { get; set; }
    
    /// <summary>
    /// Additional contextual data (for custom field resolution)
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Matching strategy for duplicate detection
/// </summary>
public enum MatchingStrategy
{
    /// <summary>
    /// Exact match on all fields
    /// </summary>
    Exact = 1,
    
    /// <summary>
    /// Fuzzy string matching (Levenshtein distance)
    /// </summary>
    Fuzzy = 2,
    
    /// <summary>
    /// Phonetic matching (Soundex, Metaphone)
    /// Good for names
    /// </summary>
    Phonetic = 3,
    
    /// <summary>
    /// Combined: exact for dates/numbers, fuzzy for strings
    /// </summary>
    Combined = 4
}

/// <summary>
/// Result of a single entity match from duplicate detection
/// </summary>
public class EntityMatch
{
    public Guid ExistingEntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public Dictionary<string, object> MatchedFields { get; set; } = new();
    public List<string> ConflictingFields { get; set; } = new();
}

/// <summary>
/// Result of processing a collection mapping
/// </summary>
public class CollectionMappingResult
{
    public bool Success { get; set; } = true;
    public List<CreatedEntityInfo> EntitiesCreated { get; set; } = new();
    public int ItemsRequiringReview { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Information about an entity created during collection processing
/// </summary>
public class CreatedEntityInfo
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> FieldValues { get; set; } = new();
    
    /// <summary>
    /// True if this is the primary entity from the collection row
    /// False if this is a related entity (Contact, Exposure, etc.)
    /// </summary>
    public bool IsPrimaryEntity { get; set; }
    
    /// <summary>
    /// If this is a related entity, the ID of the primary entity it's linked to
    /// </summary>
    public Guid? PrimaryEntityId { get; set; }
}

/// <summary>
/// Validation result for mapping configuration
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
