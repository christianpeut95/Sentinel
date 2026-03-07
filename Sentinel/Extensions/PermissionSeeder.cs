using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Extensions
{
    public static class PermissionSeeder
    {
        public static async Task SeedPermissionsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
            {
                // Skip migration if there are pending model changes - migration needs to be created first
                return;
            }

            // Get all defined permission combinations
            var definedPermissions = GetDefinedPermissions();

            // Get existing permissions from database
            var existingPermissions = await context.Permissions.ToListAsync();
            var existingCombinations = existingPermissions
                .Select(p => new { p.Module, p.Action })
                .ToHashSet();

            // Find permissions that need to be added (check by Module+Action combination)
            var permissionsToAdd = definedPermissions
                .Where(p => !existingCombinations.Contains(new { p.Module, p.Action }))
                .ToList();

            if (permissionsToAdd.Any())
            {
                await context.Permissions.AddRangeAsync(permissionsToAdd);
                await context.SaveChangesAsync();
            }
        }

        private static List<Permission> GetDefinedPermissions()
        {
            var permissions = new List<Permission>
            {
                // Patient Permissions
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.View,
                    Name = "Patient.View",
                    Description = "View patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Create,
                    Name = "Patient.Create",
                    Description = "Create new patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Edit,
                    Name = "Patient.Edit",
                    Description = "Edit existing patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Delete,
                    Name = "Patient.Delete",
                    Description = "Delete patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Search,
                    Name = "Patient.Search",
                    Description = "Search and filter patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Merge,
                    Name = "Patient.Merge",
                    Description = "Merge duplicate patient records"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Export,
                    Name = "Patient.Export",
                    Description = "Export patient data"
                },
                new Permission
                {
                    Module = PermissionModule.Patient,
                    Action = PermissionAction.Import,
                    Name = "Patient.Import",
                    Description = "Import patient data"
                },

                // Case Permissions
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.View,
                    Name = "Case.View",
                    Description = "View case records"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Create,
                    Name = "Case.Create",
                    Description = "Create new case records"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Edit,
                    Name = "Case.Edit",
                    Description = "Edit existing case records"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Delete,
                    Name = "Case.Delete",
                    Description = "Delete case records"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Search,
                    Name = "Case.Search",
                    Description = "Search and filter case records"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Export,
                    Name = "Case.Export",
                    Description = "Export case data"
                },
                new Permission
                {
                    Module = PermissionModule.Case,
                    Action = PermissionAction.Import,
                    Name = "Case.Import",
                    Description = "Import case data"
                },

                // Settings Permissions
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.View,
                    Name = "Settings.View",
                    Description = "View settings and configuration"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.Edit,
                    Name = "Settings.Edit",
                    Description = "Edit settings and configuration"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.Create,
                    Name = "Settings.Create",
                    Description = "Create new lookup tables and configurations"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.Delete,
                    Name = "Settings.Delete",
                    Description = "Delete lookup tables and configurations"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.Import,
                    Name = "Settings.Import",
                    Description = "Import lookup data (e.g., occupations)"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.ManageCustomFields,
                    Name = "Settings.ManageCustomFields",
                    Description = "View, create, and edit custom fields"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.ManageCustomLookups,
                    Name = "Settings.ManageCustomLookups",
                    Description = "View, create, and edit custom lookup tables"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.ManageSystemLookups,
                    Name = "Settings.ManageSystemLookups",
                    Description = "View, create, and edit system lookup tables (countries, languages, etc.)"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.ManageOrganization,
                    Name = "Settings.ManageOrganization",
                    Description = "Change organization and system settings"
                },
                new Permission
                {
                    Module = PermissionModule.Settings,
                    Action = PermissionAction.ManagePermissions,
                    Name = "Settings.ManagePermissions",
                    Description = "Manage roles, permissions, and disease access control"
                },

                // Audit Permissions
                new Permission
                {
                    Module = PermissionModule.Audit,
                    Action = PermissionAction.View,
                    Name = "Audit.View",
                    Description = "View audit logs and history"
                },
                new Permission
                {
                    Module = PermissionModule.Audit,
                    Action = PermissionAction.Export,
                    Name = "Audit.Export",
                    Description = "Export audit logs"
                },

                // User Permissions
                new Permission
                {
                    Module = PermissionModule.User,
                    Action = PermissionAction.View,
                    Name = "User.View",
                    Description = "View user accounts"
                },
                new Permission
                {
                    Module = PermissionModule.User,
                    Action = PermissionAction.Create,
                    Name = "User.Create",
                    Description = "Create new user accounts"
                },
                new Permission
                {
                    Module = PermissionModule.User,
                    Action = PermissionAction.Edit,
                    Name = "User.Edit",
                    Description = "Edit user accounts"
                },
                new Permission
                {
                    Module = PermissionModule.User,
                    Action = PermissionAction.Delete,
                    Name = "User.Delete",
                    Description = "Delete user accounts"
                },
                new Permission
                {
                    Module = PermissionModule.User,
                    Action = PermissionAction.ManagePermissions,
                    Name = "User.ManagePermissions",
                    Description = "Manage user and role permissions"
                },

                // Report Permissions
                new Permission
                {
                    Module = PermissionModule.Report,
                    Action = PermissionAction.View,
                    Name = "Report.View",
                    Description = "View reports and analytics"
                },
                new Permission
                {
                    Module = PermissionModule.Report,
                    Action = PermissionAction.Export,
                    Name = "Report.Export",
                    Description = "Export reports"
                },

                // Laboratory Permissions
                new Permission
                {
                    Module = PermissionModule.Laboratory,
                    Action = PermissionAction.View,
                    Name = "Laboratory.View",
                    Description = "View laboratory results"
                },
                new Permission
                {
                    Module = PermissionModule.Laboratory,
                    Action = PermissionAction.Create,
                    Name = "Laboratory.Create",
                    Description = "Create laboratory results"
                },
                new Permission
                {
                    Module = PermissionModule.Laboratory,
                    Action = PermissionAction.Edit,
                    Name = "Laboratory.Edit",
                    Description = "Edit laboratory results"
                },
                new Permission
                {
                    Module = PermissionModule.Laboratory,
                    Action = PermissionAction.Delete,
                    Name = "Laboratory.Delete",
                    Description = "Delete laboratory results"
                },

                // Symptom Permissions
                new Permission
                {
                    Module = PermissionModule.Symptom,
                    Action = PermissionAction.View,
                    Name = "Symptom.View",
                    Description = "View symptom data on cases"
                },
                new Permission
                {
                    Module = PermissionModule.Symptom,
                    Action = PermissionAction.Create,
                    Name = "Symptom.Create",
                    Description = "Add symptoms to cases"
                },
                new Permission
                {
                    Module = PermissionModule.Symptom,
                    Action = PermissionAction.Edit,
                    Name = "Symptom.Edit",
                    Description = "Edit symptom data on cases"
                },
                new Permission
                {
                    Module = PermissionModule.Symptom,
                    Action = PermissionAction.Delete,
                    Name = "Symptom.Delete",
                    Description = "Delete symptom data from cases"
                }
            };

            return permissions;
        }
    }
}
