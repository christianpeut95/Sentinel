using Microsoft.AspNetCore.Authorization;
using Sentinel.Models;

namespace Sentinel.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionModule Module { get; }
        public PermissionAction Action { get; }

        public PermissionRequirement(PermissionModule module, PermissionAction action)
        {
            Module = module;
            Action = action;
        }

        public string GetPermissionKey()
        {
            return $"{Module}.{Action}";
        }
    }
}
