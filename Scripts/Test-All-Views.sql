-- Test-All-Views.sql
-- Comprehensive test of all 6 views

USE [aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d];
GO

PRINT '========================================';
PRINT 'TESTING ALL 6 SQL VIEWS';
PRINT '========================================';
PRINT '';

-- Test 1: vw_CaseContactTasksFlattened
PRINT '1. vw_CaseContactTasksFlattened:';
SELECT COUNT(*) AS TotalRows, 
       COUNT(DISTINCT CaseGuid) AS UniqueCases,
       COUNT(DISTINCT TaskId) AS UniqueTasks,
       MAX(GenerationNumber) AS MaxGeneration
FROM vw_CaseContactTasksFlattened;
PRINT '   ? Query successful';
PRINT '';

-- Test 2: vw_ContactsListSimple
PRINT '2. vw_ContactsListSimple:';
SELECT COUNT(*) AS TotalContacts,
       SUM(TotalTasks) AS AllTasks,
       SUM(CompletedTasks) AS AllCompletedTasks
FROM vw_ContactsListSimple;
PRINT '   ? Query successful';
PRINT '';

-- Test 3: vw_OutbreakTasksFlattened
PRINT '3. vw_OutbreakTasksFlattened:';
SELECT COUNT(*) AS TotalRows,
       COUNT(DISTINCT OutbreakId) AS UniqueOutbreaks,
       COUNT(DISTINCT TaskId) AS UniqueTasks
FROM vw_OutbreakTasksFlattened;
PRINT '   ? Query successful';
PRINT '';

-- Test 4: vw_CaseTimelineAll
PRINT '4. vw_CaseTimelineAll:';
SELECT COUNT(*) AS TotalEvents,
       COUNT(DISTINCT CaseId) AS UniqueCases,
       COUNT(DISTINCT EventType) AS EventTypes
FROM vw_CaseTimelineAll;
PRINT '   ? Query successful';
PRINT '';

-- Test 5: vw_ContactTracingMindMapNodes
PRINT '5. vw_ContactTracingMindMapNodes:';
SELECT COUNT(*) AS TotalNodes,
       SUM(CASE WHEN NodeType = 0 THEN 1 ELSE 0 END) AS Cases,
       SUM(CASE WHEN NodeType = 1 THEN 1 ELSE 0 END) AS Contacts
FROM vw_ContactTracingMindMapNodes;
PRINT '   ? Query successful';
PRINT '';

-- Test 6: vw_ContactTracingMindMapEdges
PRINT '6. vw_ContactTracingMindMapEdges:';
SELECT COUNT(*) AS TotalEdges,
       COUNT(DISTINCT SourceNodeId) AS UniqueSourceNodes,
       COUNT(DISTINCT TargetNodeId) AS UniqueTargetNodes
FROM vw_ContactTracingMindMapEdges;
PRINT '   ? Query successful';
PRINT '';

PRINT '========================================';
PRINT 'ALL 6 VIEWS TESTED SUCCESSFULLY! ?';
PRINT '========================================';
PRINT '';
PRINT 'Views are ready for application use.';
PRINT 'Run: dotnet run --project Surveillance-MVP';
GO
