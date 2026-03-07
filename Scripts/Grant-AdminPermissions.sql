-- Grant-AdminPermissions.sql
-- Quick fix to grant all permissions to a user

USE [SurveillanceMVP];
GO

DECLARE @UserId NVARCHAR(450);
DECLARE @Email NVARCHAR(256) = 'christian.peut@icloud.com'; -- Change this to your email

-- Get user ID
SELECT @UserId = Id FROM AspNetUsers WHERE Email = @Email;

IF @UserId IS NULL
BEGIN
    PRINT 'ERROR: User not found with email: ' + @Email;
END
ELSE
BEGIN
    -- Grant all permissions
    INSERT INTO UserPermissions (UserId, PermissionId)
    SELECT @UserId, Id 
    FROM Permissions 
    WHERE Id NOT IN (SELECT PermissionId FROM UserPermissions WHERE UserId = @UserId);
    
    PRINT 'SUCCESS: Granted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions to ' + @Email;
    
    -- Show total permissions
    SELECT COUNT(*) AS TotalPermissions
    FROM UserPermissions
    WHERE UserId = @UserId;
END
GO
