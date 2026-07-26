using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakCase
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public Guid CaseId { get; set; }

    public bool IsIndexCase { get; set; }

    public int? Classification { get; set; }

    public DateTime? ClassificationDate { get; set; }

    public string? ClassifiedBy { get; set; }

    public string? ClassificationNotes { get; set; }

    public int LinkMethod { get; set; }

    public int? SearchQueryId { get; set; }

    public DateTime LinkedDate { get; set; }

    public string? LinkedBy { get; set; }

    public DateTime? UnlinkedDate { get; set; }

    public string? UnlinkedBy { get; set; }

    public string? UnlinkReason { get; set; }

    public bool IsActive { get; set; }

    public virtual Case Case { get; set; } = null!;

    public virtual Outbreak Outbreak { get; set; } = null!;

    public virtual OutbreakSearchQuery? SearchQuery { get; set; }
}
