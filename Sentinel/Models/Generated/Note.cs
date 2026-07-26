using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Note
{
    public Guid Id { get; set; }

    public string Content { get; set; } = null!;

    public string? Subject { get; set; }

    public string Type { get; set; } = null!;

    public string? Recipient { get; set; }

    public Guid? PatientId { get; set; }

    public Guid? CaseId { get; set; }

    public int? OutbreakId { get; set; }

    public string? AttachmentPath { get; set; }

    public string? AttachmentFileName { get; set; }

    public long? AttachmentSize { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public virtual Case? Case { get; set; }

    public virtual Outbreak? Outbreak { get; set; }

    public virtual Patient? Patient { get; set; }
}
