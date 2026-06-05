using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models.Pathogens
{
    /// <summary>
    /// Represents a pathogen, biomarker, or analyte that can be tested for in laboratory results.
    /// Maps to LOINC codes for HL7 integration.
    /// </summary>
    public class Pathogen : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Short Name")]
        [StringLength(50)]
        public string? ShortName { get; set; }

        [Display(Name = "LOINC Code")]
        [StringLength(20)]
        public string? LOINCCode { get; set; }

        [Display(Name = "LOINC Display Name")]
        [StringLength(500)]
        public string? LOINCDisplayName { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Disease")]
        public Guid? DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Required]
        [Display(Name = "Category")]
        public PathogenCategory Category { get; set; }

        [Required]
        [Display(Name = "Result Type")]
        public ResultType ResultType { get; set; } = ResultType.Qualitative;

        [Display(Name = "Default Unit")]
        [StringLength(50)]
        public string? DefaultUnit { get; set; }

        [Display(Name = "Reference Range Low")]
        public decimal? DefaultReferenceRangeLow { get; set; }

        [Display(Name = "Reference Range High")]
        public decimal? DefaultReferenceRangeHigh { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
