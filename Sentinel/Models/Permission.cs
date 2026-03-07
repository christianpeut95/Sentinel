namespace Sentinel.Models
{
    public enum PermissionModule
    {
        Patient,
        Case,
        Settings,
        Audit,
        User,
        Report,
        Laboratory,
        Symptom,
        Task,
        Outbreak,
        Survey,
        Location,
        Event,
        Exposure
    }

    public enum PermissionAction
    {
        View,
        Create,
        Edit,
        Delete,
        Search,
        Merge,
        Export,
        Import,
        Complete,
        ManagePermissions,
        ManageCustomFields,
        ManageCustomLookups,
        ManageSystemLookups,
        ManageOrganization,
        ResetPassword
    }

    public class Permission
    {
        public int Id { get; set; }
        public PermissionModule Module { get; set; }
        public PermissionAction Action { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<RolePermission> RolePermissions { get; set; } = new();
        public List<UserPermission> UserPermissions { get; set; } = new();

        public string GetPermissionKey()
        {
            return $"{Module}.{Action}";
        }
    }
}
