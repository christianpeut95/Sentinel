using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class DiseaseCategory
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Reporting ID")]
        public string ReportingId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<Disease> Diseases { get; set; } = new List<Disease>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
