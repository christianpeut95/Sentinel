using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ContactTracingMindMapNode
{
    public Guid NodeId { get; set; }

    public string NodeLabel { get; set; } = null!;

    public string NodeName { get; set; } = null!;

    public string NodeType { get; set; } = null!;

    public Guid? DiseaseId { get; set; }

    public string? DiseaseName { get; set; }

    public string? DiseaseCode { get; set; }

    public DateTime? DateOfOnset { get; set; }

    public DateTime? DateOfNotification { get; set; }

    public DateTime DateIdentified { get; set; }

    public string? CaseStatus { get; set; }

    public int OutgoingTransmissions { get; set; }

    public int IncomingExposures { get; set; }

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public string FollowUpStatus { get; set; } = null!;

    public string? Suburb { get; set; }

    public string? State { get; set; }

    public string? Jurisdiction1 { get; set; }
}
