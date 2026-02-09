using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Settings.Roles
{
    [Authorize(Policy = "Permission.User.ManagePermissions")]
    public class PermissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPermissionService _permissionService;

        public PermissionsModel(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            IPermissionService permissionService)
        {
            _context = context;
            _roleManager = roleManager;
            _permissionService = permissionService;
        }

        public IdentityRole Role { get; set; } = default!;
        public List<Permission> GrantedPermissions { get; set; } = new();
        public List<Permission> NotGrantedPermissions { get; set; } = new();
        public Dictionary<PermissionModule, List<Permission>> GrantedByModule { get; set; } = new();
        public Dictionary<PermissionModule, List<Permission>> NotGrantedByModule { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            Role = role;

            await LoadPermissions(id);

            return Page();
        }

        private async Task LoadPermissions(string roleId)
        {
            var allPermissions = await _permissionService.GetAllPermissionsAsync();
            var grantedPermissions = await _permissionService.GetRolePermissionsAsync(roleId);
            var grantedIds = grantedPermissions.Select(p => p.Id).ToHashSet();

            GrantedPermissions = grantedPermissions;
            NotGrantedPermissions = allPermissions.Where(p => !grantedIds.Contains(p.Id)).ToList();

            GrantedByModule = GrantedPermissions
                .GroupBy(p => p.Module)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Action).ToList());

            NotGrantedByModule = NotGrantedPermissions
                .GroupBy(p => p.Module)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Action).ToList());
        }

        public async Task<IActionResult> OnPostGrantAsync(string roleId, int permissionId)
        {
            // Debug logging
            var permission = await _context.Permissions.FindAsync(permissionId);
            var permissionName = permission?.Name ?? "Unknown";
            
            Console.WriteLine($"[DEBUG] Granting permission - RoleId: {roleId}, PermissionId: {permissionId}, Name: {permissionName}");
            
            await _permissionService.GrantPermissionToRoleAsync(roleId, permissionId);
            
            // Verify it was actually saved
            var saved = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && rp.IsGranted);
            
            if (saved)
            {
                TempData["SuccessMessage"] = $"? Permission '{permissionName}' (ID: {permissionId}) granted and VERIFIED in database.";
            }
            else
            {
                TempData["ErrorMessage"] = $"? ERROR: Permission '{permissionName}' (ID: {permissionId}) was NOT saved to database!";
            }
            
            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostRevokeAsync(string roleId, int permissionId)
        {
            await _permissionService.RevokePermissionFromRoleAsync(roleId, permissionId);
            TempData["SuccessMessage"] = "Permission revoked successfully.";
            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostGrantAllAsync(string roleId)
        {
            var allPermissions = await _permissionService.GetAllPermissionsAsync();
            
            foreach (var permission in allPermissions)
            {
                await _permissionService.GrantPermissionToRoleAsync(roleId, permission.Id);
            }

            TempData["SuccessMessage"] = $"All {allPermissions.Count} permissions granted successfully.";
            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostRevokeAllAsync(string roleId)
        {
            var grantedPermissions = await _permissionService.GetRolePermissionsAsync(roleId);
            
            foreach (var permission in grantedPermissions)
            {
                await _permissionService.RevokePermissionFromRoleAsync(roleId, permission.Id);
            }

            TempData["SuccessMessage"] = $"All {grantedPermissions.Count} permissions revoked successfully.";
            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostGrantModuleAsync(string roleId, int module)
        {
            var modulePermissions = await _context.Permissions
                .Where(p => (int)p.Module == module)
                .ToListAsync();
            
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId && (int)rp.Permission!.Module == module)
                .Select(rp => rp.PermissionId)
                .ToHashSetAsync();
            
            // Only add permissions that don't already exist
            foreach (var permission in modulePermissions)
            {
                if (!existingPermissions.Contains(permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permission.Id,
                        IsGranted = true
                    });
                }
                else
                {
                    // Update existing to ensure IsGranted = true
                    var existing = await _context.RolePermissions
                        .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
                    if (existing != null)
                    {
                        existing.IsGranted = true;
                    }
                }
            }

            // Save all changes at once
            await _context.SaveChangesAsync();

            var moduleName = ((PermissionModule)module).ToString();
            TempData["SuccessMessage"] = $"All {moduleName} permissions granted successfully.";
            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostRevokeModuleAsync(string roleId, int module)
        {
            var modulePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId && (int)rp.Permission!.Module == module)
                .ToListAsync();
            
            _context.RolePermissions.RemoveRange(modulePermissions);
            await _context.SaveChangesAsync();

            var moduleName = ((PermissionModule)module).ToString();
            TempData["SuccessMessage"] = $"All {moduleName} permissions revoked successfully.";
            return RedirectToPage(new { id = roleId });
        }
    }
}
