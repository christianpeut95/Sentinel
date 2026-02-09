# Dropdown Custom Field Fix - Appearing as Text Box

## Issue Reported
A dropdown custom field is displaying as a free text box instead of a dropdown select control.

## Root Cause Analysis

### Potential Causes:
1. **LookupTable not loaded** - The `field.LookupTable` property is null
2. **LookupTable.Values empty** - The Values collection is null or empty
3. **Missing default case** - Switch statement had no fallback rendering

## Fix Applied

**File:** `Pages/Cases/Edit.cshtml`

**Changes:**
1. Added null-safety checks for `LookupTable` and `Values`
2. Added warning message when dropdown data is missing
3. Added `default` case to render text box as fallback

**Before:**
```razor
case CustomFieldType.Dropdown:
    if (field.LookupTable != null)
    {
        <select class="form-select" name="@fieldName" required="@field.IsRequired">
            <option value="">-- Select --</option>
            @foreach (var value in field.LookupTable.Values.OrderBy(v => v.DisplayOrder))
            {
                var selected = fieldValue != null && fieldValue.ToString() == value.Id.ToString();
                <option value="@value.Id" selected="@selected">@value.Value</option>
            }
        </select>
    }
    break;
}
```

**After:**
```razor
case CustomFieldType.Dropdown:
    if (field.LookupTable != null && field.LookupTable.Values != null && field.LookupTable.Values.Any())
    {
        <select class="form-select" name="@fieldName" required="@field.IsRequired">
            <option value="">-- Select --</option>
            @foreach (var value in field.LookupTable.Values.OrderBy(v => v.DisplayOrder))
            {
                var selected = fieldValue != null && fieldValue.ToString() == value.Id.ToString();
                <option value="@value.Id" selected="@selected">@value.Value</option>
            }
        </select>
    }
    else
    {
        <div class="alert alert-warning">
            <small>?? Dropdown field "@field.Label" is missing lookup table data.</small>
        </div>
    }
    break;

default:
    <input type="text" class="form-control" name="@fieldName" 
           value="@fieldValue" 
           required="@field.IsRequired" />
    break;
}
```

## Diagnostics

### Check 1: Verify Custom Field Configuration
1. Go to **Settings ? Custom Fields**
2. Find your dropdown field
3. Verify:
   - Field Type = "Dropdown" ?
   - Lookup Table is selected ?
   - Lookup Table has values ?

### Check 2: Verify Lookup Table Has Values
1. Go to **Settings ? Lookup Tables**
2. Find the associated lookup table
3. Verify it has at least one active value
4. Add values if missing

### Check 3: Check Disease Linking
1. Go to **Settings ? Diseases ? Edit [Disease]**
2. Click **Custom Fields** tab
3. Verify dropdown field is checked
4. Save

### Check 4: Database Query Verification
The CustomFieldService should be loading LookupTable with this query:

```csharp
var disease = await _context.Diseases
    .Include(d => d.DiseaseCustomFields)
        .ThenInclude(dcf => dcf.CustomFieldDefinition)
            .ThenInclude(cf => cf.LookupTable)
                .ThenInclude(lt => lt.Values)  // ? This loads dropdown options
    .FirstOrDefaultAsync(d => d.Id == diseaseId);
```

**Verify:**
- `field.LookupTable` is not null
- `field.LookupTable.Values` is not null
- `field.LookupTable.Values.Count > 0`

## Testing Steps

### Test 1: Edit Page Shows Dropdown
1. Create or edit a case with the disease that has the dropdown field
2. Go to Cases ? Edit
3. **Expected:** Dropdown appears with options
4. **If warning appears:** Check lookup table configuration

### Test 2: Dropdown Options Appear
1. Click the dropdown
2. **Expected:** See "-- Select --" and all lookup values
3. **Expected:** Values ordered by DisplayOrder

### Test 3: Save and Verify
1. Select a value from dropdown
2. Save the case
3. View case details
4. **Expected:** Selected value displays correctly

## Build Status
? Build successful

## Additional Notes

### Why Text Box Was Showing
When the switch statement didn't have a `default` case and the dropdown condition failed, nothing was rendered for that field. The browser may have been showing a fallback or cached element.

### Warning Message Purpose
If the lookup table data is missing, you'll now see:
```
?? Dropdown field "[Field Name]" is missing lookup table data.
```

This helps diagnose configuration issues immediately.

### Fallback Behavior
If the field type doesn't match any known case, it now falls back to a text input. This ensures the field is always editable, even if misconfigured.

## Related Files

### Edit Page
- `Pages/Cases/Edit.cshtml` ? Fixed
- `Pages/Cases/Edit.cshtml.cs` ? Already correct

### Details Page  
- `Pages/Cases/Details.cshtml` ? Already has default case
- `Pages/Cases/Details.cshtml.cs` ? Already correct

### Service Layer
- `Services/CustomFieldService.cs` ? Includes LookupTable and Values

### Models
- `Models/CustomFieldDefinition.cs` ? Has LookupTable navigation
- `Models/LookupTable.cs` ? Has Values collection

## Next Steps

1. **Verify the dropdown now appears** on the Edit page
2. **If warning appears**, check:
   - Custom field has LookupTableId set
   - Lookup table exists and is active
   - Lookup table has at least one value
3. **If still shows as text box**, check browser console for JavaScript errors
4. **Verify the field saves correctly** after selecting a dropdown value

## Troubleshooting

### Dropdown Still Appears as Text Box
**Possible causes:**
1. Field Type not set to "Dropdown" - Check custom field definition
2. LookupTable not assigned - Edit custom field and assign lookup table
3. Browser cache - Hard refresh (Ctrl+F5)
4. JavaScript error - Check browser console

### Warning Message Appears
**Fix steps:**
1. Edit the custom field
2. Ensure "Field Type" = Dropdown
3. Select a lookup table from the dropdown
4. Save
5. Refresh the case edit page

### Dropdown Empty (No Options)
**Fix steps:**
1. Go to Settings ? Lookup Tables
2. Find the associated table
3. Add values if none exist
4. Ensure values are marked as Active
5. Refresh the case edit page
