using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LabResultMarker
{
    public Guid Id { get; set; }

    public Guid LabResultId { get; set; }

    public Guid PathogenId { get; set; }

    public int? TestMethodId { get; set; }

    public string? QualitativeResultText { get; set; }

    public decimal? QuantitativeValue { get; set; }

    public string? QuantitativeUnit { get; set; }

    public decimal? ReferenceRangeLow { get; set; }

    public decimal? ReferenceRangeHigh { get; set; }

    public string? InterpretationFlag { get; set; }

    public string? Loinccode { get; set; }

    public string? Notes { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public DateTime? ResultFinalizedDate { get; set; }

    public string? ResultStatus { get; set; }

    public string? TestCode { get; set; }

    public int? TestResultId { get; set; }

    public virtual LabResult LabResult { get; set; } = null!;

    public virtual ICollection<LabResultMarkerHistory> LabResultMarkerHistories { get; set; } = new List<LabResultMarkerHistory>();

    public virtual Pathogen Pathogen { get; set; } = null!;

    public virtual TestMethod? TestMethod { get; set; }

    public virtual TestResult? TestResult { get; set; }
}
