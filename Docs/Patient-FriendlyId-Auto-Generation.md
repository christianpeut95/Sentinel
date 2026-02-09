# Patient FriendlyId Auto-Generation

## Overview
Implemented automatic generation of Patient FriendlyId (Patient ID) when creating or editing patients. The FriendlyId is now automatically assigned immediately after saving a patient record.

## Implementation Details

### New Service Created

**IPatientIdGeneratorService.cs** / **PatientIdGeneratorService.cs**
- Generates sequential patient IDs in format: `P000001`, `P000002`, etc.
- Scans existing patient records to find the highest number
- Increments by 1 to generate the next available ID
- Pads with zeros to ensure 6-digit format

### Changes Made

1. **Program.cs**
   - Registered `IPatientIdGeneratorService` as a scoped service

2. **Create.cshtml.cs (Patient Creation)**
   - Added `IPatientIdGeneratorService` dependency injection
   - Automatically generates FriendlyId before saving new patient
   - FriendlyId is generated only if it's null or empty

3. **Edit.cshtml.cs (Patient Editing)**
   - Added `IPatientIdGeneratorService` dependency injection
   - Automatically generates FriendlyId if it's null or empty on save
   - Useful for backfilling FriendlyIds for existing patients without IDs

## ID Format

The generated Patient ID follows this format:
- **Prefix:** `P` (for Patient)
- **Number:** 6-digit zero-padded number (e.g., 000001, 000002, etc.)
- **Examples:** P000001, P000002, P000123, P001234

## Benefits

1. **Automatic Assignment:** No manual intervention needed
2. **Sequential IDs:** Easy to track and reference patients
3. **Unique Constraint:** Database enforces uniqueness via index
4. **Backfill Support:** Existing patients without IDs get them when edited
5. **Collision-Free:** Checks existing IDs before generating new ones

## Usage

### Creating New Patients
When creating a new patient through the Create Patient page:
1. Fill in patient details
2. Click "Create Patient"
3. **FriendlyId is automatically generated** (e.g., P000001)
4. Patient is saved with the new ID

### Editing Existing Patients
When editing a patient that has no FriendlyId (null or empty):
1. Open the Edit page for the patient
2. Make any changes (or none)
3. Click "Save"
4. **FriendlyId is automatically generated** if it was previously null
5. Patient is updated with the new ID

### Viewing Patient ID
The Patient ID (FriendlyId) is now displayed:
- On the Patient Details page (as a badge at the top of Demographics section)
- In any patient listings or reports where displayed

## Technical Notes

- The service uses EF Core to query existing patient IDs
- Generation is done server-side during the save operation
- The unique index on `FriendlyId` prevents duplicates
- If a generated ID somehow conflicts, EF Core will throw a constraint violation error
- The service extracts numeric parts from existing IDs to find the maximum

## Future Enhancements

Possible improvements:
1. Configurable ID format (prefix, padding length)
2. Alternative ID schemes (year-based, location-based)
3. Bulk backfill operation for existing patients
4. ID range reservation for different user groups
5. Display FriendlyId in patient search results and lists

## Testing

To test the functionality:

1. **Test New Patient Creation:**
- Create a new patient
- Verify FriendlyId is generated with current year (e.g., P-2025-0001)
- Create another patient
- Verify FriendlyId increments (e.g., P-2025-0002)
- Test sequence across year boundaries if possible

2. **Test Backfill on Edit:**
   - Find a patient with no FriendlyId (if any exist)
   - Edit and save the patient
   - Verify FriendlyId is now assigned

3. **Test ID Display:**
   - Open patient Details page
   - Verify Patient ID appears in Demographics section

## Code Locations

- **Service Interface:** `Services/IPatientIdGeneratorService.cs`
- **Service Implementation:** `Services/PatientIdGeneratorService.cs`
- **Service Registration:** `Program.cs` (line ~45)
- **Create Integration:** `Pages/Patients/Create.cshtml.cs`
- **Edit Integration:** `Pages/Patients/Edit.cshtml.cs`
- **Display:** `Pages/Patients/Details.cshtml` (Demographics card)
