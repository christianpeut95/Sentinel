using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class JurisdictionType
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        [Display(Name = "Field Number")]
        public int FieldNumber { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Display Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Short Code")]
        public string? Code { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        public ICollection<Jurisdiction> Jurisdictions { get; set; } = new List<Jurisdiction>();
    }
}
