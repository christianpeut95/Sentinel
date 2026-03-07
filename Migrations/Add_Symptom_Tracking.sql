-- =====================================================================
-- Add Symptom Tracking System
-- =====================================================================
-- This migration adds support for tracking symptoms associated with cases
-- Includes: Symptoms lookup, CaseSymptoms junction, DiseaseSymptoms association
-- =====================================================================

USE [SurveillanceMVP]
GO

PRINT 'Starting Symptom Tracking Migration...';
PRINT '';

-- =====================================================================
-- 1. Create Symptoms Lookup Table
-- =====================================================================
PRINT '1. Creating Symptoms table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Symptoms')
BEGIN
    CREATE TABLE [dbo].[Symptoms] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(50) NULL,
        [ExportCode] NVARCHAR(50) NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(450) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(450) NULL,
        
        -- Soft delete
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(450) NULL,
        
        CONSTRAINT [PK_Symptoms] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Symptoms_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_Symptoms_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_Symptoms_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [AspNetUsers]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Symptoms_IsDeleted_IsActive] 
        ON [dbo].[Symptoms]([IsDeleted] ASC, [IsActive] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_Symptoms_Code] 
        ON [dbo].[Symptoms]([Code] ASC) 
        WHERE [Code] IS NOT NULL;
    
    CREATE NONCLUSTERED INDEX [IX_Symptoms_SortOrder] 
        ON [dbo].[Symptoms]([SortOrder] ASC) 
        WHERE [IsDeleted] = 0;

    PRINT '   ? Symptoms table created';
END
ELSE
BEGIN
    PRINT '   ? Symptoms table already exists';
END

GO

-- =====================================================================
-- 2. Create CaseSymptoms Junction Table
-- =====================================================================
PRINT '2. Creating CaseSymptoms junction table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CaseSymptoms')
BEGIN
    CREATE TABLE [dbo].[CaseSymptoms] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CaseId] UNIQUEIDENTIFIER NOT NULL,
        [SymptomId] INT NOT NULL,
        [OnsetDate] DATETIME2 NULL,
        [Severity] NVARCHAR(20) NULL,
        [Notes] NVARCHAR(1000) NULL,
        [OtherSymptomText] NVARCHAR(200) NULL,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(450) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(450) NULL,
        
        -- Soft delete
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(450) NULL,
        
        CONSTRAINT [PK_CaseSymptoms] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CaseSymptoms_Cases] FOREIGN KEY ([CaseId]) REFERENCES [Cases]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseSymptoms_Symptoms] FOREIGN KEY ([SymptomId]) REFERENCES [Symptoms]([Id]),
        CONSTRAINT [FK_CaseSymptoms_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_CaseSymptoms_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_CaseSymptoms_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [AspNetUsers]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_CaseSymptoms_CaseId] 
        ON [dbo].[CaseSymptoms]([CaseId] ASC) 
        WHERE [IsDeleted] = 0;
    
    CREATE NONCLUSTERED INDEX [IX_CaseSymptoms_SymptomId] 
        ON [dbo].[CaseSymptoms]([SymptomId] ASC) 
        WHERE [IsDeleted] = 0;
    
    CREATE NONCLUSTERED INDEX [IX_CaseSymptoms_OnsetDate] 
        ON [dbo].[CaseSymptoms]([OnsetDate] ASC) 
        WHERE [IsDeleted] = 0;

    PRINT '   ? CaseSymptoms table created';
END
ELSE
BEGIN
    PRINT '   ? CaseSymptoms table already exists';
END

GO

-- =====================================================================
-- 3. Create DiseaseSymptoms Association Table
-- =====================================================================
PRINT '3. Creating DiseaseSymptoms association table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DiseaseSymptoms')
BEGIN
    CREATE TABLE [dbo].[DiseaseSymptoms] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DiseaseId] UNIQUEIDENTIFIER NOT NULL,
        [SymptomId] INT NOT NULL,
        [IsCommon] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(450) NULL,
        [UpdatedAt] DATETIME2 NULL,
        [UpdatedBy] NVARCHAR(450) NULL,
        
        -- Soft delete
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(450) NULL,
        
        CONSTRAINT [PK_DiseaseSymptoms] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DiseaseSymptoms_Diseases] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiseaseSymptoms_Symptoms] FOREIGN KEY ([SymptomId]) REFERENCES [Symptoms]([Id]),
        CONSTRAINT [FK_DiseaseSymptoms_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_DiseaseSymptoms_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_DiseaseSymptoms_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [AspNetUsers]([Id])
    );

    -- Ensure unique disease-symptom combinations
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DiseaseSymptoms_DiseaseId_SymptomId] 
        ON [dbo].[DiseaseSymptoms]([DiseaseId] ASC, [SymptomId] ASC) 
        WHERE [IsDeleted] = 0;
    
    CREATE NONCLUSTERED INDEX [IX_DiseaseSymptoms_SymptomId] 
        ON [dbo].[DiseaseSymptoms]([SymptomId] ASC) 
        WHERE [IsDeleted] = 0;

    PRINT '   ? DiseaseSymptoms table created';
END
ELSE
BEGIN
    PRINT '   ? DiseaseSymptoms table already exists';
END

GO

-- =====================================================================
-- 4. Seed Common Symptoms
-- =====================================================================
PRINT '4. Seeding common symptoms...';

IF NOT EXISTS (SELECT * FROM [Symptoms] WHERE [Code] = 'FEVER')
BEGIN
    SET IDENTITY_INSERT [Symptoms] OFF;
    
    INSERT INTO [Symptoms] ([Name], [Code], [ExportCode], [SortOrder], [IsActive]) VALUES
    ('Fever', 'FEVER', 'SYM001', 10, 1),
    ('Chills', 'CHILLS', 'SYM002', 20, 1),
    ('Cough', 'COUGH', 'SYM003', 30, 1),
    ('Shortness of Breath', 'SOB', 'SYM004', 40, 1),
    ('Fatigue', 'FATIGUE', 'SYM005', 50, 1),
    ('Muscle or Body Aches', 'MYALGIA', 'SYM006', 60, 1),
    ('Headache', 'HEADACHE', 'SYM007', 70, 1),
    ('Loss of Taste or Smell', 'ANOSMIA', 'SYM008', 80, 1),
    ('Sore Throat', 'SORE_THROAT', 'SYM009', 90, 1),
    ('Congestion or Runny Nose', 'CONGESTION', 'SYM010', 100, 1),
    ('Nausea or Vomiting', 'NAUSEA', 'SYM011', 110, 1),
    ('Diarrhea', 'DIARRHEA', 'SYM012', 120, 1),
    ('Abdominal Pain', 'ABD_PAIN', 'SYM013', 130, 1),
    ('Rash', 'RASH', 'SYM014', 140, 1),
    ('Joint Pain', 'ARTHRALGIA', 'SYM015', 150, 1),
    ('Confusion', 'CONFUSION', 'SYM016', 160, 1),
    ('Seizures', 'SEIZURE', 'SYM017', 170, 1),
    ('Jaundice', 'JAUNDICE', 'SYM018', 180, 1),
    ('Bleeding', 'BLEEDING', 'SYM019', 190, 1),
    ('Swollen Lymph Nodes', 'LYMPH_NODES', 'SYM020', 200, 1),
    ('Night Sweats', 'NIGHT_SWEATS', 'SYM021', 210, 1),
    ('Weight Loss', 'WEIGHT_LOSS', 'SYM022', 220, 1),
    ('Difficulty Swallowing', 'DYSPHAGIA', 'SYM023', 230, 1),
    ('Vision Changes', 'VISION_CHG', 'SYM024', 240, 1),
    ('Hearing Loss', 'HEARING_LOSS', 'SYM025', 250, 1),
    ('Other', 'OTHER', 'SYM999', 999, 1);

    DECLARE @RowCount INT = @@ROWCOUNT;
    PRINT '   ? Inserted ' + CAST(@RowCount AS NVARCHAR) + ' common symptoms';
END
ELSE
BEGIN
    PRINT '   ? Symptoms already seeded';
END

GO

-- =====================================================================
-- 5. Verification
-- =====================================================================
PRINT '';
PRINT '5. Verifying migration...';

DECLARE @SymptomCount INT;
DECLARE @TablesExist INT = 0;

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Symptoms')
    SET @TablesExist = @TablesExist + 1;
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CaseSymptoms')
    SET @TablesExist = @TablesExist + 1;
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DiseaseSymptoms')
    SET @TablesExist = @TablesExist + 1;

SELECT @SymptomCount = COUNT(*) FROM [Symptoms] WHERE [IsDeleted] = 0;

PRINT '   Tables created: ' + CAST(@TablesExist AS NVARCHAR) + '/3';
PRINT '   Active symptoms: ' + CAST(@SymptomCount AS NVARCHAR);

IF @TablesExist = 3 AND @SymptomCount > 0
BEGIN
    PRINT '';
    PRINT '? Symptom Tracking Migration completed successfully!';
END
ELSE
BEGIN
    PRINT '';
    PRINT '? Warning: Migration may be incomplete. Please review.';
END

GO

PRINT '';
PRINT '=====================================================================';
PRINT 'Next Steps:';
PRINT '1. Update Entity Framework models (Symptom, CaseSymptom, DiseaseSymptom)';
PRINT '2. Update ApplicationDbContext with new DbSets';
PRINT '3. Create symptom management pages in Settings/Lookups';
PRINT '4. Update Case Details/Edit pages to include symptom tracking';
PRINT '5. Optional: Associate common symptoms with diseases';
PRINT '=====================================================================';
