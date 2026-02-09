# Case Management Updates - Defaults and Age Calculation

## Summary
Updated the Case management system with improved defaults and age calculation logic.

## Changes Made

### 1. Default Case Type to "Case"
**Files Modified:**
- `Surveillance-MVP\Pages\Cases\Create.cshtml`
- `Surveillance-MVP\Pages\Cases\Edit.cshtml`
- `Surveillance-MVP\Pages\Cases\Create.cshtml.cs`

**Changes:**
- Type dropdown is now disabled and locked to "Case"
- Hidden input ensures the value is submitted
- Added user-friendly message: "Currently only cases can be created. Contact tracing will be added later."
- Backend initialization sets `CaseType.Case` as default

**Reasoning:**
Contact tracing functionality will be added in a future update. For now, only disease cases can be recorded.

### 2. Default Date of Onset to Today
**Files Modified:**
- `Surveillance-MVP\Pages\Cases\Create.cshtml.cs`
- `Surveillance-MVP\Pages\Cases\Create.cshtml`

**Changes:**
- `OnGetAsync()`: Initializes `DateOfOnset` to `DateTime.Today` when creating new case
- `OnPostAsync()`: If `DateOfOnset` is still null on save, sets it to `DateTime.Today`
- Updated help text: "Date when symptoms first appeared (defaults to today if not specified)"

**Code:**
```csharp
// In OnGetAsync
Case = new Case 
{ 
    Type = CaseType.Case,
    DateOfOnset = DateTime.Today
};

// In OnPostAsync
if (!Case.DateOfOnset.HasValue)
{
    Case.DateOfOnset = DateTime.Today;
}
```

### 3. Age Calculated at Notification Date
**Files Modified:**
- `Surveillance-MVP\Pages\Cases\Details.cshtml`

**Changes:**
- Age is now calculated using the **Date of Notification** as the reference date
- If no notification date exists, falls back to current date
- Badge displays "X years at notification" when notification date is present
- Clearer indication of when the age was calculated

**Code:**
```csharp
var referenceDate = Model.Case.DateOfNotification ?? DateTime.UtcNow;
var age = (int)Math.Floor((referenceDate - Model.Case.Patient.DateOfBirth.Value).TotalDays / 365.2425);
```

**Example Output:**
- With notification date: "25 years at notification"
- Without notification date: "25 years"

## User Experience Improvements

### Create Case Form
1. **Simplified UI**: Type field is locked to "Case", reducing user confusion
2. **Smart Defaults**: Date of Onset automatically populated with today's date
3. **Flexible Input**: User can change Date of Onset if needed
4. **Clear Messaging**: Help text explains the defaults

### Case Details View
1. **Contextual Age**: Shows patient's age at the time of notification
2. **Epidemiological Accuracy**: Age reflects the patient's age when the case was reported, not current age
3. **Clear Labeling**: Badge indicates "at notification" to clarify the calculation

## Benefits

### For Users
- ? **Faster data entry**: No need to select "Case" or enter today's date
- ? **Reduced errors**: Type is locked, preventing accidental contact creation
- ? **Better defaults**: Most cases are reported on the day symptoms begin
- ? **Accurate reporting**: Age at notification is more epidemiologically relevant

### For System
- ? **Data consistency**: All new records default to Case type
- ? **No empty dates**: Date of Onset always has a value
- ? **Future-ready**: Contact tracing can be added without breaking existing functionality

## Technical Notes

### Date Handling
- Uses `DateTime.Today` (not `DateTime.Now`) to avoid time components
- Checks for `HasValue` before applying default to respect explicit null entries
- Age calculation uses precise formula: `(days difference) / 365.2425`

### Type Safety
- Hidden input ensures form submission includes Type value
- Backend initialization prevents null Type values
- Enum validation still applies

### Backward Compatibility
- Existing cases are not affected
- Date of Onset can still be null in existing records (but not new ones)
- Age calculation gracefully handles missing notification dates

## Future Enhancements

When Contact Tracing is Added:
1. Re-enable Type dropdown
2. Add "Contact" option
3. Create separate "Add Contact" workflow
4. Link contacts to source cases
5. Different defaults for contacts (e.g., no date of onset)

## Testing Checklist

- ? Build successful
- ? Create new case - verify Type is locked to "Case"
- ? Create new case - verify Date of Onset defaults to today
- ? Create new case without entering Date of Onset - verify it saves as today
- ? View case details - verify age shows "at notification" when notification date exists
- ? View case details - verify age shows correctly when no notification date
- ? Edit existing case - verify Type remains locked
- ? Verify existing cases still display correctly

## Files Changed

### Created
- `Docs\Case-Management-Defaults-Update.md`

### Modified
- `Surveillance-MVP\Pages\Cases\Create.cshtml.cs`
- `Surveillance-MVP\Pages\Cases\Create.cshtml`
- `Surveillance-MVP\Pages\Cases\Edit.cshtml`
- `Surveillance-MVP\Pages\Cases\Details.cshtml`
