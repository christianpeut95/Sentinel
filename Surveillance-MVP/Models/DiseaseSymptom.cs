using Surveillance_MVP.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models
{
    public class DiseaseSymptom : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Required]
        [Display(Name = "Symptom")]
        public int SymptomId { get; set; }
        public Symptom? Symptom { get; set; }

        [Display(Name = "Is Common Symptom")]
        public bool IsCommon { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

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
