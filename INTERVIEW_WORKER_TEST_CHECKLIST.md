# Interview Worker System - Quick Test Checklist

## ? Pre-Test Setup

- [ ] Migration applied: `dotnet ef database update`
- [ ] Worker user configured (see quick start guide)
- [ ] Interview task created with `IsInterviewTask = true`
- [ ] App restarted (if was debugging)

---

## ?? Test 1: Worker Dashboard Loads

**Steps:**
1. Login as interview worker
2. Navigate to `/dashboard/interview-queue`

**Expected:**
- ? Page loads (no 404)
- ? Statistics cards visible
- ? "Get Next Task" button or current task shown
- ? Availability toggle button visible

---

## ?? Test 2: Get Next Task

**Steps:**
1. Click "Get Next Task" button

**Expected:**
- ? Task appears in "Current Task" panel
- ? Patient name shown
- ? Phone number displayed prominently
- ? Call outcome buttons visible
- ? Success message shown

---

## ?? Test 3: Log Call Outcome

**Steps:**
1. With current task loaded
2. Click "No Answer" button
3. Modal opens
4. Enter notes
5. Click Submit

**Expected:**
- ? Modal appears
- ? Notes field functional
- ? Form submits
- ? Success message: "Call outcome logged: NoAnswer"
- ? Attempt count increases (0/3 ? 1/3)
- ? Call history table updates

---

## ?? Test 4: Escalation

**Steps:**
1. Log "No Answer" three times for same task

**Expected:**
- ? After 3rd attempt, task disappears from worker queue
- ? New task auto-loads (if available)
- ? Task escalated in database (`EscalationLevel > 0`)

---

## ?? Test 5: Supervisor Dashboard (Admin/Supervisor Only)

**Steps:**
1. Login as admin or supervisor
2. Navigate to `/dashboard/supervise-interviews`

**Expected:**
- ? Page loads (no 404)
- ? Statistics cards visible
- ? Worker performance table shown
- ? Unassigned tasks listed
- ? Escalated tasks shown (if any)

---

## ?? Test 6: Manual Assignment

**Steps:**
1. On supervisor dashboard
2. Click "Assign" button on unassigned task
3. Select worker from dropdown
4. Click "Assign"

**Expected:**
- ? Modal opens
- ? Worker dropdown populated
- ? Assignment succeeds
- ? Success message shown
- ? Task disappears from unassigned list
- ? Task appears in worker's queue

---

## ?? Test 7: Statistics Update

**Steps:**
1. As worker, complete a task
2. Refresh page

**Expected:**
- ? "Completed" count increases
- ? "Calls Today" count increases
- ? "Completion Rate %" updates

---

## ?? Test 8: Availability Toggle

**Steps:**
1. On worker dashboard
2. Click availability toggle (green ? red or red ? green)

**Expected:**
- ? Button changes color
- ? Status saved
- ? Success message shown
- ? Reflects in supervisor dashboard

---

## ?? Test 9: Multiple Workers

**Steps:**
1. Configure 2+ workers
2. Create 5+ interview tasks
3. Workers click "Get Next Task"

**Expected:**
- ? Tasks distributed evenly (round-robin)
- ? No task assigned to multiple workers
- ? Each worker gets different task

---

## ?? Test 10: Language Matching (If Configured)

**Steps:**
1. Create task with `LanguageRequired = 'Spanish'`
2. Configure worker with Spanish in languages
3. Worker clicks "Get Next Task"

**Expected:**
- ? Spanish-required task assigned to Spanish-speaking worker
- ? Language coverage shown in supervisor dashboard

---

## ?? Common Issues & Fixes

### Issue: 404 Error
**Fix:** 
- Verify Blazor `.razor` files are deleted
- Only `.cshtml` files should exist
- Restart app

### Issue: "Get Next Task" returns nothing
**Fix:**
- Verify task has `IsInterviewTask = true`
- Verify task has `AssignedToUserId = NULL`
- Verify task `Status = 0` (Pending)
- Check worker capacity not exceeded

### Issue: Modal doesn't open
**Fix:**
- Check browser console for JavaScript errors
- Verify Bootstrap is loaded
- Clear browser cache

### Issue: Supervisor can't access dashboard
**Fix:**
- Add user to "Admin" or "Supervisor" role
- Verify role exists in database

---

## ? Success Criteria

All tests pass when:
- ? No 404 errors
- ? Pages load correctly
- ? Data displays properly
- ? Forms submit successfully
- ? Statistics update in real-time
- ? Tasks assign and complete correctly
- ? Escalation works automatically

---

## ?? Test Results Template

```
Date: __________
Tester: __________

Test 1: Worker Dashboard Loads          [ PASS / FAIL ]
Test 2: Get Next Task                    [ PASS / FAIL ]
Test 3: Log Call Outcome                 [ PASS / FAIL ]
Test 4: Escalation                       [ PASS / FAIL ]
Test 5: Supervisor Dashboard             [ PASS / FAIL ]
Test 6: Manual Assignment                [ PASS / FAIL ]
Test 7: Statistics Update                [ PASS / FAIL ]
Test 8: Availability Toggle              [ PASS / FAIL ]
Test 9: Multiple Workers                 [ PASS / FAIL ]
Test 10: Language Matching               [ PASS / FAIL ]

Overall Status: [ PASS / FAIL ]

Notes:
_______________________________________________________
_______________________________________________________
```

---

**Quick Command Reference:**

```bash
# Apply migration
cd Surveillance-MVP
dotnet ef database update

# Run app
dotnet run

# View logs
dotnet run --verbosity detailed
```

**Database Check:**
```sql
-- Verify interview tasks exist
SELECT * FROM CaseTasks WHERE IsInterviewTask = 1;

-- Verify workers configured
SELECT Email, IsInterviewWorker, PrimaryLanguage, AvailableForAutoAssignment 
FROM AspNetUsers WHERE IsInterviewWorker = 1;

-- View call attempts
SELECT * FROM TaskCallAttempts ORDER BY AttemptedAt DESC;
```

---

? **System Ready for Testing!**
