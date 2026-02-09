# Custom Fields Form Filtering and Display Fix

## Issues Fixed

### 1. Custom Fields Showing on All Forms
**Problem**: All custom fields were appearing on both patient and case forms regardless of their `ShowOnPatientForm` and `ShowOnCaseForm` flags.

**Root Cause**: The service methods `PatientCustomFieldService.GetCreateEditFieldsAsync()` and `CustomFieldService.GetEffectiveFieldsForDiseaseAsync()` were not filtering based on the appropriate form context flags.

**Solution**: 
- Modified `PatientCustomFieldService.GetCreateEditFieldsAsync()` to filter by `ShowOnPatientForm`
- Modified `PatientCustomFieldService.GetDetailsFieldsAsync()` to filter by `ShowOnPatientForm`
- Modified `CustomFieldService.GetEffectiveFieldsForDiseaseAsync()` to filter direct and inherited fields by `ShowOnCaseForm`

**Files Modified**:
- `Surveillance-MVP\Services\PatientCustomFieldService.cs`
  - Added `&& f.ShowOnPatientForm` filter to `GetCreateEditFieldsAsync()` method
  - Added `&& f.ShowOnPatientForm` filter to `GetDetailsFieldsAsync()` method
  
- `Surveillance-MVP\Services\CustomFieldService.cs`
  - Added `&& cf.ShowOnCaseForm` filter for direct fields
  - Added `&& cf.ShowOnCaseForm` filter for inherited fields

### 2. Lookup Fields Displaying Internal Value Instead of Display Text
**Problem**: On the case details page, custom fields of type "Dropdown" were displaying the internal `Value` instead of the user-friendly `DisplayText`.

**Root Cause**: In `Details.cshtml`, line 330 was using `lookupValue?.Value` instead of `lookupValue?.DisplayText`.

**Solution**: Changed the display logic to use `DisplayText` property.

**Files Modified**:
- `Surveillance-MVP\Pages\Cases\Details.cshtml`
  - Changed `<span>@(lookupValue?.Value ?? "-")</span>` to `<span>@(lookupValue?.DisplayText ?? "-")</span>`

### 3. Dropdown Selection Logic Simplified
**Problem**: The `_CustomFieldsForm.cshtml` partial had complex and potentially incorrect logic for selecting the current dropdown value.

**Root Cause**: The selection logic was checking both `DisplayText` and `Id`, but the stored value is always the `Id`.

**Solution**: Simplified the selection logic to only compare with the stored `Id` value.

**Files Modified**:
- `Surveillance-MVP\Pages\Shared\_CustomFieldsForm.cshtml`
  - Changed `selected="@(fieldValue == lookupValue.DisplayText || (Model.Values != null && Model.Values.ContainsKey(field.Id) && Model.Values[field.Id] == lookupValue.Id.ToString()))"` 
  - To `selected="@(fieldValue == lookupValue.Id.ToString())"`

## Testing Recommendations

1. **Patient Forms**: 
   - Verify that only custom fields with `ShowOnPatientForm = true` appear on patient create/edit/details pages
   - Verify that case-specific custom fields do NOT appear on patient forms

2. **Case Forms**: 
   - Verify that only custom fields with `ShowOnCaseForm = true` appear on case create/edit/details pages
   - Verify that patient-specific custom fields do NOT appear on case forms
   - Test with diseases that have both direct and inherited custom fields

3. **Lookup Field Display**:
- Create/edit a custom field of type "Dropdown" with a lookup table
- Assign values to cases
- Verify that the case details page displays the `DisplayText` value, not the internal `Value`
- Verify that the case edit page dropdowns show the `DisplayText` in the options
- Verify that patient pages also display lookup values correctly

4. **Dropdown Selection**:
   - Edit a case with dropdown custom fields
   - Verify that the correct values are pre-selected in the dropdowns
   - Verify that changes to dropdown values are saved correctly

## Impact

- Custom fields will now correctly filter based on their intended context (patient vs case)
- Users will see user-friendly display text for lookup values instead of internal codes
- Form editing experience improved with simplified and correct dropdown selection logic

## Build Status

? Build successful - all changes compile without errors
