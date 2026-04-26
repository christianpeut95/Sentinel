# Person Details Form - Simplified Inline Implementation

## Overview
Replaced the searchable relationship selector with a simple inline form that captures all person details in one step. The form displays all 4 fields at once with Tab navigation between them.

## Changes Made

### 1. **Renamed Method**
- `showRelationshipSelector(personName)` → `showPersonDetailsForm(personName)`
- Updated call site in `handleMenuItemSelection()` (line ~2169)

### 2. **Form Structure**
The new form contains exactly 4 fields, all visible inline:

```
Name: John Smith (shown in header, not editable)
┌─────────────────────────────────────┐
│ Relationship:  [________________]   │  ← datalist with suggestions
│ Phone:         [________________]   │  ← type="tel"
│ Age/DOB:       [________________]   │  ← accepts "30" or "1994-05-15"
│ Notes:         [________________]   │  ← textarea, 3 rows
│                [________________]   │
│                                      │
│  [Save Person]  [Cancel]             │
└─────────────────────────────────────┘
```

### 3. **Features**

**Navigation**:
- Tab: Move between fields (native browser behavior)
- Enter on any input (except textarea): Save person
- Backspace on empty first field: Return to name entry
- Escape anywhere: Cancel and return to name entry

**Relationship Field**:
- Uses HTML5 `<datalist>` for suggestions (native dropdown)
- Suggestions appear as you type
- Custom relationships allowed (just type anything)
- No filtering logic needed (browser handles it)

**Data Capture**:
All fields saved to entity metadata:
```javascript
metadata: {
    relationship: "Contact",
    phone: "(555) 123-4567",
    ageDob: "30",  // or "1994-05-15"
    notes: "Met at coffee shop"
}
```

### 4. **Removed Features**
✗ Keyboard shortcuts (letter keys for field selection)
✗ Smart prioritization/progressive disclosure
✗ Visual indicators for missing data
✗ Review panel
✗ Arrow key navigation between suggestions
✗ Complex filtering logic

### 5. **Simplified Behavior**

**Save Button**:
- Captures all field values (empty fields stored as null)
- Creates entity with metadata
- Closes form and inserts entity chip

**Cancel Button / Escape Key**:
- Returns to person name entry
- Pre-fills name so user can edit it
- Preserves state correctly

**Backspace on Empty First Field**:
- Same behavior as Cancel
- Allows quick correction of name without reaching for Escape

## Technical Implementation

### Form HTML Structure
- Container: `min-width: 400px; max-width: 500px`
- Header shows person name (read-only display)
- 4 input fields with labels and placeholders
- Button row with Save (primary) and Cancel (secondary)

### Event Handlers
1. **Save Handler**: Attached to Save button and Enter key on inputs
2. **Cancel Handler**: Attached to Cancel button and Escape key
3. **Backspace Handler**: Only on first field when empty
4. **Tab Navigation**: Native browser behavior (no custom code)

### State Management
- `data-menu-type="person-details"` for proper handler attachment
- Preserves `currentState.textarea` and `entryId` during navigation
- Returns to person menu with name pre-filled if canceled

## User Experience

### Typical Flow
1. Type `..John` → Press Enter
2. Form appears with 4 fields
3. Tab through: Relationship → Phone → Age → Notes
4. Press Enter (or click Save)
5. Person chip inserted with all details

### Quick Corrections
1. Realize name is wrong
2. Backspace on empty Relationship field
3. Name entry reappears with "John" pre-filled
4. Edit to "John Smith" → Enter
5. Form reappears, continue filling

## Benefits

✅ **Keyboard-optimized**: Pure Tab/Enter navigation
✅ **Predictable**: All fields always visible, no surprises
✅ **Simple**: No complex state management or shortcuts to learn
✅ **Fast**: No progressive disclosure delays
✅ **Accessible**: Standard form patterns, native controls

## Files Modified

- `wwwroot/js/timeline/entity-quick-add.js`
  - Lines 712-927: New `showPersonDetailsForm()` method
  - Line ~2169: Updated call site

## Testing Checklist

- [ ] Form appears after entering person name
- [ ] All 4 fields visible and editable
- [ ] Tab moves between fields in order
- [ ] Enter on any input (except Notes textarea) saves
- [ ] Relationship datalist shows suggestions
- [ ] Custom relationships can be typed
- [ ] Phone and Age/DOB accept any text
- [ ] Notes textarea accepts multiple lines
- [ ] Save button creates entity with all metadata
- [ ] Cancel button returns to name entry
- [ ] Backspace on empty first field returns to name entry
- [ ] Escape key cancels from any field
- [ ] Name is preserved when returning to name entry
- [ ] Entity chip displays correctly after save

## Future Considerations

**Optional Enhancements** (if needed later):
- Phone number formatting/validation
- Age/DOB parsing (calculate age from DOB)
- Required field validation
- Character limits on fields

**Note**: Current implementation intentionally omits validation to keep the form simple and fast. All fields are optional.
