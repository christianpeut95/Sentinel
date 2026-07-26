using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Event
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int? EventTypeId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public int? EstimatedAttendees { get; set; }

    public bool? IsIndoor { get; set; }

    public Guid? OrganizerOrganizationId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? LastModified { get; set; }

    public string? LastModifiedByUserId { get; set; }

    public virtual EventType? EventType { get; set; }

    public virtual ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();

    public virtual Location Location { get; set; } = null!;

    public virtual Organization? OrganizerOrganization { get; set; }

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();
}
