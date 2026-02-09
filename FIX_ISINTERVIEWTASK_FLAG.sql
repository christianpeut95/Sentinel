-- FIX: Set IsInterviewTask = 1 for assigned tasks
-- This will make them appear in the Supervisor Dashboard

-- First, check what we have
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,
    ct.AssignedToUserId,
    u.Email,
    ct.Status
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.AssignedToUserId IS NOT NULL;

-- OPTION 1: Mark ALL assigned tasks as interview tasks
UPDATE CaseTasks
SET IsInterviewTask = 1,
    ModifiedAt = GETUTCDATE()
WHERE AssignedToUserId IS NOT NULL
  AND IsInterviewTask = 0;

-- OPTION 2: Mark specific tasks as interview tasks (safer)
-- Update this to only mark tasks for actual interview workers
UPDATE ct
SET ct.IsInterviewTask = 1,
    ct.ModifiedAt = GETUTCDATE()
FROM CaseTasks ct
INNER JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE u.IsInterviewWorker = 1
  AND ct.IsInterviewTask = 0;

-- Verify the fix
SELECT 
    IsInterviewTask,
    Status,
    COUNT(*) AS TaskCount
FROM CaseTasks
WHERE AssignedToUserId IS NOT NULL
GROUP BY IsInterviewTask, Status
ORDER BY IsInterviewTask DESC, Status;
