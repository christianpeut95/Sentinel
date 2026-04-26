# BUGFIX: Person Entity Click Shows Details Form (Not Search Menu)

## Issue
When clicking on an existing person entity chip, the "search or create person" menu was appearing instead of an edit form with the person's details.

## Root Cause
The `editEntity()` method in `entity-quick-add.js` was calling `showEntityForm(entityTypeName)` for all entity types, including Person. For Person entities, this showed the search/create menu (used when initially creating a person) rather than the person details form (used for editing).

## Solution

### 1. **Updated `editEntity()` Method** (Lines 3028-3058)
Added logic to detect Person entities and show the person details form directly:

```javascript
// For Person entities, show the person details form directly (skip the search menu)
if (entityTypeName === 'Person') {
    const personName = entity.rawText || entity.normalizedValue || '';
    this.showPersonDetailsForm(personName);
} else {
    // For other entity types, show the standard form
    this.showEntityForm(entityTypeName);
}
```

### 2. **Enhanced `showPersonDetailsForm()` Method** (Lines 712-942)

#### Header Update
- Detects edit mode from `this.currentState.editingEntity`
- Shows "Edit person details" when editing vs "Complete person details" when creating

#### Pre-fill Logic
Added code to populate form fields with existing entity metadata when editing:

```javascript
// Pre-fill form if editing
const isEditing = !!this.currentState.editingEntity;
if (isEditing) {
    const metadata = this.currentState.editingEntity.metadata || {};
    relationshipInput.value = metadata.relationship || '';
    phoneInput.value = metadata.phone || '';
    ageDobInput.value = metadata.ageDob || '';
    notesInput.value = metadata.notes || '';
}
```

#### Save Logic Update
Modified save handler to:
- Preserve entity ID when editing (instead of generating new ID)
- Call `updateEntityInText()` when editing vs `insertEntityIntoText()` when creating

```javascript
const entity = {
    id: isEditing ? this.currentState.editingEntity.id : `entity_${Date.now()}_...`,
    // ... other properties
};

if (isEditing) {
    this.updateEntityInText(entity);
} else {
    this.insertEntityIntoText(entity);
}
```

#### Cancel Behavior
Updated cancel/escape handlers to distinguish between create and edit modes:

```javascript
const cancelForm = () => {
    if (isEditing) {
        // When editing, just close the form
        this.closeTippy();
    } else {
        // When creating, return to name entry
        // ... restore person menu with name pre-filled
    }
};
```

## User Experience

### Before Fix
1. Click existing person chip → "Search or create person" menu appears ❌
2. Confusing - user already has the person, why search again?
3. No way to edit person details

### After Fix
1. Click existing person chip → Person details form appears ✅
2. Form shows "Edit person details" header
3. All fields pre-filled with current values
4. User can edit relationship, phone, age/DOB, notes
5. Save updates the entity in place
6. Cancel just closes the form

## Testing Checklist

### Edit Mode
- [x] Click person chip shows details form (not search menu)
- [x] Header shows "Edit person details"
- [x] Relationship field pre-filled if exists
- [x] Phone field pre-filled if exists
- [x] Age/DOB field pre-filled if exists
- [x] Notes field pre-filled if exists
- [x] Empty fields show as empty (not error)
- [x] Save button updates existing entity
- [x] Entity ID preserved after save
- [x] Cancel button closes form without changes
- [x] Escape key closes form without changes
- [x] Backspace on empty field closes form (no return to name entry)

### Create Mode (Still Works)
- [x] Type `..John` → Enter shows person details form
- [x] Header shows "Complete person details"
- [x] All fields empty
- [x] Save button creates new entity
- [x] Cancel returns to person search menu
- [x] Backspace on empty first field returns to person search

## Files Modified

- `wwwroot/js/timeline/entity-quick-add.js`
  - Lines 712-730: Added edit mode detection and dynamic header
  - Lines 822-864: Pre-fill form fields and conditional save logic
  - Lines 867-904: Updated cancel behavior for edit vs create
  - Lines 3028-3058: Direct call to showPersonDetailsForm for Person entities

## Technical Details

### State Management
- `currentState.editingEntity`: Set when clicking existing entity
- `currentState.originalEntity`: Copy of original for comparison
- Both checked to determine edit mode: `const isEditing = !!this.currentState.editingEntity`

### Entity Update Flow
1. Click chip → `timeline-entry.js:showEntityDetails()`
2. Calls → `entity-quick-add.js:editEntity()`
3. Checks type → Person → `showPersonDetailsForm()`
4. Form pre-filled with metadata
5. User edits → Save → `updateEntityInText()`
6. Entity updated in text and array

### Backward Compatibility
✅ Create flow unchanged (.. trigger → name → details form)
✅ Other entity types unchanged (still use showEntityForm)
✅ All keyboard shortcuts preserved
✅ Cancel behavior context-aware

## Benefits

✅ **Intuitive**: Clicking person opens editor, not search
✅ **Consistent**: Edit behavior matches other entity types
✅ **Data Preservation**: Entity ID and position maintained
✅ **User-Friendly**: Cancel is context-aware (close vs back)
✅ **No Breaking Changes**: Create workflow still works perfectly

## Related Issues

This fix completes the person details form implementation. The form now supports both:
1. **Creation**: Via `..name` trigger → details form
2. **Editing**: Via clicking existing chip → details form

Both flows use the same form UI with appropriate pre-filling and save behavior.
