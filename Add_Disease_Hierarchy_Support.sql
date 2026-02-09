-- =====================================================================
-- Add Cascading/Inheritance Support to Disease Access Control
-- =====================================================================
-- This migration adds support for hierarchical permission inheritance
-- Run this script against your database
-- =====================================================================

-- Add ApplyToChildren and InheritedFromDiseaseId to RoleDiseaseAccess
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RoleDiseaseAccess]') AND name = 'ApplyToChildren')
BEGIN
    ALTER TABLE [dbo].[RoleDiseaseAccess]
    ADD [ApplyToChildren] BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added ApplyToChildren to RoleDiseaseAccess';
END
ELSE
    PRINT 'ApplyToChildren already exists in RoleDiseaseAccess';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RoleDiseaseAccess]') AND name = 'InheritedFromDiseaseId')
BEGIN
    ALTER TABLE [dbo].[RoleDiseaseAccess]
    ADD [InheritedFromDiseaseId] UNIQUEIDENTIFIER NULL;
    
    PRINT 'Added InheritedFromDiseaseId to RoleDiseaseAccess';
END
ELSE
    PRINT 'InheritedFromDiseaseId already exists in RoleDiseaseAccess';

-- Add ApplyToChildren and InheritedFromDiseaseId to UserDiseaseAccess
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserDiseaseAccess]') AND name = 'ApplyToChildren')
BEGIN
    ALTER TABLE [dbo].[UserDiseaseAccess]
    ADD [ApplyToChildren] BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added ApplyToChildren to UserDiseaseAccess';
END
ELSE
    PRINT 'ApplyToChildren already exists in UserDiseaseAccess';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserDiseaseAccess]') AND name = 'InheritedFromDiseaseId')
BEGIN
    ALTER TABLE [dbo].[UserDiseaseAccess]
    ADD [InheritedFromDiseaseId] UNIQUEIDENTIFIER NULL;
    
    PRINT 'InheritedFromDiseaseId to UserDiseaseAccess';
END
ELSE
    PRINT 'InheritedFromDiseaseId already exists in UserDiseaseAccess';

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RoleDiseaseAccess_InheritedFrom' AND object_id = OBJECT_ID('RoleDiseaseAccess'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RoleDiseaseAccess_InheritedFrom
    ON RoleDiseaseAccess (InheritedFromDiseaseId)
    WHERE InheritedFromDiseaseId IS NOT NULL;
    
    PRINT 'Created index IX_RoleDiseaseAccess_InheritedFrom';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDiseaseAccess_InheritedFrom' AND object_id = OBJECT_ID('UserDiseaseAccess'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserDiseaseAccess_InheritedFrom
    ON UserDiseaseAccess (InheritedFromDiseaseId)
    WHERE InheritedFromDiseaseId IS NOT NULL;
    
    PRINT 'Created index IX_UserDiseaseAccess_InheritedFrom';
END

-- Verification queries
PRINT '';
PRINT 'Verification - RoleDiseaseAccess columns:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('RoleDiseaseAccess')
  AND c.name IN ('ApplyToChildren', 'InheritedFromDiseaseId')
ORDER BY c.name;

PRINT '';
PRINT 'Verification - UserDiseaseAccess columns:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('UserDiseaseAccess')
  AND c.name IN ('ApplyToChildren', 'InheritedFromDiseaseId')
ORDER BY c.name;

PRINT '';
PRINT 'Migration completed successfully!';
