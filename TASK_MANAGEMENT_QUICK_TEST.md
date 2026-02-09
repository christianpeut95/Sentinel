# ?? Task Management System - Quick Test Guide

## Step-by-Step Testing Instructions

### Prerequisites ?
1. Build successful ? (Already done)
2. Database migrations applied ? (Already done)
3. **Next:** Run seed scripts

---

## ?? STEP 1: Run Seed Scripts

### Script 1: Seed Task Types
**File:** `Surveillance-MVP/Migrations/ManualScripts/SeedDefaultTaskTypes.sql`

**What it does:**
- Creates 8 customizable task types
- Replaces the old hardcoded enum approach

**Execute in SQL Server Management Studio or your database tool**

### Script 2: Seed Task Templates & Disease Assignments  
**File:** `Surveillance-MVP/Migrations/ManualScripts/SeedTaskTemplateConfiguration.sql`

**What it does:**
- Creates 10 task templates for common diseases
- Assigns templates to diseases (Measles, COVID-19, TB, Salmonella, etc.)
- Sets up task inheritance for disease hierarchies

**Execute after Script 1**

---

## ?? STEP 2: Test Auto-Creation

### Test Case 1: Measles Case
1. Navigate to **Cases ? Create New Case**
2. Select or create a patient
3. Select disease: **Measles**
4. Set Date of Onset: Today
5. Set Date of Notification: Today
6. Click **Create**

**Expected Result:** ?
- Case created successfully
- 2 tasks auto-created:
  1. **Measles Isolation** (High priority, due 4 days from onset)
  2. **Urgent Contact Tracing** (Urgent priority, due 1 day from notification)

### Test Case 2: COVID-19 Case
1. Create a new case
2. Select disease: **COVID-19** (or similar)
3. Complete and save

**Expected Result:** ?
- Daily Symptom Check task created for contacts

### Test Case 3: Tuberculosis Case
1. Create a new case
2. Select disease: **Tuberculosis**
3. Complete and save

**Expected Result:** ?
- TB Contact Investigation task created

---

## ?? STEP 3: View Tasks on Case Details

1. After creating the case, you'll be redirected to **Case Details** page
2. Scroll down to the right column
3. Look for the **Tasks** section (below Exposures)

**What you should see:** ?
```
???????????????????????????????????????????????
? ?? Tasks (2)                                ?
???????????????????????????????????????????????
? Task Name              Priority   Due Date  ?
? Measles Isolation      ?? High    Feb 12    ?
? Urgent Contact Tracing ?? Urgent  ?? Due Now?
???????????????????????????????????????????????
Total: 2 | Pending: 2 | In Progress: 0
```

**Task Details Include:**
- ? Task name and description
- ? Task type (badge)
- ? Priority (color-coded badge)
- ? Assigned to (Patient, Investigator, etc.)
- ? Due date
- ? Status (Pending, In Progress, Completed, Overdue)

---

## ?? STEP 4: Test Disease Change

1. Go to **Cases ? Edit** for an existing case
2. Change the disease to a different one (e.g., from Measles to COVID-19)
3. Click **Save**
4. Return to **Case Details**

**Expected Result:** ?
- Old tasks remain (not deleted)
- **NEW tasks for the new disease are created**

---

## ?? STEP 5: Verify in Database

### Check CaseTasks Table
```sql
SELECT 
    ct.Id,
    c.FriendlyId AS CaseId,
    tt.Name AS TaskName,
    ct.Priority,
    ct.Status,
    ct.DueDate,
    ct.CreatedAt
FROM CaseTasks ct
INNER JOIN Cases c ON ct.CaseId = c.Id
INNER JOIN TaskTemplates tt ON ct.TaskTemplateId = tt.Id
ORDER BY ct.CreatedAt DESC;
```

**Expected Result:** ?
- One row per auto-created task
- Tasks linked to correct case
- Due dates calculated correctly
- Status = Pending (0)

### Check DiseaseTaskTemplates Table
```sql
SELECT 
    d.Name AS DiseaseName,
    tt.Name AS TaskTemplateName,
    dtt.AutoCreateOnCaseCreation,
    dtt.IsActive
FROM DiseaseTaskTemplates dtt
INNER JOIN Diseases d ON dtt.DiseaseId = d.Id
INNER JOIN TaskTemplates tt ON dtt.TaskTemplateId = tt.Id
WHERE dtt.IsActive = 1
ORDER BY d.Name, tt.Name;
```

**Expected Result:** ?
- Diseases have assigned task templates
- AutoCreateOnCaseCreation = 1 for templates that should trigger

---

## ?? Visual Indicators to Check

### Priority Colors
- ?? **Low** - Blue badge
- ? **Medium** - Gray badge  
- ?? **High** - Yellow badge
- ?? **Urgent** - Red badge

### Status Colors
- ? **Pending** - Gray badge
- ?? **In Progress** - Blue badge
- ?? **Completed** - Green badge
- ? **Cancelled** - Dark badge
- ?? **Overdue** - Red badge (with red row background)

### Due Date Warnings
- ?? **Overdue** - Red warning badge
- ?? **Due Today** - Yellow warning badge
- ?? **Due Soon** - Blue info badge (within 3 days)

---

## ? Troubleshooting

### Problem: No tasks created
**Check:**
1. Disease has task templates in Settings ? Diseases ? Edit ? Tasks tab
2. Task templates have `TriggerType = OnCaseCreation`
3. Templates are Active (`IsActive = 1`)
4. Debug window shows task creation logs

**Quick Fix:**
```sql
-- Verify disease has templates
SELECT * FROM DiseaseTaskTemplates 
WHERE DiseaseId = '<your-disease-guid>' AND IsActive = 1;

-- If empty, assign templates manually or re-run seed script
```

### Problem: Tasks not showing on Case Details
**Check:**
1. Tasks exist in `CaseTasks` table for the case
2. Page model includes `public List<CaseTask> Tasks { get; set; }`
3. `GetTasksForCase()` is called in `OnGetAsync()`

### Problem: Build errors
**Check:**
1. All using statements present in Details.cshtml.cs
2. ITaskService registered in Program.cs
3. Correct enum names (CaseTaskStatus.Pending, not NotStarted)

---

## ?? Success Criteria

You'll know it's working when:
- [x] Seed scripts execute without errors
- [x] New cases automatically get tasks in the database
- [x] Tasks section appears on Case Details page
- [x] Tasks display with correct priorities and due dates
- [x] Status badges are color-coded
- [x] Empty state shows when no tasks exist
- [x] Task count matches between UI and database

---

## ?? Sample Test Data

### Diseases with Task Templates (After Seed)
1. **Measles** - 2 tasks
   - Measles Isolation
   - Urgent Contact Tracing

2. **COVID-19** - 1 task
   - Daily Symptom Check (for contacts)

3. **Tuberculosis** - 1 task
   - TB Contact Investigation

4. **Meningococcal Disease** - 1 task
   - Prophylactic Antibiotics (for contacts)

5. **Salmonella** (all subtypes) - 1 task
   - Detailed Food History Questionnaire

6. **Legionellosis** - 1 task
   - Water System Exposure Investigation

7. **Pertussis** - 1 task
   - Identify Vulnerable Contacts

---

## ?? What to Look For

### In the UI
1. **Case Create Page** - No changes (tasks created in background)
2. **Case Details Page** - New "Tasks" section in right column
3. **Task Display** - Table with all task details
4. **Empty State** - Friendly message when no tasks
5. **Color Coding** - Priorities and statuses clearly visible

### In the Database
1. **CaseTasks** - Populated when case is created
2. **DiseaseTaskTemplates** - Links between diseases and templates
3. **TaskTemplates** - 10 templates created by seed script
4. **TaskTypes** - 8 customizable types created

### In Debug Output
Look for these messages:
```
Tasks auto-created for case <CaseId>
Task created: <TaskName> for case <CaseId>
```

---

## ?? Ready to Test!

1. ? Run seed scripts
2. ? Create a test case with Measles or COVID-19
3. ? Check Case Details page for Tasks section
4. ? Verify tasks in database
5. ? Test disease change scenario
6. ? Celebrate! ??

---

*Quick Reference Card*
*Last Updated: February 6, 2026*
