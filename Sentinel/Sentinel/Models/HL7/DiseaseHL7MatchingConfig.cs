using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models.HL7
{
    /// <summary>
    /// Configuration for HL7 message matching behavior per disease.
    /// Controls whether text matching is used as fallback when LOINC/SNOMED codes don't match.
    /// </summary>
    public class DiseaseHL7MatchingConfig
    {
        [Key]
        public Guid DiseaseId { get; set; }
        public Disease Disease { get; set; } = null!;

        [Display(Name = "Override Parent HL7 Matching Rules")]
        public bool OverrideParentRules { get; set; } = false;

        // Test Method Matching Rules
        [Display(Name = "Use Text Matching (Fallback)")]
        public bool TestMethod_UseTextFallback { get; set; } = false;

        [Display(Name = "Normalize Whitespace")]
        public bool TestMethod_NormalizeWhitespace { get; set; } = false;

        [Display(Name = "Ignore Punctuation")]
        public bool TestMethod_IgnorePunctuation { get; set; } = false;

        [Display(Name = "Case Insensitive")]
        public bool TestMethod_CaseInsensitive { get; set; } = false;

        // Specimen Type Matching Rules
        [Display(Name = "Use Text Matching (Fallback)")]
        public bool SpecimenType_UseTextFallback { get; set; } = false;

        [Display(Name = "Normalize Whitespace")]
        public bool SpecimenType_NormalizeWhitespace { get; set; } = false;

        [Display(Name = "Ignore Punctuation")]
        public bool SpecimenType_IgnorePunctuation { get; set; } = false;

        [Display(Name = "Case Insensitive")]
        public bool SpecimenType_CaseInsensitive { get; set; } = false;

        // Pathogen Matching Rules
        [Display(Name = "Use Text Matching (Fallback)")]
        public bool Pathogen_UseTextFallback { get; set; } = false;

        [Display(Name = "Normalize Whitespace")]
        public bool Pathogen_NormalizeWhitespace { get; set; } = false;

        [Display(Name = "Ignore Punctuation")]
        public bool Pathogen_IgnorePunctuation { get; set; } = false;

        [Display(Name = "Case Insensitive")]
        public bool Pathogen_CaseInsensitive { get; set; } = false;

        // Test Result Matching Rules
        [Display(Name = "Use Text Matching (Fallback)")]
        public bool TestResult_UseTextFallback { get; set; } = false;

        [Display(Name = "Normalize Whitespace")]
        public bool TestResult_NormalizeWhitespace { get; set; } = false;

        [Display(Name = "Ignore Punctuation")]
        public bool TestResult_IgnorePunctuation { get; set; } = false;

        [Display(Name = "Case Insensitive")]
        public bool TestResult_CaseInsensitive { get; set; } = false;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
