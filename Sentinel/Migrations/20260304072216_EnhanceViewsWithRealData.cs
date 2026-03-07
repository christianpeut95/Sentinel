using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceViewsWithRealData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing minimal views
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_OutbreakTasksFlattened;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseTimelineAll;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactTracingMindMapNodes;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactTracingMindMapEdges;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactsListSimple;");

            // ================================================================
            // vw_CaseContactTasksFlattened - ENHANCED with Recursive Transmission Chains
            // ================================================================
            migrationBuilder.Sql(@"
CREATE VIEW vw_CaseContactTasksFlattened AS

WITH TransmissionChain AS (
    -- Anchor: Root cases (no source case)
    SELECT 
        c.Id AS CaseId,
        c.FriendlyId AS CaseNumber,
        CAST(NULL AS UNIQUEIDENTIFIER) AS SourceCaseId,
        CAST(NULL AS NVARCHAR(MAX)) AS SourceCaseNumber,
        0 AS TransmissionDepth,
        CAST(c.FriendlyId AS NVARCHAR(MAX)) AS TransmissionChainPath
    FROM Cases c
    WHERE c.IsDeleted = 0
      AND NOT EXISTS (
          SELECT 1 
          FROM ExposureEvents ee 
          WHERE ee.ExposedCaseId = c.Id 
            AND ee.IsDeleted = 0
            AND ee.ExposureType = 3
      )
    
    UNION ALL
    
    -- Recursive: Cases exposed by other cases
    SELECT
        exposedCase.Id,
        exposedCase.FriendlyId,
        tc.CaseId AS SourceCaseId,
        CAST(tc.CaseNumber AS NVARCHAR(MAX)) AS SourceCaseNumber,
        tc.TransmissionDepth + 1,
        CAST(tc.TransmissionChainPath + ' ? ' + exposedCase.FriendlyId AS NVARCHAR(MAX))
    FROM TransmissionChain tc
    INNER JOIN ExposureEvents ee ON ee.SourceCaseId = tc.CaseId
    INNER JOIN Cases exposedCase ON ee.ExposedCaseId = exposedCase.Id
    WHERE tc.TransmissionDepth < 10
      AND ee.IsDeleted = 0
      AND exposedCase.IsDeleted = 0
      AND ee.ExposureType = 3
)

SELECT 
    tc.CaseId AS CaseGuid,
    tc.CaseNumber,
    tc.TransmissionDepth AS GenerationNumber,
    tc.TransmissionChainPath,
    tc.SourceCaseNumber AS TransmittedByCase,
    
    c.Type AS CaseTypeEnum,
    CASE c.Type 
        WHEN 0 THEN 'Case' 
        WHEN 1 THEN 'Contact' 
        ELSE 'Unknown'
    END AS CaseType,
    c.DateOfOnset,
    c.DateOfNotification,
    
    cs.Name AS CaseStatus,
    
    p.FriendlyId AS PatientId,
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
    
    ee.Id AS ExposureEventId,
    
    CASE ee.ExposureType
        WHEN 0 THEN 'Unknown'
        WHEN 1 THEN 'Event'
        WHEN 2 THEN 'Location'
        WHEN 3 THEN 'Contact'
        WHEN 4 THEN 'Travel'
        WHEN 5 THEN 'Locally Acquired'
        ELSE 'Unknown'
    END AS ExposureType,
    
    CASE ee.ExposureStatus
        WHEN 0 THEN 'Unknown'
        WHEN 1 THEN 'Potential Exposure'
        WHEN 2 THEN 'Under Investigation'
        WHEN 3 THEN 'Confirmed Exposure'
        WHEN 4 THEN 'Ruled Out'
        ELSE 'Unknown'
    END AS ExposureStatusDisplay,
    
    ee.ExposureStartDate,
    ee.ExposureEndDate,
    ee.Description AS ExposureDescription,
    CASE ee.ConfidenceLevel
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        ELSE NULL
    END AS ConfidenceLevel,
    
    cc.Name AS ContactClassification,
    
    evt.Id AS EventId,
    evt.Name AS EventName,
    evtType.Name AS EventType,
    evt.StartDateTime AS EventStartDate,
    evt.EndDateTime AS EventEndDate,
    evt.EstimatedAttendees,
    CASE WHEN evt.IsIndoor = 1 THEN 'Indoor' WHEN evt.IsIndoor = 0 THEN 'Outdoor' ELSE 'Unknown' END AS EventSetting,
    evtOrg.Name AS EventOrganizer,
    
    loc.Id AS LocationId,
    loc.Name AS LocationName,
    locType.Name AS LocationType,
    loc.Address AS LocationAddress,
    CASE WHEN loc.IsHighRisk = 1 THEN 'Yes' ELSE 'No' END AS LocationIsHighRisk,
    locOrg.Name AS LocationOrganization,
    
    t.Id AS TaskId,
    CAST(t.Id AS NVARCHAR(50)) AS TaskNumber,
    t.Title AS TaskTitle,
    t.Description AS TaskDescription,
    
    CASE t.Status
        WHEN 0 THEN 'NotStarted'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'OnHold'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS TaskStatus,
    
    CASE t.Priority
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        WHEN 3 THEN 'Urgent'
        ELSE 'Normal'
    END AS TaskPriority,
    
    t.DueDate AS TaskDueDate,
    t.CreatedAt AS TaskCreatedAt,
    t.CompletedAt AS TaskCompletedAt,
    t.CancelledAt AS TaskCancelledAt,
    t.IsInterviewTask,
    
    tt.Name AS TaskType,
    
    CASE t.AssignmentType
        WHEN 0 THEN 'User'
        WHEN 1 THEN 'Group'
        WHEN 2 THEN 'Role'
        ELSE 'User'
    END AS AssignmentType,
    
    u.Email AS AssignedToEmail,
    CONCAT(u.FirstName, ' ', u.LastName) AS AssignedToName,
    
    CASE 
        WHEN t.IsInterviewTask = 1 AND t.SurveyResponseJson IS NOT NULL THEN 'Completed'
        WHEN t.IsInterviewTask = 1 THEN 'Pending'
        ELSE 'Not Interview Task'
    END AS SurveyStatus,
    
    DATEDIFF(DAY, ee.ExposureEndDate, c.DateOfOnset) AS IncubationPeriodDays,
    DATEDIFF(DAY, GETDATE(), t.DueDate) AS DaysUntilTaskDue,
    DATEDIFF(DAY, t.CreatedAt, COALESCE(t.CompletedAt, GETDATE())) AS TaskAgeDays,
    
    CASE 
        WHEN t.Status = 3 THEN 'Complete'
        WHEN t.Status = 4 THEN 'Cancelled'
        WHEN t.DueDate < GETDATE() AND t.Status NOT IN (3, 4) THEN 'Overdue'
        WHEN t.DueDate < DATEADD(DAY, 3, GETDATE()) THEN 'Due Soon'
        WHEN t.Status = 1 THEN 'In Progress'
        ELSE 'On Track'
    END AS TaskDueStatus,
    
    GETDATE() AS CaseCreatedAt,
    GETDATE() AS CaseUpdatedAt

FROM TransmissionChain tc
INNER JOIN Cases c ON tc.CaseId = c.Id
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
LEFT JOIN Jurisdictions j1 ON c.Jurisdiction1Id = j1.Id
LEFT JOIN Jurisdictions j2 ON c.Jurisdiction2Id = j2.Id
LEFT JOIN Jurisdictions j3 ON c.Jurisdiction3Id = j3.Id
LEFT JOIN ExposureEvents ee ON ee.ExposedCaseId = c.Id AND ee.IsDeleted = 0
LEFT JOIN ContactClassifications cc ON ee.ContactClassificationId = cc.Id
LEFT JOIN Events evt ON ee.EventId = evt.Id
LEFT JOIN EventTypes evtType ON evt.EventTypeId = evtType.Id
LEFT JOIN Organizations evtOrg ON evt.OrganizerOrganizationId = evtOrg.Id
LEFT JOIN Locations loc ON ee.LocationId = loc.Id OR evt.LocationId = loc.Id
LEFT JOIN LocationTypes locType ON loc.LocationTypeId = locType.Id
LEFT JOIN Organizations locOrg ON loc.OrganizationId = locOrg.Id
LEFT JOIN CaseTasks t ON t.CaseId = c.Id
LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
LEFT JOIN AspNetUsers u ON t.AssignedToUserId = u.Id

WHERE c.IsDeleted = 0 AND p.IsDeleted = 0;
            ");

            // ================================================================
            // vw_OutbreakTasksFlattened - ENHANCED with real outbreak and task data
            // ================================================================
            migrationBuilder.Sql(@"
CREATE VIEW vw_OutbreakTasksFlattened AS
SELECT 
    o.Id AS OutbreakId,
    o.Name AS OutbreakName,
    CAST(o.Id AS NVARCHAR(50)) AS OutbreakReferenceNumber,
    '' AS DiseaseName,
    c.Id AS CaseGuid,
    c.FriendlyId AS CaseNumber,
    CONCAT(p.GivenName, ' ', p.FamilyName) AS PatientName,
    t.Id AS TaskId,
    t.Title AS TaskTitle,
    tt.Name AS TaskType,
    CASE t.Status
        WHEN 0 THEN 'NotStarted'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'OnHold'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS TaskStatus,
    t.DueDate,
    t.CompletedAt AS CompletedDate,
    CONCAT(u.FirstName, ' ', u.LastName) AS AssignedToName,
    u.Email AS AssignedToEmail,
    o.StartDate AS OutbreakCreatedAt
FROM Outbreaks o
LEFT JOIN OutbreakCases oc ON o.Id = oc.OutbreakId
LEFT JOIN Cases c ON oc.CaseId = c.Id AND c.IsDeleted = 0
LEFT JOIN Patients p ON c.PatientId = p.Id AND p.IsDeleted = 0
LEFT JOIN CaseTasks t ON c.Id = t.CaseId
LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
LEFT JOIN AspNetUsers u ON t.AssignedToUserId = u.Id;
            ");

            // ================================================================
            // vw_CaseTimelineAll - ENHANCED with all case events
            // ================================================================
            migrationBuilder.Sql(@"
CREATE VIEW vw_CaseTimelineAll AS
SELECT 
    c.Id AS CaseId,
    'CaseNotification' AS EventType,
    c.DateOfNotification AS EventDate,
    CONCAT('Case notified: ', c.FriendlyId) AS EventDescription,
    '' AS ActorName,
    COALESCE(c.DateOfNotification, GETDATE()) AS SortDate
FROM Cases c
WHERE c.IsDeleted = 0 AND c.DateOfNotification IS NOT NULL

UNION ALL

SELECT 
    c.Id AS CaseId,
    'LabResult' AS EventType,
    lr.ResultDate AS EventDate,
    CONCAT('Lab: ', tt.Name, ' - ', tr.Name) AS EventDescription,
    '' AS ActorName,
    lr.ResultDate AS SortDate
FROM Cases c
INNER JOIN LabResults lr ON c.Id = lr.CaseId AND lr.IsDeleted = 0
LEFT JOIN TestTypes tt ON lr.TestTypeId = tt.Id
LEFT JOIN TestResults tr ON lr.TestResultId = tr.Id
WHERE c.IsDeleted = 0

UNION ALL

SELECT 
    c.Id AS CaseId,
    'TaskCompleted' AS EventType,
    t.CompletedAt AS EventDate,
    CONCAT('Task: ', t.Title) AS EventDescription,
    CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
    t.CompletedAt AS SortDate
FROM Cases c
INNER JOIN CaseTasks t ON c.Id = t.CaseId AND t.Status = 3
LEFT JOIN AspNetUsers u ON t.CompletedByUserId = u.Id
WHERE c.IsDeleted = 0 AND t.CompletedAt IS NOT NULL

UNION ALL

SELECT 
    c.Id AS CaseId,
    'Note' AS EventType,
    n.CreatedAt AS EventDate,
    CONCAT('Note: ', LEFT(n.Content, 50), CASE WHEN LEN(n.Content) > 50 THEN '...' ELSE '' END) AS EventDescription,
    n.CreatedBy AS ActorName,
    n.CreatedAt AS SortDate
FROM Cases c
INNER JOIN Notes n ON c.Id = n.CaseId AND n.IsDeleted = 0
WHERE c.IsDeleted = 0;
            ");

            // ================================================================
            // vw_ContactTracingMindMapNodes - ENHANCED with full case data
            // ================================================================
            migrationBuilder.Sql(@"
CREATE VIEW vw_ContactTracingMindMapNodes AS
SELECT 
    c.Id AS NodeId,
    c.FriendlyId AS NodeLabel,
    c.Type AS NodeType,
    CONCAT(p.GivenName, ' ', p.FamilyName) AS PersonName,
    d.Name AS DiseaseName,
    cs.Name AS CaseStatus,
    c.DateOfOnset,
    c.IsDeleted
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
WHERE c.IsDeleted = 0 AND p.IsDeleted = 0;
            ");

            // ================================================================
            // vw_ContactTracingMindMapEdges - ENHANCED with exposure relationships
            // ================================================================
            migrationBuilder.Sql(@"
CREATE VIEW vw_ContactTracingMindMapEdges AS
SELECT 
    ee.Id AS EdgeId,
    ee.SourceCaseId AS SourceNodeId,
    c.Id AS TargetNodeId,
    CASE ee.ExposureType
        WHEN 0 THEN 'Unknown'
        WHEN 1 THEN 'Event'
        WHEN 2 THEN 'Location'
        WHEN 3 THEN 'Direct Contact'
        WHEN 4 THEN 'Travel'
        WHEN 5 THEN 'Locally Acquired'
        ELSE 'Unknown'
    END AS EdgeType,
    ee.ExposureStartDate AS ExposureDate,
    CASE ee.ConfidenceLevel
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        ELSE 'Medium'
    END AS ConfidenceLevel
FROM ExposureEvents ee
INNER JOIN Cases c ON ee.ExposedCaseId = c.Id
WHERE ee.IsDeleted = 0 
  AND c.IsDeleted = 0
  AND ee.SourceCaseId IS NOT NULL;
            ");

            // ================================================================
            // vw_ContactsListSimple - ENHANCED with exposure and task data
            // ================================================================
            migrationBuilder.Sql(@"
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
    
    CASE 
        WHEN ee.ExposureType = 0 THEN 'Unknown'
        WHEN ee.ExposureType = 1 THEN 'Event'
        WHEN ee.ExposureType = 2 THEN 'Location'
        WHEN ee.ExposureType = 3 THEN 'Contact'
        WHEN ee.ExposureType = 4 THEN 'Travel'
        WHEN ee.ExposureType = 5 THEN 'Locally Acquired'
        ELSE 'Unknown'
    END AS ExposureType,
    
    CASE 
        WHEN ee.ExposureType = 1 THEN evt.Name
        WHEN ee.ExposureType = 2 THEN loc.Name
        WHEN ee.ExposureType = 3 THEN CONCAT(psrc.GivenName, ' ', psrc.FamilyName)
        ELSE NULL
    END AS ExposureSourceName,
    
    (SELECT COUNT(*) FROM CaseTasks ct WHERE ct.CaseId = c.Id) AS TotalTasks,
    (SELECT COUNT(*) FROM CaseTasks ct WHERE ct.CaseId = c.Id AND ct.Status = 3) AS CompletedTasks,
    
    GETDATE() AS CreatedAt,
    GETDATE() AS UpdatedAt
    
FROM Cases c
INNER JOIN Patients p ON c.PatientId = p.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN ExposureEvents ee ON c.Id = ee.ExposedCaseId AND ee.IsDeleted = 0
LEFT JOIN Events evt ON ee.EventId = evt.Id
LEFT JOIN Locations loc ON ee.LocationId = loc.Id
LEFT JOIN Cases csrc ON ee.SourceCaseId = csrc.Id
LEFT JOIN Patients psrc ON csrc.PatientId = psrc.Id AND psrc.IsDeleted = 0
WHERE c.Type = 1 AND c.IsDeleted = 0 AND p.IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback drops enhanced views
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_OutbreakTasksFlattened;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseTimelineAll;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactTracingMindMapNodes;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactTracingMindMapEdges;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_ContactsListSimple;");
        }
    }
}
