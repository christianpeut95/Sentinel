using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class SpecimenType
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Specimen Type")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        // SNOMED CT Standardization
        [Display(Name = "SNOMED CT Code")]
        [StringLength(20)]
        [RegularExpression(@"^\d{6,18}$", ErrorMessage = "SNOMED CT code must be 6-18 digits")]
        public string? SnomedCode { get; set; }

        [Display(Name = "SNOMED CT Display")]
        [StringLength(200)]
        public string? SnomedDisplay { get; set; }

        // Secondary Mappings
        [Display(Name = "LOINC System Code")]
        [StringLength(20)]
        public string? LoincSystemCode { get; set; }

        [Display(Name = "HL7 v2 Code")]
        [StringLength(20)]
        public string? Hl7Code { get; set; }

        // Clinical Attributes
        [Display(Name = "Body Site")]
        [StringLength(100)]
        public string? BodySite { get; set; }

        [Display(Name = "Collection Method")]
        [StringLength(100)]
        public string? CollectionMethod { get; set; }

        // Legacy
        [Display(Name = "Export Code (Legacy)")]
        [StringLength(50)]
        public string? ExportCode { get; set; }

        [Display(Name = "Is Invasive")]
        public bool IsInvasive { get; set; } = false;

        [Display(Name = "Is Sterile Site")]
        public bool IsSterileSite { get; set; } = false;

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
    }
}
