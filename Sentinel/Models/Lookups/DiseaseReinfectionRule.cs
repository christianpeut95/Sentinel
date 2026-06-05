using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    /// <summary>
    /// Defines rules for handling reinfections and chronic diseases
    /// Determines when lab results should create new cases vs. append to existing cases
    /// </summary>
    public class DiseaseReinfectionRule : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Required]
        [Display(Name = "Rule Type")]
        public ReinfectionRuleType RuleType { get; set; }

        [Display(Name = "Reinfection Window (Days)")]
        [Range(0, 3650)]
        public int? ReinfectionWindowDays { get; set; }

        [Display(Name = "Is Chronic Disease")]
        public bool IsChronic { get; set; } = false;

        [Display(Name = "Always Create New Case")]
        public bool AlwaysCreateNewCase { get; set; } = false;

        [Display(Name = "Description")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Case Matching Strategy")]
        public CaseMatchingStrategy CaseMatchingStrategy { get; set; } = CaseMatchingStrategy.DateWindowMatching;

        [Display(Name = "Match on Test Type")]
        public bool MatchOnTestType { get; set; } = false;

        [Display(Name = "Match on Result Type")]
        public bool MatchOnResultType { get; set; } = false;

        [Display(Name = "Require Confirmation for New Case")]
        public bool RequireConfirmationForNewCase { get; set; } = false;

        [Display(Name = "Notification Message")]
        [StringLength(1000)]
        public string? NotificationMessage { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Notes")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Types of reinfection rules
    /// </summary>
    public enum ReinfectionRuleType
    {
        [Display(Name = "No Reinfection (Single Case per Patient)")]
        NoReinfection = 0,

        [Display(Name = "Time Window (New Case After X Days)")]
        TimeWindow = 1,

        [Display(Name = "Always New Case (Each Positive = New Case)")]
        AlwaysNewCase = 2,

        [Display(Name = "Chronic Disease (Always Append to Existing)")]
        ChronicDisease = 3,

        [Display(Name = "Manual Review Required")]
        ManualReview = 4
    }

    /// <summary>
    /// Strategy for matching lab results to existing cases
    /// </summary>
    public enum CaseMatchingStrategy
    {
        [Display(Name = "Date Window Matching")]
        DateWindowMatching = 1,

        [Display(Name = "Most Recent Open Case")]
        MostRecentOpenCase = 2,

        [Display(Name = "Any Open Case")]
        AnyOpenCase = 3,

        [Display(Name = "Exact Date Match")]
        ExactDateMatch = 4,

        [Display(Name = "Manual Assignment Only")]
        ManualAssignmentOnly = 5
    }
}
