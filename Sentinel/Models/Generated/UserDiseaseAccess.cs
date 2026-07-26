using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class UserDiseaseAccess
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public Guid DiseaseId { get; set; }

    public bool IsAllowed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? GrantedByUserId { get; set; }

    public string? Reason { get; set; }

    public bool ApplyToChildren { get; set; }

    public Guid? InheritedFromDiseaseId { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual AspNetUser? GrantedByUser { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
