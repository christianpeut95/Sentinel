-- Manual SQL script to seed HL7.GenerateTestFiles permission
-- Run this if you need to add the permission without restarting the app

-- 1. First, get the HL7 module and GenerateTestFiles action enum values
-- HL7 = 9 (based on order in enum)
-- GenerateTestFiles = 18 (based on order in enum)

-- 2. Check if permission already exists
IF NOT EXISTS (
    SELECT 1 FROM Permissions 
    WHERE Module = 9 AND Action = 18
)
BEGIN
    -- Insert the new permission
    INSERT INTO Permissions (Module, Action, Name, Description)
    VALUES (9, 18, 'HL7.GenerateTestFiles', 'Generate HL7 test files for validation and testing');

    PRINT 'Created permission: HL7.GenerateTestFiles';
END
ELSE
BEGIN
    PRINT 'Permission HL7.GenerateTestFiles already exists';
END

-- 3. Assign to Admin role
DECLARE @PermissionId INT = (SELECT Id FROM Permissions WHERE Module = 9 AND Action = 18);
DECLARE @AdminRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin');

IF @AdminRoleId IS NOT NULL AND @PermissionId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @AdminRoleId AND PermissionId = @PermissionId
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
        VALUES (@AdminRoleId, @PermissionId, 1);

        PRINT 'Assigned HL7.GenerateTestFiles permission to Admin role';
    END
    ELSE
    BEGIN
        PRINT 'Admin role already has HL7.GenerateTestFiles permission';
    END
END

-- 4. Assign to Surveillance Manager role
DECLARE @ManagerRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Surveillance Manager');

IF @ManagerRoleId IS NOT NULL AND @PermissionId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @ManagerRoleId AND PermissionId = @PermissionId
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
        VALUES (@ManagerRoleId, @PermissionId, 1);

        PRINT 'Assigned HL7.GenerateTestFiles permission to Surveillance Manager role';
    END
    ELSE
    BEGIN
        PRINT 'Surveillance Manager role already has HL7.GenerateTestFiles permission';
    END
END

-- 5. Also add other HL7 permissions for completeness (Configure, Process, View, etc.)
-- These will be created automatically by the seeding service, but we'll ensure the critical ones exist

-- HL7.View (Module=9, Action=0)
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module = 9 AND Action = 0)
BEGIN
    INSERT INTO Permissions (Module, Action, Name, Description)
    VALUES (9, 0, 'HL7.View', 'View HL7 messages and configurations');
END

-- HL7.Configure (Module=9, Action=17)
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module = 9 AND Action = 17)
BEGIN
    INSERT INTO Permissions (Module, Action, Name, Description)
    VALUES (9, 17, 'HL7.Configure', 'Configure HL7 integrations');
END

-- Assign HL7.View and HL7.Configure to Admin and Surveillance Manager
DECLARE @ViewPermId INT = (SELECT Id FROM Permissions WHERE Module = 9 AND Action = 0);
DECLARE @ConfigPermId INT = (SELECT Id FROM Permissions WHERE Module = 9 AND Action = 17);

-- Admin gets all HL7 permissions
IF @AdminRoleId IS NOT NULL
BEGIN
    IF @ViewPermId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @AdminRoleId AND PermissionId = @ViewPermId)
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@AdminRoleId, @ViewPermId, 1);
    END

    IF @ConfigPermId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @AdminRoleId AND PermissionId = @ConfigPermId)
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@AdminRoleId, @ConfigPermId, 1);
    END
END

-- Surveillance Manager gets all HL7 permissions
IF @ManagerRoleId IS NOT NULL
BEGIN
    IF @ViewPermId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @ManagerRoleId AND PermissionId = @ViewPermId)
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@ManagerRoleId, @ViewPermId, 1);
    END

    IF @ConfigPermId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @ManagerRoleId AND PermissionId = @ConfigPermId)
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted) VALUES (@ManagerRoleId, @ConfigPermId, 1);
    END
END

PRINT 'HL7 permissions seeded successfully!';
