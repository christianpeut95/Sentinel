using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class ContactClassification
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Contact Classification")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();
    }
}
