using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models.Lookups
{
    public class ResultUnits
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Unit")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Abbreviation")]
        [StringLength(20)]
        public string? Abbreviation { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
    }
}
