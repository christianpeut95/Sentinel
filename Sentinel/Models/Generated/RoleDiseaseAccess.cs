using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class RoleDiseaseAccess
{
    public int Id { get; set; }

    public string RoleId { get; set; } = null!;

    public Guid DiseaseId { get; set; }

    public bool IsAllowed { get; set; }

    public bool ApplyToChildren { get; set; }

    public Guid? InheritedFromDiseaseId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual AspNetRole Role { get; set; } = null!;
}
