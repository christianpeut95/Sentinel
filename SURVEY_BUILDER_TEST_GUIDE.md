# ?? Survey Builder GUI - Quick Test Guide

## ? 5-Minute Test

### **Test 1: Create New Survey (Visual Mode)**

1. **Navigate:**
   ```
   Settings ? Survey Templates ? Create Survey Template
   ```

2. **Fill Basic Info:**
   - Name: "Test Food Survey"
   - Category: "Foodborne"

3. **Use Visual Builder:**
   - You should see a drag-and-drop interface
   - Left panel = Toolbox with question types
   - Center = Survey canvas
   - Right = Properties/preview

4. **Add Questions:**
   - Drag "Text" question to canvas
   - Click on it to see properties panel
   - Change name to: `patientName`
   - Change title to: "Patient Name"
   - Check "Required"

5. **Add More:**
   - Drag "Date" question
   - Name: `symptomOnset`
   - Title: "When did symptoms start?"

6. **Preview:**
   - Look at built-in preview panel (right side)
   - Should see your survey rendering live

7. **Save:**
   - Click "Create Survey Template"
   - Should save successfully
   - Should redirect to list page

8. **Verify:**
   - Go to Survey Templates list
   - Your new survey should be there
   - Click "Details" to verify JSON was saved

---

### **Test 2: Edit Existing Survey**

1. **Navigate to list:**
   ```
   Settings ? Survey Templates
   ```

2. **Click Edit** on any survey

3. **Visual Builder Loads:**
   - Should see existing questions
   - Should be able to drag/modify them

4. **Make a Change:**
   - Add a new question OR
   - Modify existing question title

5. **Save:**
   - Click "Update Survey Template"
   - Version should increment

---

### **Test 3: Visual ? JSON Mode Toggle**

1. **While editing a survey:**

2. **Start in Visual Mode:**
   - See drag-and-drop builder

3. **Click "JSON" Toggle:**
   - Should switch to JSON editor
   - Should see formatted JSON
   - Should match your visual design

4. **Make JSON Change:**
   - Modify a question title in JSON
   - Or add a new question

5. **Click "Visual" Toggle:**
   - Should switch back to visual
   - Should see your JSON changes reflected

6. **Test Sync:**
   - Toggle back and forth several times
   - Changes should persist

---

## ?? Visual Indicators

### **What You Should See**

**Visual Mode (Default):**
```
???????????????????????????????????????????????????
? Survey Builder           [?Visual] [ JSON ]     ?
???????????????????????????????????????????????????
? Toolbox  ? Survey Canvas        ? Preview       ?
?          ?                      ?               ?
? Single   ? ???????????????????? ? ???????????? ?
? Input    ? ? Patient Name     ? ? ? Preview  ? ?
? ?????????? ? (Required)       ? ? ? renders  ? ?
? Multiple ? ???????????????????? ? ? here     ? ?
? Text     ?                      ? ???????????? ?
? Number   ? [+ Add Question]     ?               ?
???????????????????????????????????????????????????
```

**JSON Mode:**
```
???????????????????????????????????????????????????
? Survey Builder           [ Visual] [?JSON ]     ?
???????????????????????????????????????????????????
? {                                                ?
?   "title": "Test Food Survey",                  ?
?   "elements": [                                  ?
?     {                                            ?
?       "type": "text",                            ?
?       "name": "patientName",                     ?
?       "title": "Patient Name",                   ?
?       "isRequired": true                         ?
?     }                                            ?
?   ]                                              ?
? }                                                ?
?                                                  ?
? [Validate JSON]  [Format JSON]                  ?
???????????????????????????????????????????????????
```

---

## ? Common Issues & Fixes

### **Issue: Creator doesn't load**
**Symptom:** Blank white space where builder should be

**Check:**
```javascript
// Open browser console (F12)
// Look for errors like:
"SurveyCreator is not defined"
"Failed to load resource"
```

**Fix:**
- CDN might be blocked
- Check internet connection
- Try clearing browser cache

---

### **Issue: Existing survey doesn't load**
**Symptom:** Blank survey when editing

**Check:**
```javascript
// Browser console should show:
console.error('Could not parse existing JSON')
```

**Fix:**
- Survey JSON might be corrupted
- Switch to JSON mode to see raw JSON
- Validate and fix JSON manually

---

### **Issue: Changes don't save**
**Symptom:** Click save but survey doesn't update

**Check:**
```javascript
// Form submission is blocked
// Check browser console for validation errors
```

**Fix:**
- Ensure survey has at least one question
- Check that JSON is valid
- Look for JavaScript errors in console

---

### **Issue: Toggle doesn't work**
**Symptom:** Clicking Visual/JSON does nothing

**Check:**
- jQuery loaded?
- Bootstrap loaded?
- No JavaScript errors?

**Fix:**
- Check `_Layout.cshtml` has jQuery
- Clear browser cache
- Hard refresh (Ctrl+Shift+R)

---

## ?? Debugging Tools

### **Browser Console Commands**

```javascript
// Check if SurveyJS Creator loaded
typeof SurveyCreator

// Get current survey JSON
surveyCreator.JSON

// Check survey is valid
JSON.stringify(surveyCreator.JSON)

// Force sync to hidden field
$('#hiddenSurveyJson').val(JSON.stringify(surveyCreator.JSON))
```

### **Network Tab**
Check these resources load:
- ? `survey-core.min.js`
- ? `survey-creator-core.min.js`
- ? `defaultV2.min.css`
- ? `survey-creator-core.min.css`

---

## ? Success Indicators

### **You Know It's Working When:**

1. **Visual Builder Loads:**
   - See three panels (Toolbox, Canvas, Preview)
   - Toolbox has question types
   - Canvas says "Drag questions here"

2. **Drag-and-Drop Works:**
   - Can drag question types
   - Drop onto canvas
   - Question appears

3. **Properties Panel Works:**
   - Click question
   - Right panel shows properties
   - Can change name, title, required, etc.

4. **Preview Updates:**
   - As you add questions
   - Preview panel updates live
   - Shows exactly how survey will render

5. **Mode Toggle Works:**
   - Click JSON tab
   - See formatted JSON
   - Click Visual tab
   - See builder again

6. **Save Works:**
   - Click save button
   - Success message appears
   - Redirects to list
   - Survey appears in database

---

## ?? Feature Checklist

### **Basic Functions**
- [ ] Create page loads visual builder
- [ ] Edit page loads with existing survey
- [ ] Can drag questions to canvas
- [ ] Can configure question properties
- [ ] Preview panel shows live updates
- [ ] Save button works
- [ ] JSON stored in database

### **Mode Toggle**
- [ ] Can switch to JSON mode
- [ ] JSON shows formatted correctly
- [ ] Can edit JSON
- [ ] Validate JSON button works
- [ ] Format JSON button works
- [ ] Can switch back to Visual
- [ ] Changes sync between modes

### **Question Types**
- [ ] Can add text questions
- [ ] Can add dropdown questions
- [ ] Can add date questions
- [ ] Can add checkbox questions
- [ ] Can add radio buttons
- [ ] Can add rating questions

### **Advanced Features**
- [ ] Can add multiple pages
- [ ] Can set up skip logic
- [ ] Can make questions required
- [ ] Can add validation rules
- [ ] Can reorder questions by dragging
- [ ] Can delete questions

---

## ?? Performance Check

### **Load Times**
- Visual Builder should load: **< 2 seconds**
- Switching modes: **< 500ms**
- Saving survey: **< 1 second**

### **Large Surveys**
Test with 20+ questions:
- [ ] Visual builder still responsive
- [ ] Preview renders correctly
- [ ] Mode switching still fast
- [ ] Save completes successfully

---

## ?? Acceptance Criteria

### **Minimum Requirements**

? **Visual builder loads on Create page**  
? **Visual builder loads on Edit page with existing data**  
? **Can drag at least 5 question types**  
? **Can configure question properties**  
? **Mode toggle works (Visual ? JSON)**  
? **Save creates/updates survey template**  
? **JSON stored correctly in database**  
? **No console errors**  
? **No build errors**  
? **Existing surveys still work**  

---

## ?? Known Limitations

### **By Design**
- Preview mode is read-only (use SurveyJS Creator's built-in preview)
- Some advanced features require Pro version (we don't need them)
- First load might be slow (CDN download)

### **Not Issues**
- ? "Advanced" tab in properties - that's normal
- ? Some question types have many options - that's expected
- ? JSON mode shows raw JSON - working as designed

---

## ?? Support

### **If Something Doesn't Work:**

1. **Check browser console** (F12 ? Console tab)
2. **Check network tab** (F12 ? Network tab)
3. **Try different browser** (Edge, Chrome, Firefox)
4. **Clear cache** and hard refresh
5. **Check internet connection** (CDN access required)

### **Still Stuck?**

Verify these files were updated:
- ? `CreateSurveyTemplate.cshtml`
- ? `EditSurveyTemplate.cshtml`

Look for:
- Visual/JSON toggle buttons
- `surveyCreatorContainer` div
- SurveyJS script tags

---

## ?? Quick Smoke Test (2 minutes)

```bash
# 1. Start app
dotnet run

# 2. Navigate to:
https://localhost:7XXX/Settings/Surveys/CreateSurveyTemplate

# 3. Should see:
- Visual builder interface
- Toolbox on left
- Canvas in center
- Preview on right
- Visual/JSON toggle at top

# 4. Quick test:
- Drag one question
- Click save
- Should work!
```

---

**Status:** ? Ready for testing  
**Estimated Test Time:** 5-10 minutes  
**Risk Level:** Low (non-breaking change)

**Last Updated:** February 7, 2026
