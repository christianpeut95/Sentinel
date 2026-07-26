using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Permission
{
    public int Id { get; set; }

    public int Module { get; set; }

    public int Action { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
