using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class VwContactsListSimple
{
    public Guid ContactId { get; set; }

    public string ContactNumber { get; set; } = null!;

    public DateTime? DateIdentified { get; set; }

    public DateTime? ContactDateOfOnset { get; set; }

    public Guid PatientId { get; set; }

    public string ContactName { get; set; } = null!;

    public string ContactFirstName { get; set; } = null!;

    public string ContactLastName { get; set; } = null!;

    public DateTime? ContactDob { get; set; }

    public string? ContactMobile { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactSuburb { get; set; }

    public string? ContactState { get; set; }

    public string? DiseaseName { get; set; }

    public string ExposureType { get; set; } = null!;

    public string? ExposureSourceName { get; set; }

    public int? TotalTasks { get; set; }

    public int? CompletedTasks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
