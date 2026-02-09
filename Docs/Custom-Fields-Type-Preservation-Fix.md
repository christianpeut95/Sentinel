# Custom Field Type Changing to Text - FIXED

## Issue Reported
When editing a custom field that was created as a Dropdown, the field type changes to Text after saving, even though it's still linked to a lookup table.

## Root Cause
**Critical Bug:** The FieldType dropdown on the Edit page was marked as `disabled` to prevent users from changing the type after creation. However, **disabled form fields do not submit their values** when the form is posted!

### What Was Happening:
1. User creates a Dropdown custom field ? FieldType = 3 (Dropdown)
2. User edits the field
3. FieldType dropdown shows "Dropdown" but is disabled
4. User saves changes
5. **Disabled field doesn't POST** ? FieldType value is missing
6. Server receives no FieldType value ? defaults to 0 (Text)
7. Database updates: FieldType = 0 (Text)
8. LookupTableId remains set, but FieldType is now Text!

## Fix Applied

**File:** `Pages/Settings/CustomFields/Edit.cshtml`

**Before:**
```razor
<div class="mb-3">
    <label asp-for="Input.FieldType" class="form-label">Field Type *</label>
    <select asp-for="Input.FieldType" asp-items="Model.FieldTypeOptions" 
            class="form-select" id="fieldTypeSelect" disabled>
    </select>
    <small class="form-text text-muted">Field type cannot be changed after creation</small>
</div>
```

**After:**
```razor
<div class="mb-3">
    <label asp-for="Input.FieldType" class="form-label">Field Type *</label>
    <select asp-for="Input.FieldType" asp-items="Model.FieldTypeOptions" 
            class="form-select" id="fieldTypeSelect" disabled>
    </select>
    <input type="hidden" asp-for="Input.FieldType" />  <!-- ? ADDED -->
    <small class="form-text text-muted">Field type cannot be changed after creation</small>
</div>
```

## How It Works Now

1. The dropdown shows the current FieldType (disabled for display only)
2. The hidden input preserves the actual value during POST
3. When form is submitted, hidden input sends the correct FieldType value
4. Server receives FieldType correctly
5. Database maintains the correct type (e.g., Dropdown = 3)

## Impact

### Before Fix:
- ? Any edit to a custom field would reset FieldType to Text (0)
- ? Dropdown fields would become text fields after first edit
- ? LookupTable would remain linked but unused
- ? Cases would show text box instead of dropdown

### After Fix:
- ? FieldType is preserved when editing
- ? Dropdown fields remain dropdowns
- ? LookupTable continues to work
- ? Cases display correct field type

## Why Disabled Fields Don't POST

This is standard HTML behavior:
- **Disabled** elements are not included in form submission
- **Readonly** elements ARE included in form submission
- **Hidden** elements ARE included in form submission

### Solution Pattern:
When you need to show a value but prevent editing:
```html
<!-- Show the value (disabled for UX) -->
<select disabled>...</select>

<!-- Preserve the value for POST -->
<input type="hidden" name="FieldType" value="3" />
```

## Testing Steps

### Test 1: Edit Existing Dropdown Field
1. Go to Settings ? Custom Fields
2. Find a field with Type = "Dropdown"
3. Click Edit
4. Change any other field (e.g., Label, Category)
5. Save
6. **Expected:** FieldType remains "Dropdown"
7. **Verify:** Check the field - should still show dropdown options

### Test 2: Verify on Case Edit
1. Edit a case that uses this custom field
2. **Expected:** Field appears as dropdown (not text box)
3. **Expected:** Dropdown shows all lookup values
4. Select a value and save
5. **Verify:** Value saves correctly

### Test 3: Verify Field Types Preserved
Test with each field type:
- ? Text ? stays Text
- ? Number ? stays Number
- ? Date ? stays Date
- ? Dropdown ? stays Dropdown ? (PRIMARY FIX)
- ? Checkbox ? stays Checkbox
- ? TextArea ? stays TextArea
- ? Email ? stays Email
- ? Phone ? stays Phone

## Database Cleanup (If Needed)

If you have fields that were already corrupted (changed to Text), you'll need to fix them:

### Option 1: Re-create the Field
1. Delete the corrupted field
2. Create new field with correct type
3. Re-link to diseases

### Option 2: Direct Database Fix (SQL)
```sql
-- Find corrupted dropdown fields (have LookupTableId but FieldType = 0)
SELECT Id, Name, Label, FieldType, LookupTableId
FROM CustomFieldDefinitions
WHERE FieldType = 0 AND LookupTableId IS NOT NULL;

-- Fix them back to Dropdown (FieldType = 3)
UPDATE CustomFieldDefinitions
SET FieldType = 3
WHERE FieldType = 0 AND LookupTableId IS NOT NULL;
```

**WARNING:** Only run this SQL if you're sure these fields should be dropdowns!

## Build Status
? Build successful

## Related Issues
This bug would have affected ANY field that was edited after creation, not just dropdowns. All field types would reset to Text (0).

## Prevention
This pattern should be used anywhere a disabled field needs to preserve its value:
- Always pair `disabled` display elements with `hidden` inputs
- Alternatively, use `readonly` if editing should be prevented but value should POST

## Files Modified
- `Pages/Settings/CustomFields/Edit.cshtml` - Added hidden input for FieldType

## Impact Summary
**Critical Bug Fixed:** Custom field types now persist correctly through edits. This affects all custom fields in the system across both Patient and Case forms.

---

## How to Verify the Fix

1. **Check existing field:** Edit any dropdown custom field and verify it stays as dropdown
2. **Create new dropdown:** Create a new dropdown field, edit it, verify it stays dropdown
3. **Test on cases:** Verify dropdown appears correctly on case edit pages
4. **Test values:** Verify dropdown values save and display correctly

The system is now stable for custom field editing! ??
