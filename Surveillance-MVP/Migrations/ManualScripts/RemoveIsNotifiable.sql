-- Manual migration to remove IsNotifiable column from Diseases table
-- Run this if the automated migration fails due to existing columns

-- Remove IsNotifiable column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Diseases]') AND name = 'IsNotifiable')
BEGIN
    ALTER TABLE [Diseases] DROP COLUMN [IsNotifiable];
    PRINT 'Successfully removed IsNotifiable column from Diseases table';
END
ELSE
BEGIN
    PRINT 'IsNotifiable column does not exist in Diseases table - skipping';
END
GO
