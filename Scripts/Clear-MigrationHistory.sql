-- Clear-MigrationHistory.sql
-- This script clears the __EFMigrationsHistory table if it exists

USE [aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d];
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    TRUNCATE TABLE [__EFMigrationsHistory];
    PRINT 'Migration history cleared';
END
ELSE
BEGIN
    PRINT 'No migration history table found';
END
GO
