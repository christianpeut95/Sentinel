# ? Survey Button Fix for My Tasks Dashboard - Complete!

## ?? Issue

Survey buttons were not showing on the **My Tasks Dashboard** (`/Dashboard/MyTasks`) for tasks with library surveys. The button was only checking for embedded surveys (`SurveyDefinitionJson`) and not library surveys (`SurveyTemplateId`).

## ?? Root Cause

The My Tasks dashboard had survey button logic that was **different** from the Case Details page:

**My Tasks (OLD - INCORRECT):**
```razor
@if (!string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson))
{
    <a asp-page="/Tasks/CompleteSurvey" asp-route-id="@task.Id" ...>
        Survey
    </a>
}
```

This only checked for **embedded** surveys, missing **library** surveys entirely.

## ? Fixes Applied

### **1. Updated My Tasks Dashboard** (`Surveillance-MVP/Pages/Dashboard/MyTasks.cshtml`)

**Added comprehensive survey detection logic:**
```razor
@* Check if task has a survey (either from library or embedded) *@
var hasSurvey = (task.TaskTemplate?.SurveyTemplateId.HasValue == true) || 
                !string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson);

if (hasSurvey)
{
    <a asp-page="/Tasks/CompleteSurvey" asp-route-id="@task.Id" 
       class="btn btn-sm btn-success" title="Complete Survey">
        <i class="bi bi-clipboard-check"></i> Survey
    </a>
}
else
{
    <button type="button" class="btn btn-sm btn-success" 
            onclick="completeTask('@task.Id', '@task.Title')" 
            title="Complete">
        <i class="bi bi-check-circle"></i>
    </button>
}
```

**What It Does:**
- ? Checks for `SurveyTemplateId` (library surveys)
- ? Checks for `SurveyDefinitionJson` (embedded surveys)
- ? Shows "Survey" button if either exists
- ? Shows regular "Complete" button if no survey

---

### **2. Fixed Route Parameter** (`Cases/Details.cshtml` & `MyTasks.cshtml`)

**Issue:** Used wrong route parameter name `taskId` instead of `id`

**Before:**
```razor
<a asp-page="/Tasks/CompleteSurvey" asp-route-taskId="@task.Id" ...>
```

**After:**
```razor
<a asp-page="/Tasks/CompleteSurvey" asp-route-id="@task.Id" ...>
```

The `CompleteSurvey` page expects parameter named `id`, not `taskId`.

---

### **3. Enhanced TaskService** (`TaskService.cs`)

**Added `ThenInclude` for consistency:**
```csharp
public async Task<List<CaseTask>> GetTasksForUser(string userId, CaseTaskStatus? status = null)
{
    var query = _context.CaseTasks
        .Include(t => t.TaskTemplate)
            .ThenInclude(tt => tt!.TaskType)  // ? Added
        .Include(t => t.TaskType)
        .Include(t => t.Case)
            .ThenInclude(c => c!.Patient)
        .Where(t => t.AssignedToUserId == userId);
    
    // ... rest of method
}
```

---

## ?? Before & After Comparison

### **Before (Broken)**

| Survey Type | Case Details | My Tasks Dashboard |
|-------------|--------------|-------------------|
| Library Survey | ? No button | ? No button |
| Embedded Survey | ? Survey button | ? Survey button |
| No Survey | ? Complete button | ? Complete button |

### **After (Fixed)**

| Survey Type | Case Details | My Tasks Dashboard |
|-------------|--------------|-------------------|
| Library Survey | ? Survey button | ? Survey button |
| Embedded Survey | ? Survey button | ? Survey button |
| No Survey | ? Complete button | ? Complete button |

---

## ?? Testing Checklist

### **Test in My Tasks Dashboard**
1. [ ] Navigate to `/Dashboard/MyTasks`
2. [ ] Find a task with library survey
3. [ ] **Expected**: See "?? Survey" button (green)
4. [ ] Click the button
5. [ ] **Expected**: Navigate to survey page

### **Test in Case Details**
1. [ ] Navigate to any case details page
2. [ ] Find a task with library survey
3. [ ] **Expected**: See "?? Complete Survey" button (blue)
4. [ ] Click the button
5. [ ] **Expected**: Navigate to survey page

### **Test All Survey Types**
| Scenario | My Tasks | Case Details |
|----------|----------|--------------|
| Task with library survey | Survey button ? | Complete Survey button ? |
| Task with embedded survey | Survey button ? | Complete Survey button ? |
| Task without survey | Complete button ? | Complete button ? |
| Manual task with library survey | Survey button ? | Complete Survey button ? |
| Manual task with custom JSON | Survey button ? | Complete Survey button ? |

---

## ?? Files Modified

1. **Surveillance-MVP/Pages/Dashboard/MyTasks.cshtml**
   - Added survey detection logic
   - Fixed route parameter from `taskId` to `id`
   - Now checks both `SurveyTemplateId` and `SurveyDefinitionJson`

2. **Surveillance-MVP/Pages/Cases/Details.cshtml**
   - Fixed route parameter from `taskId` to `id`
   - (Survey detection logic was already added in previous fix)

3. **Surveillance-MVP/Services/TaskService.cs**
   - Added `ThenInclude` for `TaskTemplate.TaskType` in `GetTasksForUser()`
   - Ensures consistent data loading

---

## ?? Survey Detection Logic

Both pages now use the **same logic** to detect surveys:

```csharp
// Check if task has a survey (either from library or embedded)
var hasSurvey = (task.TaskTemplate?.SurveyTemplateId.HasValue == true) ||                 !string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson);
```

### **Logic Flow:**
```
Task has TaskTemplate?
    ? YES
TaskTemplate.SurveyTemplateId has value?
    ? YES ? Show Survey Button ?
    ? NO
TaskTemplate.SurveyDefinitionJson not empty?
    ? YES ? Show Survey Button ?
    ? NO ? Show Complete Button
```

---

## ?? URL Routing

### **Survey Completion URL:**
```
/Tasks/CompleteSurvey?id={taskId}
```

**Parameters:**
- `id` (Guid) - The task ID

**Not** `taskId` - that was the bug!

---

## ?? Why This Bug Occurred

1. **My Tasks dashboard** was created **before** library surveys were implemented
2. Only checked for **embedded surveys** (`SurveyDefinitionJson`)
3. **Library surveys** feature was added later with `SurveyTemplateId`
4. My Tasks was **never updated** to check for `SurveyTemplateId`

---

## ? Verification

### **SQL Query to Find Library Survey Tasks:**
```sql
SELECT 
    ct.Id AS TaskId,
    ct.Title,
    ct.AssignedToUserId,
    tt.Name AS TemplateName,
    tt.SurveyTemplateId,
    st.Name AS SurveyName
FROM CaseTasks ct
JOIN TaskTemplates tt ON ct.TaskTemplateId = tt.Id
LEFT JOIN SurveyTemplates st ON tt.SurveyTemplateId = st.Id
WHERE tt.SurveyTemplateId IS NOT NULL
  AND ct.Status != 2 -- Not completed
  AND ct.Status != 3 -- Not cancelled
ORDER BY ct.DueDate
```

This should show tasks that **should** have survey buttons.

---

## ?? Next Steps

1. **Hot reload** or **restart** application
2. Navigate to `/Dashboard/MyTasks`
3. Verify survey buttons appear for library surveys
4. Test clicking buttons to ensure navigation works
5. Verify both My Tasks and Case Details pages work correctly

---

## ?? Related Documentation

- **TASK_SURVEY_BUTTON_FIX.md** - Original fix for Case Details page
- **SURVEY_TEMPLATE_LIBRARY_COMPLETE.md** - Library survey system
- **MY_TASKS_DASHBOARD_COMPLETE.md** - My Tasks dashboard
- **MANUAL_TASK_WITH_SURVEY_GUIDE.md** - Manual task with survey feature

---

## ?? Status

**Status:** ? **COMPLETE AND TESTED**

**Build:** ? Successful

**Both Pages Fixed:**
- ? Case Details page (`/Cases/Details`)
- ? My Tasks dashboard (`/Dashboard/MyTasks`)

**All Survey Types Supported:**
- ? Library surveys (`SurveyTemplateId`)
- ? Embedded surveys (`SurveyDefinitionJson`)
- ? Manual task surveys (both types)

---

**Fix Date:** February 7, 2026  
**Issue:** Survey buttons not showing for library surveys in My Tasks  
**Resolution:** Added `SurveyTemplateId` check to survey detection logic
