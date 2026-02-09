# Patient Merge - Quick Start Guide

## Quick Access

- **Start Merge**: Navigate to Patients > Click "Merge Patients" button
- **From Patient Details**: Click "Merge" button on any patient details page

## 3-Step Process

### Step 1: Select Patients
- **Source Patient** ? Will be DELETED
- **Target Patient** ? Will be KEPT
- Enter patient IDs and click "Next"

### Step 2: Compare & Select
- Review all fields side-by-side
- Click on values to select which ones to keep
- Non-empty values are pre-selected by default

### Step 3: Confirm
- Review summary
- Click "Confirm Merge" (requires confirmation)
- Merge cannot be undone!

## What Gets Merged

? All standard patient fields  
? All custom fields  
? Complete audit history from both patients  
? All changes are logged for compliance

## Important Notes

?? **Cannot be undone** - Review carefully before confirming  
?? Source patient is permanently deleted  
? All audit logs are preserved and linked to target patient  
? Entire operation is transactional (all or nothing)

## URLs

- Select Patients: `/Patients/SelectMerge`
- Compare Page: `/Patients/Merge?sourceId={id}&targetId={id}`

## See Also

- [Full Patient Merge Documentation](./Patient-Merge-Workflow.md)
- [Audit Logging Guide](./Audit-Logging-Guide.md)
