# ?? Survey Template Library - Test Guide

## Test Scenarios

### **? Test 1: Create Survey Template**

**Steps:**
1. Navigate to Settings ? Survey Template Library
2. Click "Create Survey Template"
3. Fill in:
   ```
   Name: Test Food Survey
   Category: Foodborne
   Description: Test survey for food investigations
   Tags: test, food, demo
   ```
4. Select diseases: Salmonella, E. coli
5. Enter simple survey JSON:
   ```json
   {
     "title": "Food History",
     "elements": [
       {
         "type": "text",
         "name": "lastMeal",
         "title": "What was your last meal?"
       }
     ]
   }
   ```
6. Add input mapping:
   ```json
   {
     "patientName": "Patient.GivenName"
   }
   ```
7. Click "Validate JSON" ? Should show success
8. Click "Preview Survey" ? Should display survey
9. Click "Create Survey Template"

**Expected Result:**
- ? Template created successfully
- ? Redirects to Survey Template Library
- ? Shows success message
- ? Template appears in list with 0 usage count

---

### **? Test 2: Edit Survey Template**

**Steps:**
1. From Survey Template Library, click "Edit" on template
2. Change name to "Test Food Survey v2"
3. Update survey JSON (add new question):
   ```json
   {
     "title": "Food History",
     "elements": [
       {
         "type": "text",
         "name": "lastMeal",
         "title": "What was your last meal?"
       },
       {
         "type": "text",
         "name": "drinkWater",
         "title": "Did you drink tap water?"
       }
     ]
   }
   ```
4. Click "Update Survey Template"

**Expected Result:**
- ? Template updated successfully
- ? Version incremented from 1 to 2
- ? Shows success message with version info
- ? Changes reflected in list

---

### **? Test 3: View Template Details**

**Steps:**
1. From Survey Template Library, click "View" on template
2. Review all displayed information

**Expected Result:**
- ? Shows template name, category, version
- ? Shows applicable diseases
- ? Shows usage count (should be 0)
- ? Shows formatted JSON
- ? Preview button works
- ? Edit button present (if not system template)
- ? No task templates listed in "Used By" section

---

### **? Test 4: Use Template in Task Template**

**Steps:**
1. Navigate to Settings ? Task Templates
2. Click Edit on any task template
3. Go to "Survey Configuration" tab
4. Select radio button "Use Survey Library"
5. Select "Test Food Survey v2" from dropdown
6. Click "Save Survey Configuration"

**Expected Result:**
- ? Configuration saved successfully
- ? Shows "Using library survey" badge
- ? Survey library section visible
- ? Custom survey section hidden
- ? Links to view/edit template work

---

### **? Test 5: Switch from Library to Custom**

**Steps:**
1. On same task template, go to Survey Configuration tab
2. Select radio button "Custom Survey"
3. Enter custom survey JSON
4. Click "Save Survey Configuration"

**Expected Result:**
- ? Configuration saved
- ? Custom survey section visible
- ? Library survey section hidden
- ? Template dropdown cleared
- ? `SurveyTemplateId` set to null in database

---

### **? Test 6: Usage Tracking**

**Steps:**
1. Configure task template to use library survey (Test 4)
2. Go back to Survey Template Library
3. Find "Test Food Survey v2"
4. Check usage count

**Expected Result:**
- ? Usage count shows "1"
- ? "1 Tasks Using" badge displayed
- ? Details page lists the task template

---

### **? Test 7: Delete Protection (In Use)**

**Steps:**
1. With template still used by task template
2. Click "Delete" button on template
3. Confirm deletion

**Expected Result:**
- ? Deletion fails
- ? Error message: "Cannot delete: Template is used by 1 task template(s)"
- ? Template still exists

---

### **? Test 8: Remove Template from Task**

**Steps:**
1. Edit task template that uses library survey
2. Go to Survey Configuration tab
3. Select "Custom Survey" or clear survey entirely
4. Save

**Expected Result:**
- ? Task template no longer references library
- ? Survey template usage count decremented
- ? Template can now be deleted

---

### **? Test 9: Delete Template (Success)**

**Steps:**
1. Ensure template not used by any task
2. Click "Delete" on template
3. Confirm deletion

**Expected Result:**
- ? Deletion succeeds
- ? Success message: "Survey template deleted successfully"
- ? Template removed from list
- ? Template no longer in database

---

### **? Test 10: System Template Protection**

**Steps:**
1. Create a template
2. Manually set `IsSystemTemplate = 1` in database
3. Try to edit or delete

**Expected Result:**
- ? Edit blocked (disabled in UI or redirects with error)
- ? Delete blocked with error: "Cannot delete system templates"
- ? "System Template" badge displayed

---

### **? Test 11: Survey Service Integration**

**Steps:**
1. Create survey template with mappings
2. Configure task template to use it
3. Configure disease to use that task template
4. Create a case with that disease
5. Navigate to the auto-created task
6. Click "Complete Survey"

**Expected Result:**
- ? Survey loads from library (not embedded)
- ? Pre-populated fields work (input mapping)
- ? Survey displays correctly
- ? Can submit survey
- ? Output mapping saves data to case

---

### **? Test 12: Usage Tracking on Survey Completion**

**Steps:**
1. Before completing survey, note `UsageCount` and `LastUsedAt`
2. Complete survey (Test 11)
3. Check template in database or details page

**Expected Result:**
- ? `UsageCount` incremented
- ? `LastUsedAt` updated to current timestamp
- ? Details page shows updated stats

---

### **? Test 13: Backwards Compatibility**

**Steps:**
1. Find task template with embedded survey (not library)
2. Create case with that disease
3. Complete task survey

**Expected Result:**
- ? Old embedded survey still works
- ? No errors or warnings
- ? Survey loads and saves correctly
- ? Doesn't affect library templates

---

### **? Test 14: Search and Filter**

**Steps:**
1. Create multiple templates with different categories/tags
2. Use search box: Enter "food"
3. Use category filter: Select "Foodborne"
4. Use status filter: Select "Active Only"

**Expected Result:**
- ? Search filters by name, description, tags
- ? Category filter works
- ? Status filter works
- ? Can combine filters
- ? "Clear" button resets all filters

---

### **? Test 15: Disease Association Display**

**Steps:**
1. Create template with 5 diseases selected
2. View in list page
3. View in details page

**Expected Result:**
- ? List page shows first 3 diseases + "+2 more" badge
- ? Details page shows all 5 diseases
- ? Disease names display correctly
- ? Can unselect diseases in edit

---

### **? Test 16: JSON Validation**

**Steps:**
1. Create/Edit template
2. Enter invalid JSON: `{bad json`
3. Click "Validate JSON"
4. Try to save

**Expected Result:**
- ? Validation fails with error message
- ? Save fails with model error
- ? User stays on page with error displayed

---

### **? Test 17: Survey Preview**

**Steps:**
1. Create/Edit template
2. Enter valid survey JSON
3. Click "Preview Survey"

**Expected Result:**
- ? Modal opens
- ? Survey renders correctly in modal
- ? Can interact with survey questions
- ? Can close modal
- ? Preview doesn't save data

---

### **? Test 18: Version Display**

**Steps:**
1. Create template (Version 1)
2. Edit name only, save
3. Edit survey JSON, save
4. Edit name again, save

**Expected Result:**
- After step 1: Version = 1
- After step 2: Version = 1 (unchanged)
- After step 3: Version = 2 (incremented)
- After step 4: Version = 2 (unchanged)

---

### **? Test 19: Multiple Task Templates Using Same Survey**

**Steps:**
1. Create one survey template
2. Configure 3 different task templates to use it
3. Check survey template details

**Expected Result:**
- ? Usage count = 3
- ? Details page lists all 3 task templates
- ? Can click to edit each task template
- ? Editing survey affects all 3 tasks

---

### **? Test 20: Mapping Override**

**Steps:**
1. Create survey template with default mappings
2. Use in task template
3. Override mappings in task template
4. Complete survey

**Expected Result:**
- ? Uses survey from library
- ? Uses overridden mappings (not defaults)
- ? Mappings work correctly

---

## ?? Database Verification Queries

### Check Template:
```sql
SELECT * FROM SurveyTemplates WHERE Name LIKE '%Test%'
```

### Check Usage:
```sql
SELECT tt.Name, tt.SurveyTemplateId
FROM TaskTemplates tt
WHERE tt.SurveyTemplateId IS NOT NULL
```

### Check Disease Association:
```sql
SELECT st.Name, d.Name as DiseaseName
FROM SurveyTemplates st
JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
JOIN Diseases d ON std.DiseaseId = d.Id
WHERE st.Name LIKE '%Test%'
```

---

## ? Checklist

- [ ] Test 1: Create Template
- [ ] Test 2: Edit Template
- [ ] Test 3: View Details
- [ ] Test 4: Use in Task Template
- [ ] Test 5: Switch to Custom
- [ ] Test 6: Usage Tracking
- [ ] Test 7: Delete Protection (In Use)
- [ ] Test 8: Remove from Task
- [ ] Test 9: Delete Template
- [ ] Test 10: System Template Protection
- [ ] Test 11: Survey Service Integration
- [ ] Test 12: Usage Tracking on Completion
- [ ] Test 13: Backwards Compatibility
- [ ] Test 14: Search and Filter
- [ ] Test 15: Disease Association
- [ ] Test 16: JSON Validation
- [ ] Test 17: Survey Preview
- [ ] Test 18: Version Control
- [ ] Test 19: Multiple Task Usage
- [ ] Test 20: Mapping Override

---

**All tests passed? ? Ready for production!**
