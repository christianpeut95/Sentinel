# ?? Survey Creation Not Saving - Troubleshooting Guide

## ?? Quick Fix Applied

Added enhanced error handling and logging to help debug the issue.

---

## ?? Diagnostic Steps

### **Step 1: Check Validation Errors**

**After restarting the app:**

1. Go to Settings ? Survey Templates
2. Click "Create Survey Template"
3. Fill in the form
4. Click "Create Survey Template"

**Look for:**
- Red error messages below form fields
- Alert box at top showing validation errors

---

### **Step 2: Check Server Logs**

**Visual Studio Output Window:**
Look for these log messages:

```
CreateSurveyTemplate POST started
Adding survey template [Name] with ID [Guid]
Saving changes to database...
Save successful!
```

**If you see errors:**
```
Error creating survey template: [Error Message]
```

Copy the error message - it will tell us what's wrong.

---

### **Step 3: Check Browser Console**

1. Press F12
2. Go to Console tab
3. Click "Create Survey Template"
4. Look for red errors

---

### **Step 4: Check Network Tab**

1. Press F12
2. Go to Network tab
3. Click "Create Survey Template"
4. Find the POST request
5. Check Status Code:
   - **200** = OK but returned to form (validation error)
   - **302** = Redirect (success!)
   - **400** = Bad request
   - **500** = Server error

---

## ?? Common Issues & Solutions

### **Issue 1: Name Field Required**

**Error:** "The Name field is required"

**Solution:** Make sure to enter a survey name (it's required)

---

### **Issue 2: Database Constraint Violation**

**Error:** "Cannot insert duplicate key" or "Foreign key constraint"

**Possible Causes:**
1. Duplicate survey name (if there's a unique constraint)
2. Invalid disease ID selected

**Solution:**
```sql
-- Check if survey name already exists
SELECT * FROM SurveyTemplates WHERE Name = 'YourSurveyName'

-- Check disease IDs are valid
SELECT * FROM Diseases WHERE IsActive = 1
```

---

### **Issue 3: JSON Serialization Error**

**Error:** "Error serializing survey definition"

**Solution:** Already handled - the code creates valid JSON automatically

---

### **Issue 4: Version Fields Missing**

**Error:** "VersionNumber cannot be null" or similar

**Solution:** Already fixed - code sets:
- VersionNumber = "1.0"
- VersionStatus = Draft
- Version = 1

---

### **Issue 5: User Identity Null**

**Error:** "CreatedBy cannot be null"

**Check:**
```csharp
User.Identity?.Name
```

If null, user might not be authenticated properly.

**Solution:** Make sure you're logged in

---

### **Issue 6: Entity Framework Context Issue**

**Error:** "DbContext disposed" or "Tracking error"

**Solution:** Restart application

---

## ?? Manual Database Check

If survey seems to save but doesn't show up:

```sql
-- Check if survey was created
SELECT TOP 10 *
FROM SurveyTemplates
ORDER BY CreatedAt DESC

-- Check specific survey
SELECT *
FROM SurveyTemplates
WHERE Name = 'YourSurveyName'

-- Check disease associations
SELECT st.Name, d.Name AS DiseaseName
FROM SurveyTemplates st
LEFT JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
LEFT JOIN Diseases d ON std.DiseaseId = d.Id
WHERE st.Name = 'YourSurveyName'
```

---

## ?? Test Survey Creation

**Minimal Test:**
1. Name: "Test Survey"
2. Description: (leave blank)
3. Category: "Other"
4. Tags: (leave blank)
5. Diseases: (leave unchecked)
6. Click "Create Survey Template"

**Expected Result:**
- Success message appears
- Redirected to Edit page
- Survey shows Version 1.0 [Draft]

---

## ?? What Gets Created

When you create a survey, the database should have:

```sql
-- SurveyTemplates record
Id: [new GUID]
Name: "Your Survey Name"
Description: "Your description"
Category: "Selected category"
SurveyDefinitionJson: '{"title":"Your Survey Name","description":"Click 'Open Visual Designer'...","elements":[]}'
VersionNumber: "1.0"
VersionStatus: 0 (Draft)
Version: 1
CreatedAt: [timestamp]
CreatedBy: [your username]
IsActive: 1
```

---

## ?? Video Debug Steps

**Record your screen while:**
1. Filling in the form
2. Clicking submit
3. Show what happens

**Capture:**
- Any error messages
- Browser console (F12)
- Visual Studio Output window

**Share:**
- Screenshot of error
- Console logs
- Server logs

---

## ?? Emergency Workaround

If still not working, try creating via SQL:

```sql
DECLARE @SurveyId UNIQUEIDENTIFIER = NEWID()

INSERT INTO SurveyTemplates (
    Id,
    Name,
    Description,
    SurveyDefinitionJson,
    VersionNumber,
    VersionStatus,
    Version,
    CreatedAt,
    CreatedBy,
    IsActive
)
VALUES (
    @SurveyId,
    'Manual Test Survey',
    'Created via SQL for testing',
    '{"title":"Manual Test Survey","description":"Test","elements":[]}',
    '1.0',
    0, -- Draft
    1,
    GETUTCDATE(),
    'admin',
    1
)

-- Verify
SELECT * FROM SurveyTemplates WHERE Id = @SurveyId
```

Then navigate to: `/Settings/Surveys/EditSurveyTemplate?id=[SurveyId]`

---

## ?? What to Report

If issue persists, provide:

1. **Error Message** (exact text)
2. **Server Logs** (from Output window)
3. **Browser Console** (any red errors)
4. **Network Status** (HTTP status code)
5. **Database State** (run the SELECT queries above)
6. **Form Data** (what you entered)

---

## ? Success Indicators

You'll know it worked when:

? Success message appears: "Survey template 'X' created successfully! Now design your survey."
? Page redirects to Edit page
? Version badge shows "Version 1.0" and "[Draft]"
? "Open Visual Designer" button appears
? Survey appears in Survey Templates list

---

**Enhanced Logging Added:**
- POST start/end
- ModelState validation details
- Database save confirmation
- Detailed error messages

**Enhanced UI Added:**
- Validation error summary at top
- Field-level error messages
- Clear feedback on what's wrong

---

**Next Step:** Restart your app and try creating a survey. You should now see detailed error messages if something goes wrong!
