using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Organization
{
    public Guid Id { get; set; }

    public string FriendlyId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? OrganizationTypeId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ContactPerson { get; set; }

    public string? ExportCode { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Hl7configuration> Hl7configurations { get; set; } = new List<Hl7configuration>();

    public virtual ICollection<Hl7message> Hl7messageLaboratoryOrganizations { get; set; } = new List<Hl7message>();

    public virtual ICollection<Hl7message> Hl7messageOrderingProviderOrganizations { get; set; } = new List<Hl7message>();

    public virtual ICollection<LabResult> LabResultLaboratories { get; set; } = new List<LabResult>();

    public virtual ICollection<LabResult> LabResultOrderingProviders { get; set; } = new List<LabResult>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual OrganizationType? OrganizationType { get; set; }
}
