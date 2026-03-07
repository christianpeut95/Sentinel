-- ===============================================
-- Backup History Table Migration
-- ===============================================
-- Tracks all database backups for auditing and restore

CREATE TABLE BackupHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BackupType NVARCHAR(50) NOT NULL, -- 'Full', 'DataOnly', 'ConfigOnly'
    BackupFileName NVARCHAR(500) NOT NULL,
    BackupFilePath NVARCHAR(1000) NOT NULL,
    SizeInBytes BIGINT NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NOT NULL,
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedBy NVARCHAR(256) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    INDEX IX_BackupHistory_StartTime (StartTime DESC),
    INDEX IX_BackupHistory_BackupType (BackupType),
    INDEX IX_BackupHistory_Success (Success)
);

-- Add comments
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Stores history of database backups for auditing and restore purposes' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'BackupHistory';

GO

-- Seed initial data (optional)
INSERT INTO BackupHistory (BackupType, BackupFileName, BackupFilePath, SizeInBytes, StartTime, EndTime, Success, CreatedBy)
VALUES ('Full', 'InitialSetup.bak', 'C:\DatabaseBackups\InitialSetup.bak', 0, GETDATE(), GETDATE(), 1, 'System');

GO
