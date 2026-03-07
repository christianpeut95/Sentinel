-- Grant-Admin-154f3bd9.sql
-- Grant all permissions to christian.peut@icloud.com

USE SurveillanceMVP;
GO

DECLARE @UserId NVARCHAR(450);
SET @UserId = '154f3bd9-8ab9-4645-adcf-1f5b7cab39cf';

-- Verify user exists
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @UserId)
BEGIN
    PRINT 'ERROR: User not found!';
    RETURN;
END

-- Show current user
SELECT Email, UserName FROM AspNetUsers WHERE Id = @UserId;

-- Grant all permissions
INSERT INTO UserPermissions (UserId, PermissionId, IsGranted)
SELECT @UserId, Id, 1
FROM Permissions 
WHERE Id NOT IN (
    SELECT PermissionId 
    FROM UserPermissions 
    WHERE UserId = @UserId
);

-- Show result
PRINT 'Granted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' permissions';

-- Show total
SELECT COUNT(*) AS TotalGrantedPermissions
FROM UserPermissions
WHERE UserId = @UserId AND IsGranted = 1;
GO
