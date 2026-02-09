# Custom Fields Not Saving - Troubleshooting Guide

## Issue
Custom field data is not being saved when creating or editing patients.

## Changes Made to Fix

### 1. Added Missing Custom Field Save in Edit Page
**Problem:** The Edit page was completely missing the code to save custom field values.
**Fix:** Added custom field saving logic to `Edit.cshtml.cs` OnPostAsync method.

### 2. Improved Error Handling
**Problem:** Exceptions during custom field saves were being silently caught or causing the entire save to fail.
**Fix:** 
- Wrapped custom field saves in try-catch blocks
- Patient data saves first, then custom fields save separately
- If custom fields fail, a warning is shown but patient creation/update succeeds
- Error messages now display what went wrong

### 3. Reload Custom Fields on Error
**Problem:** When validation fails and the page is redisplayed, custom fields weren't being reloaded.
**Fix:** Custom field definitions are now reloaded in all error scenarios.

### 4. Added Warning Message Display
**Problem:** No way to see if custom fields failed to save.
**Fix:** Added warning message alerts to both Create and Edit pages.

## How to Test

### Test 1: Create Patient with Custom Fields
1. Go to Patients > Create
2. Fill in required patient fields (First Name, Last Name)
3. Scroll down to custom fields section
4. Fill in some custom field values
5. Click "Create Patient"
6. Check for:
   - ? Success message: "Patient created successfully"
   - ?? Warning message (if custom fields failed): "Patient created but some custom fields failed to save: [error details]"
7. View the patient details page
8. Verify custom field values appear

### Test 2: Edit Patient Custom Fields
1. Go to an existing patient's Edit page
2. Modify some custom field values
3. Click "Save Changes"
4. Check for success or warning messages
5. View the patient details page
6. Verify custom field changes were saved

### Test 3: Check for Errors in Browser Console
1. Open browser Developer Tools (F12)
2. Go to Console tab
3. Create or edit a patient
4. Look for any JavaScript errors

## Debugging Steps

### Step 1: Verify Custom Fields Are Configured
1. Go to Settings > Custom Fields
2. Verify you have at least one custom field defined
3. Check that "Show on Create/Edit" is enabled for the field
4. Note the field's Category and Display Order

### Step 2: Check if Form is Posting Data
Add this temporary code to `Create.cshtml.cs` OnPostAsync method (after line 73, inside the try block):

```csharp
// TEMPORARY DEBUG CODE
var allFormKeys = Request.Form.Keys.ToList();
var customFieldKeys = allFormKeys.Where(k => k.StartsWith("customfield_")).ToList();
System.Diagnostics.Debug.WriteLine($"Total form keys: {allFormKeys.Count}");
System.Diagnostics.Debug.WriteLine($"Custom field keys found: {string.Join(", ", customFieldKeys)}");
foreach (var key in customFieldKeys)
{
    System.Diagnostics.Debug.WriteLine($"{key} = {Request.Form[key]}");
}
// END DEBUG CODE
```

Run the app in Visual Studio with debugging and check the Output window for these debug messages.

### Step 3: Check Database Tables
After creating/editing a patient with custom fields, check the database:

```sql
-- Check if custom field definitions exist
SELECT * FROM CustomFieldDefinitions WHERE IsActive = 1 AND ShowOnCreateEdit = 1;

-- Check if patient custom field values were saved (replace 123 with actual PatientId)
SELECT * FROM PatientCustomFieldStrings WHERE PatientId = 123;
SELECT * FROM PatientCustomFieldNumbers WHERE PatientId = 123;
SELECT * FROM PatientCustomFieldDates WHERE PatientId = 123;
SELECT * FROM PatientCustomFieldBooleans WHERE PatientId = 123;
SELECT * FROM PatientCustomFieldLookups WHERE PatientId = 123;
```

### Step 4: Enable Detailed Error Messages
In `appsettings.Development.json`, ensure detailed errors are enabled:

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

This will log all EF Core queries and show more details about database errors.

### Step 5: Check Field Type Handling
Different field types require different input values:

- **Text/TextArea/Email/Phone**: Any string value
- **Number**: Must be a valid decimal number
- **Date**: Must be in format "yyyy-MM-dd" (HTML date input format)
- **Checkbox**: Posts "on" or "true" when checked, nothing when unchecked
- **Dropdown**: Must be the ID of a `LookupValue` record

### Step 6: Verify SaveChangesAsync is Called
The service method `SavePatientFieldValuesAsync` does NOT call SaveChangesAsync internally. It must be called after:

```csharp
await _customFieldService.SavePatientFieldValuesAsync(Patient.Id, customFieldValues);
await _context.SaveChangesAsync();  // ? This is required!
```

## Common Issues and Solutions

### Issue: "No custom fields showing on Create/Edit"
**Solution:** 
- Verify custom fields have `ShowOnCreateEdit = true`
- Check `IsActive = true`
- Ensure `OnGetAsync()` is calling `_customFieldService.GetCreateEditFieldsAsync()`

### Issue: "Custom fields show but values don't save"
**Possible causes:**
1. Form field naming is wrong (should be `customfield_{FieldDefinitionId}`)
2. SaveChangesAsync not being called after SavePatientFieldValuesAsync
3. Exception being thrown in SavePatientFieldValuesAsync
4. Required field validation failing

**Solution:** Add debug code (Step 2) to verify form data is posting correctly.

### Issue: "Checkbox always shows as unchecked in Details"
**Solution:** Checkboxes need special handling:
- When checked, form posts value "on" or "true"
- When unchecked, form posts nothing (key is missing)
- The service should handle both cases

### Issue: "Dropdown values not saving"
**Possible causes:**
1. Lookup table not properly configured
2. LookupValue IDs don't exist
3. Dropdown posting string value instead of ID

**Solution:** 
- Verify lookup table has active values
- Check form is posting the LookupValue ID, not the display text

### Issue: "Get 'Field not found' or null reference exceptions"
**Solution:**
- Verify FieldDefinitionId exists in CustomFieldDefinitions table
- Check field has `IsActive = true`
- Ensure the field type matches the data being saved

## Expected Behavior After Fixes

### Create Patient
1. Patient saves successfully
2. Custom fields save after patient
3. If custom fields fail, warning shown but patient is created
4. Success message shows: "Patient [Name] has been created successfully"
5. Or warning: "Patient created but some custom fields failed to save: [details]"

### Edit Patient
1. Custom fields load with existing values
2. Patient updates successfully
3. Custom fields update after patient
4. If custom fields fail, warning shown but patient is updated
5. Success message shows: "Patient [Name] has been updated successfully"
6. Or warning: "Patient updated but some custom fields failed to save: [details]"

### View Details
1. Custom fields display grouped by category
2. Empty fields show "-"
3. Checkboxes show Yes/No with icons
4. Email/Phone are clickable links
5. Dates formatted as "dd MMM yyyy"

## Next Steps if Issue Persists

1. Run the debugging steps above
2. Check Visual Studio Output window for exceptions
3. Review the generated SQL in EF Core logs
4. Verify database migrations have created the required tables
5. Check that the `PatientCustomFieldService` is properly registered in `Program.cs`
6. Ensure no custom validation or middleware is blocking the save

## Code Changes Summary

**Files Modified:**
- `Pages/Patients/Create.cshtml.cs` - Added try-catch for custom fields, reload on errors
- `Pages/Patients/Edit.cshtml.cs` - Added custom field save logic (was missing!), error handling
- `Pages/Patients/Create.cshtml` - Added warning message alert
- `Pages/Patients/Edit.cshtml` - Added warning message alert

**Key Code Addition (missing in Edit):**
```csharp
// Save custom field values
try
{
    var customFieldValues = Request.Form
        .Where(kvp => kvp.Key.StartsWith("customfield_"))
        .ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.ToString());
    
    if (customFieldValues.Any())
    {
        await _customFieldService.SavePatientFieldValuesAsync(Patient.Id, customFieldValues);
        await _context.SaveChangesAsync();
    }
}
catch (Exception cfEx)
{
    TempData["WarningMessage"] = $"Patient updated but some custom fields failed to save: {cfEx.Message}";
}
```

## Testing Checklist

- [ ] Custom fields appear on Create page
- [ ] Custom fields appear on Edit page with existing values
- [ ] Text field saves correctly
- [ ] Number field saves correctly
- [ ] Date field saves correctly
- [ ] Checkbox saves when checked
- [ ] Checkbox saves when unchecked
- [ ] Dropdown saves selected value
- [ ] Required field validation works
- [ ] Empty non-required fields save as null
- [ ] Custom fields display correctly on Details page
- [ ] Warning messages show if custom field save fails
- [ ] Patient still saves even if custom fields fail
- [ ] Edit preserves values when validation fails

Run through this checklist after applying the fixes. If any item fails, use the debugging steps above to identify the specific issue.
