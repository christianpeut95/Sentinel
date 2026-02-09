-- =====================================================================
-- Add Settings.ManagePermissions Permission
-- =====================================================================
-- This script adds the missing Settings.ManagePermissions permission
-- and optionally grants it to a role of your choice
-- =====================================================================

-- Step 1: Insert the new permission
-- Settings Module = 2, ManagePermissions Action = 8
INSERT INTO Permissions (Module, Action, Name, Description)
VALUES (
    2,  -- PermissionModule.Settings
    8,  -- PermissionAction.ManagePermissions
    'Settings.ManagePermissions',
    'Manage roles, permissions, and disease access control'
);

-- Get the ID of the newly inserted permission
DECLARE @PermissionId INT = SCOPE_IDENTITY();

PRINT 'Created Permission ID: ' + CAST(@PermissionId AS VARCHAR(10));

-- =====================================================================
-- Step 2: Grant to a Role (OPTIONAL - Uncomment the section you need)
-- =====================================================================

-- Option A: Grant to ALL roles (use with caution - gives all roles this permission)
-- UNCOMMENT BELOW TO USE:
/*
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
SELECT 
    Id,
    @PermissionId,
    GETUTCDATE()
FROM AspNetRoles;

PRINT 'Granted to ALL roles';
*/

-- =====================================================================
-- Option B: Grant to a specific role by name
-- UNCOMMENT AND EDIT THE ROLE NAME BELOW:
/*
DECLARE @RoleName NVARCHAR(256) = 'Administrator';  -- CHANGE THIS TO YOUR ROLE NAME

INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
SELECT 
    Id,
    @PermissionId,
    GETUTCDATE()
FROM AspNetRoles
WHERE Name = @RoleName;

IF @@ROWCOUNT > 0
    PRINT 'Granted to role: ' + @RoleName;
ELSE
    PRINT 'Role not found: ' + @RoleName;
*/

-- =====================================================================
-- Option C: Grant to a specific role by ID
-- UNCOMMENT AND PASTE THE ROLE ID BELOW:
/*
DECLARE @RoleId NVARCHAR(450) = 'paste-role-id-here';  -- CHANGE THIS TO YOUR ROLE ID

INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
VALUES (@RoleId, @PermissionId, GETUTCDATE());

PRINT 'Granted to role ID: ' + @RoleId;
*/

-- =====================================================================
-- Verification Query - Check if permission was created
-- =====================================================================
SELECT 
    Id,
    Module,
    Action,
    Name,
    Description
FROM Permissions
WHERE Name = 'Settings.ManagePermissions';

-- =====================================================================
-- Helper Query - List all roles to find the one you want to grant to
-- =====================================================================
PRINT '';
PRINT 'Available Roles:';
SELECT Id, Name FROM AspNetRoles ORDER BY Name;

-- =====================================================================
-- Helper Query - Check current role permissions
-- =====================================================================
PRINT '';
PRINT 'Current Role Permission Grants:';
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    p.Description,
    rp.CreatedAt AS GrantedAt
FROM RolePermissions rp
INNER JOIN AspNetRoles r ON rp.RoleId = r.Id
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE p.Module = 2  -- Settings Module
ORDER BY r.Name, p.Name;

-- =====================================================================
-- USAGE INSTRUCTIONS:
-- =====================================================================
-- 1. Run this script as-is to create the permission
-- 2. Note the Permission ID that's created
-- 3. Uncomment ONE of the options (A, B, or C) to grant to a role
-- 4. Edit the role name or ID in the option you chose
-- 5. Run the script again
-- 6. Log out and log back in to refresh permissions
-- =====================================================================
