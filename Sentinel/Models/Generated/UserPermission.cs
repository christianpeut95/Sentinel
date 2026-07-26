using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class UserPermission
{
    public string UserId { get; set; } = null!;

    public int PermissionId { get; set; }

    public bool IsGranted { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
