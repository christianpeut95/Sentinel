-- DIAGNOSTIC: Check why CurrentlyAssignedTasks shows 0

-- 1. Check if ANY interview tasks exist at all
SELECT 
    COUNT(*) AS TotalInterviewTasks,
    SUM(CASE WHEN AssignedToUserId IS NOT NULL THEN 1 ELSE 0 END) AS AssignedInterviewTasks,
    SUM(CASE WHEN AssignedToUserId IS NULL THEN 1 ELSE 0 END) AS UnassignedInterviewTasks
FROM CaseTasks
WHERE IsInterviewTask = 1;

-- 2. Check the specific filter we're using in the code
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,
    ct.Status,
    ct.AssignedToUserId,
    u.Email AS AssignedToEmail,
    u.FirstName + ' ' + u.LastName AS AssignedToWorker,
    CASE ct.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Cancelled'
        WHEN 4 THEN 'WaitingForPatient'
        WHEN 5 THEN 'Overdue'
        ELSE 'Unknown'
    END AS StatusName
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.IsInterviewTask = 1
  AND ct.AssignedToUserId IS NOT NULL
  AND ct.Status IN (0, 1, 4)  -- Pending, InProgress, WaitingForPatient
ORDER BY ct.CreatedAt DESC;

-- 3. Check ALL tasks (to see what we have)
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,
    ct.Status,
    ct.AssignedToUserId,
    u.Email AS AssignedToEmail,
    CASE ct.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Cancelled'
        WHEN 4 THEN 'WaitingForPatient'
        WHEN 5 THEN 'Overdue'
        ELSE 'Unknown'
    END AS StatusName
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.AssignedToUserId IS NOT NULL
ORDER BY ct.IsInterviewTask DESC, ct.CreatedAt DESC;

-- 4. Check if the problem is IsInterviewTask = 0 or NULL
SELECT 
    IsInterviewTask,
    COUNT(*) AS TaskCount,
    SUM(CASE WHEN AssignedToUserId IS NOT NULL AND Status IN (0,1,4) THEN 1 ELSE 0 END) AS ActiveAssignedCount
FROM CaseTasks
GROUP BY IsInterviewTask;

-- 5. Check interview workers
SELECT 
    Id,
    Email,
    FirstName + ' ' + LastName AS FullName,
    IsInterviewWorker,
    AvailableForAutoAssignment,
    CurrentTaskCapacity
FROM AspNetUsers
WHERE IsInterviewWorker = 1;
