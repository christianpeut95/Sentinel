# Dashboard Consistency Testing Guide

## Prerequisites
- Stop the application
- Restart the application to load the updated code
- Have test data with:
  - At least 1 interview worker assigned to tasks
  - Mix of task statuses: Pending, InProgress, Completed, Cancelled, WaitingForPatient
  - At least 1 supervisor account

## Test Scenario Setup

### 1. Check Current Database State
Run this SQL to see what you have:
```sql
-- See all tasks and their statuses
SELECT 
    ct.Id,
    ct.Title,
    ct.Status,
    ct.IsInterviewTask,
    ct.AssignedToUserId,
    u.FirstName + ' ' + u.LastName AS AssignedTo,
    p.GivenName + ' ' + p.FamilyName AS PatientName,
    CASE ct.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Cancelled'
        WHEN 4 THEN 'WaitingForPatient'
        WHEN 5 THEN 'Overdue'
    END AS StatusName
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
LEFT JOIN Cases c ON ct.CaseId = c.Id
LEFT JOIN Patients p ON c.PatientId = p.Id
WHERE ct.IsInterviewTask = 1
ORDER BY ct.AssignedToUserId, ct.Status;
```

### 2. Expected Active Task Count
```sql
-- Count ACTIVE interview tasks (should match all three dashboards)
SELECT 
    ct.AssignedToUserId,
    u.FirstName + ' ' + u.LastName AS Worker,
    COUNT(*) AS ActiveTaskCount
FROM CaseTasks ct
LEFT JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
WHERE ct.IsInterviewTask = 1
  AND ct.AssignedToUserId IS NOT NULL
  AND ct.Status IN (0, 1, 4)  -- Pending, InProgress, WaitingForPatient
GROUP BY ct.AssignedToUserId, u.FirstName, u.LastName
ORDER BY Worker;
```

## Test Cases

### Test 1: My Tasks Dashboard
**Page:** `/Dashboard/MyTasks`

**Test Steps:**
1. Log in as an interview worker who has assigned tasks
2. Navigate to My Tasks
3. Count the tasks shown in the list

**Expected Results:**
- ? Should show only tasks with status: Pending, InProgress, WaitingForPatient
- ? Should NOT show Completed tasks
- ? Should NOT show Cancelled tasks
- ? Statistics at top should include all tasks (for counts)
- ? Task list should show only active tasks

**Verification:**
- Compare task count to SQL query result from Setup Step 2
- Verify no "Completed" badges appear in the list
- Click "All" filter - now should see completed tasks too

---

### Test 2: Interviewer Dashboard (Interview Queue)
**Page:** `/Dashboard/InterviewQueue`

**Test Steps:**
1. Log in as same interview worker from Test 1
2. Navigate to Interview Queue
3. Count "My Tasks" shown

**Expected Results:**
- ? Task count should match Test 1 exactly
- ? Should show same tasks as My Tasks dashboard
- ? Should include WaitingForPatient tasks
- ? Should show call attempts if any

**Verification:**
- Task count = My Tasks active count
- Same task IDs appear in both lists

---

### Test 3: Supervisor Dashboard
**Page:** `/Dashboard/SuperviseInterviews`

**Test Steps:**
1. Log in as a supervisor or admin
2. Navigate to Supervise Interviews
3. Check "Currently Assigned Tasks" section
4. Note the count per worker

**Expected Results:**
- ? Should show all workers' active tasks
- ? Each worker's count should match their My Tasks count
- ? Should NOT include completed/cancelled tasks
- ? Should include WaitingForPatient tasks

**Verification:**
- Sum of all workers' tasks = total active tasks from SQL
- Individual worker counts match Tests 1 & 2

---

### Test 4: Task Assignment Capacity
**Test Steps:**
1. As supervisor, check a worker's current capacity:
   - Look at their "Tasks In Progress" count
2. Try to auto-assign a new task to that worker
3. Check if capacity is respected

**Expected Results:**
- ? Capacity count should match active task count (Pending + InProgress + WaitingForPatient)
- ? Should NOT count completed/cancelled tasks toward capacity
- ? Worker should be able to receive tasks if under capacity

**Verification:**
```sql
-- Check a specific worker's capacity
DECLARE @WorkerId NVARCHAR(450) = 'WORKER-ID-HERE';

SELECT 
    u.FirstName + ' ' + u.LastName AS Worker,
    u.CurrentTaskCapacity AS MaxCapacity,
    COUNT(ct.Id) AS CurrentlyAssigned,
    (u.CurrentTaskCapacity - COUNT(ct.Id)) AS AvailableSlots
FROM AspNetUsers u
LEFT JOIN CaseTasks ct ON ct.AssignedToUserId = u.Id 
    AND ct.Status IN (0, 1, 4)  -- Active statuses
WHERE u.Id = @WorkerId
GROUP BY u.FirstName, u.LastName, u.CurrentTaskCapacity;
```

---

### Test 5: Status Transitions
**Test Steps:**
1. Take a task from Interview Queue
2. Log a call attempt: "No Answer"
3. Check that task still appears in all dashboards
4. Log another call: "Completed"
5. Check that task disappears from active lists

**Expected Results:**
- ? After "No Answer": Task should still show (now InProgress)
- ? After "Callback Requested": Task should still show (now WaitingForPatient)
- ? After "Completed": Task should disappear from active lists
- ? Completed task should appear when "All" filter is used in My Tasks

---

## Expected Numbers Example

If you have this data:
- Worker A: 5 Pending, 3 InProgress, 2 WaitingForPatient, 10 Completed
- Worker B: 2 Pending, 1 InProgress, 0 WaitingForPatient, 5 Completed

**Expected Dashboard Counts:**

| Dashboard | Worker A | Worker B | Total |
|-----------|----------|----------|-------|
| My Tasks (Worker A) | 10 | - | 10 |
| My Tasks (Worker B) | - | 3 | 3 |
| Interviewer Queue (A) | 10 | - | 10 |
| Interviewer Queue (B) | - | 3 | 3 |
| Supervisor Dashboard | 10 | 3 | 13 |

**All counts should match!** ?

---

## Common Issues to Watch For

### ? Issue 1: Counts Don't Match
**Symptom:** My Tasks shows 20, Interview Queue shows 15  
**Cause:** One dashboard still using old filter  
**Check:** Review the query filters in both page models

### ? Issue 2: Completed Tasks Still Showing
**Symptom:** Tasks with "Completed" badge appear in active list  
**Cause:** Status filter not applied correctly  
**Check:** Verify status filter includes only (0, 1, 4)

### ? Issue 3: Worker Appears "Over Capacity"
**Symptom:** Worker has 5 tasks but capacity is 3  
**Cause:** Completed tasks counted toward capacity  
**Check:** Capacity calculation query

### ? Issue 4: WaitingForPatient Tasks Missing
**Symptom:** After callback requested, task disappears  
**Cause:** WaitingForPatient status not in filter  
**Check:** All three status filters include status 4

---

## Success Criteria
- ? All three dashboards show identical active task counts
- ? Completed/cancelled tasks don't appear in active lists
- ? WaitingForPatient tasks appear in all dashboards
- ? Capacity calculations are accurate
- ? Status transitions work correctly
- ? No compilation errors
- ? No null reference exceptions

## Rollback Plan
If issues occur:
1. Revert changes to `MyTasks.cshtml.cs`
2. Revert changes to `TaskAssignmentService.cs`
3. Rebuild and restart
4. Report specific issue encountered
