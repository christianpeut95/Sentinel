using Microsoft.AspNetCore.Identity;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;
using System.Security.Claims;

namespace Surveillance_MVP.Helpers
{
    public class PermissionHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PermissionHelper(
            IPermissionService permissionService,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _permissionService = permissionService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> CanAsync(PermissionModule module, PermissionAction action)
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            return await _permissionService.HasPermissionAsync(userId, module, action);
        }

        public async Task<bool> CanViewPatients() => await CanAsync(PermissionModule.Patient, PermissionAction.View);
        public async Task<bool> CanCreatePatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Create);
        public async Task<bool> CanEditPatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Edit);
        public async Task<bool> CanDeletePatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Delete);
        public async Task<bool> CanSearchPatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Search);
        public async Task<bool> CanMergePatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Merge);
        public async Task<bool> CanExportPatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Export);
        public async Task<bool> CanImportPatients() => await CanAsync(PermissionModule.Patient, PermissionAction.Import);

        public async Task<bool> CanViewSettings() => await CanAsync(PermissionModule.Settings, PermissionAction.View);
        public async Task<bool> CanEditSettings() => await CanAsync(PermissionModule.Settings, PermissionAction.Edit);
        public async Task<bool> CanCreateSettings() => await CanAsync(PermissionModule.Settings, PermissionAction.Create);
        public async Task<bool> CanDeleteSettings() => await CanAsync(PermissionModule.Settings, PermissionAction.Delete);
        public async Task<bool> CanManageCustomFields() => await CanAsync(PermissionModule.Settings, PermissionAction.ManageCustomFields);
        public async Task<bool> CanManageCustomLookups() => await CanAsync(PermissionModule.Settings, PermissionAction.ManageCustomLookups);
        public async Task<bool> CanManageSystemLookups() => await CanAsync(PermissionModule.Settings, PermissionAction.ManageSystemLookups);
        public async Task<bool> CanManageOrganization() => await CanAsync(PermissionModule.Settings, PermissionAction.ManageOrganization);

        public async Task<bool> CanViewAudit() => await CanAsync(PermissionModule.Audit, PermissionAction.View);
        public async Task<bool> CanExportAudit() => await CanAsync(PermissionModule.Audit, PermissionAction.Export);

        public async Task<bool> CanViewUsers() => await CanAsync(PermissionModule.User, PermissionAction.View);
        public async Task<bool> CanCreateUsers() => await CanAsync(PermissionModule.User, PermissionAction.Create);
        public async Task<bool> CanEditUsers() => await CanAsync(PermissionModule.User, PermissionAction.Edit);
        public async Task<bool> CanDeleteUsers() => await CanAsync(PermissionModule.User, PermissionAction.Delete);
        public async Task<bool> CanManagePermissions() => await CanAsync(PermissionModule.User, PermissionAction.ManagePermissions);
        public async Task<bool> CanResetPasswords() => await CanAsync(PermissionModule.User, PermissionAction.ResetPassword);

        public async Task<bool> CanViewReports() => await CanAsync(PermissionModule.Report, PermissionAction.View);
        public async Task<bool> CanExportReports() => await CanAsync(PermissionModule.Report, PermissionAction.Export);
    }
}
