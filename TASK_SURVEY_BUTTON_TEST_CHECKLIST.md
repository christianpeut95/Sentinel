# ?? Task Survey Button - Quick Test Checklist

## Before Testing
- [ ] **Hot Reload** or **Restart Application**
- [ ] Have at least one survey template in the library
- [ ] Have at least one task template configured

---

## ? Test Scenarios

### **Scenario 1: Template Task with Library Survey**
1. [ ] Go to Settings ? Task Templates
2. [ ] Edit a task template
3. [ ] Go to Survey Configuration tab
4. [ ] Select "Use Survey Library"
5. [ ] Choose a survey from dropdown
6. [ ] Save
7. [ ] Go to Settings ? Diseases
8. [ ] Edit a disease
9. [ ] Assign the task template to the disease
10. [ ] Create a new case with that disease
11. [ ] Navigate to case details
12. [ ] **Expected**: Task shows "?? Complete Survey" button (blue)
13. [ ] Click "Complete Survey"
14. [ ] **Expected**: Survey page opens

**Result**: ? Pass / ? Fail

---

### **Scenario 2: Template Task with Embedded Survey**
1. [ ] Go to Settings ? Task Templates
2. [ ] Create or edit a task template
3. [ ] Go to Survey Configuration tab
4. [ ] Select "Custom Survey"
5. [ ] Enter JSON:
   ```json
   {
     "title": "Test Survey",
     "elements": [
       {
         "type": "text",
         "name": "question1",
         "title": "Test Question"
       }
     ]
   }
   ```
6. [ ] Save
7. [ ] Assign to disease
8. [ ] Create case with that disease
9. [ ] Navigate to case details
10. [ ] **Expected**: Task shows "?? Complete Survey" button (blue)

**Result**: ? Pass / ? Fail

---

### **Scenario 3: Template Task WITHOUT Survey**
1. [ ] Go to Settings ? Task Templates
2. [ ] Create or edit a task template
3. [ ] Go to Survey Configuration tab
4. [ ] Leave "No Survey" selected (or clear survey)
5. [ ] Save
6. [ ] Assign to disease
7. [ ] Create case with that disease
8. [ ] Navigate to case details
9. [ ] **Expected**: Task shows "? Complete" button (green)

**Result**: ? Pass / ? Fail

---

### **Scenario 4: Manual Task with Library Survey**
1. [ ] Navigate to any case details page
2. [ ] Click "Add Task"
3. [ ] Switch to "Manual Entry" tab
4. [ ] Fill in:
   - Title: "Test Manual Task with Survey"
   - Task Type: Interview
   - Priority: Medium
5. [ ] Check "Include a survey with this task"
6. [ ] Select "Use Survey Library"
7. [ ] Choose a survey from dropdown
8. [ ] Click "Create Task"
9. [ ] **Expected**: Success message with "Survey configured"
10. [ ] **Expected**: Task shows "?? Complete Survey" button (blue)
11. [ ] Click "Complete Survey"
12. [ ] **Expected**: Survey page opens with selected survey

**Result**: ? Pass / ? Fail

---

### **Scenario 5: Manual Task with Custom Survey**
1. [ ] Navigate to any case details page
2. [ ] Click "Add Task"
3. [ ] Switch to "Manual Entry" tab
4. [ ] Fill in:
   - Title: "Test Manual Task Custom"
   - Task Type: Follow-up
   - Priority: High
5. [ ] Check "Include a survey with this task"
6. [ ] Select "Custom Survey JSON"
7. [ ] Enter JSON:
   ```json
   {
     "title": "Custom Test",
     "elements": [
       {
         "type": "text",
         "name": "customQuestion",
         "title": "Custom Question"
       }
     ]
   }
   ```
8. [ ] Click "Create Task"
9. [ ] **Expected**: Success message with "Survey configured"
10. [ ] **Expected**: Task shows "?? Complete Survey" button (blue)
11. [ ] Click "Complete Survey"
12. [ ] **Expected**: Survey page opens with custom survey

**Result**: ? Pass / ? Fail

---

### **Scenario 6: Manual Task WITHOUT Survey**
1. [ ] Navigate to any case details page
2. [ ] Click "Add Task"
3. [ ] Switch to "Manual Entry" tab
4. [ ] Fill in:
   - Title: "Simple Manual Task"
   - Task Type: Investigation
   - Priority: Low
5. [ ] Do NOT check "Include a survey"
6. [ ] Click "Create Task"
7. [ ] **Expected**: Success message (no survey mention)
8. [ ] **Expected**: Task shows "? Complete" button (green)
9. [ ] Click "Complete"
10. [ ] **Expected**: Completion modal opens (not survey page)

**Result**: ? Pass / ? Fail

---

## ?? Visual Checks

### Button Appearance
- [ ] **Complete Survey** button is **blue** (btn-primary)
- [ ] **Complete Survey** button has **clipboard icon** (bi-clipboard-check)
- [ ] **Mark Complete** button is **green** (btn-success)
- [ ] **Mark Complete** button has **check circle icon** (bi-check-circle)

### Button Behavior
- [ ] **Complete Survey** navigates to `/Tasks/CompleteSurvey?taskId=...`
- [ ] **Mark Complete** opens a modal (doesn't navigate)
- [ ] Buttons only show for tasks that are NOT completed or cancelled

---

## ?? Common Issues & Solutions

### Issue: "Complete Survey" button not showing
**Check:**
- [ ] Is `TaskTemplate` properly loaded? (Check browser dev tools network tab)
- [ ] Does `TaskTemplate.SurveyTemplateId` have a value in database?
- [ ] Did you hot reload or restart the app after code changes?

**Solution:**
```sql
-- Check if survey is linked
SELECT Id, Name, SurveyTemplateId, SurveyDefinitionJson 
FROM TaskTemplates 
WHERE Id = '<your-task-template-id>'
```

---

### Issue: Button shows but survey page errors
**Check:**
- [ ] Is the survey template active?
- [ ] Does the survey JSON have valid SurveyJS format?
- [ ] Check browser console for JavaScript errors

**Solution:**
- Validate JSON in survey template
- Check survey template is not deleted
- Verify SurveyJS library is loaded

---

### Issue: Manual task shows wrong button
**Check:**
- [ ] Was temporary task template created?
- [ ] Check if `CaseTask.TaskTemplateId` is not null
- [ ] Verify survey was included during creation

**Solution:**
```sql
-- Check manual task setup
SELECT ct.Id, ct.Title, ct.TaskTemplateId, tt.SurveyTemplateId, tt.SurveyDefinitionJson
FROM CaseTasks ct
LEFT JOIN TaskTemplates tt ON ct.TaskTemplateId = tt.Id
WHERE ct.Title = '<your-manual-task-title>'
```

---

## ?? Test Summary

| Scenario | Expected Button | Expected Action | Result |
|----------|----------------|-----------------|--------|
| Template + Library | Complete Survey (Blue) | Navigate to survey | ? |
| Template + Embedded | Complete Survey (Blue) | Navigate to survey | ? |
| Template + No Survey | Complete (Green) | Open modal | ? |
| Manual + Library | Complete Survey (Blue) | Navigate to survey | ? |
| Manual + Custom | Complete Survey (Blue) | Navigate to survey | ? |
| Manual + No Survey | Complete (Green) | Open modal | ? |

**Legend:** ? Not tested | ? Passed | ? Failed

---

## ? Sign-Off

**Tester Name**: ___________________  
**Date**: ___________________  
**All Tests Passed**: ? Yes ? No  
**Issues Found**: ___________________  

---

## ?? Notes
_Add any observations, issues, or suggestions here:_

---

**Last Updated**: February 7, 2026
