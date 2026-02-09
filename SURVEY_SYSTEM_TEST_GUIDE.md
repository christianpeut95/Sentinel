# Survey System - Quick Test Guide

## ?? Test the Complete Survey System (10 Minutes)

This guide will walk you through testing all the new admin UI features.

---

## Prerequisites
- App is running
- You're logged in as an admin user
- Database migration has been applied

---

## Test 1: Configure Survey on Task Template (3 min)

### Steps:
1. Navigate to **Settings ? Task Templates**
2. Click **Edit** on any task template (or create a new one)
3. Click the **"Survey Configuration"** tab

### Test Survey JSON:
Paste this simple survey:
```json
{
  "title": "Test Survey",
  "description": "Testing the survey system",
  "elements": [
    {
      "type": "text",
      "name": "patientName",
      "title": "Patient Name",
      "isRequired": true,
      "readOnly": true
    },
    {
      "type": "text",
      "name": "testDate",
      "title": "Test Date",
      "inputType": "date",
      "isRequired": true,
      "defaultValue": "today()"
    },
    {
      "type": "checkbox",
      "name": "symptoms",
      "title": "Symptoms Observed",
      "choices": [
        "Fever",
        "Cough",
        "Fatigue",
        "Shortness of breath"
      ]
    },
    {
      "type": "boolean",
      "name": "followupNeeded",
      "title": "Follow-up needed?",
      "isRequired": true
    },
    {
      "type": "comment",
      "name": "notes",
      "title": "Additional Notes"
    }
  ]
}
```

### Verify:
- [ ] **Validate JSON** button shows "Valid JSON!"
- [ ] **Format JSON** button prettifies the code
- [ ] **Preview Survey** button opens modal with rendered survey
- [ ] Preview shows all 5 questions correctly
- [ ] **Save Survey Configuration** saves successfully
- [ ] Success message appears

---

## Test 2: Configure Survey Mappings (2 min)

### Steps:
1. Navigate to **Settings ? Diseases**
2. Click **Edit** on a disease (preferably one with tasks)
3. Click the **"Tasks & Surveys"** tab

### If No Tasks Show:
Go to **Settings ? Diseases** ? Edit ? Disease Tasks tab and add a task template first.

### Configure Mappings:
1. **Expand** the task you just configured
2. In **Input Mapping JSON**, paste:
```json
{
  "patientName": "Patient.GivenName"
}
```

3. In **Output Mapping JSON**, paste:
```json
{
  "testDate": "Case.CustomFields.TestDate",
  "followupNeeded": "Case.CustomFields.FollowupNeeded"
}
```

4. Click **"Save Mappings"**

### Verify:
- [ ] JSON editors display correctly
- [ ] **Save Mappings** button saves successfully
- [ ] Success message appears: "Survey mappings saved successfully"
- [ ] **Clear Mappings** button clears both fields
- [ ] **Edit Survey Definition** link works

---

## Test 3: Complete Survey as End User (5 min)

### Setup:
1. Create a **new case** for the disease you configured
2. The task should auto-create (if configured as auto-create)
3. If manual, go to **Cases ? Details** and create the task manually

### Steps:
1. Navigate to **Dashboard ? My Tasks**
2. Find the task you created
3. Verify **"Survey" button** appears (clipboard icon)
4. Click **"Survey"** button

### On Survey Page:
1. Verify task context shows (patient name, case info, disease)
2. Verify survey renders correctly
3. Verify **patientName** field is pre-populated (read-only)
4. Verify **testDate** has today's date
5. Select some **symptoms** checkboxes
6. Select **Yes** for follow-up
7. Add some **notes**
8. Verify **"Complete Survey"** button is enabled
9. Click **"Complete Survey"**

### Verify:
- [ ] Survey page loads correctly
- [ ] Task context displays at top
- [ ] Survey renders with SurveyJS styling
- [ ] Pre-populated field shows patient name
- [ ] Date field has today's date
- [ ] All questions render correctly
- [ ] Required field validation works
- [ ] Complete button submits successfully
- [ ] Redirects to My Tasks with success message
- [ ] Task status changes to "Completed"

---

## Test 4: Verify Data Saved (1 min)

### Check Survey Response:
1. Go to **Cases ? Details** for the case
2. Scroll to **Tasks** section
3. Click on the completed task
4. Verify survey response is stored

### Check Custom Fields:
1. On Case Details page
2. Check if custom fields were created/updated:
   - `TestDate` - Should have the date entered
   - `FollowupNeeded` - Should be true/false

### Verify in Database (Optional):
```sql
-- Check survey response
SELECT Title, SurveyResponseJson 
FROM CaseTasks 
WHERE SurveyResponseJson IS NOT NULL;

-- Check custom fields
SELECT * FROM CaseCustomFieldStrings 
WHERE CaseId = 'YOUR_CASE_ID';
```

### Verify:
- [ ] Survey response JSON is stored
- [ ] Custom fields were created/updated
- [ ] Task marked as complete
- [ ] All data persisted correctly

---

## Test 5: Edge Cases (Optional - 2 min)

### Test Invalid JSON:
1. Go back to **Edit Task Template ? Survey Configuration**
2. Paste invalid JSON: `{invalid json}`
3. Click **"Validate JSON"**
4. Verify error message appears
5. Try to save
6. Verify server-side validation rejects it

### Test Clear Survey:
1. In Survey Configuration tab
2. Click **"Clear Survey"** button
3. Confirm the dialog
4. Verify JSON is cleared
5. Save
6. Verify survey is disabled for this task

### Test Clear Mappings:
1. Go to **Disease Edit ? Tasks & Surveys**
2. Expand a task with mappings
3. Click **"Clear Mappings"**
4. Verify both textareas are cleared
5. Save
6. Verify mappings are removed

### Verify:
- [ ] Invalid JSON shows error
- [ ] Can't save invalid JSON
- [ ] Clear Survey works
- [ ] Clear Mappings works
- [ ] All validations work correctly

---

## Test 6: UI/UX Features (1 min)

### Test Tab Persistence:
1. Edit a task template
2. Switch to Survey Configuration tab
3. Refresh the page
4. Verify **Survey Configuration tab is still active**

### Test Accordion:
1. Go to Disease Edit ? Tasks & Surveys
2. Expand a task accordion
3. Collapse it
4. Expand a different one
5. Verify only one accordion open at a time

### Test Modal:
1. Edit Task Template ? Survey Configuration
2. Paste valid survey JSON
3. Click **"Preview Survey"**
4. Verify modal opens
5. Verify survey renders in modal
6. Close modal
7. Verify modal closes cleanly

### Verify:
- [ ] Tab persistence works (localStorage)
- [ ] Accordion works correctly
- [ ] Modal opens and closes properly
- [ ] All UI interactions smooth

---

## ?? Success Criteria

If all tests pass, you have a **fully functional survey system**!

### ? You Should See:
- Survey configuration UI is intuitive
- JSON validation works
- Survey preview renders correctly
- Field mappings save successfully
- Users can complete surveys
- Responses save to database
- Custom fields update correctly
- All UI features work smoothly

### ? If Any Tests Fail:
1. Check browser console for JavaScript errors
2. Check server logs for backend errors
3. Verify database migration applied
4. Verify SurveyJS CDN links in _Layout.cshtml
5. Verify ISurveyService is registered in Program.cs
6. Rebuild solution and restart app

---

## Performance Check

### Survey Load Time:
- Should load in < 500ms
- Check Network tab in browser DevTools

### Survey Save Time:
- Should save in < 1 second
- Check for any delays

### Expected Performance:
- Page load: 50-200ms
- Survey render: 50-100ms
- Survey save: 100-300ms
- Total user experience: < 2 seconds from click to completion

---

## Browser Testing

Test in multiple browsers:
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if Mac)
- [ ] Mobile browser (optional)

---

## Final Checklist

- [ ] All 6 tests passed
- [ ] No console errors
- [ ] No server errors
- [ ] Performance acceptable
- [ ] UI responsive and smooth
- [ ] Data persists correctly
- [ ] Documentation makes sense
- [ ] You understand how to configure surveys
- [ ] You can train other admins

---

## ?? Congratulations!

You've successfully tested the complete survey system. The system is **production-ready** and you can start configuring real surveys for your use cases.

**Next Steps:**
1. Configure real surveys for your diseases
2. Train staff on survey completion
3. Monitor usage and gather feedback
4. Adjust surveys as needed

**Need Help?**
- Review `SURVEY_ADMIN_QUICK_REF.md` for configuration tips
- Check `SURVEY_SYSTEM_100_PERCENT_COMPLETE.md` for complete documentation
- Use sample surveys from `SeedSurveySampleData.sql` as templates

**You're all set! Happy surveying! ???**
