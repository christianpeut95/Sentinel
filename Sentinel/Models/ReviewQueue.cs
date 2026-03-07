using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sentinel.Models.Lookups;

namespace Sentinel.Models;

public class ReviewQueue
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    public Guid? CaseId { get; set; }
    [ForeignKey(nameof(CaseId))]
    public Case? Case { get; set; }

    public Guid? PatientId { get; set; }
    [ForeignKey(nameof(PatientId))]
    public Patient? Patient { get; set; }

    public Guid? DiseaseId { get; set; }
    [ForeignKey(nameof(DiseaseId))]
    public Disease? Disease { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChangeType { get; set; } = "New";

    [MaxLength(100)]
    public string? TriggerField { get; set; }

    public string? ChangeSnapshot { get; set; }

    public int Priority { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    public string ReviewStatus { get; set; } = "Pending";

    [MaxLength(50)]
    public string? ReviewAction { get; set; }

    [MaxLength(255)]
    public string? GroupKey { get; set; }

    public int GroupCount { get; set; } = 1;

    // ========================================
    // COLLECTION MAPPING REVIEW FIELDS (Phase 2)
    // ========================================
    
    /// <summary>
    /// JSON array of potential entity matches found during duplicate detection
    /// Format: EntityMatch[] serialized
    /// Used when reviewing duplicate entities
    /// </summary>
    public string? PotentialMatchesJson { get; set; }
    
    /// <summary>
    /// JSON object containing the proposed entity data that will be created if approved
    /// Format: Dictionary<string, object> serialized
    /// </summary>
    public string? ProposedEntityDataJson { get; set; }
    
    /// <summary>
    /// JSON object containing the original survey row data
    /// Used for audit trail and debugging
    /// </summary>
    public string? CollectionSourceDataJson { get; set; }
    
    /// <summary>
    /// If user selects an existing entity to link to instead of creating new
    /// </summary>
    public Guid? SelectedExistingEntityId { get; set; }

    [MaxLength(450)]
    public string? ReviewedByUserId { get; set; }
    [ForeignKey(nameof(ReviewedByUserId))]
    public ApplicationUser? ReviewedBy { get; set; }

    public DateTime? ReviewedDate { get; set; }

    public string? ReviewNotes { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser? CreatedBy { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Guid? TaskId { get; set; }
    [ForeignKey(nameof(TaskId))]
    public CaseTask? Task { get; set; }
}

public static class ReviewEntityTypes
{
    public const string LabResult = "LabResult";
    public const string Exposure = "Exposure";
    public const string Contact = "Contact";
    public const string CaseChange = "CaseChange";
    public const string ClinicalNotification = "ClinicalNotification";
    public const string PatientUpdate = "PatientUpdate";
    
    // Collection Mapping Review Types
    public const string DuplicatePatient = "DuplicatePatient";
    public const string NewPatient = "NewPatient";  // ? NEW: Always-review patient (no duplicates)
    public const string DuplicateContact = "DuplicateContact";
    public const string DuplicateExposure = "DuplicateExposure";
    public const string BulkEntityCreation = "BulkEntityCreation";
}

public static class ReviewChangeTypes
{
    public const string New = "New";
    public const string Updated = "Updated";
    public const string FieldChanged = "FieldChanged";
    
    // Collection Mapping Change Types
    public const string PotentialDuplicate = "PotentialDuplicate";
    public const string BulkCreate = "BulkCreate";
    public const string PendingCreation = "PendingCreation";  // ? NEW: Always-review (no duplicates detected)
}

public static class ReviewStatuses
{
    public const string Pending = "Pending";
    public const string Reviewed = "Reviewed";
    public const string Dismissed = "Dismissed";
}

public static class ReviewActions
{
    public const string Confirmed = "Confirmed";
    public const string TaskCreated = "TaskCreated";
    public const string Dismissed = "Dismissed";
}

public static class ReviewPriorities
{
    public const int Low = 0;
    public const int Medium = 1;
    public const int High = 2;
    public const int Urgent = 3;
}
