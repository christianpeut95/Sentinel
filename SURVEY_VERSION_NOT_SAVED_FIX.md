# ?? Survey Version "Parent Survey Not Found" Fix

## ? The Problem

When attempting to create a new version of a survey using the **"Save As Version"** button, users encountered the error:

```
version not saved parent survey not found
```

### ?? Root Cause

The issue occurred when users tried to create a version of a **new, unsaved survey**:

1. User creates a new survey via `/Settings/Surveys/DesignSurvey`
2. The new survey has a temporary ID: `00000000-0000-0000-0000-000000000000` (Guid.Empty)
3. User clicks **"Save As Version"** before saving the survey
4. The API receives `ParentSurveyId = 00000000-0000-0000-0000-000000000000`
5. Database lookup fails because no survey exists with that ID
6. API returns **404 Not Found** error

### ?? Why This Happened

**File:** `Surveillance-MVP\Pages\Settings\Surveys\DesignSurvey.cshtml.cs`
```csharp
public async Task<IActionResult> OnGetAsync(Guid? id, string? returnUrl)
{
    if (id == null)
    {
        // New survey
        SurveyTemplateId = Guid.Empty.ToString(); // ?? This is the issue!
        SurveyName = "New Survey Template";
        SurveyDefinitionJson = "null";
        return Page();
    }
    // ...
}
```

When creating a **new survey** (no `id` provided), the code sets:
- `SurveyTemplateId = Guid.Empty.ToString()` = `"00000000-0000-0000-0000-000000000000"`

This temporary ID is then used in the JavaScript:
```javascript
const surveyId = '@Model.SurveyTemplateId'; // "00000000-0000-0000-0000-000000000000"
```

When user clicks "Save As Version", this empty GUID is sent to the API, which can't find a survey with that ID.

---

## ? The Solution

Implemented **three-layer protection** to prevent this error:

### 1?? **Client-Side Prevention** (Primary)

**File:** `Surveillance-MVP\Pages\Settings\Surveys\DesignSurvey.cshtml`

#### Added Detection:
```javascript
const surveyId = '@Model.SurveyTemplateId';
const isNewSurvey = surveyId === '00000000-0000-0000-0000-000000000000';
```

#### Disabled Button on Page Load:
```javascript
$(document).ready(function() {
    // Disable "Save As Version" button for new surveys
    if (isNewSurvey) {
        $('#saveAsVersionBtn')
            .prop('disabled', true)
            .attr('title', 'Save the survey first before creating versions')
            .addClass('disabled');
    }
});
```

**Result:** Button is visually disabled and shows helpful tooltip

#### Added Click Handler Guard:
```javascript
$('#saveAsVersionBtn').on('click', function() {
    // Check if this is a new survey
    if (isNewSurvey) {
        alert('Please save the survey first before creating versions. Click "Save & Close" to save this survey.');
        return;
    }
    
    showSaveAsVersionModal();
});
```

**Result:** Even if button is somehow clicked, user gets clear instructions

---

### 2?? **Server-Side Validation** (API)

**File:** `Surveillance-MVP\Controllers\SurveyVersionController.cs`

#### Added Early Validation:
```csharp
[HttpPost("SaveAsNewVersion")]
public async Task<IActionResult> SaveAsNewVersion([FromBody] SaveAsVersionRequest request)
{
    try
    {
        _logger.LogInformation("Attempting to create new version from parent survey {ParentSurveyId}", request.ParentSurveyId);
        
        // ? NEW: Validate empty GUID
        if (request.ParentSurveyId == Guid.Empty)
        {
            _logger.LogWarning("Attempted to create version with empty ParentSurveyId");
            return BadRequest("Cannot create a version from an unsaved survey. Please save the survey first.");
        }
        
        var sourceSurvey = await _context.SurveyTemplates
            .FirstOrDefaultAsync(st => st.Id == request.ParentSurveyId);

        if (sourceSurvey == null)
        {
            _logger.LogWarning("Source survey {ParentSurveyId} not found in database", request.ParentSurveyId);
            return NotFound($"Source survey not found. The survey may have been deleted or the ID is invalid.");
        }
        
        // ... rest of logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating new survey version");
        return StatusCode(500, "Error creating new version: " + ex.Message);
    }
}
```

**Benefits:**
- ? Explicit check for `Guid.Empty`
- ? Returns **400 Bad Request** (not 404) with clear message
- ? Logs warning for debugging
- ? Provides helpful error message to user

---

### 3?? **Improved Error Display** (Frontend)

**File:** `Surveillance-MVP\Pages\Settings\Surveys\DesignSurvey.cshtml`

#### Enhanced Error Handling:
```javascript
error: function(xhr, status, error) {
    let errorMessage = 'Failed to create version';
    
    try {
        const responseText = xhr.responseText;
        if (responseText) {
            if (xhr.status === 400) {
                // Bad request - show the specific error
                errorMessage = responseText;
            } else if (xhr.status === 404) {
                // Not found
                errorMessage = responseText || 'Parent survey not found. The survey may have been deleted.';
            } else {
                errorMessage = responseText || error || 'Unknown error occurred';
            }
        }
    } catch (e) {
        console.error('Error parsing error response:', e);
        errorMessage += ': ' + (xhr.statusText || error || 'Unknown error');
    }
    
    alert('Error: ' + errorMessage);
}
```

**Result:** Users see the **exact error message** from the server

---

## ?? User Experience Flow

### ? **For New Surveys** (Before First Save)

1. User clicks **"New Survey Template"**
2. Survey Designer opens
3. **"Save As Version"** button is **disabled** (grayed out)
4. Hovering shows tooltip: *"Save the survey first before creating versions"*
5. User must click **"Save & Close"** to save survey first
6. After save, user can reopen survey and create versions

### ? **For Existing Surveys**

1. User opens existing survey for editing
2. **"Save As Version"** button is **enabled**
3. Click button ? version modal opens
4. Enter version details ? create version successfully

---

## ?? Testing

### Test Case 1: New Survey
1. Navigate to **Settings ? Surveys ? Survey Templates**
2. Click **"New Survey Template"**
3. **Expected:** "Save As Version" button is disabled
4. Try to click button
5. **Expected:** Button doesn't respond (disabled)

### Test Case 2: Existing Survey
1. Navigate to **Settings ? Surveys ? Survey Templates**
2. Click **"Edit"** on any existing survey
3. **Expected:** "Save As Version" button is enabled
4. Click **"Save As Version"**
5. **Expected:** Modal opens successfully

### Test Case 3: Manually Trigger Error (Edge Case)
1. Use browser dev tools to enable the button on a new survey
2. Click **"Save As Version"**
3. **Expected:** Alert shows: *"Please save the survey first before creating versions..."*

### Test Case 4: Database Error
1. Edit existing survey
2. Delete the survey from database (via SQL)
3. Try to create version
4. **Expected:** Error shows: *"Source survey not found. The survey may have been deleted..."*

---

## ?? Files Modified

### 1. **DesignSurvey.cshtml**
- Added `isNewSurvey` detection
- Disabled button on page load for new surveys
- Added guard in click handler
- Improved error message display

### 2. **SurveyVersionController.cs**
- Added `Guid.Empty` validation
- Improved error messages
- Added logging for debugging

---

## ?? Technical Details

### Why Use `Guid.Empty` Instead of `null`?

**From `DesignSurvey.cshtml.cs`:**
```csharp
if (id == null)
{
    SurveyTemplateId = Guid.Empty.ToString(); // Must be a valid GUID string for JavaScript
}
```

**Reasons:**
1. JavaScript needs a valid string value
2. Can't pass `null` to JavaScript reliably
3. `Guid.Empty` is a recognizable "no value" indicator
4. Allows for simple string comparison: `=== '00000000-0000-0000-0000-000000000000'`

### API Request Structure

**Frontend sends:**
```json
{
  "parentSurveyId": "00000000-0000-0000-0000-000000000000",
  "versionNumber": "2.0",
  "versionNotes": "Added new questions",
  "publishImmediately": false,
  "surveyDefinitionJson": "{...}",
  "outputMappingJson": "{...}",
  "inputMappingJson": "{...}"
}
```

**API validates:**
```csharp
if (request.ParentSurveyId == Guid.Empty) // ? Catches the empty GUID
```

---

## ?? Additional Improvements

### Future Enhancement Ideas

1. **Auto-Save Draft:**
   - Automatically save new surveys as draft before allowing versioning
   - Generate real GUID on first auto-save

2. **Better Visual Feedback:**
   - Show badge: "?? Unsaved Survey" next to title
   - Change "Save As Version" to "Save First" when disabled

3. **Guided Workflow:**
   - Show wizard: "To create versions, first save your survey"
   - Automatically redirect to save flow

4. **Version from Template:**
   - Allow creating first version without saving base survey
   - System creates both base + version in single transaction

---

## ? Summary

### Problem
- Users could attempt to version unsaved surveys
- Resulted in confusing "parent survey not found" error

### Solution
- **Prevent:** Disable button for new surveys
- **Validate:** Check for empty GUID in API
- **Inform:** Show clear error messages

### Result
- ? No more confusing errors
- ? Clear user guidance
- ? Proper validation at all layers
- ? Better logging for debugging

---

**Status:** ? **FIXED AND TESTED**

**Related Files:**
- `Surveillance-MVP\Pages\Settings\Surveys\DesignSurvey.cshtml`
- `Surveillance-MVP\Pages\Settings\Surveys\DesignSurvey.cshtml.cs`
- `Surveillance-MVP\Controllers\SurveyVersionController.cs`
- `SURVEY_VERSIONING_COMPLETE_GUIDE.md`

**Last Updated:** February 7, 2026
