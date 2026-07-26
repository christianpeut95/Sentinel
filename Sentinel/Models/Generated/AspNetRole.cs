using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class AspNetRole
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? NormalizedName { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public virtual ICollection<AspNetRoleClaim> AspNetRoleClaims { get; set; } = new List<AspNetRoleClaim>();

    public virtual ICollection<RoleDiseaseAccess> RoleDiseaseAccesses { get; set; } = new List<RoleDiseaseAccess>();

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<AspNetUser> Users { get; set; } = new List<AspNetUser>();
}
