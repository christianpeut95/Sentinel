using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models.Lookups
{
    public class Symptom : IAuditable, ISoftDeletable
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Symptom Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Code")]
        [StringLength(50)]
        public string? Code { get; set; }

        [Display(Name = "Export Code")]
        [StringLength(50)]
        public string? ExportCode { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        // Navigation properties
        public ICollection<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();
        public ICollection<DiseaseSymptom> DiseaseSymptoms { get; set; } = new List<DiseaseSymptom>();

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
