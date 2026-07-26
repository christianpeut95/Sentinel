using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseTimelineAll
{
    public Guid CaseId { get; set; }

    public string CaseNumber { get; set; } = null!;

    public string PatientName { get; set; } = null!;

    public string? DiseaseName { get; set; }

    public string EventType { get; set; } = null!;

    public DateTime EventDate { get; set; }

    public string? EventUser { get; set; }

    public string EventDescription { get; set; } = null!;

    public int EventSequence { get; set; }
}
