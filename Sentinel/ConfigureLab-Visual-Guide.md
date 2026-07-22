# ConfigureLab UI Changes - Visual Guide

## Before & After Comparison

### 1. Page Header - NEW Configuration Scope Indicator

**ADDED: Configuration Scope Alert**
```
┌────────────────────────────────────────────────────────────────┐
│ ℹ️ Configuration Scope                                         │
│ These field mappings apply ONLY to messages from:             │
│ • Sending Facility (MSH-4): Quest Diagnostics                 │
│ • Sending Application (MSH-3): QUEST                           │
│ Other lab configurations have separate mappings.               │
│ View all configurations →                                      │
└────────────────────────────────────────────────────────────────┘
```

**ADDED: Configuration Switcher + Options**
```
┌───────────────────────────────────┬────────────────────┐
│ Viewing configuration:            │ [⚙️ Options ▼]    │
│ [Quest Diagnostics (QUEST)    ▼] │                    │
└───────────────────────────────────┴────────────────────┘
                                     │
                                     ├─ 🔄 Reset all mappings
                                     ├─────────────────────
                                     └─ 👁️ View sample message
```

**ADDED: Progress Indicator**
```
┌────────────────────────────────────────────────────────────────┐
│ Mapping Progress                           [5 / 8 Required]   │
│ ████████████████░░░░░░░░░░ 62%                                │
│ 5 confirmed, 2 need attention, 1 not found                    │
└────────────────────────────────────────────────────────────────┘
```

---

### 2. Field Cards - "Not Found" State

**BEFORE:**
```
┌────────────────────────────────────────────────────────────────┐
│ Ordering Provider                              [Not set]      │
│ The healthcare provider who ordered the test                  │
├────────────────────────────────────────────────────────────────┤
│ 🔍 Couldn't find this field in the sample message.            │
│                                                                 │
│ [📤 Try another sample message]                               │
│ [⚙️ Set a fixed default value]                                │
│ [⏭️ Skip this field (not needed for this lab)]               │
└────────────────────────────────────────────────────────────────┘
```

**AFTER:**
```
┌────────────────────────────────────────────────────────────────┐
│ Ordering Provider                              [Not set]      │
│ The healthcare provider who ordered the test                  │
├────────────────────────────────────────────────────────────────┤
│ 🔍 Couldn't find this field in the sample message.            │
│                                                                 │
│ [📋 Browse all fields in message] ← NEW! PRIMARY ACTION       │
│ [📤 Try another sample message]                               │
│ [⚙️ Set a fixed default value]                                │
│ [⏭️ Skip this field (not needed for this lab)]               │
└────────────────────────────────────────────────────────────────┘
```

---

### 3. Field Cards - "Confirmed" State

**BEFORE:**
```
┌────────────────────────────────────────────────────────────────┐
│ Patient Last Name                              [✓ Confirmed]  │
├────────────────────────────────────────────────────────────────┤
│ USING THIS VALUE                                               │
│ SMITH                                            [✏️ Change]   │
│ From PID-5.1 (patient information)                            │
└────────────────────────────────────────────────────────────────┘
```

**AFTER:**
```
┌────────────────────────────────────────────────────────────────┐
│ Patient Last Name                              [✓ Confirmed]  │
├────────────────────────────────────────────────────────────────┤
│ USING THIS VALUE                                               │
│ SMITH                          [✏️ Change] [❌ Clear] ← NEW!  │
│ From PID-5.1 (patient information)                            │
└────────────────────────────────────────────────────────────────┘
```

---

### 4. NEW: Browse All Fields Modal

**Full-Screen Modal (activated by "Browse all fields in message" button):**

```
╔════════════════════════════════════════════════════════════════════════════════╗
║ Find Field: Ordering Provider                                            [×]  ║
║ Select the correct field from your HL7 message                                 ║
╠═══════════════════════════════════╦════════════════════════════════════════════╣
║ SAMPLE MESSAGE                    ║ ALL FIELDS IN MESSAGE                      ║
╟───────────────────────────────────╫────────────────────────────────────────────╢
║ MSH|^~\&|QUEST||SENTINEL||...    ║ ▾ MSH (Message Header) (5 fields)          ║
║ PID|1||MRN123||SMITH^JANE||...   ║   ○ MSH-3: QUEST                            ║
║ OBR|1|||87798^CT NAAT||...       ║     Sending Application                     ║
║ OBX|1|ST|87798||POSITIVE||...    ║   ○ MSH-4: Quest Lab                        ║
║                                   ║     Sending Facility                        ║
║                                   ║                                             ║
║                                   ║ ▾ PID (Patient Identification) (8 fields)  ║
║                                   ║   ○ PID-3.1: MRN123                         ║
║                                   ║     Patient ID/MRN                          ║
║                                   ║   ○ PID-5.1: SMITH                          ║
║                                   ║     Patient Last Name                       ║
║                                   ║   ○ PID-5.2: JANE                           ║
║                                   ║     Patient First Name                      ║
║                                   ║                                             ║
║                                   ║ ▾ OBR (Observation Request) (12 fields)    ║
║                                   ║   ○ OBR-3.1: ACC123                         ║
║                                   ║     Filler Order Number (Accession)         ║
║                                   ║   ● OBR-16: Dr. Smith, John  ← Selected    ║
║                                   ║     Ordering Provider                       ║
║                                   ║                                             ║
║                                   ║ ▸ OBX (Observation Result) (8 fields)      ║
╠═══════════════════════════════════╩════════════════════════════════════════════╣
║ ℹ️ Selected: OBR-16 = "Dr. Smith, John"      [Cancel] [✓ Confirm Selection]  ║
╚════════════════════════════════════════════════════════════════════════════════╝
```

**Features:**
- ✅ Side-by-side view of raw message and parsed fields
- ✅ Grouped by segment type (collapsible)
- ✅ Shows field path (e.g., OBR-16), actual value, and friendly description
- ✅ Radio buttons for selection
- ✅ Highlights selected field with path and value
- ✅ Confirm button confirms mapping and closes modal

---

### 5. Options Dropdown Menu

**NEW: Options dropdown in header**
```
┌─────────────────────────────────┐
│ ⚙️ Options                      │
├─────────────────────────────────┤
│ 🔄 Reset all mappings           │
│ ──────────────────────────────  │
│ 👁️ View sample message          │
└─────────────────────────────────┘
```

**"Reset all mappings" confirmation:**
```
┌─────────────────────────────────────────────────────────┐
│ Reset all field mappings for this configuration?        │
│ This cannot be undone.                                  │
│                                                          │
│                              [Cancel]  [OK]             │
└─────────────────────────────────────────────────────────┘
```

---

## Key Interaction Flows

### Flow 1: Manual Field Selection (Main Enhancement)

```
User uploads sample
    ↓
System can't auto-detect "Ordering Provider"
    ↓
User clicks [📋 Browse all fields in message]
    ↓
Modal opens with split view:
  Left: Raw HL7 message
  Right: All fields grouped by segment
    ↓
User expands "OBR (Observation Request)"
    ↓
User sees:
  ○ OBR-3.1: ACC123 - "Filler Order Number"
  ○ OBR-4.2: CT NAAT - "Test Name/Description"
  ● OBR-16: Dr. Smith, John - "Ordering Provider" ← Clicks this
    ↓
Bottom shows: "Selected: OBR-16 = 'Dr. Smith, John'"
    ↓
User clicks [✓ Confirm Selection]
    ↓
Modal closes → Page reloads → Field now confirmed! ✅
```

### Flow 2: Clear Individual Field

```
Field is confirmed with value "SMITH"
    ↓
User clicks [❌ Clear]
    ↓
Confirmation: "Clear the mapping for 'Patient Last Name'?"
    ↓
User clicks OK
    ↓
Page reloads → Field back to "Not set"
    ↓
User can now select different value or browse all fields
```

### Flow 3: Reset Entire Configuration

```
Configuration partially set up (5/8 fields)
    ↓
User clicks [⚙️ Options] → [🔄 Reset all mappings]
    ↓
Confirmation: "Reset all field mappings? This cannot be undone."
    ↓
User clicks OK
    ↓
All field mappings deleted
Sample message preserved
    ↓
Page reloads → All fields back to "Not set"
Progress: 0/8 fields
    ↓
User can re-analyze or start fresh
```

---

## Responsive Behavior

### Desktop (≥1200px)
- Browse modal: Full side-by-side layout
- Configuration switcher: Inline with options dropdown
- Progress bar: Full width

### Tablet (768px - 1199px)
- Browse modal: Stacked (message top, fields bottom)
- Configuration switcher: Full width above options
- Progress bar: Full width

### Mobile (<768px)
- Browse modal: Single column, scrollable
- Configuration switcher: Full width dropdown
- Options: Full width button
- Progress bar: Simplified (just % and badge)

---

## Color Coding

### Status Indicators
```
✓ Confirmed     → Green (--signal-dk: #0A8A53)
⚠️ Needs Attention → Yellow (--watch: #F5BE6B)
ℹ️ Not Set      → Blue (#6B8CF5)
```

### Action Buttons
```
Primary Action   → Signal color (Browse all fields)
Secondary Action → Ghost/outline (Change, Upload different)
Destructive      → Watch color (Clear, Reset all)
```

### Field Cards
```
Confirmed       → Left border: Green (3px solid)
Needs Attention → Left border: Yellow (3px solid)
Not Found       → Left border: Gray (3px solid)
```

---

## Accessibility

### Keyboard Navigation
- ✅ All buttons keyboard-accessible
- ✅ Modal can be closed with ESC
- ✅ Radio buttons navigable with arrow keys
- ✅ Confirm button disabled until selection made

### Screen Readers
- ✅ Semantic HTML (form-check, labels)
- ✅ ARIA labels on interactive elements
- ✅ Status announcements ("Selected: OBR-16 = ...")

### Focus Management
- ✅ Focus trapped in modal when open
- ✅ Focus returns to trigger button on close
- ✅ Visible focus indicators on all controls

---

## Performance Optimizations

### Browse Fields Modal
- Segments collapsed by default (only MSH expanded)
- Lazy rendering (only visible fields rendered initially)
- Debounced search (if search added in future)

### Page Load
- AllConfigurations loaded once in OnGetAsync
- Field counts computed properties (no DB calls)
- Progress calculated client-side

### Network
- Browse fields: Single POST request
- Returns all fields in one JSON payload
- No polling or real-time updates needed

---

## Browser Compatibility

Tested/Compatible with:
- ✅ Chrome 120+
- ✅ Edge 120+
- ✅ Firefox 121+
- ✅ Safari 17+

Dependencies:
- Bootstrap 5.3 (modals, dropdowns)
- Bootstrap Icons 1.11
- Native JavaScript (no jQuery)

---

## Error Handling

### No Sample Message
```
User clicks "Browse all fields" but no sample uploaded
    ↓
Alert: "No sample message available"
Modal does not open
```

### Invalid Configuration
```
User navigates with invalid configId
    ↓
Redirect to SelectLab page
Error message: "Lab configuration not found."
```

### Network Failure
```
User confirms field selection
Network request fails
    ↓
Browser default error handling
User can retry
```

---

## Summary

**What Changed:**
1. ✅ Added configuration scope indicator (who/what this mapping applies to)
2. ✅ Added configuration switcher (switch between lab configs)
3. ✅ Added progress indicator (visual completion status)
4. ✅ Added browse all fields modal (manual field selection)
5. ✅ Added clear button on confirmed fields (undo individual mapping)
6. ✅ Added reset all option (clear entire configuration)
7. ✅ Added options dropdown (central place for actions)

**What Didn't Change:**
- ✅ Existing auto-detection logic still works
- ✅ Existing candidate selection still works
- ✅ Existing save configuration still works
- ✅ Database schema unchanged
- ✅ API contract unchanged (only added new handlers)

**Net Effect:**
- 🎯 Users can now ALWAYS complete lab configuration (no dead ends)
- 🎯 Configuration scope is crystal clear (no confusion)
- 🎯 Full control over field mappings (reset, clear, change)
- 🎯 Visual progress feedback (know completion status)
