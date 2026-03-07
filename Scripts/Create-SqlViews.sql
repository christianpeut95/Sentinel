-- Create SQL Views for Surveillance MVP
-- These views provide flattened data for reporting and dashboard displays

-- ===================================================================
-- vw_ContactsListSimple: Fast contact list for main index page
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ContactsListSimple')
    DROP VIEW vw_ContactsListSimple;
GO

CREATE VIEW vw_ContactsListSimple AS
SELECT 
    c.Id AS ContactId,
    c.CaseNumber AS ContactNumber,
    c.DateIdentified,
    c.DateOfOnset AS ContactDateOfOnset,
    
    -- Patient details
    p.Id AS PatientId,
    CONCAT(p.FirstName, ' ', p.LastName) AS ContactName,
    p.FirstName AS ContactFirstName,
    p.LastName AS ContactLastName,
    p.DateOfBirth AS ContactDOB,
    p.Mobile AS ContactMobile,
    p.Email AS ContactEmail,
    p.Suburb AS ContactSuburb,
    p.State AS ContactState,
    
    -- Disease
    d.Name AS DiseaseName,
    
    -- Exposure source information
    CASE 
        WHEN ee.ExposureTypeEnum = 0 THEN 'Event'
        WHEN ee.ExposureTypeEnum = 1 THEN 'Location'
        WHEN ee.ExposureTypeEnum = 2 THEN 'Contact'
        ELSE 'Unknown'
    END AS ExposureType,
    
    CASE 
        WHEN ee.ExposureTypeEnum = 0 THEN e.Name
        WHEN ee.ExposureTypeEnum = 1 THEN l.Name
        WHEN ee.ExposureTypeEnum = 2 THEN CONCAT(psrc.FirstName, ' ', psrc.LastName)
        ELSE NULL
    END AS ExposureSourceName,
    
    -- Task metrics
    (SELECT COUNT(*) FROM CaseTasks WHERE CaseId = c.Id) AS TotalTasks,
    (SELECT COUNT(*) FROM CaseTasks WHERE CaseId = c.Id AND StatusEnum = 3) AS CompletedTasks,
    
    c.CreatedAt,
    c.UpdatedAt
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN ExposureEvents ee ON c.ExposureEventId = ee.Id
LEFT JOIN Events e ON ee.EventId = e.Id
LEFT JOIN Locations l ON ee.LocationId = l.Id
LEFT JOIN Cases csrc ON ee.SourceCaseId = csrc.Id
LEFT JOIN Patients psrc ON csrc.PatientId = psrc.Id
WHERE c.CaseTypeEnum = 1 -- Contacts only
  AND c.IsDeleted = 0
  AND p.IsDeleted = 0;
GO

-- ===================================================================
-- vw_CaseContactTasksFlattened: Main case tracking view
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CaseContactTasksFlattened')
    DROP VIEW vw_CaseContactTasksFlattened;
GO

CREATE VIEW vw_CaseContactTasksFlattened AS
SELECT 
    -- Case identification
    c.Id AS CaseGuid,
    c.CaseNumber,
    c.CaseTypeEnum,
    CASE c.CaseTypeEnum 
        WHEN 0 THEN 'Case'
        WHEN 1 THEN 'Contact'
        ELSE 'Unknown'
    END AS CaseType,
    
    -- Transmission chain (simplified - not recursive)
    0 AS GenerationNumber,
    c.CaseNumber AS TransmissionChainPath,
    CASE 
        WHEN ee.SourceCaseId IS NOT NULL THEN csrc.CaseNumber
        ELSE NULL
    END AS TransmittedByCase,
    
    -- Case details
    c.DateOfOnset,
    c.DateOfNotification,
    cs.Name AS CaseStatus,
    
    -- Patient information
    p.Id AS PatientId,
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
    p.FirstName AS PatientFirstName,
    p.LastName AS PatientLastName,
    p.DateOfBirth AS PatientDOB,
    DATEDIFF(YEAR, p.DateOfBirth, COALESCE(c.DateOfOnset, c.CreatedAt)) AS AgeAtOnset,
    p.Suburb AS PatientSuburb,
    p.State AS PatientState,
    p.Mobile AS PatientMobile,
    p.Email AS PatientEmail,
    
    -- Disease
    d.Name AS DiseaseName,
    d.Code AS DiseaseCode,
    
    -- Jurisdiction
    j1.Name AS Jurisdiction1,
    j2.Name AS Jurisdiction2,
    j3.Name AS Jurisdiction3,
    
    -- Exposure details
    ee.Id AS ExposureEventId,
    CASE 
        WHEN ee.ExposureTypeEnum = 0 THEN 'Event'
        WHEN ee.ExposureTypeEnum = 1 THEN 'Location'
        WHEN ee.ExposureTypeEnum = 2 THEN 'Contact'
        ELSE 'Unknown'
    END AS ExposureType,
    
    CASE 
        WHEN ee.ExposureStatusEnum = 0 THEN 'Suspected'
        WHEN ee.ExposureStatusEnum = 1 THEN 'Probable'
        WHEN ee.ExposureStatusEnum = 2 THEN 'Confirmed'
        ELSE NULL
    END AS ExposureStatusDisplay,
    
    ee.ExposureDate,
    ee.ExposureLocation,
    
    cc.Name AS ContactClassification,
    
    CASE 
        WHEN ee.ConfidenceLevelEnum = 0 THEN 'Low'
        WHEN ee.ConfidenceLevelEnum = 1 THEN 'Medium'
        WHEN ee.ConfidenceLevelEnum = 2 THEN 'High'
        ELSE NULL
    END AS ConfidenceLevel,
    
    -- Task details (one row per task)
    t.Id AS TaskId,
    t.Title AS TaskTitle,
    tt.Name AS TaskType,
    CASE t.StatusEnum
        WHEN 0 THEN 'NotStarted'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'OnHold'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS TaskStatus,
    
    t.DueDate AS TaskDueDate,
    t.CompletedDate AS TaskCompletedDate,
    t.CreatedAt AS TaskCreatedAt,
    
    CONCAT(uAssigned.FirstName, ' ', uAssigned.LastName) AS AssignedToName,
    uAssigned.Email AS AssignedToEmail,
    
    CASE t.AssignmentTypeEnum
        WHEN 0 THEN 'User'
        WHEN 1 THEN 'Group'
        WHEN 2 THEN 'Role'
        ELSE 'Unknown'
    END AS AssignmentType,
    
    c.CreatedAt AS CaseCreatedAt,
    c.UpdatedAt AS CaseUpdatedAt
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.CaseStatusId = cs.Id
LEFT JOIN ExposureEvents ee ON c.ExposureEventId = ee.Id
LEFT JOIN ContactClassifications cc ON ee.ContactClassificationId = cc.Id
LEFT JOIN Cases csrc ON ee.SourceCaseId = csrc.Id
LEFT JOIN Jurisdictions j1 ON c.Jurisdiction1Id = j1.Id
LEFT JOIN Jurisdictions j2 ON c.Jurisdiction2Id = j2.Id
LEFT JOIN Jurisdictions j3 ON c.Jurisdiction3Id = j3.Id
LEFT JOIN CaseTasks t ON c.Id = t.CaseId AND t.IsDeleted = 0
LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
LEFT JOIN AspNetUsers uAssigned ON t.AssignedToUserId = uAssigned.Id
WHERE c.IsDeleted = 0
  AND p.IsDeleted = 0;
GO

-- ===================================================================
-- vw_OutbreakTasksFlattened: Outbreak task tracking
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_OutbreakTasksFlattened')
    DROP VIEW vw_OutbreakTasksFlattened;
GO

CREATE VIEW vw_OutbreakTasksFlattened AS
SELECT 
    o.Id AS OutbreakId,
    o.Name AS OutbreakName,
    o.ReferenceNumber AS OutbreakReferenceNumber,
    d.Name AS DiseaseName,
    
    c.Id AS CaseGuid,
    c.CaseNumber,
    
    -- Patient info
    CONCAT(p.FirstName, ' ', p.LastName) AS PatientName,
    
    -- Task details
    t.Id AS TaskId,
    t.Title AS TaskTitle,
    tt.Name AS TaskType,
    CASE t.StatusEnum
        WHEN 0 THEN 'NotStarted'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'OnHold'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS TaskStatus,
    
    t.DueDate,
    t.CompletedDate,
    
    CONCAT(uAssigned.FirstName, ' ', uAssigned.LastName) AS AssignedToName,
    uAssigned.Email AS AssignedToEmail,
    
    o.CreatedAt AS OutbreakCreatedAt
FROM Outbreaks o
INNER JOIN Diseases d ON o.DiseaseId = d.Id
LEFT JOIN OutbreakCases oc ON o.Id = oc.OutbreakId
LEFT JOIN Cases c ON oc.CaseId = c.Id AND c.IsDeleted = 0
LEFT JOIN Patients p ON c.PatientId = p.Id AND p.IsDeleted = 0
LEFT JOIN CaseTasks t ON c.Id = t.CaseId AND t.IsDeleted = 0
LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
LEFT JOIN AspNetUsers uAssigned ON t.AssignedToUserId = uAssigned.Id
WHERE o.IsDeleted = 0;
GO

-- ===================================================================
-- vw_CaseTimelineAll: Combined timeline of all case events
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CaseTimelineAll')
    DROP VIEW vw_CaseTimelineAll;
GO

CREATE VIEW vw_CaseTimelineAll AS
-- Case creation events
SELECT 
    c.Id AS CaseId,
    'CaseCreated' AS EventType,
    c.CreatedAt AS EventDate,
    CONCAT('Case created: ', c.CaseNumber) AS EventDescription,
    CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
    c.CreatedAt AS SortDate
FROM Cases c
LEFT JOIN AspNetUsers u ON c.CreatedBy = u.Id
WHERE c.IsDeleted = 0

UNION ALL

-- Lab results
SELECT 
    c.Id AS CaseId,
    'LabResult' AS EventType,
    lr.ResultDate AS EventDate,
    CONCAT('Lab result: ', tt.Name, ' - ', tr.Name) AS EventDescription,
    NULL AS ActorName,
    lr.ResultDate AS SortDate
FROM Cases c
INNER JOIN LabResults lr ON c.Id = lr.CaseId AND lr.IsDeleted = 0
LEFT JOIN TestTypes tt ON lr.TestTypeId = tt.Id
LEFT JOIN TestResults tr ON lr.TestResultId = tr.Id
WHERE c.IsDeleted = 0

UNION ALL

-- Task completions
SELECT 
    c.Id AS CaseId,
    'TaskCompleted' AS EventType,
    t.CompletedDate AS EventDate,
    CONCAT('Task completed: ', t.Title) AS EventDescription,
    CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
    t.CompletedDate AS SortDate
FROM Cases c
INNER JOIN CaseTasks t ON c.Id = t.CaseId AND t.IsDeleted = 0 AND t.StatusEnum = 3
LEFT JOIN AspNetUsers u ON t.CompletedBy = u.Id
WHERE c.IsDeleted = 0

UNION ALL

-- Notes
SELECT 
    c.Id AS CaseId,
    'Note' AS EventType,
    n.CreatedAt AS EventDate,
    CONCAT('Note: ', LEFT(n.Content, 50), CASE WHEN LEN(n.Content) > 50 THEN '...' ELSE '' END) AS EventDescription,
    CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
    n.CreatedAt AS SortDate
FROM Cases c
INNER JOIN Notes n ON c.Id = n.CaseId AND n.IsDeleted = 0
LEFT JOIN AspNetUsers u ON n.CreatedBy = u.Id
WHERE c.IsDeleted = 0;
GO

-- ===================================================================
-- vw_ContactTracingMindMapNodes: Mind map nodes for visualization
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ContactTracingMindMapNodes')
    DROP VIEW vw_ContactTracingMindMapNodes;
GO

CREATE VIEW vw_ContactTracingMindMapNodes AS
SELECT 
    c.Id AS NodeId,
    c.CaseNumber AS NodeLabel,
    c.CaseTypeEnum AS NodeType,
    CONCAT(p.FirstName, ' ', p.LastName) AS PersonName,
    d.Name AS DiseaseName,
    cs.Name AS CaseStatus,
    c.DateOfOnset,
    c.IsDeleted
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.CaseStatusId = cs.Id
WHERE c.IsDeleted = 0 AND p.IsDeleted = 0;
GO

-- ===================================================================
-- vw_ContactTracingMindMapEdges: Mind map edges for visualization
-- ===================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ContactTracingMindMapEdges')
    DROP VIEW vw_ContactTracingMindMapEdges;
GO

CREATE VIEW vw_ContactTracingMindMapEdges AS
SELECT 
    ee.Id AS EdgeId,
    ee.SourceCaseId AS SourceNodeId,
    c.Id AS TargetNodeId,
    CASE ee.ExposureTypeEnum
        WHEN 0 THEN 'Event'
        WHEN 1 THEN 'Location'
        WHEN 2 THEN 'Direct Contact'
        ELSE 'Unknown'
    END AS EdgeType,
    ee.ExposureDate,
    CASE ee.ConfidenceLevelEnum
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        ELSE NULL
    END AS ConfidenceLevel
FROM ExposureEvents ee
INNER JOIN Cases c ON ee.Id = c.ExposureEventId
WHERE ee.IsDeleted = 0 
  AND c.IsDeleted = 0
  AND ee.SourceCaseId IS NOT NULL;
GO

PRINT 'All SQL views created successfully';
