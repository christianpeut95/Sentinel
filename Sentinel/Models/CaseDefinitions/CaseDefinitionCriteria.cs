using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;

namespace Sentinel.Models.CaseDefinitions
{
    public class CaseDefinitionCriteria
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Case Definition")]
        public int CaseDefinitionId { get; set; }
        public CaseDefinition? CaseDefinition { get; set; }

        [Display(Name = "Parent Criteria")]
        public int? ParentCriteriaId { get; set; }
        public CaseDefinitionCriteria? ParentCriteria { get; set; }

        [Display(Name = "Criterion Type")]
        public CriterionType CriterionType { get; set; }

        [Display(Name = "Logical Operator")]
        public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.AND;

        [Display(Name = "Group Number")]
        public int GroupNumber { get; set; } = 1;

        // ============================================
        // Generic Criteria Fields (Clinical, Epidemiological, Demographic, Custom)
        // ============================================

        [StringLength(200)]
        [Display(Name = "Field Path")]
        public string? FieldPath { get; set; }

        [Display(Name = "Operator")]
        public ComparisonOperator? Operator { get; set; }

        [Display(Name = "Value")]
        public string? ValueJson { get; set; }

        [StringLength(500)]
        [Display(Name = "Display Text")]
        public string? DisplayText { get; set; }

        // ============================================
        // Laboratory Criteria Fields (CriterionType = Laboratory)
        // ============================================

        [Display(Name = "Acceptable Specimen Types (JSON array of IDs)")]
        public string? AcceptableSpecimenTypesJson { get; set; }

        [Display(Name = "Specimen Storage Preference")]
        public DataStoragePreference? SpecimenStoragePreference { get; set; }

        [Display(Name = "Canonical Specimen Type")]
        public int? CanonicalSpecimenTypeId { get; set; }
        public SpecimenType? CanonicalSpecimenType { get; set; }

        [Display(Name = "Acceptable Pathogens (JSON array of GUIDs)")]
        public string? AcceptablePathogensJson { get; set; }

        [Display(Name = "Biomarker Storage Preference")]
        public DataStoragePreference? BiomarkerStoragePreference { get; set; }

        [Display(Name = "Canonical Pathogen")]
        public Guid? CanonicalPathogenId { get; set; }
        public Pathogen? CanonicalPathogen { get; set; }

        [Display(Name = "Acceptable Test Methods (JSON array of IDs)")]
        public string? AcceptableTestMethodsJson { get; set; }

        [Display(Name = "Test Method Storage Preference")]
        public DataStoragePreference? TestMethodStoragePreference { get; set; }

        [Display(Name = "Canonical Test Method")]
        public int? CanonicalTestMethodId { get; set; }
        public TestMethod? CanonicalTestMethod { get; set; }

        [Display(Name = "Acceptable Results (JSON array of IDs)")]
        public string? AcceptableResultsJson { get; set; }

        [Display(Name = "Result Storage Preference")]
        public DataStoragePreference? ResultStoragePreference { get; set; }

        [Display(Name = "Canonical Test Result")]
        public int? CanonicalTestResultId { get; set; }
        public TestResult? CanonicalTestResult { get; set; }

        [Display(Name = "Required")]
        public bool? IsRequired { get; set; }

        [Display(Name = "Require All Elements Match")]
        public bool? RequireAllElementsMatch { get; set; }

        // ============================================
        // Common Fields
        // ============================================

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }

        // Navigation property for child criteria
        public ICollection<CaseDefinitionCriteria> ChildCriteria { get; set; } = new List<CaseDefinitionCriteria>();
    }
}
