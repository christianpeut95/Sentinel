using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class TaskType
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Code { get; set; }

        [Display(Name = "Icon Class")]
        [StringLength(50)]
        public string? IconClass { get; set; } = "bi-clipboard-check";

        [Display(Name = "Color Class")]
        [StringLength(50)]
        public string? ColorClass { get; set; } = "bg-info";

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Interview Task")]
        public bool IsInterviewTask { get; set; }

        // Navigation
        public ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
    }
}
