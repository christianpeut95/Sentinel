using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakTimeline
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public DateTime EventDate { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int EventType { get; set; }

    public Guid? RelatedCaseId { get; set; }

    public int? RelatedNoteId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public virtual Outbreak Outbreak { get; set; } = null!;
}
