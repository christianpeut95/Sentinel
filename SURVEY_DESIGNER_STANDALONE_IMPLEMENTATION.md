# Survey Designer - Standalone Page Implementation

## ? Problem Solved

**Issue:** SurveyJS Creator was experiencing initialization errors when embedded in the EditSurveyTemplate page:
```
Uncaught TypeError: Cannot read properties of undefined (reading 'SurveyTemplateText')
```

**Root Cause:** The SurveyJS Creator Knockout library was trying to access properties before proper initialization, likely due to:
- Complex page context with forms and other UI elements
- Iframe-like embedding causing scoping issues
- Library loading race conditions

**Solution:** Created a **standalone dedicated page** for the survey designer that runs independently.

---

## ?? New Files Created

### 1. `/Settings/Surveys/DesignSurvey.cshtml`
- **Purpose:** Full-page, standalone survey designer
- **Layout:** None (runs in its own window)
- **Features:**
  - Clean, distraction-free designer interface
  - Full-screen SurveyJS Creator
  - Save & Close, Cancel, Preview buttons
  - Auto-save status indicator
  - Unsaved changes warning

### 2. `/Settings/Surveys/DesignSurvey.cshtml.cs`
- **Purpose:** Backend for standalone designer
- **Key Methods:**
  - `OnGetAsync(Guid? id)` - Loads survey template for editing
  - `OnPostAsync(Guid? id, SaveSurveyRequest request)` - Saves survey JSON
  - Auto-increments version when definition changes
  - Validates JSON before saving

---

## ?? Updated Files

### `EditSurveyTemplate.cshtml`
**Changed:** Removed embedded SurveyJS Creator, replaced with button-based approach

**Before:**
- Embedded `<div id="surveyCreatorContainer">` with complex initialization
- Mode toggle between Visual and JSON
- Heavy JavaScript loading all SurveyJS libraries
- Initialization retry logic with 50 attempts
- Complex form synchronization

**After:**
- Simple button: "Open Visual Designer"
- Opens designer in popup window (1400x900 optimized size)
- Shows survey status: "No Survey Defined" or "Survey exists (X questions)"
- Optional "View JSON" button for read-only viewing
- Minimal JavaScript - no library loading

---

## ?? How It Works

### User Flow

```
1. User opens EditSurveyTemplate page
   ?? Sees button: "Open Visual Designer"

2. Clicks button
   ?? Opens DesignSurvey page in popup window

3. Designer loads independently
   ?? SurveyJS Creator initializes cleanly
   ?? No conflicts with parent page

4. User designs survey
   ?? Add/edit questions, logic, themes
   ?? Real-time preview available

5. Clicks "Save & Close"
   ?? Saves via AJAX to DesignSurvey.cshtml.cs
   ?? Increments version if definition changed
   ?? Reloads parent window (optional)
   ?? Closes designer window

6. Returns to EditSurveyTemplate
   ?? Shows updated survey status
   ?? Can view JSON or reopen designer
```

---

## ?? Benefits

### 1. **Isolation**
- Designer runs in its own context
- No interference from parent page elements
- Clean initialization every time

### 2. **Better UX**
- Full-screen workspace
- Dedicated focus on survey design
- No distractions from other form fields

### 3. **Reliability**
- Eliminates complex initialization race conditions
- Simpler error handling
- Better browser compatibility

### 4. **Performance**
- SurveyJS libraries only load when needed
- Parent page loads faster (no heavy libraries)
- Cleaner separation of concerns

### 5. **Maintainability**
- Easier to debug
- Simpler code structure
- Clear separation between designer and template management

---

## ?? Testing

### Test Cases

#### 1. Create New Survey
```
1. Navigate to EditSurveyTemplate (existing template)
2. Click "Open Visual Designer"
3. Verify popup opens at correct size
4. Verify SurveyJS Creator loads without errors
5. Add questions, configure logic
6. Click "Save & Close"
7. Verify popup closes
8. Verify parent page shows updated survey
```

#### 2. Edit Existing Survey
```
1. Open template with existing survey
2. Click "Edit in Visual Designer"
3. Verify existing survey loads correctly
4. Make changes
5. Click "Save & Close"
6. Verify version increments
7. Verify changes are saved
```

#### 3. Cancel Without Saving
```
1. Open designer
2. Make changes
3. Click "Cancel"
4. Verify "unsaved changes" warning appears
5. Confirm cancel
6. Verify popup closes
7. Verify no changes saved
```

#### 4. Preview Survey
```
1. Open designer
2. Add questions
3. Click "Preview"
4. Verify modal shows working survey
5. Test filling out survey
6. Close preview
7. Continue editing
```

#### 5. Browser Compatibility
```
Test in:
- Chrome (latest)
- Edge (latest)
- Firefox (latest)
- Safari (if on Mac)

Verify:
- Popup opens correctly
- Designer renders properly
- Save functionality works
- Window closes on save
```

---

## ?? Security Considerations

### 1. **Authorization**
- `[Authorize]` attribute on DesignSurveyModel
- Only authenticated users can access
- User identity logged with changes

### 2. **JSON Validation**
- Server-side validation of survey JSON
- Checks for required properties (title, elements/pages)
- Prevents saving invalid JSON

### 3. **CSRF Protection**
- Uses ASP.NET Core built-in anti-forgery
- AJAX POST includes validation token

---

## ?? API Reference

### DesignSurvey Page

#### GET `/Settings/Surveys/DesignSurvey/{id}`
**Parameters:**
- `id` (Guid, optional): Survey template ID
- `returnUrl` (string, optional): URL to return to after save

**Response:** HTML page with SurveyJS Creator

#### POST `/Settings/Surveys/DesignSurvey/{id}`
**Parameters:**
- `id` (Guid): Survey template ID

**Body (JSON):**
```json
{
  "surveyDefinitionJson": "{\"title\":\"Survey\",\"elements\":[...]}"
}
```

**Response (JSON):**
```json
{
  "success": true,
  "version": 2
}
```

**Errors:**
- 400: Invalid JSON or missing required fields
- 404: Survey template not found
- 500: Server error during save

---

## ?? Future Enhancements

### Possible Improvements

1. **Auto-Save**
   - Save draft every 30 seconds
   - Restore unsaved changes on page reload

2. **Collaboration**
   - Real-time editing indicators
   - Lock mechanism to prevent concurrent edits
   - Version history viewer

3. **Templates**
   - Pre-built question templates
   - Drag-and-drop question library
   - Import from other surveys

4. **Advanced Features**
   - Survey versioning with diff view
   - A/B testing configuration
   - Conditional logic builder UI

5. **Export/Import**
   - Export survey as PDF
   - Import from Google Forms, SurveyMonkey
   - Share survey templates between tenants

---

## ?? Troubleshooting

### Issue: Popup Blocked
**Symptom:** Designer doesn't open, or user sees "popup blocked" message

**Solution:**
1. Browser is blocking popups
2. Add site to allowed popups list
3. Or use Ctrl+Click (opens in new tab instead)

### Issue: SurveyJS Not Loading
**Symptom:** Designer shows blank screen

**Solution:**
1. Check browser console for errors
2. Verify SurveyJS libraries are present in `/wwwroot/lib/`
3. Check libman.json has correct versions
4. Run `libman restore` if needed

### Issue: Save Fails
**Symptom:** "Error saving survey" message

**Solution:**
1. Check browser network tab for error details
2. Verify user has permission to edit template
3. Check server logs for exceptions
4. Verify JSON is valid (not null or empty)

### Issue: Version Not Incrementing
**Symptom:** Version stays same after changes

**Solution:**
- Version only increments if definition actually changes
- If you open/close without changes, version stays same
- This is expected behavior

---

## ?? Code Snippets

### Open Designer from Any Page

```javascript
function openSurveyDesigner(surveyTemplateId) {
    const designerUrl = `/Settings/Surveys/DesignSurvey/${surveyTemplateId}`;
    const width = 1400;
    const height = 900;
    const left = (screen.width - width) / 2;
    const top = (screen.height - height) / 2;
    
    const designerWindow = window.open(
        designerUrl,
        'surveyDesigner',
        `width=${width},height=${height},left=${left},top=${top},menubar=no,toolbar=no`
    );
    
    if (designerWindow) {
        designerWindow.focus();
    } else {
        alert('Please allow popups for this site.');
    }
}
```

### Check If Survey Has Questions (Razor)

```razor
@{
    int questionCount = 0;
    if (!string.IsNullOrWhiteSpace(Model.SurveyTemplate.SurveyDefinitionJson))
    {
        var doc = System.Text.Json.JsonDocument.Parse(Model.SurveyTemplate.SurveyDefinitionJson);
        if (doc.RootElement.TryGetProperty("elements", out var elements))
        {
            questionCount = elements.GetArrayLength();
        }
    }
}

@if (questionCount > 0)
{
    <span class="badge bg-success">@questionCount question(s)</span>
}
else
{
    <span class="badge bg-secondary">No questions</span>
}
```

---

## ? Summary

**Problem:** Embedded SurveyJS Creator had initialization errors  
**Solution:** Created standalone designer page that opens in popup  
**Result:** Clean, reliable survey editing experience

**Files Added:**
- `DesignSurvey.cshtml` - Standalone designer UI
- `DesignSurvey.cshtml.cs` - Backend logic

**Files Modified:**
- `EditSurveyTemplate.cshtml` - Simplified to button-based approach

**Impact:**
- ? Fixes SurveyJS initialization errors
- ? Better user experience (full-screen designer)
- ? Simpler, more maintainable code
- ? Faster parent page load times
- ? Better browser compatibility

---

**Last Updated:** February 7, 2026  
**Status:** ? Implemented and Ready for Testing
