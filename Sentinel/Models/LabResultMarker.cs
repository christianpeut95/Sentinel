using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;

namespace Sentinel.Models
{
    /// <summary>
    /// Individual pathogen/biomarker test result within a lab order
    /// </summary>
    public class LabResultMarker : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Lab Result")]
        public Guid LabResultId { get; set; }
        public LabResult? LabResult { get; set; }

        [Display(Name = "Pathogen/Biomarker")]
        public Guid? PathogenId { get; set; }
        public Pathogen? Pathogen { get; set; }

        [Display(Name = "Test Method")]
        public int? TestMethodId { get; set; }
        public TestMethod? TestMethod { get; set; }

        // Standardized Qualitative Result
        [Display(Name = "Qualitative Result")]
        public int? TestResultId { get; set; }
        public TestResult? TestResult { get; set; }

        // Free-text override for non-standard results
        [Display(Name = "Qualitative Result (Text)")]
        [StringLength(500)]
        public string? QualitativeResultText { get; set; }

        [Display(Name = "Quantitative Value")]
        public decimal? QuantitativeValue { get; set; }

        [Display(Name = "Quantitative Unit")]
        [StringLength(50)]
        public string? QuantitativeUnit { get; set; }

        [Display(Name = "Reference Range Low")]
        public decimal? ReferenceRangeLow { get; set; }

        [Display(Name = "Reference Range High")]
        public decimal? ReferenceRangeHigh { get; set; }

        [Display(Name = "Interpretation Flag")]
        [StringLength(20)]
        public string? InterpretationFlag { get; set; }

        [Display(Name = "LOINC Code")]
        [StringLength(20)]
        public string? LOINCCode { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Result Status")]
        [StringLength(10)]
        public string? ResultStatus { get; set; } // P=Preliminary, F=Final, C=Corrected, X=Cancelled

        [Display(Name = "Result Finalized Date")]
        public DateTime? ResultFinalizedDate { get; set; }

        [Display(Name = "Test Code")]
        [StringLength(50)]
        public string? TestCode { get; set; } // HL7 OBX-3 identifier for matching

        // Audit Trail
        public ICollection<LabResultMarkerHistory> History { get; set; } = new List<LabResultMarkerHistory>();

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
