using Sentinel.Models.Lookups;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class Case : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Display(Name = "Case ID")]
        [StringLength(20)]
        public string FriendlyId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Patient")]
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Display(Name = "Date of Onset")]
        [DataType(DataType.Date)]
        public DateTime? DateOfOnset { get; set; }

        [Display(Name = "Date of Notification")]
        [DataType(DataType.Date)]
        public DateTime? DateOfNotification { get; set; }

        // Clinical Notification fields
        [Display(Name = "Clinical Notification Date")]
        [DataType(DataType.Date)]
        public DateTime? ClinicalNotificationDate { get; set; }

        [Display(Name = "Clinical Notifier Organisation")]
        [StringLength(200)]
        public string? ClinicalNotifierOrganisation { get; set; }

        [Display(Name = "Clinical Notification Notes")]
        [StringLength(1000)]
        public string? ClinicalNotificationNotes { get; set; }

        [Display(Name = "Confirmation Status")]
        public int? ConfirmationStatusId { get; set; }
        public CaseStatus? ConfirmationStatus { get; set; }

        [Display(Name = "Disease")]
        public Guid? DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        // Hospitalization fields
        [Display(Name = "Hospitalised")]
        public YesNoUnknown? Hospitalised { get; set; }

        [Display(Name = "Hospital")]
        public Guid? HospitalId { get; set; }
        public Organization? Hospital { get; set; }

        [Display(Name = "Date of Admission")]
        [DataType(DataType.Date)]
        public DateTime? DateOfAdmission { get; set; }

        [Display(Name = "Date of Discharge")]
        [DataType(DataType.Date)]
        public DateTime? DateOfDischarge { get; set; }

        [Display(Name = "Died due to disease?")]
        public YesNoUnknown? DiedDueToDisease { get; set; }

        // Jurisdiction Fields
        public int? Jurisdiction1Id { get; set; }
        public Jurisdiction? Jurisdiction1 { get; set; }

        public int? Jurisdiction2Id { get; set; }
        public Jurisdiction? Jurisdiction2 { get; set; }

        public int? Jurisdiction3Id { get; set; }
        public Jurisdiction? Jurisdiction3 { get; set; }

        public int? Jurisdiction4Id { get; set; }
        public Jurisdiction? Jurisdiction4 { get; set; }

        public int? Jurisdiction5Id { get; set; }
        public Jurisdiction? Jurisdiction5 { get; set; }

        // Address Snapshot - captured at case creation/update time
        // Preserves historical location data even if patient moves
        [Display(Name = "Address")]
        [StringLength(500)]
        public string? CaseAddressLine { get; set; }

        [Display(Name = "Suburb")]
        [StringLength(200)]
        public string? CaseCity { get; set; }

        [Display(Name = "State")]
        public int? CaseStateId { get; set; }
        public State? CaseState { get; set; }

        [Display(Name = "Postcode")]
        [StringLength(20)]
        public string? CasePostalCode { get; set; }

        [Display(Name = "Latitude")]
        public double? CaseLatitude { get; set; }

        [Display(Name = "Longitude")]
        public double? CaseLongitude { get; set; }

        [Display(Name = "Address Captured At")]
        public DateTime? CaseAddressCapturedAt { get; set; }

        [Display(Name = "Address Manually Set")]
        public bool CaseAddressManualOverride { get; set; } = false;

        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
        public ICollection<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();
        public ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();
        public ICollection<CaseTask> Tasks { get; set; } = new List<CaseTask>();

        [Required]
        [Display(Name = "Type")]
        public CaseType Type { get; set; }
        
        // Custom Fields Navigation Properties
        public ICollection<CaseCustomFieldString> CustomFieldStrings { get; set; } = new List<CaseCustomFieldString>();
        public ICollection<CaseCustomFieldNumber> CustomFieldNumbers { get; set; } = new List<CaseCustomFieldNumber>();
        public ICollection<CaseCustomFieldDate> CustomFieldDates { get; set; } = new List<CaseCustomFieldDate>();
        public ICollection<CaseCustomFieldBoolean> CustomFieldBooleans { get; set; } = new List<CaseCustomFieldBoolean>();
        public ICollection<CaseCustomFieldLookup> CustomFieldLookups { get; set; } = new List<CaseCustomFieldLookup>();

        // Soft Delete Properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }

    public enum CaseType
    {
        [Display(Name = "Case")]
        Case = 1,

        [Display(Name = "Contact")]
        Contact = 2
    }

    public enum YesNoUnknown
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Yes")]
        Yes = 1,

        [Display(Name = "No")]
        No = 2
    }
}
