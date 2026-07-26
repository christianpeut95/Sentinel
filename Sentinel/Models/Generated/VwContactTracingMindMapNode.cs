using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class VwContactTracingMindMapNode
{
    public Guid NodeId { get; set; }

    public string NodeLabel { get; set; } = null!;

    public int NodeType { get; set; }

    public string PersonName { get; set; } = null!;

    public string? DiseaseName { get; set; }

    public string? CaseStatus { get; set; }

    public DateTime? DateOfOnset { get; set; }

    public bool IsDeleted { get; set; }
}
