-- Migration: Add Disease Access Control System
-- Date: $(Get-Date)
-- Description: Adds disease-based access control with RoleDiseaseAccess and UserDiseaseAccess tables

-- Step 1: Add AccessLevel column to Diseases table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Diseases]') AND name = 'AccessLevel')
BEGIN
    ALTER TABLE [dbo].[Diseases]
    ADD [AccessLevel] INT NOT NULL DEFAULT 0;
    
    PRINT 'Added AccessLevel column to Diseases table';
END
GO

-- Step 2: Create RoleDiseaseAccess table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleDiseaseAccess')
BEGIN
    CREATE TABLE [dbo].[RoleDiseaseAccess] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [RoleId] NVARCHAR(450) NOT NULL,
        [DiseaseId] UNIQUEIDENTIFIER NOT NULL,
        [IsAllowed] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedByUserId] NVARCHAR(450) NULL,
        CONSTRAINT [PK_RoleDiseaseAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleDiseaseAccess_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) 
            REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleDiseaseAccess_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) 
            REFERENCES [dbo].[Diseases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleDiseaseAccess_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX [IX_RoleDiseaseAccess_RoleId_DiseaseId] 
        ON [dbo].[RoleDiseaseAccess] ([RoleId], [DiseaseId]);

    PRINT 'Created RoleDiseaseAccess table';
END
GO

-- Step 3: Create UserDiseaseAccess table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserDiseaseAccess')
BEGIN
    CREATE TABLE [dbo].[UserDiseaseAccess] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [DiseaseId] UNIQUEIDENTIFIER NOT NULL,
        [IsAllowed] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2(7) NULL,
        [GrantedByUserId] NVARCHAR(450) NULL,
        [Reason] NVARCHAR(500) NULL,
        CONSTRAINT [PK_UserDiseaseAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserDiseaseAccess_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserDiseaseAccess_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) 
            REFERENCES [dbo].[Diseases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDiseaseAccess_AspNetUsers_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX [IX_UserDiseaseAccess_UserId_DiseaseId] 
        ON [dbo].[UserDiseaseAccess] ([UserId], [DiseaseId]);

    CREATE INDEX [IX_UserDiseaseAccess_ExpiresAt] 
        ON [dbo].[UserDiseaseAccess] ([ExpiresAt]);

    PRINT 'Created UserDiseaseAccess table';
END
GO

-- Step 4: Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Diseases_AccessLevel')
BEGIN
    CREATE INDEX [IX_Diseases_AccessLevel] ON [dbo].[Diseases] ([AccessLevel]);
    PRINT 'Created index on Diseases.AccessLevel';
END
GO

PRINT 'Disease Access Control migration completed successfully!';
