-- Fix IsNotifiable Issue
-- This script ensures the IsNotifiable column is removed if it exists
-- and prevents errors if it doesn't exist

USE [SurveillanceMVP]; -- Update this to your actual database name
GO

-- Check if IsNotifiable column exists and remove it
IF EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Diseases]') 
    AND name = 'IsNotifiable'
)
BEGIN
    PRINT 'IsNotifiable column exists - removing it...';
    
    -- Drop any constraints on the column first
    DECLARE @sql NVARCHAR(MAX) = '';
    
    SELECT @sql = @sql + 'ALTER TABLE [Diseases] DROP CONSTRAINT ' + QUOTENAME(dc.name) + ';'
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[Diseases]') AND c.name = 'IsNotifiable';
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql;
        PRINT 'Dropped default constraints on IsNotifiable column';
    END
    
    -- Now drop the column
    ALTER TABLE [Diseases] DROP COLUMN [IsNotifiable];
    PRINT 'Successfully removed IsNotifiable column from Diseases table';
END
ELSE
BEGIN
    PRINT 'IsNotifiable column does not exist - no action needed';
END
GO

-- Verify the fix
IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Diseases]') 
    AND name = 'IsNotifiable'
)
BEGIN
    PRINT 'VERIFICATION: IsNotifiable column is not in Diseases table - OK';
END
ELSE
BEGIN
    PRINT 'ERROR: IsNotifiable column still exists in Diseases table';
END
GO
