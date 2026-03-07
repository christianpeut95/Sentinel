using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public enum TaskCategory
    {
        [Display(Name = "Isolation")]
        Isolation = 0,

        [Display(Name = "Medication")]
        Medication = 1,

        [Display(Name = "Monitoring")]
        Monitoring = 2,

        [Display(Name = "Survey")]
        Survey = 3,

        [Display(Name = "Lab Test")]
        LabTest = 4,

        [Display(Name = "Education")]
        Education = 5,

        [Display(Name = "Contact Tracing")]
        ContactTracing = 6,

        [Display(Name = "Follow-Up")]
        FollowUp = 7
    }

    /// <summary>
    /// Triggers that determine when tasks should be automatically created.
    /// Cases and Contacts both use the Cases table but have separate triggers
    /// to support different clinical workflows in epidemiological investigations.
    /// </summary>
    public enum TaskTrigger
    {
        /// <summary>
        /// Fired when a new investigation case (CaseType.Case) is created.
        /// Use for: Initial investigation, patient interviews, medical record reviews.
        /// </summary>
        [Display(Name = "On Case Creation")]
        OnCaseCreation = 0,

        /// <summary>
        /// Fired when a new contact (CaseType.Contact) is created.
        /// Use for: Prophylaxis, contact monitoring, exposure education.
        /// Note: Contacts are stored in Cases table but represent exposed individuals,
        /// not confirmed disease cases, and require different clinical workflows.
        /// </summary>
        [Display(Name = "On Contact Creation")]
        OnContactCreation = 1,

        /// <summary>
        /// Fired when a lab result is confirmed for a case.
        /// Use for: Confirmation letters, state reporting, treatment updates.
        /// </summary>
        [Display(Name = "On Lab Confirmation")]
        OnLabConfirmation = 2,

        /// <summary>
        /// Fired when symptom onset date is recorded.
        /// Use for: Isolation instructions, communicability period calculations.
        /// </summary>
        [Display(Name = "On Symptom Onset")]
        OnSymptomOnset = 3,

        /// <summary>
        /// Fired when an exposure event is recorded.
        /// Use for: Risk assessment, exposure investigation.
        /// </summary>
        [Display(Name = "On Exposure Recorded")]
        OnExposureRecorded = 4,

        /// <summary>
        /// Manually created by users, not auto-triggered.
        /// Use for: Ad-hoc tasks, special circumstances.
        /// </summary>
        [Display(Name = "Manual")]
        Manual = 5
    }

    public enum TaskAssignmentType
    {
        [Display(Name = "Patient")]
        Patient = 0,

        [Display(Name = "Investigator")]
        Investigator = 1,

        [Display(Name = "Anyone")]
        Anyone = 2
    }

    public enum CaseTaskStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,

        [Display(Name = "In Progress")]
        InProgress = 1,

        [Display(Name = "Completed")]
        Completed = 2,

        [Display(Name = "Cancelled")]
        Cancelled = 3,

        [Display(Name = "Overdue")]
        Overdue = 4,

        [Display(Name = "Waiting for Patient")]
        WaitingForPatient = 5
    }

    public enum TaskPriority
    {
        [Display(Name = "Low")]
        Low = 0,

        [Display(Name = "Medium")]
        Medium = 1,

        [Display(Name = "High")]
        High = 2,

        [Display(Name = "Urgent")]
        Urgent = 3
    }

    public enum TaskDueCalculationMethod
    {
        [Display(Name = "From Symptom Onset")]
        FromSymptomOnset = 0,

        [Display(Name = "From Notification Date")]
        FromNotificationDate = 1,

        [Display(Name = "From Contact Date")]
        FromContactDate = 2,

        [Display(Name = "From Task Creation")]
        FromTaskCreation = 3,

        [Display(Name = "From Earliest Exposure")]
        FromEarliestExposure = 4,

        [Display(Name = "From Latest Exposure")]
        FromLatestExposure = 5
    }

    public enum RecurrencePattern
    {
        [Display(Name = "Daily")]
        Daily = 0,

        [Display(Name = "Twice Daily")]
        TwiceDaily = 1,

        [Display(Name = "Weekly")]
        Weekly = 2,

        [Display(Name = "Every Other Day")]
        EveryOtherDay = 3
    }

    public enum TaskInheritanceBehavior
    {
        [Display(Name = "Inherit - Apply to all descendants")]
        Inherit = 0,

        [Display(Name = "No Inheritance - Only this disease")]
        NoInheritance = 1,

        [Display(Name = "Selective - Only specified sub-diseases")]
        Selective = 2
    }

    public enum TaskAssignmentMethod
    {
        [Display(Name = "Manual")]
        Manual = 0,

        [Display(Name = "Auto - Round Robin")]
        AutoRoundRobin = 1,

        [Display(Name = "Auto - Language Match")]
        AutoLanguageMatch = 2,

        [Display(Name = "Supervisor Assignment")]
        SupervisorAssignment = 3
    }
}

