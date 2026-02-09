# ?? SurveyJS Visual Builder - Quick Reference

## ? CORRECT API (Working)

```javascript
// Check if libraries loaded
if (typeof Survey !== 'undefined' && typeof SurveyCreatorCore !== 'undefined') {
    
    // Create the survey builder using SurveyCreatorModel
    const creator = new SurveyCreatorCore.SurveyCreatorModel({
        showLogicTab: true,
        showTranslationTab: false,
        isAutoSave: false,
        showJSONEditorTab: false
    });
    
    // Render to DOM
    creator.render("surveyCreatorContainer");
    
    // Set/Get JSON
    creator.JSON = { title: "My Survey", elements: [] };
    const surveyJson = creator.JSON;
}
```

---

## ? WRONG APIs (Don't Use)

```javascript
// ? WRONG - Not exported
new SurveyCreator.SurveyCreator(options);

// ? WRONG - Doesn't exist on Survey
new Survey.SurveyCreatorModel(options);

// ? WRONG - Not a constructor
new SurveyCreatorCore.SurveyCreator(options);

// ? WRONG - Old API
Survey.SurveyCreator(options);
```

---

## ?? Global Objects

### **Survey** (Core Library)
```javascript
window.Survey = {
    Model,              // Run surveys
    Serializer,         // JSON serialization
    StylesManager,      // Theming
    // ... 200+ properties
}
```

### **SurveyCreatorCore** (Creator Library)
```javascript
window.SurveyCreatorCore = {
    SurveyCreatorModel,     // ? Visual builder constructor
    CreatorBase,
    PropertyGrid,
    // ... creator components
}
```

**Correct Usage:**
```javascript
const creator = new SurveyCreatorCore.SurveyCreatorModel(options);
```

---

## ?? Full Working Example

```html
<!-- Include Libraries -->
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/lib/survey-creator-core/survey-creator-core.min.css" rel="stylesheet" />

<script src="~/lib/survey-core/survey.core.min.js"></script>
<script src="~/lib/survey-creator-core/survey-creator-core.min.js"></script>

<!-- Container -->
<div id="surveyCreatorContainer" style="height: 600px;"></div>

<script>
    // Wait for libraries
    function initCreator() {
        if (typeof Survey === 'undefined' || typeof SurveyCreatorCore === 'undefined') {
            setTimeout(initCreator, 100);
            return;
        }
        
        console.log('? Libraries loaded!');
        
        // Create builder with correct constructor
        const creator = new SurveyCreatorCore.SurveyCreatorModel({
            showLogicTab: true,
            showTranslationTab: false,
            isAutoSave: false,
            showJSONEditorTab: false
        });
        
        // Render
        creator.render("surveyCreatorContainer");
        
        // Set initial survey
        creator.JSON = {
            title: "New Survey",
            elements: [
                {
                    type: "text",
                    name: "question1",
                    title: "What is your name?"
                }
            ]
        };
        
        // Listen for changes
        creator.onModified.add(function(sender, options) {
            console.log('Survey modified:', sender.JSON);
        });
    }
    
    // Start
    initCreator();
</script>
```

---

## ?? Common Operations

### **Load Existing Survey**
```javascript
const existingJson = JSON.parse(document.getElementById('surveyJson').value);
creator.JSON = existingJson;
```

### **Get Survey JSON**
```javascript
const json = creator.JSON;
const jsonString = JSON.stringify(json, null, 2);
```

### **Switch to Preview**
```javascript
creator.activeTab = "test";  // "designer", "test", "logic"
```

### **Check if Modified**
```javascript
if (creator.isModified) {
    console.log('Survey has unsaved changes');
}
```

### **Save Handler**
```javascript
creator.saveSurveyFunc = function(saveNo, callback) {
    const json = creator.JSON;
    // Save to your backend
    callback(saveNo, true);  // true = success
};
```

---

## ?? Customization Options

```javascript
const creator = new SurveyCreatorCore.SurveyCreator({
    // Tabs
    showLogicTab: true,
    showTranslationTab: false,
    showJSONEditorTab: false,
    showEmbeddedSurveyTab: false,
    
    // Designer
    showDesignerTab: true,
    showTestSurveyTab: true,
    
    // Behavior
    isAutoSave: false,
    showSaveButton: false,
    
    // Toolbox
    questionTypes: ["text", "checkbox", "radiogroup", "dropdown", "comment"],
    
    // Theme
    theme: "defaultV2"
});
```

---

## ?? Debugging

### **Check Libraries Loaded**
```javascript
console.log('Survey:', typeof Survey);
console.log('SurveyCreatorCore:', typeof SurveyCreatorCore);
console.log('SurveyCreatorModel class:', typeof SurveyCreatorCore.SurveyCreatorModel);
```

### **Expected Output**
```
Survey: object
SurveyCreatorCore: object
SurveyCreatorModel class: function
```

---

## ?? Library Files

```
wwwroot/lib/
??? survey-core/
?   ??? survey.core.min.js     (Core: Survey runtime)
?   ??? defaultV2.min.css       (Theme)
??? survey-creator-core/
    ??? survey-creator-core.min.js  (Creator: Visual builder)
    ??? survey-creator-core.min.css (Creator styles)
```

---

## ?? Quick Test

Open browser console on Create/Edit Survey Template page:

```javascript
// Should work
new SurveyCreatorCore.SurveyCreatorModel({ isAutoSave: false });

// Test render
document.body.innerHTML = '<div id="test"></div>';
const c = new SurveyCreatorCore.SurveyCreatorModel();
c.render("test");
```

---

## ?? Documentation

- **SurveyJS Docs:** https://surveyjs.io/form-library/documentation/overview
- **Creator Docs:** https://surveyjs.io/survey-creator/documentation/overview
- **Examples:** https://surveyjs.io/survey-creator/examples/

---

**Version:** SurveyJS v1.9.131  
**API:** `SurveyCreatorCore.SurveyCreatorModel`  
**Status:** ? Working
