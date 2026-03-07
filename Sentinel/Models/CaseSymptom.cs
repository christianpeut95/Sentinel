using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class CaseSymptom : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Case")]
        public Guid CaseId { get; set; }
        public Case? Case { get; set; }

        [Required]
        [Display(Name = "Symptom")]
        public int SymptomId { get; set; }
        public Symptom? Symptom { get; set; }

        [Display(Name = "Onset Date")]
        [DataType(DataType.Date)]
        public DateTime? OnsetDate { get; set; }

        [Display(Name = "Severity")]
        [StringLength(20)]
        public string? Severity { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Other Symptom Description")]
        [StringLength(200)]
        public string? OtherSymptomText { get; set; }

        // Audit properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Soft delete properties
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}
