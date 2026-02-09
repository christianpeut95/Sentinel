-- =====================================================================
-- Fix Symptom Table Column Names - DeletedBy to DeletedByUserId
-- =====================================================================
USE [aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d]
GO

PRINT 'Fixing symptom table column names...';
PRINT '';

-- =====================================================================
-- 1. Fix Symptoms table
-- =====================================================================
PRINT '1. Fixing Symptoms table...';

-- Check if old column exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Symptoms') AND name = 'DeletedBy')
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Symptoms_DeletedBy')
    BEGIN
        ALTER TABLE [Symptoms] DROP CONSTRAINT [FK_Symptoms_DeletedBy];
        PRINT '   - Dropped FK_Symptoms_DeletedBy';
    END

    -- Rename column
    EXEC sp_rename 'Symptoms.DeletedBy', 'DeletedByUserId', 'COLUMN';
    PRINT '   - Renamed DeletedBy to DeletedByUserId';

    -- Recreate foreign key with correct name
    ALTER TABLE [Symptoms] 
        ADD CONSTRAINT [FK_Symptoms_DeletedByUserId] 
        FOREIGN KEY ([DeletedByUserId]) 
        REFERENCES [AspNetUsers]([Id]);
    PRINT '   - Recreated FK_Symptoms_DeletedByUserId';
END
ELSE
BEGIN
    PRINT '   - Column already correct';
END

-- =====================================================================
-- 2. Fix CaseSymptoms table
-- =====================================================================
PRINT '2. Fixing CaseSymptoms table...';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CaseSymptoms') AND name = 'DeletedBy')
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CaseSymptoms_DeletedBy')
    BEGIN
        ALTER TABLE [CaseSymptoms] DROP CONSTRAINT [FK_CaseSymptoms_DeletedBy];
        PRINT '   - Dropped FK_CaseSymptoms_DeletedBy';
    END

    -- Rename column
    EXEC sp_rename 'CaseSymptoms.DeletedBy', 'DeletedByUserId', 'COLUMN';
    PRINT '   - Renamed DeletedBy to DeletedByUserId';

    -- Recreate foreign key with correct name
    ALTER TABLE [CaseSymptoms] 
        ADD CONSTRAINT [FK_CaseSymptoms_DeletedByUserId] 
        FOREIGN KEY ([DeletedByUserId]) 
        REFERENCES [AspNetUsers]([Id]);
    PRINT '   - Recreated FK_CaseSymptoms_DeletedByUserId';
END
ELSE
BEGIN
    PRINT '   - Column already correct';
END

-- =====================================================================
-- 3. Fix DiseaseSymptoms table
-- =====================================================================
PRINT '3. Fixing DiseaseSymptoms table...';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiseaseSymptoms') AND name = 'DeletedBy')
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DiseaseSymptoms_DeletedBy')
    BEGIN
        ALTER TABLE [DiseaseSymptoms] DROP CONSTRAINT [FK_DiseaseSymptoms_DeletedBy];
        PRINT '   - Dropped FK_DiseaseSymptoms_DeletedBy';
    END

    -- Rename column
    EXEC sp_rename 'DiseaseSymptoms.DeletedBy', 'DeletedByUserId', 'COLUMN';
    PRINT '   - Renamed DeletedBy to DeletedByUserId';

    -- Recreate foreign key with correct name
    ALTER TABLE [DiseaseSymptoms] 
        ADD CONSTRAINT [FK_DiseaseSymptoms_DeletedByUserId] 
        FOREIGN KEY ([DeletedByUserId]) 
        REFERENCES [AspNetUsers]([Id]);
    PRINT '   - Recreated FK_DiseaseSymptoms_DeletedByUserId';
END
ELSE
BEGIN
    PRINT '   - Column already correct';
END

PRINT '';
PRINT '? Column name fix completed successfully!';
GO
