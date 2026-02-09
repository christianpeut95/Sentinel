# Supervisor Dashboard Shows 0 Assigned Tasks - Fix

## Problem
The Supervisor Dashboard "Currently Assigned Tasks" count shows **0** even though tasks are assigned to users.

## Root Cause
Tasks in the database have `IsInterviewTask = 0` (false) by default.

The Supervisor Dashboard query filters by:
```csharp
WHERE t.IsInterviewTask && 
      t.AssignedToUserId != null &&
      (t.Status == Pending || InProgress || WaitingForPatient)
```

If `IsInterviewTask = 0`, tasks won't show up even if they're assigned!

## Diagnosis

### Step 1: Run this SQL to check your data
```sql
-- Check if you have any interview tasks
SELECT 
    IsInterviewTask,
    COUNT(*) AS TaskCount,
    SUM(CASE WHEN AssignedToUserId IS NOT NULL AND Status IN (0,1,4) THEN 1 ELSE 0 END) AS ActiveAssignedCount
FROM CaseTasks
GROUP BY IsInterviewTask;
```

**Expected Result:**
- If you see `IsInterviewTask = 0` with assigned tasks, that's the problem!
- Interview tasks should have `IsInterviewTask = 1`

### Step 2: Check assigned tasks
```sql
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,  -- THIS SHOULD BE 1
    ct.Status,
    ct.AssignedToUserId,
    u.Email AS AssignedTo
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.AssignedToUserId IS NOT NULL
ORDER BY ct.IsInterviewTask DESC;
```

## Solution

### Quick Fix: Update Existing Tasks

Run this SQL to mark assigned tasks as interview tasks:

```sql
-- OPTION 1: Mark ALL assigned tasks as interview tasks
UPDATE CaseTasks
SET IsInterviewTask = 1,
    ModifiedAt = GETUTCDATE()
WHERE AssignedToUserId IS NOT NULL
  AND IsInterviewTask = 0;

-- Verify it worked
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,
    ct.Status,
    u.Email AS AssignedTo
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.IsInterviewTask = 1
  AND ct.AssignedToUserId IS NOT NULL
  AND ct.Status IN (0, 1, 4);
```

**OR** (safer - only for actual interview workers):

```sql
-- OPTION 2: Mark only tasks assigned to interview workers
UPDATE ct
SET ct.IsInterviewTask = 1,
    ct.ModifiedAt = GETUTCDATE()
FROM CaseTasks ct
INNER JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE u.IsInterviewWorker = 1
  AND ct.IsInterviewTask = 0;
```

### Permanent Fix: Update Task Creation Code

When creating tasks that should appear in the Interview Queue, always set `IsInterviewTask = true`.

#### Example - Manual Task Creation:
```csharp
var task = new CaseTask
{
    Id = Guid.NewGuid(),
    CaseId = caseId,
    Title = "Patient Interview",
    Description = description,
    TaskTypeId = taskTypeId,
    AssignedToUserId = userId,
    IsInterviewTask = true,  // ? ADD THIS!
    Status = CaseTaskStatus.Pending,
    CreatedAt = DateTime.UtcNow
};
```

#### Example - TaskService.CreateTasksForCase:
```csharp
var task = new CaseTask
{
    // ... other properties ...
    IsInterviewTask = template.IsInterviewTask,  // ? Use template setting
    // OR
    IsInterviewTask = (template.AssignmentType == TaskAssignmentType.InterviewWorker),  // ? Based on type
};
```

## Testing

### 1. After Running SQL Fix
```sql
-- Should now show interview tasks
SELECT 
    ct.Id,
    ct.Title,
    ct.IsInterviewTask,
    ct.Status,
    u.FirstName + ' ' + u.LastName AS Worker
FROM CaseTasks ct
INNER JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.IsInterviewTask = 1
  AND ct.Status IN (0, 1, 4)
ORDER BY u.LastName, ct.Priority;
```

### 2. Refresh Supervisor Dashboard
1. Navigate to `/Dashboard/SuperviseInterviews`
2. Click "Refresh" button
3. Check "Currently Assigned Tasks" count
4. Should now show the correct number!

### 3. Verify Task Counts Match
Run all three queries and compare:

```sql
-- Supervisor Dashboard count
SELECT COUNT(*) AS SupervisorDashboardCount
FROM CaseTasks
WHERE IsInterviewTask = 1
  AND AssignedToUserId IS NOT NULL
  AND Status IN (0, 1, 4);

-- Interviewer Dashboard count (for specific worker)
DECLARE @WorkerId NVARCHAR(450) = 'YOUR-WORKER-ID';
SELECT COUNT(*) AS InterviewerDashboardCount
FROM CaseTasks
WHERE AssignedToUserId = @WorkerId
  AND Status IN (0, 1, 4);

-- My Tasks count (for specific worker)
DECLARE @UserId NVARCHAR(450) = 'YOUR-USER-ID';
SELECT COUNT(*) AS MyTasksCount
FROM CaseTasks
WHERE AssignedToUserId = @UserId
  AND Status IN (0, 1, 4);
```

## Why This Happens

### When IsInterviewTask = 0
- **My Tasks**: Shows the task (doesn't filter by IsInterviewTask)
- **Interviewer Dashboard**: Shows the task (doesn't filter by IsInterviewTask)
- **Supervisor Dashboard**: DOESN'T show the task (filters by IsInterviewTask = 1)

This is why My Tasks and Interviewer Dashboard might show tasks, but Supervisor Dashboard shows 0!

## Prevention

### For New Projects
Set default in model:
```csharp
public class CaseTask
{
    [Display(Name = "Is Interview Task")]
    public bool IsInterviewTask { get; set; } = true;  // ? Default to true
}
```

### For Task Templates
Ensure templates have IsInterviewTask set:
```sql
UPDATE TaskTemplates
SET IsInterviewTask = 1
WHERE AssignmentType = 2;  -- InterviewWorker assignment type
```

### For Auto-Assignment
In `TaskAssignmentService.AssignNextTaskAsync`:
```csharp
var unassignedTask = await _context.CaseTasks
    .Where(t => t.IsInterviewTask &&  // Already filtering here!
               t.AssignedToUserId == null && 
               t.Status == CaseTaskStatus.Pending)
    .FirstOrDefaultAsync();
```

This means auto-assigned tasks MUST have `IsInterviewTask = true` to be picked up!

## Summary

**Problem**: `IsInterviewTask = 0` on assigned tasks  
**Quick Fix**: Run SQL UPDATE to set `IsInterviewTask = 1`  
**Permanent Fix**: Ensure new tasks set `IsInterviewTask = true`  
**Verification**: All three dashboards should now show matching counts

## Files Reference
- `Surveillance-MVP\Services\TaskAssignmentService.cs` - Query filters
- `Surveillance-MVP\Pages\Dashboard\SuperviseInterviews.cshtml.cs` - Dashboard model
- `Surveillance-MVP\Models\CaseTask.cs` - Task model
- `FIX_ISINTERVIEWTASK_FLAG.sql` - SQL fix script
- `DIAGNOSTIC_SUPERVISOR_DASHBOARD_ZERO_TASKS.sql` - Diagnostic queries
