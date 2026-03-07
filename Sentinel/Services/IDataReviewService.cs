using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services;

public interface IDataReviewService
{
    /// <summary>
    /// Queue an item for manual review
    /// </summary>
    Task<int> QueueForReviewAsync(
        string entityType,
        int entityId,
        Guid? diseaseId = null,
        Guid? caseId = null,
        Guid? patientId = null,
        string changeType = "New",
        string? triggerField = null,
        object? changeSnapshot = null,
        bool autoCreateTask = true);

    /// <summary>
    /// Get effective review settings for a disease (with hierarchy)
    /// </summary>
    Task<DiseaseReviewSettings> GetEffectiveReviewSettingsAsync(Guid diseaseId);

    /// <summary>
    /// Confirm/Accept a review item
    /// </summary>
    Task<bool> ConfirmReviewAsync(int reviewQueueId, string? notes = null);

    /// <summary>
    /// Dismiss a review item
    /// </summary>
    Task<bool> DismissReviewAsync(int reviewQueueId, string? notes = null);

    /// <summary>
    /// Create a task for a review item and mark as reviewed
    /// </summary>
    Task<Guid?> CreateTaskForReviewAsync(int reviewQueueId, string taskTitle, string? taskDescription = null, DateTime? dueDate = null, string? assignedToUserId = null);

    /// <summary>
    /// Get review queue items with filtering and pagination
    /// </summary>
    Task<ReviewQueueResult> GetReviewQueueAsync(
        string? entityType = null,
        List<Guid>? diseaseIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? reviewStatus = "Pending",
        int skip = 0,
        int take = 50);

    /// <summary>
    /// Get review item details with full context
    /// </summary>
    Task<ReviewQueueDetail?> GetReviewItemDetailAsync(int reviewQueueId);

    /// <summary>
    /// Generate group key for similar changes
    /// </summary>
    string GenerateGroupKey(string entityType, string? triggerField, object? newValue, Guid? diseaseId);
}

public class DiseaseReviewSettings
{
    public int GroupingWindowHours { get; set; } = 6;
    public bool AutoQueueLabResults { get; set; } = true;
    public bool AutoQueueExposures { get; set; } = false;
    public bool AutoQueueContacts { get; set; } = false;
    public bool AutoQueueConfirmationChanges { get; set; } = true;
    public bool AutoQueueDiseaseChanges { get; set; } = true;
    public bool AutoQueueClinicalNotifications { get; set; } = false;
    public bool AutoQueueNewCases { get; set; } = false;
    public int DefaultPriority { get; set; } = 1;
}

public class ReviewQueueResult
{
    public List<ReviewQueueItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public bool HasMore { get; set; }
}

public class ReviewQueueItem
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? TriggerField { get; set; }
    public string? ChangeSnapshot { get; set; }  // JSON snapshot of changes
    public int Priority { get; set; }
    public string? ReviewStatus { get; set; } = string.Empty;
    public string? GroupKey { get; set; }
    public int GroupCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? CaseId { get; set; }
    public string? CaseFriendlyId { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public Guid? DiseaseId { get; set; }
    public string? DiseaseName { get; set; }
    public bool HasTask { get; set; }
    public string? TaskStatus { get; set; }
    
    // ? Patient Demographics - Complete Set
    public string? PatientFriendlyId { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public int? PatientAge { get; set; }
    public string? PatientGender { get; set; }
    public string? PatientSexAtBirth { get; set; }
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public string? PatientAddressLine { get; set; }
    public string? PatientCity { get; set; }
    public string? PatientState { get; set; }
    public string? PatientPostalCode { get; set; }
    
    // Visual grouping properties (for UI display, not persisted to DB)
    public string? VisualGroupId { get; set; }
    public int VisualGroupCount { get; set; }
    public bool IsPartOfVisualGroup { get; set; }
    public List<VisualGroupMember> VisualGroupMembers { get; set; } = new();
}

public class VisualGroupMember
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? TriggerField { get; set; }
    public string? ChangeType { get; set; }
    public string ChangeSummary { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class ReviewQueueDetail
{
    public ReviewQueueItem ReviewItem { get; set; } = new();
    public Dictionary<string, object?> ChangeSnapshot { get; set; } = new();
    public Dictionary<string, object?> EntityData { get; set; } = new();
    public PatientContext? Patient { get; set; }
    public CaseContext? Case { get; set; }
    public List<RelatedItem> RelatedItems { get; set; } = new();
    public List<EntityMatch> PotentialMatches { get; set; } = new();  // ? For duplicate detection
}

public class PatientContext
{
    public Guid Id { get; set; }
    public string FriendlyId { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? EmailAddress { get; set; }
    public string? MobilePhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
}

public class CaseContext
{
    public Guid Id { get; set; }
    public string FriendlyId { get; set; } = string.Empty;
    public string DiseaseName { get; set; } = string.Empty;
    public string ConfirmationStatus { get; set; } = string.Empty;
    public DateTime? DateOfOnset { get; set; }
    public DateTime? DateOfNotification { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RelatedItem
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
