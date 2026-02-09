-- ========================================================================
-- Cleanup Task Management Tables
-- ========================================================================
-- This script removes task management tables if they exist
-- Run this ONLY if the AddTaskManagementSystem migration failed
-- ========================================================================

-- Check what exists
PRINT 'Checking for existing task tables...';

IF OBJECT_ID('DiseaseTaskTemplates', 'U') IS NOT NULL
    PRINT '  - DiseaseTaskTemplates exists';

IF OBJECT_ID('CaseTasks', 'U') IS NOT NULL
    PRINT '  - CaseTasks exists';

IF OBJECT_ID('TaskTemplates', 'U') IS NOT NULL
    PRINT '  - TaskTemplates exists';

-- Drop tables in correct order (respecting foreign keys)
PRINT 'Dropping task tables...';

IF OBJECT_ID('DiseaseTaskTemplates', 'U') IS NOT NULL
BEGIN
    DROP TABLE DiseaseTaskTemplates;
    PRINT '  ? Dropped DiseaseTaskTemplates';
END

IF OBJECT_ID('CaseTasks', 'U') IS NOT NULL
BEGIN
    DROP TABLE CaseTasks;
    PRINT '  ? Dropped CaseTasks';
END

IF OBJECT_ID('TaskTemplates', 'U') IS NOT NULL
BEGIN
    DROP TABLE TaskTemplates;
    PRINT '  ? Dropped TaskTemplates';
END

PRINT 'Cleanup complete. Now run: dotnet ef database update';
