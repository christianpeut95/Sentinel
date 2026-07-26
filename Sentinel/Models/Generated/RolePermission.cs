using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class RolePermission
{
    public string RoleId { get; set; } = null!;

    public int PermissionId { get; set; }

    public bool IsGranted { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual AspNetRole Role { get; set; } = null!;
}
