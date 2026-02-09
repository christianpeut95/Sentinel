using Surveillance_MVP.Models.Lookups;
using System;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models
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

        [Display(Name = "Confirmation Status")]
        public int? ConfirmationStatusId { get; set; }
        public CaseStatus? ConfirmationStatus { get; set; }

        [Display(Name = "Disease")]
        public Guid? DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        public ICollection<Note> Notes { get; set; } = new List<Note>();
        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
        public ICollection<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();
        public ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();

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
}
