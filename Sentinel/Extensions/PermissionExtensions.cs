using Microsoft.AspNetCore.Authorization;
using Sentinel.Models;

namespace Sentinel.Extensions
{
    public static class PermissionExtensions
    {
        public static string ToPolicy(this PermissionModule module, PermissionAction action)
        {
            return $"Permission.{module}.{action}";
        }

        public static AuthorizeAttribute RequirePermission(PermissionModule module, PermissionAction action)
        {
            return new AuthorizeAttribute { Policy = $"Permission.{module}.{action}" };
        }
    }
}
