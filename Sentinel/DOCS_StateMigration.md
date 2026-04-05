# State Field Migration Guide

## Overview
The State field has been migrated from a free-text `string` field to a validated foreign key `StateId` referencing the `States` lookup table.

## Changes Completed

### 1. Database Schema
- ✅ Created `States` lookup table with 8 Australian states/territories
- ✅ Migrated `Patient.State` (string) → `Patient.StateId` (int FK)
- ✅ Migrated `Case.CaseState` (string) → `Case.CaseStateId` (int FK)
- ✅ Added data migration to map existing state strings to State IDs

### 2. Models Updated
- ✅ `Models/Lookups/State.cs` - Created new lookup model
- ✅ `Models/Patient.cs` - Changed to `StateId` + navigation property
- ✅ `Models/Case.cs` - Changed to `CaseStateId` + navigation property
- ✅ `Data/ApplicationDbContext.cs` - Added `DbSet<State>`

### 3. Services Updated
- ✅ `Services/PatientAddressService.cs` - Now copies `StateId` instead of `State` string

### 4. Patient Edit Page
- ✅ `Pages/Patients/Edit.cshtml` - Changed State input to dropdown
- ✅ `Pages/Patients/Edit.cshtml.cs` - Loads States, compares StateId, includes State navigation

## Changes Required (Manual)

### Pages Needing State Display Updates
Replace `@patient.State` or `@Model.Patient.State` with `@patient.State?.Code` or `@Model.Patient.State?.Name`

1. **Pages/Patients/Create.cshtml**
   - Line 76: `@duplicate.Patient.State` → `@duplicate.Patient.State?.Code`
   - Line 113: `<input type="hidden" asp-for="Patient.State" />` → `<input type="hidden" asp-for="Patient.StateId" />`

2. **Pages/Patients/Details.cshtml**
   - Search for `Model.Patient.State` and change to `Model.Patient.State?.Code`

3. **Pages/Cases/Create.cshtml**
   - Line 463: `patient.state` → `patient.stateCode` (JavaScript)
   - Line 477: `$('#editState').val(patient.state` → update to use stateId

4. **Pages/Cases/CreateNew.cshtml**
   - Line 1285: `patient.state` → `patient.stateCode` (JavaScript)
   - Line 1312: Similar JavaScript update

5. **Pages/DataInbox/Review.cshtml**
   - Line 418: `@duplicateData.NewPatient.State` → `@duplicateData.NewPatient.State?.Code`
   - Line 497: `@match.Patient.State` → `@match.Patient.State?.Code`
   - Line 953: `@Model.ReviewDetail.Patient.State` → `@Model.ReviewDetail.Patient.State?.Code`
   - Line 1766: `State = patient.State` → `StateId = patient.StateId`
   - Line 1834: `patient.State = ...` → `patient.StateId = ...` (needs State lookup)

### Page Models Needing Updates
Add StateId dropdown to any Create pages:

```csharp
ViewData["StateId"] = new SelectList(_context.States.Where(s => s.IsActive).OrderBy(s => s.Code), "Id", "Code");
```

### JavaScript / API Updates
If there are APIs returning patient data as JSON, ensure they include:
- `stateId` (the foreign key)
- `stateCode` (for display - from State.Code)
- `stateName` (for full name - from State.Name)

## Australian States Seeded

| ID | Code | Name |
|----|------|------|
| 1 | NSW | New South Wales |
| 2 | VIC | Victoria |
| 3 | QLD | Queensland |
| 4 | SA | South Australia |
| 5 | WA | Western Australia |
| 6 | TAS | Tasmania |
| 7 | NT | Northern Territory |
| 8 | ACT | Australian Capital Territory |

## Migration Files Created

1. `20260401212041_AddStateLookupTable.cs` - Creates States table and seeds data
2. `20260401212108_ConvertPatientStateToForeignKey.cs` - Migrates Patient.State
3. `20260401212153_ConvertCaseStateToForeignKey.cs` - Migrates Case.CaseState

## To Apply Migrations

**IMPORTANT: Stop the application first before running migrations!**

```powershell
dotnet ef database update
```

## Testing Checklist

- [ ] Patient Edit page loads and shows State dropdown
- [ ] Selecting a state saves StateId correctly
- [ ] Patient Details page displays state code
- [ ] Case creation copies StateId from patient
- [ ] Address change detection works with StateId
- [ ] Data inbox review pages display states correctly
- [ ] Existing data migrated correctly (check a few patients)

## Rollback Plan

If needed, you can rollback migrations:

```powershell
dotnet ef database update 20260331215823_AddInheritAddressSettingsFromParent
```

This will roll back all three State migrations.
