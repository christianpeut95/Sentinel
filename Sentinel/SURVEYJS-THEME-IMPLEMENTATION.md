# SurveyJS Sentinel Theme - Implementation Guide

This document explains how the Sentinel Design System has been applied to SurveyJS components throughout the application.

---

## 📋 Quick Reference

### Files Modified
- ✅ **Created**: `/wwwroot/css/sentinel-survey-theme.css` - Theme override stylesheet
- ✅ **Created**: `/wwwroot/css/SURVEYJS-THEME-README.md` - User-facing documentation
- 📝 **To Update**: Any page that includes SurveyJS surveys

### Implementation Checklist
- [x] Theme CSS created with full design token coverage
- [x] Documentation written
- [ ] Apply to existing survey pages (see below)
- [ ] Test in Survey Creator (Designer mode)
- [ ] Test in runtime survey rendering
- [ ] Validate accessibility (keyboard navigation, screen reader)

---

## 🎨 Design System Mapping

### SurveyJS → Sentinel Token Mapping

| SurveyJS Element | Default Color | Sentinel Token | Hex Value |
|------------------|---------------|----------------|-----------|
| Primary button | Blue | `--sn-signal-dk` | #159C6E |
| Secondary button | Gray | `--sn-forest` | #0C2A20 |
| Selected checkbox | Blue | `--sn-signal-dk` | #159C6E |
| Focus ring | Blue | `--sn-signal` + shadow | rgba(61,213,152,.25) |
| Error state | Red | `--sn-outbreak` | #E04D2B |
| Input border | Gray | `--sn-hairline` | #D8D4C4 |
| Page background | White | `--sn-bone` | #F5F3EC |
| Card background | White | `--sn-chalk` | #FBFAF5 |

### Typography Mapping

| Element | Default Font | Sentinel Font | Size | Weight |
|---------|--------------|---------------|------|--------|
| Survey title | Sans-serif | Geist | 40px | 500 |
| Page title | Sans-serif | Geist | 28px | 500 |
| Panel title | Sans-serif | Geist | 20px | 600 |
| Question title | Sans-serif | Geist | 14px | 500 |
| Input text | Sans-serif | Geist | 14px | 400 |
| Case IDs/codes | Sans-serif | **Geist Mono** | 13px | 400 |
| Table headers | Sans-serif | **Geist Mono** | 10px | 500 (uppercase) |

---

## 🔧 Implementation Steps

### Step 1: Ensure Geist Fonts Are Loaded

Check that your main layout includes Geist fonts. If using `_LayoutSentinel.cshtml`:

```html
<head>
    <!-- ... other head content ... -->
    <link href="https://fonts.googleapis.com/css2?family=Geist:wght@300;400;500;600;700&family=Geist+Mono:wght@400;500&display=swap" rel="stylesheet">
</head>
```

### Step 2: Update Survey Pages

For **any page** that renders a SurveyJS survey, add these stylesheets **in order**:

#### Example: Case Investigation Survey
```razor
@page "/cases/{caseId}/survey"
@model Sentinel.Pages.Cases.CaseSurveyModel

<!-- Survey CSS -->
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" />

<div id="surveyContainer"></div>

<script>
    var survey = new Survey.Model(@Html.Raw(Model.SurveyJson));

    survey.onComplete.add(function(sender) {
        // Save survey results
        $.ajax({
            url: '/api/survey/submit',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                caseId: '@Model.CaseId',
                results: sender.data
            }),
            success: function() {
                window.location.href = '/cases/@Model.CaseId';
            }
        });
    });

    $("#surveyContainer").Survey({ model: survey });
</script>
```

#### Example: Survey Designer Page (Already Implemented)
File: `Pages/Settings/Surveys/DesignSurvey.cshtml`

```html
<!-- Line ~18-20: CSS includes -->
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/lib/survey-creator-core/survey-creator-core.min.css" rel="stylesheet" />
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" /> <!-- ADD THIS LINE -->
```

### Step 3: Test Theme Application

1. **Navigate to Survey Designer**: `/Settings/Surveys/DesignSurvey/{id}`
2. **Verify**:
   - Buttons are Signal Green (#159C6E) and Forest (#0C2A20), not blue
   - Focus rings are Signal Green with glow
   - Panels have rounded corners (8px)
   - Typography uses Geist
3. **Test interactions**:
   - Click a checkbox → should turn Signal Green
   - Focus an input → should show Signal Green ring
   - Hover a button → should darken smoothly

---

## 🎯 Pages That Need Theme Applied

### Survey Runtime Pages
Search your codebase for files that render surveys at runtime:

```bash
# Find pages that use SurveyJS
rg "survey-core" --type razor
rg "new Survey.Model" --type razor
```

Likely candidates:
- [ ] `/Pages/Cases/CompleteSurvey.cshtml` (if exists)
- [ ] `/Pages/Tasks/ViewSurveyResult.cshtml` ← **Already includes survey-core**
- [ ] Any custom survey rendering pages

### Update Process for Each Page

1. **Open the file**
2. **Find the CSS includes section** (usually near top of `<head>`)
3. **Add** after `defaultV2.min.css`:
   ```html
   <link href="~/css/sentinel-survey-theme.css" rel="stylesheet" />
   ```
4. **Test** the page in browser
5. **Check** for style conflicts (especially if page uses custom CSS)

---

## 🧪 Testing Guide

### Visual Testing Checklist

Open a survey page and verify:

#### Typography
- [ ] Headings use Geist font (check DevTools → Computed → font-family)
- [ ] Case IDs / codes use Geist Mono
- [ ] Letter-spacing looks tight on headings (negative tracking)
- [ ] Line height feels comfortable (not cramped)

#### Colors
- [ ] Page background is Bone (#F5F3EC) - warm off-white
- [ ] Cards/panels are Chalk (#FBFAF5) - slightly lighter
- [ ] Primary button is Signal Green (#159C6E), not blue
- [ ] Focus rings glow Signal Green
- [ ] Error messages show red (#E04D2B) with peach background

#### Spacing & Layout
- [ ] Inputs are 36px tall (Sentinel standard)
- [ ] Panels have 32px internal padding
- [ ] Gaps between questions feel comfortable (16-24px)
- [ ] Border radius is 6-8px (not sharp corners)

#### Interactive Elements
- [ ] Checkboxes: white → Signal Green when checked
- [ ] Radio buttons: white → Signal Green when selected
- [ ] Dropdowns: focus shows Signal Green ring
- [ ] Buttons: hover darkens smoothly (no jump)
- [ ] Progress bar: Signal Green fill

### Functional Testing

1. **Complete a survey** from start to finish
2. **Trigger validation errors** (leave required fields empty)
   - Errors should show in red (#E04D2B)
3. **Test all question types**:
   - Text input
   - Textarea
   - Dropdown
   - Checkbox
   - Radio buttons
   - Rating
   - Matrix
   - Boolean/switch
   - File upload
4. **Navigate with keyboard**:
   - Tab through all fields
   - Space to select checkboxes/radios
   - Enter to submit
5. **Test on mobile** (or resize browser to < 768px)
   - Buttons should stack vertically
   - Padding should reduce
   - Text should remain readable

### Accessibility Testing

#### Keyboard Navigation
```
Tab       → Move to next field
Shift+Tab → Move to previous field
Space     → Toggle checkbox/radio
Enter     → Submit form / activate button
Esc       → Close dropdown (if open)
```

#### Screen Reader (NVDA/JAWS)
- [ ] Questions announce title + type
- [ ] Required fields announce "required"
- [ ] Error messages are read aloud
- [ ] Buttons have descriptive labels

#### Browser DevTools
1. Open **Lighthouse** tab
2. Run **Accessibility audit**
3. Target: **90+ score**
4. Fix any contrast issues (unlikely with Sentinel colors)

---

## 🔍 Common Issues & Solutions

### Issue 1: Theme Not Applying
**Symptom**: Buttons still blue, fonts still system default

**Diagnosis**:
```javascript
// Open browser console
console.log(getComputedStyle(document.querySelector('.sv-btn--primary')).backgroundColor);
// Expected: rgb(21, 156, 110) = #159C6E
// If wrong: rgb(0, 119, 204) = SurveyJS default blue
```

**Solution**:
1. Check CSS load order (use DevTools → Network tab)
2. `sentinel-survey-theme.css` must load **after** `defaultV2.min.css`
3. Clear browser cache (Ctrl+F5)
4. If still failing, add `!important`:
   ```css
   .sv_main .sv-btn--primary {
     background: var(--sn-signal-dk) !important;
   }
   ```

### Issue 2: Fonts Not Loading (Geist)
**Symptom**: Text looks like Arial/system font

**Diagnosis**:
```javascript
// Browser console
console.log(getComputedStyle(document.querySelector('.sv_main')).fontFamily);
// Expected: "Geist", ui-sans-serif, ...
// If wrong: Arial, sans-serif
```

**Solution**:
1. Check if Geist fonts are loaded (DevTools → Network → filter "font")
2. Ensure layout includes:
   ```html
   <link href="https://fonts.googleapis.com/css2?family=Geist:wght@300;400;500;600;700&family=Geist+Mono:wght@400;500&display=swap" rel="stylesheet">
   ```
3. If using `_LayoutSentinel.cshtml`, verify it's applied to the page:
   ```razor
   @{
       Layout = "_LayoutSentinel";
   }
   ```

### Issue 3: Designer Mode (Creator) Not Themed
**Symptom**: Survey preview looks correct, but toolbox/property panel still blue

**Solution**:
The creator has its own CSS. Ensure both are loaded:
```html
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/lib/survey-creator-core/survey-creator-core.min.css" rel="stylesheet" />
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" /> <!-- Must be LAST -->
```

The theme includes `.sd-root-modern` overrides for creator UI.

### Issue 4: Custom CSS Conflicts
**Symptom**: Some styles work, others don't

**Diagnosis**:
```javascript
// Find conflicting styles
var el = document.querySelector('.sv-btn--primary');
var styles = window.getMatchedCSSRules(el); // or use DevTools
console.log(styles);
```

**Solution**:
1. Check for competing CSS (Bootstrap, custom styles)
2. Use more specific selectors:
   ```css
   body .sv_main .sv-btn--primary { /* more specific */ }
   ```
3. Or increase specificity with `:where()`:
   ```css
   :where(.sv_main) .sv-btn--primary { /* same specificity */ }
   ```

---

## 📊 CSS Specificity Reference

The theme uses these selector patterns (from least to most specific):

```css
/* 1. Element selectors (specificity: 0,0,1) */
input { }

/* 2. Class selectors (specificity: 0,1,0) */
.sv-btn { }

/* 3. Compound selectors (specificity: 0,2,1) */
.sv_main .sv-btn--primary { }

/* 4. Pseudo-classes (specificity: 0,2,1) */
.sv-btn:hover { }

/* 5. !important (overrides all) */
.sv-btn--primary { background: red !important; }
```

**Best practice**: Avoid `!important` unless absolutely necessary. The theme is designed to integrate cleanly without it.

---

## 🔄 Maintenance & Updates

### When to Update the Theme

1. **Design System Changes**: If `UI Guidelines.html` is updated (new colors, spacing, fonts)
2. **SurveyJS Updates**: When upgrading `survey-core` or `survey-creator-core` libraries
3. **New Question Types**: If SurveyJS adds new components

### Update Process

1. **Review changes** in Design System or SurveyJS changelog
2. **Edit** `sentinel-survey-theme.css`
3. **Test** in both runtime and designer modes
4. **Update** version number in this file + README
5. **Commit** with descriptive message:
   ```bash
   git add wwwroot/css/sentinel-survey-theme.css
   git commit -m "feat(ui): update SurveyJS theme to match Design System v0.3"
   ```

### Version Control

Track theme versions in this table:

| Theme Version | Design System | SurveyJS Core | Date | Changes |
|---------------|---------------|---------------|------|---------|
| **1.0** | v0.2 | 1.9.x | 2026-04-27 | Initial implementation |
| 1.1 | v0.2 | 1.9.x | TBD | (Future) Add custom question types |

---

## 🤝 Getting Help

### Resources
- **Design System**: `/wwwroot/design/UI Guidelines.html`
- **SurveyJS Docs**: https://surveyjs.io/form-library/documentation/manage-default-themes-and-styles
- **CSS Custom Properties**: https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties

### Debugging Tools
1. **Browser DevTools** (F12)
   - **Elements** tab → inspect computed styles
   - **Network** tab → verify CSS loads
   - **Console** tab → check for JS errors
2. **Lighthouse** (Chrome DevTools)
   - Accessibility audit
   - Performance check
3. **WAVE Extension** (accessibility)
   - https://wave.webaim.org/extension/

### Contact
For questions about:
- **Design tokens/colors** → Refer to Design System documentation
- **SurveyJS functionality** → Official SurveyJS support forums
- **Theme bugs/issues** → Open issue in project repository

---

## 📝 Final Checklist

Before deploying:
- [ ] Theme CSS file created and contains all component overrides
- [ ] README documentation written for end-users
- [ ] All survey pages updated with theme stylesheet
- [ ] Visual testing completed (typography, colors, spacing)
- [ ] Functional testing completed (all question types work)
- [ ] Accessibility testing passed (keyboard nav, screen reader)
- [ ] Mobile responsive testing done (< 768px)
- [ ] Designer mode tested (creator UI themed correctly)
- [ ] No console errors or warnings
- [ ] Cross-browser testing (Chrome, Firefox, Edge, Safari)

---

**Implementation Date**: 2026-04-27  
**Design System Version**: v0.2 Alpha  
**Status**: ✅ **Ready for Integration**

Next step: Update existing survey pages to include the theme stylesheet.
