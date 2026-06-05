using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    /// <summary>
    /// Laboratory test methodology (PCR, Culture, ELISA, etc.)
    /// Uses SNOMED CT Procedure codes for standardization
    /// </summary>
    public class TestMethod
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Test Method")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        // SNOMED CT Standardization (Procedure codes)
        [Display(Name = "SNOMED CT Code")]
        [StringLength(20)]
        [RegularExpression(@"^\d{6,18}$", ErrorMessage = "SNOMED CT code must be 6-18 digits")]
        public string? SnomedCode { get; set; }

        [Display(Name = "SNOMED CT Display")]
        [StringLength(200)]
        public string? SnomedDisplay { get; set; }

        [Display(Name = "LOINC Method Code")]
        [StringLength(20)]
        public string? LoincMethodCode { get; set; }

        [Display(Name = "Export Code")]
        [StringLength(50)]
        public string? ExportCode { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();
    }
}
