using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ContactTracingMindMapEdge
{
    public Guid EdgeId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid TargetNodeId { get; set; }

    public string SourceLabel { get; set; } = null!;

    public string TargetLabel { get; set; } = null!;

    public int ExposureTypeEnum { get; set; }

    public string ExposureType { get; set; } = null!;

    public int ExposureStatusEnum { get; set; }

    public string ExposureStatus { get; set; } = null!;

    public string? EdgeLabel { get; set; }

    public string? EventName { get; set; }

    public string? EventType { get; set; }

    public string? LocationName { get; set; }

    public string? LocationType { get; set; }

    public string? LocationAddress { get; set; }

    public string? ContactClassification { get; set; }

    public DateTime? ExposureStartDate { get; set; }

    public DateTime? ExposureEndDate { get; set; }

    public string EdgeStyle { get; set; } = null!;

    public string? EdgeColor { get; set; }

    public int? EdgeWeight { get; set; }
}
