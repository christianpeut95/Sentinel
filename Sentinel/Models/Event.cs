using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class Event : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Event Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Event Type")]
        public int? EventTypeId { get; set; }
        public EventType? EventType { get; set; }

        [Required]
        [Display(Name = "Location")]
        public Guid LocationId { get; set; }
        public Location? Location { get; set; }

        [Required]
        [Display(Name = "Start Date/Time")]
        public DateTime StartDateTime { get; set; }

        [Display(Name = "End Date/Time")]
        public DateTime? EndDateTime { get; set; }

        [Display(Name = "Estimated Attendees")]
        public int? EstimatedAttendees { get; set; }

        [Display(Name = "Indoor Event")]
        public bool? IsIndoor { get; set; }

        [Display(Name = "Organized By")]
        public Guid? OrganizerOrganizationId { get; set; }
        public Organization? OrganizerOrganization { get; set; }

        [Display(Name = "Description")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
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
