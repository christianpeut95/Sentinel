using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models.CaseDefinitions
{
    public class CaseDefinition
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Definition Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Display(Name = "Apply to Child Diseases")]
        public bool ApplyToChildDiseases { get; set; } = false;

        [Required]
        [Display(Name = "Confirmation Status")]
        public int ConfirmationStatusId { get; set; }
        public CaseStatus? ConfirmationStatus { get; set; }

        [Display(Name = "Status")]
        public CaseDefinitionStatus Status { get; set; } = CaseDefinitionStatus.Draft;

        [Display(Name = "Active From")]
        [DataType(DataType.Date)]
        public DateTime DateActiveFrom { get; set; } = DateTime.UtcNow;

        [Display(Name = "Active To")]
        [DataType(DataType.Date)]
        public DateTime? DateActiveTo { get; set; }

        [Display(Name = "Allow Auto Classification")]
        public bool AllowAutoClassification { get; set; } = false;

        [Display(Name = "Enable Auto-Evaluation")]
        public bool EnableAutoEvaluation { get; set; } = true;

        [Display(Name = "Create Review Queue on Change")]
        public bool CreateReviewQueueOnChange { get; set; } = false;

        [Display(Name = "Create Review Queue on Suggestion")]
        public bool CreateReviewQueueOnSuggestion { get; set; } = true;

        // Audit fields
        [Display(Name = "Created By")]
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified By")]
        [StringLength(450)]
        public string? ModifiedBy { get; set; }

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }

        // Navigation properties
        public ICollection<CaseDefinitionCriteria> Criteria { get; set; } = new List<CaseDefinitionCriteria>();
        public ICollection<CaseClassificationHistory> ClassificationHistories { get; set; } = new List<CaseClassificationHistory>();
    }
}
