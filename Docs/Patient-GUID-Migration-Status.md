# Patient GUID Migration - COMPLETED ?

## Overview
Successfully migrated Patient ID from `int` to `Guid` and added a FriendlyId in format "P-YEAR-XXXXX".

## Summary
All compilation errors have been resolved. The system now uses:
- **GUID** for internal Patient IDs (secure, non-sequential, distributed-system friendly)
- **FriendlyId** for human-readable references (format: P-2025-00001, P-2025-00002, etc.)

## Completed Changes ?

### Models ?
- `Patient.cs` - Changed `Id` from `int` to `Guid`, added `FriendlyId` property
- `PatientCustomFieldString.cs` - Changed `PatientId` from `int` to `Guid`
- `PatientCustomFieldNumber.cs` - Changed `PatientId` from `int` to `Guid`
- `PatientCustomFieldDate.cs` - Changed `PatientId` from `int` to `Guid`
- `PatientCustomFieldBoolean.cs` - Changed `PatientId` from `int` to `Guid`
- `PatientCustomFieldLookup.cs` - Changed `PatientId` from `int` to `Guid`
- `AuditLog.cs` - Changed `EntityId` from `int` to `string` (to support both int and Guid entities)
- `IAuditable.cs` - Removed `Id` requirement (was too restrictive with different ID types)

### Database Context ?
- `ApplicationDbContext.cs`:
  - Added unique index on `Patient.FriendlyId`
  - Added `GeneratePatientFriendlyIds()` and `GeneratePatientFriendlyIdsSync()` methods
  - Added `GenerateNextFriendlyId()` and `GenerateNextFriendlyIdSync()` methods
  - Auto-generates GUID for new patients
  - Auto-generates FriendlyId in format "P-YEAR-XXXXX" (e.g., "P-2025-00001")
  - Updated audit logic to handle string EntityId

### Service Interfaces ?
- `IPatientCustomFieldService.cs` - Updated all methods to use `Guid patientId`
- `IAuditService.cs` - Updated to use `string entityId` and `Guid patientId` where appropriate
- `IPatientMergeService.cs` - Updated all methods to use `Guid` for patient IDs

### Service Implementations ?
- `PatientCustomFieldService.cs` - All methods updated to use `Guid`
  - `GetPatientFieldValuesAsync(Guid patientId)`
  - `GetPatientFieldDisplayValuesAsync(Guid patientId)`
  - `SavePatientFieldValuesAsync(Guid patientId, ...)`
  - All private helper methods (SaveStringFieldAsync, SaveNumberFieldAsync, etc.)
- `AuditService.cs` - Updated to use `string` for entityId, `Guid` for patientId
  - `GetAuditLogsAsync(string entityType, string entityId)`
  - `GetAuditLogCountAsync(string entityType, string entityId)`
  - `LogViewAsync(string entityType, string entityId, ...)`
  - `LogCustomFieldChangeAsync(Guid patientId, ...)`
  - `LogChangeAsync(string entityType, string entityId, ...)`
- `PatientMergeService.cs` - Fully updated
  - `GetMergeComparisonAsync(Guid sourcePatientId, Guid targetPatientId)`
  - `ValidateMergeAsync(Guid sourcePatientId, Guid targetPatientId)`
  - `MergePatientsAsync(Guid sourcePatientId, Guid targetPatientId, ...)`
  - All private helper methods updated
  - `MergeResult` class updated with `Guid?` properties

### Page Models ?
- `Create.cshtml.cs` - Updated to handle Guid IDs and audit logging
- `Edit.cshtml.cs` - Updated `OnGetAsync(Guid? id)`, `PatientExists(Guid id)`, all audit calls
- `Details.cshtml.cs` - Updated `OnGetAsync(Guid? id)` and audit logging
- `Delete.cshtml.cs` - Updated `OnGetAsync(Guid? id)` and `OnPostAsync(Guid? id)`
- `AuditHistory.cshtml.cs` - Updated `PatientId` property to `Guid`, updated service call
- `Merge.cshtml.cs` - Updated `SourceId` and `TargetId` to `Guid`
- `SelectMerge.cshtml.cs` - Updated all patient ID properties to `Guid`

## Next Steps

### Database Migration Required ???
The code is ready, but you need to create and run a migration:

```bash
dotnet ef migrations add ChangePatientIdToGuidAndAddFriendlyId
dotnet ef database update
```

**?? IMPORTANT**: This is a breaking change for existing databases!

### Data Migration for Existing Data ??
If you have existing patient data, you'll need a data migration script to:
1. Backup existing database
2. Create a mapping table (OldIntId ? NewGuidId)
3. Generate GUIDs for existing patients
4. Generate FriendlyIds for existing patients based on creation year/date
5. Update all foreign key references in:
   - PatientCustomFieldString
   - PatientCustomFieldNumber
   - PatientCustomFieldDate
   - PatientCustomFieldBoolean
   - PatientCustomFieldLookup
   - AuditLogs (convert int EntityIds to string for Patient entities)

### Testing Checklist ?
Before deploying to production, test:
- [ ] Patient creation creates both GUID and FriendlyId automatically
- [ ] FriendlyId is sequential per year (P-2025-00001, P-2025-00002, etc.)
- [ ] FriendlyId is unique across all patients
- [ ] Patient editing works correctly
- [ ] Patient deletion works correctly
- [ ] Patient details view displays correctly
- [ ] Patient search functionality works
- [ ] Custom fields save and load correctly
- [ ] Audit logging captures all patient operations
- [ ] Patient merge workflow completes successfully
- [ ] Audit logs from merged patients are reassigned correctly
- [ ] All views display FriendlyId instead of GUID where appropriate

### View Updates ??
You may want to update Razor views to display the FriendlyId instead of the GUID:
- Patient lists should show FriendlyId
- Patient details pages should show FriendlyId prominently
- Search results should display FriendlyId
- Audit history should show FriendlyId

Example:
```cshtml
<dt class="col-sm-3">Patient ID</dt>
<dd class="col-sm-9">@Model.Patient.FriendlyId</dd>
```

## Benefits Achieved ?
- **GUID**: Better for distributed systems, no collision risk, more secure (not sequential)
- **FriendlyId**: Human-readable, easier for staff communication and paper records
- **Flexibility**: Audit system now supports multiple entity ID types (int and Guid)
- **Future-proof**: System can handle other entities with different ID types
- **Security**: GUIDs are not predictable, reducing enumeration attacks

## Rollback Plan
If issues arise:
1. Revert code changes from source control
2. Restore database backup taken before migration
3. Alternatively, create reverse migration to convert back to int IDs (complex)
