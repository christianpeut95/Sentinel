-- Check if you have any assigned interview tasks
SELECT 
    ct.Id,
    ct.Title,
    ct.Status,
    ct.IsInterviewTask,
    ct.AssignedToUserId,
    u.FirstName + ' ' + u.LastName AS AssignedToWorker,
    p.GivenName + ' ' + p.FamilyName AS PatientName
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
LEFT JOIN Cases c ON ct.CaseId = c.Id
LEFT JOIN Patients p ON c.PatientId = p.Id
WHERE ct.IsInterviewTask = 1
  AND ct.AssignedToUserId IS NOT NULL
  AND ct.Status IN (0, 1); -- 0=Pending, 1=InProgress

-- If no results, you need to assign some tasks first!

-- To manually assign a task to a worker:
/*
UPDATE CaseTasks
SET AssignedToUserId = (SELECT TOP 1 Id FROM AspNetUsers WHERE IsInterviewWorker = 1),
    AssignmentMethod = 2, -- SupervisorAssignment
    ModifiedAt = GETUTCDATE()
WHERE Id = 'YOUR-TASK-ID-HERE';
*/
