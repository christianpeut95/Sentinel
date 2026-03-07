using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class Gender
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
