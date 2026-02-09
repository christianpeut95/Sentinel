# Patient Merge Workflow

## Overview

The patient merge feature allows you to combine two patient records into one, preserving all data and audit history. This is useful when duplicate patients have been created or when consolidating records.

## Features

- **Side-by-side comparison** of all fields from both patients
- **Field-by-field selection** of which values to keep
- **Custom field support** - merge custom field values as well
- **Audit trail preservation** - all audit logs from both patients are preserved
- **Soft delete** - the source patient is marked as deleted, not permanently removed
- **Transaction safety** - entire merge operation is wrapped in a database transaction

## How to Merge Patients

### 1. Start the Merge Process

You can start the merge process in two ways:

**Option A: From Patient Details Page**
1. Navigate to a patient's details page
2. Click the "Merge" button in the top-right corner
3. The patient will be pre-selected as the source patient

**Option B: From Patient List**
1. Click "Merge Patients" button on the Patients index page
2. Enter both patient IDs manually

### 2. Select Source and Target Patients

- **Source Patient**: This patient will be deleted after the merge
- **Target Patient**: This patient will receive the merged data and be kept

Click "Next: Compare Patients" to proceed.

### 3. Compare and Select Values

The merge comparison page shows:

#### Standard Fields
All patient fields are displayed side-by-side:
- Personal Information (Name, Date of Birth, Sex, Gender)
- Contact Information (Phone, Email, Address)
- Demographics (Country of Birth, Language, Ethnicity, ATSI Status)
- Occupation

#### Custom Fields
All custom fields are displayed with values from both patients.

#### Selecting Values
- Click on a value to select it (radio button)
- The selected value will be kept in the merged patient
- By default, non-empty values are pre-selected
- You can select values from either patient for each field

### 4. Review and Confirm

Before confirming:
- Review the audit log counts for both patients
- All audit logs will be preserved and linked to the target patient
- The merge operation cannot be undone

Click "Confirm Merge" to complete the process.

## What Happens During a Merge

1. **Data Transfer**: Selected field values are applied to the target patient
2. **Custom Fields**: Selected custom field values are merged
3. **Audit Log Reassignment**: All audit logs from the source patient are:
   - Linked to the target patient
   - Prefixed with `[From Patient {sourceId}]` for tracking
4. **Source Patient Deletion**: The source patient record is deleted
5. **Merge Audit Log**: A special audit log entry is created documenting the merge operation

## Technical Details

### Services

**IPatientMergeService** provides:
- `GetMergeComparisonAsync()` - Retrieves both patients with all related data
- `ValidateMergeAsync()` - Ensures both patients exist and are different
- `MergePatientsAsync()` - Performs the merge operation in a transaction

### Database Operations

The merge operation:
- Uses database transactions for atomicity
- Preserves referential integrity
- Logs all changes to the audit system
- Handles custom fields of all types (String, Number, Date, Boolean, Lookup)

### Security

- Requires authentication (user must be logged in)
- IP address and user ID are recorded for audit purposes
- Confirmation dialog prevents accidental merges

## Best Practices

1. **Always review carefully** - The merge cannot be undone
2. **Choose the correct target** - The target patient's ID will be preserved
3. **Check audit history** - Review both patients' audit logs before merging
4. **Custom fields** - Ensure you select the correct values for custom fields
5. **Export data first** - If unsure, export both patient records before merging

## Error Handling

The merge will fail if:
- Either patient doesn't exist
- The same patient ID is used for both source and target
- A database constraint is violated
- The transaction fails for any reason

All errors are displayed to the user with a descriptive message.

## Audit Trail

The merge operation creates audit log entries for:
- Each field value change in the target patient
- Each custom field value change
- A special "Merge" action entry documenting the merge operation

Example audit log entry:
```
Action: Modified
Field: Merge
Old Value: Patient 123
New Value: Merged into Patient 456
```

## UI Components

### Pages
- `SelectMerge.cshtml` - Patient selection page
- `Merge.cshtml` - Comparison and value selection page

### Partial Views
- `_MergeFieldRow.cshtml` - Reusable component for field comparison rows

### Services
- `IPatientMergeService` - Service interface
- `PatientMergeService` - Service implementation

## Future Enhancements

Potential improvements:
- Bulk merge operations
- Merge preview/simulation
- Undo merge functionality (restore from deleted)
- Automatic duplicate detection
- Merge suggestions based on similarity
- Export merge report (PDF/CSV)

## Related Documentation

- [Audit Logging Guide](./Audit-Logging-Guide.md)
- [Custom Fields Guide](./CustomFields-Patient-Integration-Complete.md)
- [Patient Management](./Complete-Patient-Audit-Logging.md)
