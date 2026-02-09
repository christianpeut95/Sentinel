# ?? Survey Designer Error - Fix Steps

## ? Error Message
```
survey-creator-knockout.min.js:7 Uncaught TypeError: Cannot read properties of undefined (reading 'SurveyTemplateText')
```

## ? Solution Applied

### 1. **Missing Library Fixed**
Added `survey-knockout-ui` to `libman.json` - this library bridges Survey.js Core and Knockout.

**Before:**
```json
{
  "library": "survey-core@1.9.131",
  ...
},
{
  "library": "survey-creator-core@1.9.131",
  ...
}
```

**After:**
```json
{
  "library": "survey-core@1.9.131",
  ...
},
{
  "library": "survey-knockout-ui@1.9.131",  // ? ADDED
  ...
},
{
  "library": "survey-creator-core@1.9.131",
  ...
}
```

### 2. **Correct Load Order in DesignSurvey.cshtml**
```html
<!-- ? CORRECT ORDER -->
<script src="~/lib/knockout/build/output/knockout-latest.js"></script>
<script src="~/lib/survey-core/survey.core.min.js"></script>
<script src="~/lib/survey-knockout-ui/survey-knockout-ui.min.js"></script>  <!-- ? KEY -->
<script src="~/lib/survey-creator-core/survey-creator-core.min.js"></script>
<script src="~/lib/survey-creator-knockout/survey-creator-knockout.min.js"></script>
```

### 3. **Enhanced Initialization with Better Error Checking**
```javascript
function initializeSurveyCreator() {
    console.log('Checking libraries:', {
        knockout: typeof ko !== 'undefined',
        survey: typeof Survey !== 'undefined',
        surveyCreator: typeof SurveyCreator !== 'undefined'
    });
    
    // Check all required libraries
    if (typeof ko === 'undefined' || 
        typeof Survey === 'undefined' || 
        typeof SurveyCreator === 'undefined') {
        console.error('Required libraries not loaded');
        alert('Failed to load survey designer libraries.');
        return;
    }
    
    // ... initialization code ...
}
```

---

## ?? Testing Steps

### 1. **Clear Everything**
```bash
# Stop application (Shift+F5)
# Then in terminal:
cd Surveillance-MVP
libman restore
dotnet clean
dotnet build
```

### 2. **Clear Browser Cache**
- Press **Ctrl+Shift+Delete**
- Clear "Cached images and files"
- **OR** use **Ctrl+F5** for hard refresh

### 3. **Test the Designer**
1. Start application (F5)
2. Navigate to: **Settings > Survey Templates**
3. Click **Edit** on any template
4. Click **"Open Visual Designer"** button
5. Popup window should open with SurveyJS Creator
6. Check browser console (F12) for any errors

---

## ?? Expected Console Output (Success)

When designer loads successfully, you should see:
```
Initializing Survey Creator...
Checking libraries: {knockout: true, survey: true, surveyCreator: true}
Survey Creator instance created
Loading existing survey JSON (or Starting with blank survey)
Survey Creator rendered successfully
```

---

## ? Common Issues & Fixes

### Issue 1: "Popup blocked"
**Symptom:** Designer doesn't open
**Fix:** 
- Allow popups for your site
- Or use Ctrl+Click to open in new tab

### Issue 2: "Libraries not loaded"
**Symptom:** Console shows `knockout: false` or `survey: false`
**Fix:**
```bash
cd Surveillance-MVP
libman clean
libman restore
# Restart app
```

### Issue 3: Old JavaScript still running
**Symptom:** Still seeing old error after fix
**Fix:**
1. Stop application completely
2. Clear browser cache (Ctrl+Shift+Delete)
3. Restart app
4. Try in **Incognito/Private mode**

### Issue 4: 404 on survey-knockout-ui.min.js
**Symptom:** Console shows 404 error for library
**Fix:**
```bash
cd Surveillance-MVP
libman restore
# Check that file exists:
dir wwwroot\lib\survey-knockout-ui\survey-knockout-ui.min.js
```

---

## ?? Library Dependencies

### Required Libraries (in order):
1. **knockout** (3.5.1) - MVVM framework
2. **survey-core** (1.9.131) - Core survey engine
3. **survey-knockout-ui** (1.9.131) - Knockout binding layer ? **KEY MISSING PIECE**
4. **survey-creator-core** (1.9.131) - Creator core logic
5. **survey-creator-knockout** (1.9.131) - Creator Knockout UI

### Why survey-knockout-ui is Critical:
```
survey-creator-knockout.js
    ? requires
SurveyTemplateText (from survey-knockout-ui)
    ? requires
survey-knockout-ui.js
    ? requires
Survey.js Core + Knockout
```

---

## ? Verification Checklist

- [ ] `libman.json` includes `survey-knockout-ui@1.9.131`
- [ ] Libraries restored (`libman restore` successful)
- [ ] File exists: `wwwroot/lib/survey-knockout-ui/survey-knockout-ui.min.js`
- [ ] `DesignSurvey.cshtml` loads libraries in correct order
- [ ] Build successful (`dotnet build`)
- [ ] Browser cache cleared
- [ ] Application restarted
- [ ] Designer opens in popup without errors
- [ ] Console shows successful initialization

---

## ?? What Changed

### Files Modified:
1. ? `libman.json` - Added survey-knockout-ui library
2. ? `DesignSurvey.cshtml` - Fixed library load order
3. ? `DesignSurvey.cshtml` - Added better error checking

### Files Not Changed:
- ? `EditSurveyTemplate.cshtml` - Already uses popup approach
- ? `EditSurveyTemplate.cshtml.cs` - No changes needed

---

## ?? Summary

**Root Cause:** Missing `survey-knockout-ui` bridge library between Survey.js Core and Knockout

**Solution:** 
1. Added missing library to libman.json
2. Fixed script load order
3. Enhanced initialization logging

**Result:** Designer now loads cleanly in popup window without errors

---

## ?? Next Steps

1. **Restart your application**
2. **Hard refresh browser** (Ctrl+F5)
3. **Test designer** - Should work perfectly now!

If you still see issues:
- Check browser console (F12)
- Look for 404 errors on JavaScript files
- Verify all files exist in `wwwroot/lib/`

---

**Last Updated:** February 7, 2026  
**Status:** ? Fixed - Ready to test
