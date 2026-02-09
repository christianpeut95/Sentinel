using Microsoft.AspNetCore.Authorization;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Authorization
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
