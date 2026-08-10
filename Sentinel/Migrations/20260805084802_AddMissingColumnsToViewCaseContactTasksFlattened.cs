using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumnsToViewCaseContactTasksFlattened : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing view
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;");

            // Recreate the view with all required columns
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
        CAST(tc.TransmissionChainPath + ' → ' + exposedCase.FriendlyId AS NVARCHAR(MAX))
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
    st.Name AS PatientState,
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
    CASE WHEN loc.IsHighRisk = 1 THEN 'Yes' WHEN loc.IsHighRisk = 0 THEN 'No' ELSE 'Unknown' END AS LocationIsHighRisk,
    locOrg.Name AS LocationOrganization,

    t.Id AS TaskId,
    CAST(t.Id AS NVARCHAR(50)) AS TaskNumber,
    t.Title AS TaskTitle,
    t.Description AS TaskDescription,
    tt.Name AS TaskType,

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
        WHEN 3 THEN 'Critical'
        ELSE 'Unknown'
    END AS TaskPriority,

    t.DueDate AS TaskDueDate,
    t.CreatedAt AS TaskCreatedAt,
    t.CompletedAt AS TaskCompletedAt,
    t.CancelledAt AS TaskCancelledAt,
    t.IsInterviewTask,

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
LEFT JOIN States st ON p.StateId = st.Id
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to previous version (from the previous migration)
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;");

            migrationBuilder.Sql(@"
CREATE VIEW vw_CaseContactTasksFlattened AS

WITH TransmissionChain AS (
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

    SELECT
        exposedCase.Id,
        exposedCase.FriendlyId,
        tc.CaseId AS SourceCaseId,
        CAST(tc.CaseNumber AS NVARCHAR(MAX)) AS SourceCaseNumber,
        tc.TransmissionDepth + 1,
        CAST(tc.TransmissionChainPath + ' → ' + exposedCase.FriendlyId AS NVARCHAR(MAX))
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
    st.Name AS PatientState,
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
    locOrg.Name AS LocationOrganization,

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

    t.DueDate AS TaskDueDate,
    t.CompletedAt AS TaskCompletedDate,
    t.CreatedAt AS TaskCreatedAt,

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
LEFT JOIN States st ON p.StateId = st.Id
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
        }
    }
}
