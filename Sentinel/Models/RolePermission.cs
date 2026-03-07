using Microsoft.AspNetCore.Identity;

namespace Sentinel.Models
{
    public class RolePermission
    {
        public string RoleId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;

        public IdentityRole? Role { get; set; }
        public Permission? Permission { get; set; }
    }
}
