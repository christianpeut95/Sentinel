using Microsoft.AspNetCore.Authorization;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Extensions
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
