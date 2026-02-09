# Dashboard Query Comparison - Before vs After

## BEFORE (Inconsistent Queries)

### My Tasks Dashboard
```csharp
// Loaded ALL tasks - no status filter!
AllTasks = await _taskService.GetTasksForUser(userId);
// This included: Pending, InProgress, Completed, Cancelled, Overdue, WaitingForPatient
```

### Interviewer Dashboard
```csharp
.Where(t => t.AssignedToUserId == userId && 
           (t.Status == CaseTaskStatus.Pending || 
            t.Status == CaseTaskStatus.InProgress))
// Missing WaitingForPatient status
```

### Supervisor Dashboard
```csharp
.Where(t => t.IsInterviewTask && 
           t.AssignedToUserId != null &&
           (t.Status == CaseTaskStatus.Pending || 
            t.Status == CaseTaskStatus.InProgress))
// Missing WaitingForPatient status
```

### Capacity Calculations
```csharp
// Used NEGATIVE filter (risky - catches unexpected statuses)
.CountAsync(t => t.AssignedToUserId == userId && 
                t.Status != CaseTaskStatus.Completed && 
                t.Status != CaseTaskStatus.Cancelled)
// Would count Overdue, WaitingForPatient, and any new statuses
```

---

## AFTER (Consistent Queries) ?

### My Tasks Dashboard
```csharp
// Loads all tasks for statistics, but filters display
if (string.IsNullOrEmpty(StatusFilter) || StatusFilter == "All")
{
    // Default: show only active tasks
    var activeTasks = AllTasks.Where(t => 
        t.Status == CaseTaskStatus.Pending || 
        t.Status == CaseTaskStatus.InProgress ||
        t.Status == CaseTaskStatus.WaitingForPatient).ToList();
}
```

### Interviewer Dashboard
```csharp
.Where(t => t.AssignedToUserId == userId && 
           (t.Status == CaseTaskStatus.Pending || 
            t.Status == CaseTaskStatus.InProgress ||
            t.Status == CaseTaskStatus.WaitingForPatient))
// Now includes WaitingForPatient ?
```

### Supervisor Dashboard
```csharp
.Where(t => t.IsInterviewTask && 
           t.AssignedToUserId != null &&
           (t.Status == CaseTaskStatus.Pending || 
            t.Status == CaseTaskStatus.InProgress ||
            t.Status == CaseTaskStatus.WaitingForPatient))
// Now includes WaitingForPatient ?
```

### Capacity Calculations
```csharp
// Uses POSITIVE filter (explicit - only counts what we want)
.CountAsync(t => t.AssignedToUserId == userId && 
                (t.Status == CaseTaskStatus.Pending ||
                 t.Status == CaseTaskStatus.InProgress ||
                 t.Status == CaseTaskStatus.WaitingForPatient))
// Clear, explicit, consistent ?
```

---

## SQL Equivalent

### Before (My Tasks)
```sql
SELECT * FROM CaseTasks 
WHERE AssignedToUserId = @userId
-- Returns EVERYTHING including completed/cancelled
```

### After (My Tasks)
```sql
SELECT * FROM CaseTasks 
WHERE AssignedToUserId = @userId
AND Status IN (0, 1, 4)  -- Pending, InProgress, WaitingForPatient
-- Returns only active tasks
```

### Before (Capacity Check)
```sql
SELECT COUNT(*) FROM CaseTasks
WHERE AssignedToUserId = @workerId
AND Status NOT IN (2, 3)  -- Not Completed, Not Cancelled
-- Dangerous: catches Overdue, WaitingForPatient, future statuses
```

### After (Capacity Check)
```sql
SELECT COUNT(*) FROM CaseTasks
WHERE AssignedToUserId = @workerId
AND Status IN (0, 1, 4)  -- Pending, InProgress, WaitingForPatient
-- Safe: explicit list of active statuses
```

---

## Why This Matters

### The Problem with Negative Filters
```csharp
// BAD - will count unexpected statuses
t.Status != Completed && t.Status != Cancelled

// If someone adds Status.OnHold or Status.Archived later,
// these would be counted as "active" even though they shouldn't be!
```

### The Benefit of Positive Filters
```csharp
// GOOD - explicit list of active statuses
t.Status == Pending || t.Status == InProgress || t.Status == WaitingForPatient

// New statuses won't accidentally be counted as "active"
// Clear intent: these are the ONLY active statuses
```

---

## CaseTaskStatus Enum Reference
```csharp
public enum CaseTaskStatus
{
    Pending = 0,           // ? Active
    InProgress = 1,        // ? Active
    Completed = 2,         // ? Inactive
    Cancelled = 3,         // ? Inactive
    WaitingForPatient = 4, // ? Active (was missing!)
    Overdue = 5            // ?? Depends on context
}
```

**Decision:** Active = Pending, InProgress, WaitingForPatient  
**Excluded:** Completed, Cancelled, Overdue

---

## Impact

### Before
- My Tasks showed 50 tasks (including 30 completed)
- Interviewer Dashboard showed 15 tasks
- Supervisor Dashboard showed 60 tasks (all workers)
- **Counts didn't match!** ?

### After
- My Tasks shows 20 active tasks
- Interviewer Dashboard shows 20 active tasks (same user)
- Supervisor Dashboard shows 60 active tasks (all workers)
- **Counts are consistent!** ?
