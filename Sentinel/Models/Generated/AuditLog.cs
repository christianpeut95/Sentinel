using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class AuditLog
{
    public int Id { get; set; }

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string FieldName { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ChangedByUserId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual AspNetUser? ChangedByUser { get; set; }
}
