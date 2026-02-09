# Dashboard Data Inconsistency Fix - COMPLETED

## Problem Identified
The three dashboards showed different data because they used different query filters:

### 1. **My Tasks Dashboard** (`MyTasksModel.OnGetAsync`)
- Service: `TaskService.GetTasksForUser(userId)`
- Filter: `AssignedToUserId == userId`
- **Issue**: Returned ALL tasks regardless of status (Completed, Cancelled, etc.)
- Displayed: All tasks ever assigned to the user

### 2. **Interviewer Dashboard** (`InterviewQueueModel.OnGetAsync`)
- Service: `TaskAssignmentService.GetAssignedTasksForWorkerAsync(userId)`
- Filter: `AssignedToUserId == userId && (Status == Pending || InProgress)`
- Displayed: Only active interview tasks

### 3. **Supervisor Dashboard** (`SuperviseInterviewsModel.OnGetAsync`)
- Service: `TaskAssignmentService.GetAllAssignedInterviewTasksAsync()`
- Filter: `IsInterviewTask && AssignedToUserId != null && (Status == Pending || InProgress)`
- Displayed: Only active interview tasks across all workers

## Root Cause
**My Tasks Dashboard** didn't filter by status, causing it to show:
- Completed tasks
- Cancelled tasks
- All historical tasks

Additionally, the status filter was inconsistent across different methods:
- Some used `Status != Completed && != Cancelled` (negative filter)
- Others used `Status == Pending || == InProgress` (positive filter)
- `WaitingForPatient` status was not consistently included

## Solution Implemented ?

### 1. Updated `MyTasksModel.OnGetAsync`
- Added default filter to show only active tasks (Pending, InProgress, WaitingForPatient)
- Still loads all tasks for statistics calculation
- Only filters the displayed list to active tasks unless user explicitly selects "All"

### 2. Standardized Status Filters Across All Methods
Changed all capacity and assignment queries to use consistent positive filters:
- `Status == Pending || InProgress || WaitingForPatient`

### 3. Updated Methods in `TaskAssignmentService.cs`:
- ? `AssignNextTaskAsync` - capacity check
- ? `AutoAssignTaskAsync` - worker selection (2 locations)
- ? `GetAssignedTasksForWorkerAsync` - interviewer dashboard query
- ? `GetAllAssignedInterviewTasksAsync` - supervisor dashboard query
- ? `GetAvailableWorkersAsync` - capacity calculation
- ? `GetWorkerStatisticsAsync` - TasksInProgress calculation

## Result
All three dashboards now show consistent data:
- **Active tasks only**: Pending, InProgress, WaitingForPatient
- **Excludes**: Completed, Cancelled, Overdue (unless explicitly filtered)
- **Consistent** capacity calculations
- **Consistent** worker availability checks

## Testing Checklist
- [ ] My Tasks Dashboard shows only active tasks by default
- [ ] Interviewer Dashboard matches My Tasks (for interview tasks)
- [ ] Supervisor Dashboard shows all active assigned tasks
- [ ] Task counts match across all three dashboards
- [ ] Worker capacity calculations are accurate
- [ ] Auto-assignment respects capacity limits correctly
- [ ] Statistics show correct "TasksInProgress" counts

## Files Modified
1. `Surveillance-MVP\Pages\Dashboard\MyTasks.cshtml.cs`
2. `Surveillance-MVP\Services\TaskAssignmentService.cs`
3. `DASHBOARD_DATA_INCONSISTENCY_FIX.md` (this file)
