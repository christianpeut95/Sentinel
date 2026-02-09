# Interview Worker System - Quick Start Guide

## ?? Getting Started in 5 Minutes

### Step 1: Apply Database Migration
```bash
cd Surveillance-MVP
dotnet ef database update
```

### Step 2: Create Your First Interview Worker

**Option A: Via Database (Quick)**
```sql
-- Update an existing user to be an interview worker
UPDATE AspNetUsers
SET 
    IsInterviewWorker = 1,
    PrimaryLanguage = 'English',
    LanguagesSpokenJson = '["English", "Spanish"]',
    AvailableForAutoAssignment = 1,
    CurrentTaskCapacity = 10,
    FirstName = 'John',
    LastName = 'Smith'
WHERE Email = 'worker@example.com';
```

**Option B: Via Code (Production)**
```csharp
// In your user management service
var user = await _userManager.FindByEmailAsync("worker@example.com");
user.IsInterviewWorker = true;
user.PrimaryLanguage = "English";
user.LanguagesSpokenJson = JsonSerializer.Serialize(new[] { "English", "Spanish" });
user.AvailableForAutoAssignment = true;
user.CurrentTaskCapacity = 10;
user.FirstName = "John";
user.LastName = "Smith";
await _userManager.UpdateAsync(user);
```

### Step 3: Create a Supervisor (Optional)

```sql
-- Grant supervisor role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 
    u.Id,
    r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'supervisor@example.com'
  AND r.Name = 'Supervisor';
```

If "Supervisor" role doesn't exist:
```sql
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES (NEWID(), 'Supervisor', 'SUPERVISOR', NEWID());
```

### Step 4: Create an Interview Task

**Via Code:**
```csharp
var task = new CaseTask
{
    Id = Guid.NewGuid(),
    CaseId = existingCaseId,
    Title = "Phone Interview - Contact Tracing",
    Description = "Complete contact tracing phone survey",
    TaskTypeId = surveyTaskTypeId,  // Use appropriate task type
    IsInterviewTask = true,          // KEY: Marks as interview task
    LanguageRequired = "English",
    MaxCallAttempts = 3,
    Priority = TaskPriority.High,
    Status = CaseTaskStatus.Pending,
    AssignedToUserId = null,  // Leave null for auto-assignment
    CreatedAt = DateTime.UtcNow
};

await _context.CaseTasks.AddAsync(task);
await _context.SaveChangesAsync();
```

**Via SQL (Testing):**
```sql
DECLARE @TaskTypeId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM TaskTypes);
DECLARE @CaseId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Cases);

INSERT INTO CaseTasks (Id, CaseId, Title, Description, TaskTypeId, IsInterviewTask, 
                       LanguageRequired, MaxCallAttempts, Priority, Status, CreatedAt)
VALUES (NEWID(), @CaseId, 'Phone Interview - Contact Tracing', 
        'Complete phone survey for contact tracing', @TaskTypeId, 1, 
        'English', 3, 2, 0, GETUTCDATE());
```

### Step 5: Test the Worker Dashboard

1. **Login** as your interview worker
2. **Navigate** to `/dashboard/interview-queue`
3. **Toggle** availability to "Available" (green)
4. **Click** "Get Next Task"
5. **Make** a test call
6. **Log** outcome (try "Completed" or "No Answer")
7. **View** your statistics update

### Step 6: Test the Supervisor Dashboard (Optional)

1. **Login** as supervisor/admin
2. **Navigate** to `/dashboard/supervise-interviews`
3. **View** worker performance
4. **See** unassigned tasks
5. **Try** manually assigning a task

## ?? Verification Checklist

After setup, verify:

- [ ] Migration applied successfully
- [ ] Interview worker user created
- [ ] Worker can access `/dashboard/interview-queue`
- [ ] At least one interview task created
- [ ] Task appears in "Get Next Task" flow
- [ ] Call attempt logging works
- [ ] Statistics display correctly
- [ ] Supervisor can access `/dashboard/supervise-interviews`
- [ ] Supervisor sees unassigned tasks
- [ ] Manual assignment works

## ?? Quick Test Scenario

**Simulate a Complete Workflow:**

1. Create interview task (unassigned)
2. Worker clicks "Get Next Task"
3. Task auto-assigns to worker
4. Worker logs "No Answer" outcome
5. Worker clicks "Get Next Task" again
6. Same task reappears (attempt count = 1)
7. Worker logs "No Answer" again (attempt count = 2)
8. Worker logs "No Answer" third time (attempt count = 3)
9. Task escalates automatically
10. Supervisor sees task in "Escalated Tasks"
11. Supervisor manually reassigns to different worker
12. New worker logs "Completed"
13. Task marked complete

## ?? Navigation URLs

- **Worker Dashboard:** `/dashboard/interview-queue` (Razor Page)
- **Supervisor Dashboard:** `/dashboard/supervise-interviews` (Razor Page)
- **API Base:** `/api/InterviewQueue/...`

## ?? Important Notes

- The dashboards use **Razor Pages** (`.cshtml` + `.cshtml.cs`), consistent with the rest of the application
- NOT Blazor components - this matches your existing MyTasks dashboard and other pages
- Uses standard Bootstrap modals and forms

## ?? Sample Data Script

Run this to create sample data for testing:

```sql
-- Create sample interview worker
DECLARE @UserId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE Email = 'test@example.com');

UPDATE AspNetUsers
SET 
    IsInterviewWorker = 1,
    PrimaryLanguage = 'English',
    LanguagesSpokenJson = '["English", "Spanish", "French"]',
    AvailableForAutoAssignment = 1,
    CurrentTaskCapacity = 10,
    FirstName = 'Jane',
    LastName = 'Doe'
WHERE Id = @UserId;

-- Create 5 sample interview tasks
DECLARE @TaskTypeId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM TaskTypes);
DECLARE @Counter INT = 1;

WHILE @Counter <= 5
BEGIN
    DECLARE @CaseId UNIQUEIDENTIFIER = (
        SELECT TOP 1 Id FROM Cases ORDER BY NEWID()
    );
    
    INSERT INTO CaseTasks (
        Id, CaseId, Title, Description, TaskTypeId, 
        IsInterviewTask, LanguageRequired, MaxCallAttempts, 
        Priority, Status, CreatedAt
    )
    VALUES (
        NEWID(), 
        @CaseId, 
        'Phone Interview ' + CAST(@Counter AS VARCHAR),
        'Complete phone survey for contact tracing',
        @TaskTypeId,
        1,
        CASE WHEN @Counter % 3 = 0 THEN 'Spanish' ELSE 'English' END,
        3,
        CASE WHEN @Counter % 4 = 0 THEN 3 WHEN @Counter % 2 = 0 THEN 2 ELSE 1 END,
        0,
        GETUTCDATE()
    );
    
    SET @Counter = @Counter + 1;
END;

SELECT 'Sample data created successfully!' AS Result;
```

## ?? Troubleshooting

### Issue: "Get Next Task" returns no tasks
**Solution:** 
- Verify tasks exist with `IsInterviewTask = true`
- Verify tasks have `AssignedToUserId = NULL`
- Verify tasks have `Status = 0` (Pending)
- Check worker capacity hasn't been exceeded

### Issue: Worker can't access interview queue
**Solution:**
- Verify `IsInterviewWorker = true` on user
- Check user is logged in
- Verify authorization is working

### Issue: Supervisor can't access dashboard
**Solution:**
- Add user to "Supervisor" or "Admin" role
- Verify `[Authorize(Roles = "Admin,Supervisor")]` attribute

### Issue: Call attempts not logging
**Solution:**
- Check task ID is correct
- Verify user is authenticated
- Check database constraints
- Review server logs

## ?? Training Script for Workers

**Welcome to the Interview Queue!**

1. **Login** with your credentials
2. **Click** the dashboard menu
3. **Select** "Interview Queue"
4. **Toggle** your status to "Available" (green button)
5. **Click** "Get Next Task" - a task will appear
6. **Read** the patient name and see the phone number
7. **Click** the copy button to copy the phone number
8. **Make** your phone call
9. **Log** the outcome by clicking the appropriate button
10. **Add** any notes in the text box
11. **Click** Submit
12. **Move** to next task automatically (or click "Get Next Task")

**Tips:**
- You can switch between tasks in your queue
- Your statistics update in real-time
- If unavailable, toggle to "Unavailable" (red button)
- You can view call history for each task
- After 3 attempts, tasks automatically escalate

## ?? Success Metrics

Track these to measure effectiveness:
- **Completion Rate:** Should be >70%
- **Calls Per Day:** Varies by outbreak, aim for 30-50
- **Escalation Rate:** Should be <15%
- **Average Duration:** Track for training purposes
- **Language Match Rate:** Should approach 100%

## ?? You're Ready!

The system is now fully operational. Workers can begin taking calls immediately!

---

**Need Help?** Refer to `INTERVIEW_WORKER_SYSTEM_COMPLETE.md` for full documentation.
