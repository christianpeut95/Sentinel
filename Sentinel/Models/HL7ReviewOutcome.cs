using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Categorizes the outcome of manual review for HL7 messages requiring attention
    /// </summary>
    public enum HL7ReviewOutcome
    {
        [Display(Name = "Not Reviewed")]
        NotReviewed = 0,

        [Display(Name = "Fixed - Configuration Updated")]
        FixedConfigurationUpdated = 1,

        [Display(Name = "Fixed - Manual Case Created")]
        FixedManualCaseCreated = 2,

        [Display(Name = "Fixed - Mapping Added")]
        FixedMappingAdded = 3,

        [Display(Name = "Ignored - Duplicate Test")]
        IgnoredDuplicateTest = 4,

        [Display(Name = "Ignored - Not Notifiable")]
        IgnoredNotNotifiable = 5,

        [Display(Name = "Ignored - Invalid Data")]
        IgnoredInvalidData = 6,

        [Display(Name = "Escalated - Technical Support Needed")]
        EscalatedTechnicalSupport = 7,

        [Display(Name = "Escalated - Data Quality Issue")]
        EscalatedDataQuality = 8,

        [Display(Name = "Escalated - Task Created")]
        EscalatedTaskCreated = 9,

        [Display(Name = "Reprocessed Successfully")]
        ReprocessedSuccessfully = 10,

        [Display(Name = "Pending Resolution")]
        PendingResolution = 11,

        // NoSurveillance-specific outcomes
        [Display(Name = "Confirmed Not Reportable")]
        ConfirmedNotReportable = 12,

        [Display(Name = "Requires Mapping - Not Reportable")]
        RequiresMappingNotReportable = 13,

        [Display(Name = "Manually Entered as Case")]
        ManuallyEnteredAsCase = 14,

        [Display(Name = "Requires Policy Clarification")]
        RequiresPolicyClarification = 15,

        // Generic reviewed outcome when notes capture the details
        [Display(Name = "Reviewed")]
        Reviewed = 16
    }
}
