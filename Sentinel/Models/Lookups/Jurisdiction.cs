using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class Jurisdiction
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Jurisdiction Type")]
        public int JurisdictionTypeId { get; set; }
        public JurisdictionType? JurisdictionType { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Code")]
        public string? Code { get; set; }

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Parent Jurisdiction")]
        public int? ParentJurisdictionId { get; set; }
        public Jurisdiction? ParentJurisdiction { get; set; }

        [Display(Name = "Boundary Data (GeoJSON)")]
        public string? BoundaryData { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Population")]
        public long? Population { get; set; }

        [Display(Name = "Population Year")]
        public int? PopulationYear { get; set; }

        [StringLength(200)]
        [Display(Name = "Population Source")]
        public string? PopulationSource { get; set; }

        public ICollection<Jurisdiction> ChildJurisdictions { get; set; } = new List<Jurisdiction>();
    }
}
