using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentinel.Models
{
    public class Location : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Location Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Location Type")]
        public int? LocationTypeId { get; set; }
        public LocationType? LocationType { get; set; }

        [Display(Name = "Address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Latitude")]
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [Display(Name = "Geocoding Status")]
        [StringLength(50)]
        public string? GeocodingStatus { get; set; }

        [Display(Name = "Last Geocoded")]
        public DateTime? LastGeocoded { get; set; }

        [Display(Name = "Organization")]
        public Guid? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Display(Name = "Is High Risk")]
        public bool IsHighRisk { get; set; } = false;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Notes")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();

        // Audit fields - these will be populated by the DbContext
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModified { get; set; }

        [Display(Name = "Last Modified By")]
        [StringLength(450)]
        public string? LastModifiedByUserId { get; set; }
    }
}
