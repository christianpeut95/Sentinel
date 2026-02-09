# ?? Survey Active Version Fix - Always Use Latest

## ? The Problem

When completing a task with a survey, the system was loading the **original archived version** instead of the **currently active version**.

### Scenario:
```
1. Task created ? Linked to Survey v1.0 (Active)
2. Survey updated ? v2.0 published (Active), v1.0 archived
3. User completes task ? Loads v1.0 (Archived) ?
```

**Expected Behavior:**
```
User completes task ? Loads v2.0 (Active) ?
```

---

## ?? Root Cause

### Original Code Logic:
```csharp
// Old code in SurveyService.cs
var surveyTemplate = await _context.SurveyTemplates
    .AsNoTracking()
    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId && st.IsActive);
```

**Problem:** 
- Query looked for exact survey template ID + `IsActive = true`
- When a version is archived, `IsActive` stays `true` (it's for soft-delete)
- But `VersionStatus` changes to `Archived`
- So query returned the archived version, not the active one!

**What Was Missing:**
- No check for `VersionStatus == Active`
- No lookup of the survey family's active version
- Tasks were "locked" to their original version

---

## ? The Solution

### 1. Always Find the Active Version of a Survey Family

**New Logic:**
```csharp
// Step 1: Get the original template (may be archived)
var originalTemplate = await _context.SurveyTemplates
    .AsNoTracking()
    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId);

// Step 2: Find the root parent of this survey family
var rootParentId = originalTemplate.ParentSurveyTemplateId ?? originalTemplate.Id;

// Step 3: Always use the ACTIVE version from this family
surveyTemplate = await _context.SurveyTemplates
    .AsNoTracking()
    .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
    .Where(st => st.VersionStatus == SurveyVersionStatus.Active)
    .FirstOrDefaultAsync();
```

**Benefits:**
? Tasks automatically use the latest active version  
? Survey improvements apply to all existing tasks  
? No need to update tasks when publishing new versions  
? Fallback to original version if no active version exists  

---

## ?? How It Works Now

### Data Flow:

```
Task Created
    ?
TaskTemplate.SurveyTemplateId = v1.0 ID
    ?
[Time passes, new version published]
    ?
User completes task
    ?
???????????????????????????????????????
? 1. Load original template (v1.0)   ?
?    Status: Archived                 ?
???????????????????????????????????????
    ?
???????????????????????????????????????
? 2. Find root parent ID              ?
?    rootParentId = v1.0.Id           ?
?    (v1.0 has no parent)             ?
???????????????????????????????????????
    ?
???????????????????????????????????????
? 3. Find ACTIVE version in family    ?
?    Query: WHERE                     ?
?      (Id = rootParentId OR          ?
?       ParentSurveyTemplateId =      ?
?       rootParentId)                 ?
?    AND VersionStatus = Active       ?
???????????????????????????????????????
    ?
???????????????????????????????????????
? 4. Found v2.0 (Active)              ?
?    Use v2.0 for task!               ?
???????????????????????????????????????
    ?
Survey loads with latest version ?
```

---

## ?? Testing

### Test Case 1: Task Uses Active Version

**Setup:**
1. Create survey template "Food History v1.0"
2. Create task template linked to this survey
3. Create a case task from this template
4. Publish new version "Food History v2.0"
5. Archive v1.0

**Execute:**
1. Navigate to task
2. Click "Complete Survey"

**Expected Result:**
- ? Survey v2.0 loads (the active version)
- ? Console log shows: "Using Survey Library template {Id} (Version 2.0)"
- ? No errors

**Before Fix:**
- ? Survey v1.0 loaded (archived)
- ? Or no survey loaded (if IsActive filter failed)

---

### Test Case 2: Multiple Versions Exist

**Setup:**
1. Create v1.0 ? Archive
2. Create v1.5 ? Keep as Draft
3. Create v2.0 ? Publish (Active)

**Execute:**
- Complete a task

**Expected Result:**
- ? Loads v2.0 (only Active version)
- ? Ignores v1.0 (Archived)
- ? Ignores v1.5 (Draft)

---

### Test Case 3: No Active Version (Edge Case)

**Setup:**
1. Create v1.0 ? Archive
2. Create v2.0 ? Keep as Draft
3. No active version exists

**Execute:**
- Complete a task originally linked to v1.0

**Expected Result:**
- ? Falls back to v1.0 (original)
- ?? Warning logged: "No active version found, using original template"
- ? Survey still loads

---

### Test Case 4: Brand New Task

**Setup:**
1. Create survey v2.0 (Active)
2. Create new task from template

**Execute:**
- Complete the task

**Expected Result:**
- ? Loads v2.0
- ? Works normally

---

## ?? Version Resolution Logic

### Decision Tree:

```
Task has SurveyTemplateId?
    ?
    ?? NO ? Check embedded survey
    ?
    ?? YES
        ?
        Load original template
        ?
        ?? Not Found? ? Check embedded survey
        ?
        ?? Found
            ?
            Get root parent ID
            ?
            Find Active version in family
            ?
            ?? Active Found? ? ? Use Active Version
            ?
            ?? No Active? ? ?? Use Original (Fallback)
```

---

## ?? Code Changes

### File: `Surveillance-MVP\Services\SurveyService.cs`

**Method:** `GetSurveyForTaskAsync()`

**Lines Changed:** ~37-90

**Key Changes:**
1. ? Added query for original template
2. ? Added root parent ID calculation
3. ? Added query for active version in family
4. ? Added fallback logic if no active version
5. ? Added logging for version switches
6. ? Enhanced usage tracking

---

## ?? Logging

### New Log Messages:

**When Active Version Used:**
```
Information: Task {TaskId} originally linked to version {OriginalVersion}, now using active version {ActiveVersion}
Information: Using Survey Library template {TemplateId} (Version {VersionNumber}) for Task {TaskId}
```

**When No Active Version:**
```
Warning: No active version found for survey family {RootParentId}, using original template {OriginalId}
```

**Monitoring:**
- Check logs to see if tasks are switching versions
- Identify surveys with no active version
- Track version usage patterns

---

## ?? Business Impact

### For Users:
? **Always get the latest survey version**  
? **Improved questions automatically apply**  
? **No need to recreate tasks**  
? **Consistent survey experience**  

### For Administrators:
? **Publish new versions with confidence**  
? **Changes propagate to all tasks automatically**  
? **Easy rollback** (publish previous version)  
? **No orphaned tasks on old versions**  

### For Data Quality:
? **Latest questions capture better data**  
? **Survey improvements benefit all cases**  
? **Standardized data collection**  

---

## ?? Configuration

### No Configuration Needed!

This is **automatic behavior**. The system will always:
1. Look up the survey family
2. Find the active version
3. Use it for the task

### If You Want Different Behavior:

**Option A: Keep Task on Original Version**
- Don't publish new versions
- Edit the existing version instead

**Option B: Lock Specific Tasks**
- *(Future enhancement)*
- Add `LockedSurveyVersionId` field to `CaseTask`
- Check for locked version before lookup

---

## ?? Edge Cases Handled

### 1. Survey Deleted
**Scenario:** Original survey deleted from database  
**Behavior:** Query returns null, falls back to embedded survey  
**Outcome:** ? Graceful degradation  

### 2. All Versions Archived
**Scenario:** No active version in family  
**Behavior:** Uses original (archived) version  
**Outcome:** ?? Warning logged, but survey still loads  

### 3. Multiple Active Versions (Bug)
**Scenario:** Data corruption - 2+ active versions  
**Behavior:** `.FirstOrDefaultAsync()` returns first match  
**Outcome:** ?? Works, but unpredictable which version  
**Fix:** Publish endpoint prevents this  

### 4. Circular Parent References (Bug)
**Scenario:** v2.0.ParentId = v1.0, v1.0.ParentId = v2.0  
**Behavior:** rootParentId = v1.0.Id (takes value from original)  
**Outcome:** ? Breaks circular reference  

---

## ?? Upgrade Path

### Existing Tasks Automatically Updated

**No migration needed!** Existing tasks will automatically use active versions on next completion.

**Timeline:**
```
Before Fix:
- Task1 ? v1.0 (archived) ?
- Task2 ? v1.0 (archived) ?
- Task3 ? v1.0 (archived) ?

After Restart:
- Task1 ? v2.0 (active) ?
- Task2 ? v2.0 (active) ?
- Task3 ? v2.0 (active) ?
```

**No Action Required By:**
- ? Administrators
- ? Users
- ? Database

---

## ?? Related Documentation

- `SURVEY_VERSIONING_COMPLETE_GUIDE.md` - Versioning system
- `SURVEY_VERSION_UI_COMPLETE.md` - Version management UI
- `SURVEY_RESULTS_STORAGE_GUIDE.md` - How responses are stored
- `SURVEY_SYSTEM_100_PERCENT_COMPLETE.md` - Overall survey system

---

## ?? FAQ

### Q: Will existing tasks change?
**A:** Yes! They'll automatically use the active version on next completion.

### Q: What if I want to keep old version?
**A:** Don't publish a new version. Edit the existing one instead.

### Q: Can I force a specific version?
**A:** Not currently. All tasks use the active version. (Feature request?)

### Q: What happens to responses already saved?
**A:** They're stored in `CaseTask.SurveyResponseJson` and never change. This only affects which survey loads when completing NEW tasks.

### Q: Does this affect completed tasks?
**A:** No. Completed tasks have their responses saved. This only affects tasks being completed now.

---

## ? Summary

### What Was Fixed:
? **Before:** Tasks loaded archived versions  
? **After:** Tasks always load active versions  

### How It Works:
1. Get original survey template from task
2. Find the survey family's root parent
3. Query for active version in that family
4. Use active version (or fallback to original)

### Benefits:
? Latest surveys apply to all tasks  
? No manual task updates needed  
? Better data quality  
? Easier version management  

### Files Changed:
- `Surveillance-MVP\Services\SurveyService.cs` - Main logic

### Testing:
? Build successful  
?? **Restart required** - Hot reload won't apply this  

---

**Status:** ? **COMPLETE - Restart App to Apply**

**Last Updated:** February 7, 2026  
**Version:** 1.0  
**Impact:** All survey-based tasks
