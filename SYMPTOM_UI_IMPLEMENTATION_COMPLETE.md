# Symptom Tracking UI Implementation - Complete

## ? What Was Done

### 1. Permission System Updated

**Permission Model (`Permission.cs`):**
- Added `Laboratory` and `Symptom` to `PermissionModule` enum

**Permission Seeder (`PermissionSeeder.cs`):**
- Added Laboratory permissions: View, Create, Edit, Delete
- Added Symptom permissions: View, Create, Edit, Delete

**Database Migration:**
- Created `Migrations/Add_Symptom_Lab_Permissions.sql`
- Executed migration successfully - 8 new permissions added

### 2. Case Edit Page (`Cases/Edit.cshtml` & `.cs`)

**Code-Behind Updates:**
- Added `List<Symptom> AvailableSymptoms` property
- Added `List<CaseSymptom> CaseSymptoms` property
- Load all active symptoms in `OnGetAsync()`
- Load existing case symptoms in `OnGetAsync()`
- Added `SaveCaseSymptomsAsync()` method to handle symptom save logic
- Handles checkbox state, onset dates, severity, notes, and "Other" symptom text

**UI Updates:**
- Added new "Symptoms" card between Custom Fields and Patient Information sections
- Shows all available symptoms as checkboxes with collapsible detail sections
- Each symptom has fields for:
  - Onset date
  - Severity (Mild/Moderate/Severe)
  - Notes
- Special handling for "Other" symptom with text input
- JavaScript to show/hide symptom details based on checkbox state
- Styled symptom cards with hover effects and visual feedback

### 3. Case Details Page (`Cases/Details.cshtml` & `.cs`)

**Code-Behind Updates:**
- Added `List<CaseSymptom> CaseSymptoms` property
- Load case symptoms with related symptom data in `OnGetAsync()`
- Ordered by onset date, then by symptom name

**UI Updates:**
- Added new "Symptoms" card after Custom Fields section
- Shows symptoms in a table format with:
  - Symptom name (with "Other" description if applicable)
  - Onset date (with days before notification calculation)
  - Severity (color-coded badges)
  - Notes
- Displays count badge in header
- Shows earliest symptom onset summary
- "Add Symptoms" button when no symptoms exist
- Permission-protected section (only shows if user has Symptom.View/Edit/Create permission)

## ?? Permissions

All symptom UI is protected by permission checks:

```csharp
@if (User.HasClaim(c => c.Type == "Permission" && 
     (c.Value == "Symptom.View" || c.Value == "Symptom.Edit" || c.Value == "Symptom.Create")))
{
    // Symptom UI here
}
```

## ?? UI Features

### Edit Page
- ? Checkboxes for all symptoms
- ? Collapsible details per symptom
- ? Visual feedback (border highlights) for selected symptoms
- ? Hover effects on symptom cards
- ? Automatic show/hide of detail fields
- ? Special "Other" symptom handling
- ? Info box with helpful tip

### Details Page
- ? Clean table display of symptoms
- ? Color-coded severity badges (green/yellow/red)
- ? Days-before-notification calculation
- ? Earliest onset summary
- ? Empty state with "Add Symptoms" button
- ? Edit button in header

## ?? Data Flow

1. **Loading Symptoms (Edit):**
   - Fetch all active symptoms from database
   - Fetch existing case symptoms for this case
   - Pre-populate form with existing data

2. **Saving Symptoms (Edit):**
   - Parse form data for checked symptoms
   - Soft-delete unchecked symptoms (restore if re-checked)
   - Create new CaseSymptom records for newly checked
   - Update onset dates, severity, notes for all checked
   - Special handling for "Other" symptom text

3. **Displaying Symptoms (Details):**
   - Query CaseSymptoms with Include for Symptom details
   - Order by onset date, then name
   - Display in table format with calculations

## ?? Form Fields

### Per Symptom:
- **Checkbox**: `symptom_{symptomId}`
- **Onset Date**: `symptom_onset_{symptomId}`
- **Severity**: `symptom_severity_{symptomId}`
- **Notes**: `symptom_notes_{symptomId}`

### Other Symptom Special:
- **Description**: `symptom_other_text`

## ?? Usage

### For Clinicians:
1. Navigate to Case Details or Edit
2. Scroll to "Symptoms" section
3. Check relevant symptoms
4. Fill in onset dates (important for epi analysis)
5. Select severity if known
6. Add notes for unusual presentations
7. Use "Other" for unlisted symptoms

### For Administrators:
- Manage symptoms via: Settings ? Laboratory Lookups ? Symptoms
- Add/edit/deactivate symptoms as needed
- Assign permissions to roles for symptom access

## ?? Technical Details

**Soft Delete:**
- Symptoms are soft-deleted (IsDeleted flag)
- Can be restored if symptom is re-selected
- Audit trail maintained (DeletedAt, DeletedByUserId)

**Audit Trail:**
- All symptom changes tracked via CreatedBy/UpdatedBy
- Timestamps for Created/Updated/Deleted

**Performance:**
- Uses `.Include()` for eager loading
- Indexed queries on CaseId and SymptomId
- Filtered indexes on IsDeleted

## ? Build Status

**Build: SUCCESS** ?

All files compile without errors.

## ?? Files Modified

1. `Surveillance-MVP/Models/Permission.cs` - Added modules
2. `Surveillance-MVP/Extensions/PermissionSeeder.cs` - Added permissions
3. `Surveillance-MVP/Pages/Cases/Edit.cshtml.cs` - Added symptom logic
4. `Surveillance-MVP/Pages/Cases/Edit.cshtml` - Added symptom UI
5. `Surveillance-MVP/Pages/Cases/Details.cshtml.cs` - Added symptom display
6. `Surveillance-MVP/Pages/Cases/Details.cshtml` - Added symptom display UI
7. `Migrations/Add_Symptom_Lab_Permissions.sql` - Permission migration

## ?? Ready to Use

The symptom tracking system is now fully integrated into the case management workflow. Users with appropriate permissions can add, edit, and view symptoms on any case.
