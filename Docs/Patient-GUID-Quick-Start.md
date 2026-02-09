# Patient GUID Migration - Quick Start Guide

## What Changed?

The Patient ID system has been upgraded:
- **Old**: Sequential integer IDs (1, 2, 3, ...)
- **New**: 
  - Internal GUID IDs (e.g., `a7f3b8c9-1234-5678-9abc-def012345678`)
  - Human-friendly IDs (e.g., `P-2025-00001`, `P-2025-00002`)

## For Developers

### Creating a New Patient
```csharp
var patient = new Patient 
{
    // No need to set Id - it's auto-generated as a GUID
    // No need to set FriendlyId - it's auto-generated (P-YEAR-XXXXX)
    GivenName = "John",
    FamilyName = "Doe",
    // ... other properties
};

_context.Patients.Add(patient);
await _context.SaveChangesAsync();

// After save:
// patient.Id = Guid (e.g., a7f3b8c9-1234-5678-9abc-def012345678)
// patient.FriendlyId = "P-2025-00001" (or next sequential number)
```

### Querying Patients
```csharp
// By GUID
Guid patientId = GetPatientGuidFromSomewhere();
var patient = await _context.Patients.FindAsync(patientId);

// By FriendlyId
string friendlyId = "P-2025-00001";
var patient = await _context.Patients
    .FirstOrDefaultAsync(p => p.FriendlyId == friendlyId);
```

### Route Parameters
Update your route parameters from `int` to `Guid`:

**Before:**
```csharp
public async Task<IActionResult> OnGetAsync(int? id)
```

**After:**
```csharp
public async Task<IActionResult> OnGetAsync(Guid? id)
```

### Audit Logging
When logging audit events, convert Guid to string:

**Before:**
```csharp
await _auditService.LogChangeAsync("Patient", patient.Id, "Field", oldVal, newVal, userId, ip);
```

**After:**
```csharp
await _auditService.LogChangeAsync("Patient", patient.Id.ToString(), "Field", oldVal, newVal, userId, ip);
```

### Custom Fields
Custom field services now expect Guid:

```csharp
// Get patient custom field values
var values = await _customFieldService.GetPatientFieldValuesAsync(patient.Id); // Guid

// Save custom field values
await _customFieldService.SavePatientFieldValuesAsync(patient.Id, fieldValues, userId, ip);
```

## For UI/Views

### Displaying Patient IDs
Use FriendlyId for human-readable display:

```cshtml
<h2>Patient @Model.Patient.FriendlyId</h2>
<p>Name: @Model.Patient.GivenName @Model.Patient.FamilyName</p>
```

### Links and Routes
Routes now use Guids:

```cshtml
<a asp-page="./Edit" asp-route-id="@patient.Id">Edit</a>
<!-- Generates: /Patients/Edit?id=a7f3b8c9-1234-5678-9abc-def012345678 -->
```

But display the FriendlyId:
```cshtml
<a asp-page="./Details" asp-route-id="@patient.Id">
    View Patient @patient.FriendlyId
</a>
```

## Running the Migration

### 1. Create the Migration
```bash
dotnet ef migrations add ChangePatientIdToGuidAndAddFriendlyId --project Surveillance-MVP
```

### 2. Review the Migration
Check the generated migration file in `Data/Migrations/`. It will:
- Change Patient.Id from int to uniqueidentifier (Guid)
- Add Patient.FriendlyId column
- Update all foreign keys
- Convert AuditLog.EntityId from int to string

### 3. Backup Your Database
**?? CRITICAL**: Backup your database before running the migration!

```sql
-- SQL Server example
BACKUP DATABASE [SurveillanceDB] 
TO DISK = 'C:\\Backups\\SurveillanceDB_BeforeGuidMigration.bak'
WITH FORMAT, INIT, COMPRESSION;
```

### 4. Important: Complex Migration Required

**?? WARNING**: This is NOT a simple migration! SQL Server cannot directly convert an `int IDENTITY` column to a `uniqueidentifier` (GUID). The migration requires:

1. **Creating new columns** (NewId as GUID, FriendlyId)
2. **Generating values** for existing records
3. **Updating all foreign key references** 
4. **Dropping constraints** and recreating them
5. **Dropping the old Id column** and renaming NewId to Id

**Two Migration Options:**

#### Option A: Fresh Database (Recommended for Development)
If you have **no production data** or can start fresh:

```bash
# Drop the database and recreate
dotnet ef database drop --project Surveillance-MVP --force
dotnet ef database update --project Surveillance-MVP
```

#### Option B: Migrate Existing Data (Production)
If you have **existing patient data**, you'll need a custom migration script. See the "Data Migration" section below for the complete SQL script.

**?? The EF Core migration alone will NOT work** for existing data - you must use a custom migration script!

### 5. Run the Migration (New Database Only)
```bash
dotnet ef database update --project Surveillance-MVP
```

**Skip this step if you have existing data!** Use the custom migration script instead.

## Data Migration (If You Have Existing Data)

**?? CRITICAL**: This is a **destructive operation**. You must have a backup!

If you have existing patients, you need a custom migration script because SQL Server cannot convert IDENTITY columns directly. Here's the complete process:

### Complete Migration Script

```sql
-- ============================================
-- Patient GUID Migration Script
-- ============================================
-- WARNING: Test on a backup first!
-- This script will:
-- 1. Add new GUID column and FriendlyId
-- 2. Generate values for existing patients
-- 3. Update all foreign key references
-- 4. Drop old Id column and rename NewId to Id
-- ============================================

BEGIN TRANSACTION;

-- Step 1: Add new columns to Patients table
ALTER TABLE Patients ADD 
    NewId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    FriendlyId NVARCHAR(20);

-- Step 2: Create mapping table for foreign key updates
CREATE TABLE #PatientIdMapping (
    OldId INT NOT NULL,
    NewId UNIQUEIDENTIFIER NOT NULL,
    FriendlyId NVARCHAR(20) NOT NULL,
    CreatedYear INT NOT NULL
);

-- Step 3: Generate GUIDs and FriendlyIds for existing patients
INSERT INTO #PatientIdMapping (OldId, NewId, CreatedYear)
SELECT Id, NewId, YEAR(ISNULL(CreatedAt, GETDATE()))
FROM Patients;

-- Step 4: Generate FriendlyIds (sequential per year)
DECLARE @year INT;
DECLARE @counter INT;

DECLARE year_cursor CURSOR FOR 
SELECT DISTINCT CreatedYear FROM #PatientIdMapping ORDER BY CreatedYear;

OPEN year_cursor;
FETCH NEXT FROM year_cursor INTO @year;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @counter = 1;
    
    UPDATE #PatientIdMapping
    SET FriendlyId = 'P-' + CAST(@year AS VARCHAR(4)) + '-' + 
                     RIGHT('00000' + CAST(@counter AS VARCHAR), 5),
        @counter = @counter + 1
    WHERE CreatedYear = @year
    ORDER BY OldId;
    
    FETCH NEXT FROM year_cursor INTO @year;
END;

CLOSE year_cursor;
DEALLOCATE year_cursor;

-- Step 5: Update FriendlyIds in Patients table
UPDATE p
SET p.FriendlyId = m.FriendlyId
FROM Patients p
INNER JOIN #PatientIdMapping m ON p.Id = m.OldId;

-- Step 6: Drop foreign key constraints on custom field tables
ALTER TABLE PatientCustomFieldStrings DROP CONSTRAINT IF EXISTS FK_PatientCustomFieldStrings_Patients_PatientId;
ALTER TABLE PatientCustomFieldNumbers DROP CONSTRAINT IF EXISTS FK_PatientCustomFieldNumbers_Patients_PatientId;
ALTER TABLE PatientCustomFieldDates DROP CONSTRAINT IF EXISTS FK_PatientCustomFieldDates_Patients_PatientId;
ALTER TABLE PatientCustomFieldBooleans DROP CONSTRAINT IF EXISTS FK_PatientCustomFieldBooleans_Patients_PatientId;
ALTER TABLE PatientCustomFieldLookups DROP CONSTRAINT IF EXISTS FK_PatientCustomFieldLookups_Patients_PatientId;

-- Step 7: Add new PatientId columns to custom field tables
ALTER TABLE PatientCustomFieldStrings ADD NewPatientId UNIQUEIDENTIFIER;
ALTER TABLE PatientCustomFieldNumbers ADD NewPatientId UNIQUEIDENTIFIER;
ALTER TABLE PatientCustomFieldDates ADD NewPatientId UNIQUEIDENTIFIER;
ALTER TABLE PatientCustomFieldBooleans ADD NewPatientId UNIQUEIDENTIFIER;
ALTER TABLE PatientCustomFieldLookups ADD NewPatientId UNIQUEIDENTIFIER;

-- Step 8: Update custom field tables with new GUIDs
UPDATE pcf
SET pcf.NewPatientId = m.NewId
FROM PatientCustomFieldStrings pcf
INNER JOIN #PatientIdMapping m ON pcf.PatientId = m.OldId;

UPDATE pcf
SET pcf.NewPatientId = m.NewId
FROM PatientCustomFieldNumbers pcf
INNER JOIN #PatientIdMapping m ON pcf.PatientId = m.OldId;

UPDATE pcf
SET pcf.NewPatientId = m.NewId
FROM PatientCustomFieldDates pcf
INNER JOIN #PatientIdMapping m ON pcf.PatientId = m.OldId;

UPDATE pcf
SET pcf.NewPatientId = m.NewId
FROM PatientCustomFieldBooleans pcf
INNER JOIN #PatientIdMapping m ON pcf.PatientId = m.OldId;

UPDATE pcf
SET pcf.NewPatientId = m.NewId
FROM PatientCustomFieldLookups pcf
INNER JOIN #PatientIdMapping m ON pcf.PatientId = m.OldId;

-- Step 9: Drop old PatientId columns from custom field tables
ALTER TABLE PatientCustomFieldStrings DROP COLUMN PatientId;
ALTER TABLE PatientCustomFieldNumbers DROP COLUMN PatientId;
ALTER TABLE PatientCustomFieldDates DROP COLUMN PatientId;
ALTER TABLE PatientCustomFieldBooleans DROP COLUMN PatientId;
ALTER TABLE PatientCustomFieldLookups DROP COLUMN PatientId;

-- Step 10: Rename NewPatientId to PatientId
EXEC sp_rename 'PatientCustomFieldStrings.NewPatientId', 'PatientId', 'COLUMN';
EXEC sp_rename 'PatientCustomFieldNumbers.NewPatientId', 'PatientId', 'COLUMN';
EXEC sp_rename 'PatientCustomFieldDates.NewPatientId', 'PatientId', 'COLUMN';
EXEC sp_rename 'PatientCustomFieldBooleans.NewPatientId', 'PatientId', 'COLUMN';
EXEC sp_rename 'PatientCustomFieldLookups.NewPatientId', 'PatientId', 'COLUMN';

-- Step 11: Update AuditLogs (convert EntityId from int to string for Patient records)
-- First, change column type
ALTER TABLE AuditLogs ALTER COLUMN EntityId NVARCHAR(50) NOT NULL;

-- Then update Patient audit log EntityIds to match new GUIDs (as strings)
UPDATE a
SET a.EntityId = CAST(m.NewId AS NVARCHAR(50))
FROM AuditLogs a
INNER JOIN #PatientIdMapping m ON TRY_CAST(a.EntityId AS INT) = m.OldId
WHERE a.EntityType = 'Patient';

-- Step 12: Drop old Id column from Patients table
-- First, drop any constraints on the old Id column
DECLARE @constraintName NVARCHAR(200);
DECLARE constraint_cursor CURSOR FOR 
SELECT name 
FROM sys.key_constraints 
WHERE parent_object_id = OBJECT_ID('Patients') 
AND type = 'PK';

OPEN constraint_cursor;
FETCH NEXT FROM constraint_cursor INTO @constraintName;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC('ALTER TABLE Patients DROP CONSTRAINT ' + @constraintName);
    FETCH NEXT FROM constraint_cursor INTO @constraintName;
END;

CLOSE constraint_cursor;
DEALLOCATE constraint_cursor;

-- Drop the old Id column
ALTER TABLE Patients DROP COLUMN Id;

-- Step 13: Rename NewId to Id
EXEC sp_rename 'Patients.NewId', 'Id', 'COLUMN';

-- Step 14: Add primary key constraint on new Id column
ALTER TABLE Patients ADD CONSTRAINT PK_Patients PRIMARY KEY (Id);

-- Step 15: Add unique constraint on FriendlyId
ALTER TABLE Patients ADD CONSTRAINT UQ_Patients_FriendlyId UNIQUE (FriendlyId);

-- Step 16: Recreate foreign key constraints on custom field tables
ALTER TABLE PatientCustomFieldStrings ADD CONSTRAINT FK_PatientCustomFieldStrings_Patients_PatientId 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE;

ALTER TABLE PatientCustomFieldNumbers ADD CONSTRAINT FK_PatientCustomFieldNumbers_Patients_PatientId 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE;

ALTER TABLE PatientCustomFieldDates ADD CONSTRAINT FK_PatientCustomFieldDates_Patients_PatientId 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE;

ALTER TABLE PatientCustomFieldBooleans ADD CONSTRAINT FK_PatientCustomFieldBooleans_Patients_PatientId 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE;

ALTER TABLE PatientCustomFieldLookups ADD CONSTRAINT FK_PatientCustomFieldLookups_Patients_PatientId 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE;

-- Step 17: Recreate indexes on custom field tables
CREATE UNIQUE INDEX IX_PatientCustomFieldStrings_PatientId_FieldDefinitionId 
    ON PatientCustomFieldStrings(PatientId, FieldDefinitionId);

CREATE UNIQUE INDEX IX_PatientCustomFieldNumbers_PatientId_FieldDefinitionId 
    ON PatientCustomFieldNumbers(PatientId, FieldDefinitionId);

CREATE UNIQUE INDEX IX_PatientCustomFieldDates_PatientId_FieldDefinitionId 
    ON PatientCustomFieldDates(PatientId, FieldDefinitionId);

CREATE UNIQUE INDEX IX_PatientCustomFieldBooleans_PatientId_FieldDefinitionId 
    ON PatientCustomFieldBooleans(PatientId, FieldDefinitionId);

CREATE UNIQUE INDEX IX_PatientCustomFieldLookups_PatientId_FieldDefinitionId 
    ON PatientCustomFieldLookups(PatientId, FieldDefinitionId);

-- Step 18: Clean up
DROP TABLE #PatientIdMapping;

-- If everything looks good, commit the transaction
COMMIT TRANSACTION;

-- If there were errors, run this instead:
-- ROLLBACK TRANSACTION;

PRINT 'Migration completed successfully!';
PRINT 'Patients migrated with new GUID Ids and FriendlyIds';
```

### After Running the Migration Script

1. **Verify the migration**:
```sql
-- Check that all patients have GUIDs and FriendlyIds
SELECT TOP 10 Id, FriendlyId, GivenName, FamilyName 
FROM Patients;

-- Check that custom fields are still linked
SELECT COUNT(*) as CustomFieldCount
FROM PatientCustomFieldStrings;

-- Check audit logs
SELECT TOP 10 EntityType, EntityId, Action, ChangedAt
FROM AuditLogs 
WHERE EntityType = 'Patient'
ORDER BY ChangedAt DESC;
```

2. **Update EF Core Migration History**:
Since you ran a custom migration, you need to mark the EF migration as applied without running it:

```bash
# This tells EF Core the migration is already applied
dotnet ef migrations add ChangePatientIdToGuidAndAddFriendlyId --project Surveillance-MVP
# Then manually insert into __EFMigrationsHistory table or use:
dotnet ef database update --project Surveillance-MVP
```

3. **Test thoroughly** before deploying to production!

## Troubleshooting

### Issue: "Cannot convert int to Guid"
**Solution**: Make sure all page model route parameters use `Guid?` instead of `int?`

### Issue: "Cannot convert Guid to string" in audit logging
**Solution**: Add `.ToString()` when passing Guid to audit service methods

### Issue: FriendlyId not generating
**Check**: 
1. Patient entity is saved before accessing FriendlyId
2. SaveChanges() is called (triggers auto-generation)
3. Check ApplicationDbContext.GeneratePatientFriendlyIds() method

### Issue: Duplicate FriendlyIds
**Check**: Ensure unique index is created on Patient.FriendlyId column

## Benefits

### Security
- GUIDs are not predictable (unlike sequential IDs)
- Reduces enumeration attacks
- No information leakage about patient count

### Scalability
- Safe for distributed systems
- No collision risk when merging databases
- Can generate IDs offline

### Usability
- FriendlyId is human-readable
- Easy for staff communication
- Works well on paper forms
- Searchable and memorable

## Best Practices

1. **Always display FriendlyId** to users, not the GUID
2. **Use GUID internally** for database operations and APIs
3. **Allow search by both** FriendlyId and GUID for flexibility
4. **Include FriendlyId** in exports, reports, and printouts
5. **Log FriendlyId** in audit trails for readability

## Example: Complete Patient Flow

```csharp
// 1. Create patient
var patient = new Patient 
{
    GivenName = "Jane",
    FamilyName = "Smith",
    DateOfBirth = new DateTime(1990, 5, 15)
};
_context.Patients.Add(patient);
await _context.SaveChangesAsync();
// patient.Id = Guid.NewGuid() (auto)
// patient.FriendlyId = "P-2025-00042" (auto)

// 2. Display to user
TempData["SuccessMessage"] = $"Patient {patient.FriendlyId} created successfully";

// 3. Edit patient (later)
public async Task<IActionResult> OnGetAsync(Guid? id)
{
    var patient = await _context.Patients.FindAsync(id);
    // Display patient.FriendlyId in the view
}

// 4. Audit logging
await _auditService.LogChangeAsync(
    "Patient", 
    patient.Id.ToString(),  // Convert Guid to string
    "Date of Birth",
    oldDob,
    newDob,
    userId,
    ipAddress
);
```
