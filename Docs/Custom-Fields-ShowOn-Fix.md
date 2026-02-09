# Custom Fields ShowOn Properties Fix

## Issue
The `ShowOnPatientForm` and `ShowOnCaseForm` properties were not being saved when creating or editing custom fields, even though the UI had the checkboxes.

## Root Cause
The Create and Edit page handlers were missing the code to assign these properties from the InputModel to the entity before saving to the database.

## Files Fixed

### 1. Create.cshtml.cs
**Location:** `Pages/Settings/CustomFields/Create.cshtml.cs`

**Before:**
```csharp
var field = new CustomFieldDefinition
{
    Name = Input.Name,
    Label = Input.Label,
    Category = Input.Category,
    FieldType = Input.FieldType,
    IsRequired = Input.IsRequired,
    IsSearchable = Input.IsSearchable,
    ShowOnList = Input.ShowOnList,
    DisplayOrder = Input.DisplayOrder,
    LookupTableId = Input.LookupTableId,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
```

**After:**
```csharp
var field = new CustomFieldDefinition
{
    Name = Input.Name,
    Label = Input.Label,
    Category = Input.Category,
    FieldType = Input.FieldType,
    IsRequired = Input.IsRequired,
    IsSearchable = Input.IsSearchable,
    ShowOnList = Input.ShowOnList,
    ShowOnPatientForm = Input.ShowOnPatientForm,    // ADDED
    ShowOnCaseForm = Input.ShowOnCaseForm,          // ADDED
    DisplayOrder = Input.DisplayOrder,
    LookupTableId = Input.LookupTableId,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
```

### 2. Edit.cshtml.cs
**Location:** `Pages/Settings/CustomFields/Edit.cshtml.cs`

**Before:**
```csharp
field.Name = Input.Name;
field.Label = Input.Label;
field.Category = Input.Category;
field.FieldType = Input.FieldType;
field.IsRequired = Input.IsRequired;
field.IsSearchable = Input.IsSearchable;
field.ShowOnList = Input.ShowOnList;
field.DisplayOrder = Input.DisplayOrder;
field.LookupTableId = Input.LookupTableId;
field.IsActive = Input.IsActive;
```

**After:**
```csharp
field.Name = Input.Name;
field.Label = Input.Label;
field.Category = Input.Category;
field.FieldType = Input.FieldType;
field.IsRequired = Input.IsRequired;
field.IsSearchable = Input.IsSearchable;
field.ShowOnList = Input.ShowOnList;
field.ShowOnPatientForm = Input.ShowOnPatientForm;    // ADDED
field.ShowOnCaseForm = Input.ShowOnCaseForm;          // ADDED
field.DisplayOrder = Input.DisplayOrder;
field.LookupTableId = Input.LookupTableId;
field.IsActive = Input.IsActive;
```

## Impact
Now when admins create or edit custom fields:
- ? Checking "Show on Patient Forms" will properly save and the field will appear on patient forms
- ? Checking "Show on Case Forms" will properly save and the field will be available to link to diseases
- ? Both checkboxes can be enabled simultaneously for fields used in both contexts

## Testing Steps

1. **Create a new custom field:**
   - Go to Settings ? Custom Fields ? Create
   - Fill in required fields
   - Check "Show on Patient Forms"
   - Check "Show on Case Forms"
   - Save
   - Verify both checkboxes remain checked when you edit the field

2. **Verify Patient Form field appears:**
   - Create or edit a patient
   - The new custom field should appear in the form

3. **Verify Case Form field is linkable:**
   - Go to Settings ? Diseases ? Edit any disease
   - Click "Custom Fields" tab
   - The new custom field should appear in the available fields list

4. **Edit an existing field:**
   - Edit any custom field
   - Toggle the "Show on Patient Forms" checkbox
   - Toggle the "Show on Case Forms" checkbox
   - Save
   - Verify the changes persist

## Build Status
? Build successful

## Related Files
- `Models/CustomFieldDefinition.cs` - Model with properties
- `Pages/Settings/CustomFields/Create.cshtml` - UI with checkboxes
- `Pages/Settings/CustomFields/Edit.cshtml` - UI with checkboxes
- `Pages/Settings/CustomFields/Create.cshtml.cs` - Fixed handler
- `Pages/Settings/CustomFields/Edit.cshtml.cs` - Fixed handler
