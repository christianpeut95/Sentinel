using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Data;

namespace Sentinel.Pages.Settings.Roles
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class GrantPermissionsDirectModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public GrantPermissionsDirectModel(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public string RoleId { get; set; } = "";
        public string RoleName { get; set; } = "";
        public List<Permission> AllPermissions { get; set; } = new();
        public List<int> GrantedPermissionIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            RoleId = id;
            RoleName = role.Name ?? "Unknown";

            AllPermissions = await _context.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();

            // Get granted permissions directly from database
            GrantedPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id && rp.IsGranted)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostGrantDirectAsync(string roleId, int permissionId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if exists
                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId", 
                        connection);
                    checkCmd.Parameters.AddWithValue("@RoleId", roleId);
                    checkCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                    
                    var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                    if (exists)
                    {
                        // Update
                        var updateCmd = new SqlCommand(
                            "UPDATE RolePermissions SET IsGranted = 1 WHERE RoleId = @RoleId AND PermissionId = @PermissionId", 
                            connection);
                        updateCmd.Parameters.AddWithValue("@RoleId", roleId);
                        updateCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Insert
                        var insertCmd = new SqlCommand(
                            "INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@RoleId, @PermissionId, 1)", 
                            connection);
                        insertCmd.Parameters.AddWithValue("@RoleId", roleId);
                        insertCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = $"Permission {permissionId} granted via direct SQL successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostGrantAllModuleDirectAsync(string roleId, int module)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get all permission IDs for this module
                    var getPermissionsCmd = new SqlCommand(
                        "SELECT Id FROM Permissions WHERE Module = @Module", 
                        connection);
                    getPermissionsCmd.Parameters.AddWithValue("@Module", module);

                    var permissionIds = new List<int>();
                    using (var reader = await getPermissionsCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            permissionIds.Add(reader.GetInt32(0));
                        }
                    }

                    // Grant each permission
                    foreach (var permissionId in permissionIds)
                    {
                        // Delete existing if any
                        var deleteCmd = new SqlCommand(
                            "DELETE FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId", 
                            connection);
                        deleteCmd.Parameters.AddWithValue("@RoleId", roleId);
                        deleteCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                        await deleteCmd.ExecuteNonQueryAsync();

                        // Insert new
                        var insertCmd = new SqlCommand(
                            "INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@RoleId, @PermissionId, 1)", 
                            connection);
                        insertCmd.Parameters.AddWithValue("@RoleId", roleId);
                        insertCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    var moduleName = ((PermissionModule)module).ToString();
                    TempData["SuccessMessage"] = $"All {moduleName} permissions ({permissionIds.Count}) granted via direct SQL!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage(new { id = roleId });
        }

        public async Task<IActionResult> OnPostRevokeDirectAsync(string roleId, int permissionId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var deleteCmd = new SqlCommand(
                        "DELETE FROM RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId", 
                        connection);
                    deleteCmd.Parameters.AddWithValue("@RoleId", roleId);
                    deleteCmd.Parameters.AddWithValue("@PermissionId", permissionId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                TempData["SuccessMessage"] = $"Permission {permissionId} revoked via direct SQL successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage(new { id = roleId });
        }
    }
}
