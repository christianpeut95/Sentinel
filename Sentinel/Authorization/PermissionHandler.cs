using Microsoft.AspNetCore.Authorization;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionHandler(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var hasPermission = await _permissionService.HasPermissionAsync(
                userId, 
                requirement.Module, 
                requirement.Action);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
