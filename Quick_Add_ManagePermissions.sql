-- =====================================================================
-- QUICK ADD: Settings.ManagePermissions Permission
-- =====================================================================
-- This is a simplified version for quick execution
-- =====================================================================

-- 1. Add the permission
INSERT INTO Permissions (Module, Action, Name, Description)
VALUES (2, 8, 'Settings.ManagePermissions', 'Manage roles, permissions, and disease access control');

-- 2. Find your role (run this to see available roles)
SELECT Id, Name FROM AspNetRoles;

-- 3. Grant to your role (REPLACE 'your-role-id-here' with actual role ID from step 2)
/*
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
VALUES (
    'your-role-id-here',  -- Replace with actual role ID
    (SELECT Id FROM Permissions WHERE Name = 'Settings.ManagePermissions'),
    GETUTCDATE()
);
*/

-- =====================================================================
-- ALTERNATIVE: Grant by role name instead of ID
-- =====================================================================
/*
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
SELECT 
    r.Id,
    p.Id,
    GETUTCDATE()
FROM AspNetRoles r
CROSS JOIN Permissions p
WHERE r.Name = 'Administrator'  -- CHANGE THIS TO YOUR ROLE NAME
  AND p.Name = 'Settings.ManagePermissions';
*/

-- Verify it worked
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    rp.CreatedAt
FROM RolePermissions rp
JOIN AspNetRoles r ON rp.RoleId = r.Id
JOIN Permissions p ON rp.PermissionId = p.Id
WHERE p.Name = 'Settings.ManagePermissions';
