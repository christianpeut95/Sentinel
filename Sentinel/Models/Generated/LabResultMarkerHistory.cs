using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LabResultMarkerHistory
{
    public Guid Id { get; set; }

    public Guid LabResultMarkerId { get; set; }

    public Guid Hl7messageId { get; set; }

    public DateTime ChangedAt { get; set; }

    public int ChangeType { get; set; }

    public string? PreviousQualitativeValue { get; set; }

    public decimal? PreviousQuantitativeValue { get; set; }

    public string? PreviousResultStatus { get; set; }

    public string? PreviousAbnormalFlag { get; set; }

    public string? NewQualitativeValue { get; set; }

    public decimal? NewQuantitativeValue { get; set; }

    public string? NewResultStatus { get; set; }

    public string? NewAbnormalFlag { get; set; }

    public string? ChangeReason { get; set; }

    public bool ChangedBySystem { get; set; }

    public string? ChangedByUserId { get; set; }

    public virtual AspNetUser? ChangedByUser { get; set; }

    public virtual Hl7message Hl7message { get; set; } = null!;

    public virtual LabResultMarker LabResultMarker { get; set; } = null!;
}
