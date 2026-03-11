-- Manually recreate reporting views to ensure they match the migration
-- Run this in SQL Server Management Studio or Azure Data Studio

-- Drop existing views
IF OBJECT_ID('vw_ContactsListSimple', 'V') IS NOT NULL DROP VIEW vw_ContactsListSimple;
IF OBJECT_ID('vw_CaseContactTasksFlattened', 'V') IS NOT NULL DROP VIEW vw_CaseContactTasksFlattened;
IF OBJECT_ID('vw_OutbreakTasksFlattened', 'V') IS NOT NULL DROP VIEW vw_OutbreakTasksFlattened;
IF OBJECT_ID('vw_CaseTimelineAll', 'V') IS NOT NULL DROP VIEW vw_CaseTimelineAll;
IF OBJECT_ID('vw_ContactTracingMindMapNodes', 'V') IS NOT NULL DROP VIEW vw_ContactTracingMindMapNodes;
IF OBJECT_ID('vw_ContactTracingMindMapEdges', 'V') IS NOT NULL DROP VIEW vw_ContactTracingMindMapEdges;
GO

-- Create vw_ContactsListSimple
CREATE VIEW vw_ContactsListSimple AS
SELECT 
    c.Id AS ContactId,
    c.FriendlyId AS ContactNumber,
    c.DateOfOnset AS DateIdentified,
    c.DateOfOnset AS ContactDateOfOnset,
    p.Id AS PatientId,
    CONCAT(p.GivenName, ' ', p.FamilyName) AS ContactName,
    p.GivenName AS ContactFirstName,
    p.FamilyName AS ContactLastName,
    p.DateOfBirth AS ContactDOB,
    p.MobilePhone AS ContactMobile,
    p.EmailAddress AS ContactEmail,
    p.City AS ContactSuburb,
    p.State AS ContactState,
    d.Name AS DiseaseName,
    'Contact' AS ExposureType,
    '' AS ExposureSourceName,
    0 AS TotalTasks,
    0 AS CompletedTasks,
    GETDATE() AS CreatedAt,
    GETDATE() AS UpdatedAt
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
WHERE c.Type = 1;
GO

-- Create vw_CaseContactTasksFlattened
CREATE VIEW vw_CaseContactTasksFlattened AS
SELECT 
    c.Id AS CaseGuid,
    c.FriendlyId AS CaseNumber,
    c.Type AS CaseTypeEnum,
    CASE c.Type WHEN 0 THEN 'Case' WHEN 1 THEN 'Contact' ELSE 'Unknown' END AS CaseType,
    0 AS GenerationNumber,
    c.FriendlyId AS TransmissionChainPath,
    '' AS TransmittedByCase,
    c.DateOfOnset,
    c.DateOfNotification,
    cs.Name AS CaseStatus,
    p.Id AS PatientId,
    CONCAT(p.GivenName, ' ', p.FamilyName) AS PatientName,
    p.GivenName AS PatientFirstName,
    p.FamilyName AS PatientLastName,
    p.DateOfBirth AS PatientDOB,
    DATEDIFF(YEAR, p.DateOfBirth, COALESCE(c.DateOfOnset, GETDATE())) AS AgeAtOnset,
    p.City AS PatientSuburb,
    p.State AS PatientState,
    p.MobilePhone AS PatientMobile,
    p.EmailAddress AS PatientEmail,
    d.Name AS DiseaseName,
    d.Code AS DiseaseCode,
    j1.Name AS Jurisdiction1,
    j2.Name AS Jurisdiction2,
    j3.Name AS Jurisdiction3,
    CAST(NULL AS UNIQUEIDENTIFIER) AS ExposureEventId,
    'Unknown' AS ExposureType,
    '' AS ExposureStatusDisplay,
    CAST(NULL AS DATETIME2) AS ExposureDate,
    '' AS ExposureLocation,
    '' AS ContactClassification,
    '' AS ConfidenceLevel,
    CAST(NULL AS UNIQUEIDENTIFIER) AS TaskId,
    '' AS TaskTitle,
    '' AS TaskType,
    'NotStarted' AS TaskStatus,
    CAST(NULL AS DATETIME2) AS TaskDueDate,
    CAST(NULL AS DATETIME2) AS TaskCompletedDate,
    GETDATE() AS TaskCreatedAt,
    '' AS AssignedToName,
    '' AS AssignedToEmail,
    'User' AS AssignmentType,
    GETDATE() AS CaseCreatedAt,
    GETDATE() AS CaseUpdatedAt
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
LEFT JOIN Jurisdictions j1 ON c.Jurisdiction1Id = j1.Id
LEFT JOIN Jurisdictions j2 ON c.Jurisdiction2Id = j2.Id
LEFT JOIN Jurisdictions j3 ON c.Jurisdiction3Id = j3.Id;
GO

-- Create vw_OutbreakTasksFlattened
CREATE VIEW vw_OutbreakTasksFlattened AS
SELECT 
    o.Id AS OutbreakId,
    o.Name AS OutbreakName,
    '' AS OutbreakReferenceNumber,
    '' AS DiseaseName,
    CAST(NULL AS UNIQUEIDENTIFIER) AS CaseGuid,
    '' AS CaseNumber,
    '' AS PatientName,
    CAST(NULL AS UNIQUEIDENTIFIER) AS TaskId,
    '' AS TaskTitle,
    '' AS TaskType,
    'NotStarted' AS TaskStatus,
    CAST(NULL AS DATETIME2) AS DueDate,
    CAST(NULL AS DATETIME2) AS CompletedDate,
    '' AS AssignedToName,
    '' AS AssignedToEmail,
    o.StartDate AS OutbreakCreatedAt
FROM Outbreaks o;
GO

-- Create vw_CaseTimelineAll
CREATE VIEW vw_CaseTimelineAll AS
SELECT 
    c.Id AS CaseId,
    'CaseCreated' AS EventType,
    GETDATE() AS EventDate,
    CONCAT('Case: ', c.FriendlyId) AS EventDescription,
    '' AS ActorName,
    GETDATE() AS SortDate
FROM Cases c;
GO

-- Create vw_ContactTracingMindMapNodes
CREATE VIEW vw_ContactTracingMindMapNodes AS
SELECT 
    c.Id AS NodeId,
    c.FriendlyId AS NodeLabel,
    c.Type AS NodeType,
    CONCAT(p.GivenName, ' ', p.FamilyName) AS PersonName,
    d.Name AS DiseaseName,
    cs.Name AS CaseStatus,
    c.DateOfOnset,
    CAST(0 AS BIT) AS IsDeleted
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id;
GO

-- Create vw_ContactTracingMindMapEdges
CREATE VIEW vw_ContactTracingMindMapEdges AS
SELECT 
    NEWID() AS EdgeId,
    c1.Id AS SourceNodeId,
    c2.Id AS TargetNodeId,
    'Contact' AS EdgeType,
    c2.DateOfOnset AS ExposureDate,
    'Medium' AS ConfidenceLevel
FROM Cases c1
CROSS JOIN Cases c2
WHERE c1.Id <> c2.Id AND c2.Type = 1;
GO

PRINT 'All reporting views recreated successfully';
