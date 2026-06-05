using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models
{
    public class LabResult : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Display(Name = "Lab Result ID")]
        [StringLength(20)]
        public string FriendlyId { get; set; } = string.Empty;

        // Case relationship (nullable - HL7 creates LabResults before Case assignment)
        [Display(Name = "Case")]
        public Guid? CaseId { get; set; }
        public Case? Case { get; set; }

        // Patient relationship (for HL7 processing before case creation)
        [Display(Name = "Patient")]
        public Guid? PatientId { get; set; }
        public Patient? Patient { get; set; }

        // Laboratory (Organization)
        [Display(Name = "Laboratory")]
        public Guid? LaboratoryId { get; set; }
        public Organization? Laboratory { get; set; }

        // Accession/Lab Reference
        [Display(Name = "Accession Number")]
        [StringLength(100)]
        public string? AccessionNumber { get; set; }

        // Specimen Information
        [Display(Name = "Specimen Collection Date")]
        [DataType(DataType.Date)]
        public DateTime? SpecimenCollectionDate { get; set; }

        [Display(Name = "Specimen Type")]
        public int? SpecimenTypeId { get; set; }
        public SpecimenType? SpecimenType { get; set; }

        [Display(Name = "Test For (Disease/Pathogen)")]
        public Guid? TestedDiseaseId { get; set; }
        public Disease? TestedDisease { get; set; }

        // Ordering Provider
        [Display(Name = "Ordering Provider")]
        public Guid? OrderingProviderId { get; set; }
        public Organization? OrderingProvider { get; set; }

        // Results - Current System
        [Display(Name = "Result Date")]
        [DataType(DataType.Date)]
        public DateTime? ResultDate { get; set; }

        [Display(Name = "Result Units")]
        public int? ResultUnitsId { get; set; }
        public ResultUnits? ResultUnits { get; set; }

        // Flags
        [Display(Name = "Is Amended")]
        public bool IsAmended { get; set; } = false;

        // Notes
        [Display(Name = "Notes")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Lab Interpretation")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? LabInterpretation { get; set; }

        // File Attachment
        [StringLength(500)]
        [Display(Name = "Attachment Path")]
        public string? AttachmentPath { get; set; }

        [StringLength(200)]
        [Display(Name = "Attachment Name")]
        public string? AttachmentFileName { get; set; }

        [Display(Name = "Attachment Size (bytes)")]
        public long? AttachmentSize { get; set; }

        // Pathogen/Biomarker Test Results (new system)
        public ICollection<LabResultMarker> Markers { get; set; } = new List<LabResultMarker>();

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }

        // Soft Delete Properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}
