# Sentinel SurveyJS Theme

This stylesheet overrides the default SurveyJS `defaultV2.css` theme to match the **Sentinel Design System**. All colors, typography, spacing, and component styles align with the brand guidelines defined in `/wwwroot/design/UI Guidelines.html`.

---

## 🎨 Design Tokens Applied

### Color Palette
- **Primary Brand**: Signal Green (#3DD598, #159C6E)
- **Dark Base**: Forest (#0C2A20, #13382C)
- **Neutrals**: Bone (#F5F3EC), Paper (#ECEAE1), Chalk (#FBFAF5)
- **Status Colors**: 
  - Outbreak/Critical: #E04D2B
  - Watch/Warning: #E0A43A
  - Clear/Resolved: #3DD598
  - Info: #6B8CF5

### Typography
- **Sans**: Geist (300–700 weights)
- **Mono**: Geist Mono (for case IDs, timestamps, labels)
- **Font features**: `ss01`, `ss02`, `cv11` enabled

### Spacing
- Base unit: 4px
- Scale: 4, 8, 12, 16, 24, 32px
- Applied to: padding, margins, gaps

### Visual Design
- **Border Radius**: 4px (sm), 6px (md), 8px (lg)
- **Shadows**: Subtle elevation with Signal Green focus rings
- **Transitions**: 120ms fast, 200ms base (ease-out curves)

---

## 📦 Installation & Usage

### Step 1: Include CSS Files (Correct Order)

```html
<!-- 1️⃣ SurveyJS Base Theme (Required) -->
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />

<!-- 2️⃣ Sentinel Theme Override (Must come AFTER) -->
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" />

<!-- For Survey Creator (Designer mode): -->
<link href="~/lib/survey-creator-core/survey-creator-core.min.css" rel="stylesheet" />
```

**⚠️ Order matters!** The Sentinel theme overrides SurveyJS defaults, so it must load **after** `defaultV2.min.css`.

---

### Step 2: Apply Theme in Code

#### Runtime Survey (Blazor/Razor Pages)
```razor
@page "/survey/complete/{id}"
@{
    Layout = "_LayoutSentinel"; // Use Sentinel layout
}

<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" />

<div id="surveyContainer"></div>

<script>
    var survey = new Survey.Model(@Html.Raw(Model.SurveyJson));

    // Optional: Add custom CSS class for additional styling
    survey.css = {
        ...survey.css,
        root: "sv-root-modern sn-survey"
    };

    $("#surveyContainer").Survey({ model: survey });
</script>
```

#### Survey Creator (Designer Mode)
```html
<!-- Already applied in DesignSurvey.cshtml -->
<link href="~/lib/survey-core/defaultV2.min.css" rel="stylesheet" />
<link href="~/lib/survey-creator-core/survey-creator-core.min.css" rel="stylesheet" />
<link href="~/css/sentinel-survey-theme.css" rel="stylesheet" />
```

---

## 🎯 Component Coverage

### ✅ Fully Themed Components

| Component | Status | Notes |
|-----------|--------|-------|
| Text inputs | ✅ | Signal Green focus ring, 36px height |
| Textareas | ✅ | Min-height 80px, resize vertical |
| Dropdowns/Select | ✅ | Matches input styling |
| Checkboxes | ✅ | Signal Green when checked |
| Radio buttons | ✅ | Signal Green accent |
| Buttons | ✅ | Primary (Signal), Secondary (Forest), Ghost |
| Rating scales | ✅ | Hover + selected states themed |
| Matrix questions | ✅ | Mono headers, hover rows (Chalk bg) |
| Panels | ✅ | Chalk background, 8px radius |
| Progress bar | ✅ | Signal Green fill, 6px height |
| File upload | ✅ | Dashed border, Signal hover |
| Signature pad | ✅ | Clear button styled |
| Image picker | ✅ | Signal border when selected |
| Boolean/Switch | ✅ | Signal Green when active |
| Error messages | ✅ | Outbreak red (#E04D2B) |

---

## 🛠️ Customization Examples

### Adding Status Chips to Survey Results
```html
<span class="sn-chip sn-chip--outbreak">
  <span class="sn-chip__dot"></span>
  Outbreak
</span>

<span class="sn-chip sn-chip--clear">
  <span class="sn-chip__dot"></span>
  Resolved
</span>
```

### Custom Question with Mono Font (Case IDs)
```json
{
  "type": "text",
  "name": "caseId",
  "title": "Case ID",
  "readOnly": true,
  "defaultValue": "CASE-2026-0427-A"
}
```
*Automatically styled with Geist Mono when `readOnly` is true.*

### Override Survey Background Color
```css
/* Add this to a page-specific <style> block if needed */
.sv_main {
  background-color: var(--sn-paper) !important; /* Use Paper instead of Bone */
}
```

---

## 📱 Responsive Behavior

- **Mobile breakpoint**: 768px
  - Stack buttons vertically
  - Reduce padding to 16px
  - Smaller heading sizes (32px → 24px)

### Print Styles
- Removes backgrounds, shadows, navigation buttons
- Ensures page breaks don't split questions

---

## ♿ Accessibility Features

### Keyboard Navigation
- All interactive elements have `:focus-visible` styles (2px Signal Green outline)
- Logical tab order preserved

### Screen Readers
- `.sv-hidden` class for visually hidden labels
- ARIA attributes inherit from SurveyJS core

### High Contrast Mode
- Borders increase to 2px in `prefers-contrast: high`

### Reduced Motion
- Animations disabled when `prefers-reduced-motion: reduce` is detected

---

## 🐛 Troubleshooting

### Issue: Theme not applying
**Solution**: Ensure `sentinel-survey-theme.css` loads **after** `defaultV2.min.css`. Check browser DevTools → Network tab.

### Issue: Buttons still blue/purple
**Solution**: Clear browser cache. Check for conflicting CSS (e.g., Bootstrap overrides). Add `!important` if needed:
```css
.sv_main .sv-btn--primary {
  background: var(--sn-signal-dk) !important;
}
```

### Issue: Fonts not loading (Geist)
**Solution**: Ensure Geist fonts are loaded in your layout (`_LayoutSentinel.cshtml`):
```html
<link href="https://fonts.googleapis.com/css2?family=Geist:wght@300;400;500;600;700&family=Geist+Mono:wght@400;500&display=swap" rel="stylesheet">
```

### Issue: Focus rings missing
**Solution**: The theme uses `:focus-visible` (modern browsers only). Fallback for older browsers:
```css
.sv_main *:focus {
  outline: 2px solid var(--sn-signal-dk);
}
```

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| **1.0** | 2026-04-27 | Initial release matching Sentinel UI Guidelines v0.2 |

---

## 📚 Related Documentation

- **Design System**: `/wwwroot/design/UI Guidelines.html`
- **SurveyJS Docs**: https://surveyjs.io/form-library/documentation
- **Theme Customization**: https://surveyjs.io/form-library/documentation/manage-default-themes-and-styles

---

## 🤝 Contributing

When updating this theme:

1. **Test** in both runtime survey mode AND creator/designer mode
2. **Validate** against the UI Guidelines (colors, spacing, typography)
3. **Check** accessibility with keyboard navigation + screen reader
4. **Document** any new component overrides in this README

---

**Maintained by**: Sentinel Development Team  
**Contact**: For questions about design tokens or theming, refer to the Design System documentation.
