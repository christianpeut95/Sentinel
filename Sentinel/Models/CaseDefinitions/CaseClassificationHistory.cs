using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models.CaseDefinitions
{
    public class CaseClassificationHistory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Case")]
        public Guid CaseId { get; set; }
        public Case? Case { get; set; }

        [Display(Name = "From Status")]
        public int? FromConfirmationStatusId { get; set; }
        public CaseStatus? FromConfirmationStatus { get; set; }

        [Required]
        [Display(Name = "To Status")]
        public int ToConfirmationStatusId { get; set; }
        public CaseStatus? ToConfirmationStatus { get; set; }

        [Display(Name = "Case Definition")]
        public int? CaseDefinitionId { get; set; }
        public CaseDefinition? CaseDefinition { get; set; }

        [Display(Name = "Evaluation Date")]
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Classified Date")]
        public DateTime? ClassifiedDate { get; set; }

        [Display(Name = "Classified By")]
        [StringLength(450)]
        public string? ClassifiedByUserId { get; set; }

        [Display(Name = "Auto Classified")]
        public bool IsAutoClassified { get; set; } = false;

        [Display(Name = "Is Match")]
        public bool IsMatch { get; set; }

        [Display(Name = "Recommended Action")]
        public RecommendedAction RecommendedAction { get; set; }

        [Display(Name = "Rationale")]
        public string? Rationale { get; set; }

        [Display(Name = "Criteria Results")]
        public string? CriteriaResultJson { get; set; }

        [Display(Name = "Was Applied")]
        public bool WasApplied { get; set; } = false;

        [Display(Name = "Is Current")]
        public bool IsCurrent { get; set; } = true;
    }
}
