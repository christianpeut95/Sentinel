using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models.HL7
{
    /// <summary>
    /// Maps HL7 test codes to custom fields for disease-specific data extraction
    /// Allows automatic extraction of antibiotic susceptibilities, molecular typing, etc.
    /// </summary>
    public class HL7CustomFieldMapping : IAuditable
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "HL7 Test Code")]
        public string HL7TestCode { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Test Code Description")]
        public string? TestCodeDescription { get; set; }

        [Required]
        [Display(Name = "Custom Field")]
        public int CustomFieldDefinitionId { get; set; }
        public CustomFieldDefinition? CustomFieldDefinition { get; set; }

        [Display(Name = "Extract Qualitative Result")]
        public bool ExtractQualitativeResult { get; set; } = true;

        [Display(Name = "Extract Quantitative Result")]
        public bool ExtractQuantitativeResult { get; set; } = false;

        [Display(Name = "Value Transformation")]
        [StringLength(500)]
        public string? ValueTransformation { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 0;

        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }
}
