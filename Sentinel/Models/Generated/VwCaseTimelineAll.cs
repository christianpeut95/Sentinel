using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class VwCaseTimelineAll
{
    public Guid CaseId { get; set; }

    public string EventType { get; set; } = null!;

    public DateTime? EventDate { get; set; }

    public string EventDescription { get; set; } = null!;

    public string? ActorName { get; set; }

    public DateTime? SortDate { get; set; }
}
