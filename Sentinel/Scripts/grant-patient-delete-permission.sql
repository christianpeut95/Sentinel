-- Grant Patient.Delete permission to Administrator role
-- Run this script to enable the delete button for administrators

-- Find the Patient.Delete permission ID
DECLARE @PermissionId INT;
SELECT @PermissionId = Id FROM Permissions WHERE Name = 'Patient.Delete';

-- Find the Administrator role ID
DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Administrator';

-- Check if permission is already granted
IF NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE RoleId = @AdminRoleId AND PermissionId = @PermissionId
)
BEGIN
    -- Grant the permission
    INSERT INTO RolePermissions (RoleId, PermissionId)
    VALUES (@AdminRoleId, @PermissionId);
    
    PRINT '? Patient.Delete permission granted to Administrator role';
END
ELSE
BEGIN
    PRINT '?? Patient.Delete permission already granted to Administrator role';
END

-- Verify
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    p.Description
FROM RolePermissions rp
INNER JOIN AspNetRoles r ON rp.RoleId = r.Id
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE p.Name = 'Patient.Delete';
