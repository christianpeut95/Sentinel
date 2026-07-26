using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class VwContactTracingMindMapEdge
{
    public Guid EdgeId { get; set; }

    public Guid? SourceNodeId { get; set; }

    public Guid TargetNodeId { get; set; }

    public string EdgeType { get; set; } = null!;

    public DateTime ExposureDate { get; set; }

    public string ConfidenceLevel { get; set; } = null!;
}
