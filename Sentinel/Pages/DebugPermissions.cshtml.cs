using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages
{
    [AllowAnonymous]
    public class DebugPermissionsModel : PageModel
    {
        private readonly IPermissionService _permissionService;

        public DebugPermissionsModel(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<Permission> UserPermissions { get; set; } = new();
        public Dictionary<string, bool> SpecificChecks { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                UserName = User.Identity.Name ?? "Unknown";

                UserPermissions = await _permissionService.GetUserPermissionsAsync(UserId);

                // Check specific permissions
                SpecificChecks["Case.View"] = await _permissionService.HasPermissionAsync(UserId, PermissionModule.Case, PermissionAction.View);
                SpecificChecks["Case.Create"] = await _permissionService.HasPermissionAsync(UserId, PermissionModule.Case, PermissionAction.Create);
                SpecificChecks["Case.Edit"] = await _permissionService.HasPermissionAsync(UserId, PermissionModule.Case, PermissionAction.Edit);
                SpecificChecks["Case.Delete"] = await _permissionService.HasPermissionAsync(UserId, PermissionModule.Case, PermissionAction.Delete);
                SpecificChecks["Patient.View"] = await _permissionService.HasPermissionAsync(UserId, PermissionModule.Patient, PermissionAction.View);
            }
        }
    }
}
