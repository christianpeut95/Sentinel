using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models.Lookups
{
    public class OrganizationType
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Organization Type")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}
