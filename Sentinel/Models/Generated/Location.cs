using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Location
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int? LocationTypeId { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? GeocodingStatus { get; set; }

    public DateTime? LastGeocoded { get; set; }

    public Guid? OrganizationId { get; set; }

    public bool IsHighRisk { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? LastModified { get; set; }

    public string? LastModifiedByUserId { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<ExposureEvent> ExposureEvents { get; set; } = new List<ExposureEvent>();

    public virtual LocationType? LocationType { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();
}
