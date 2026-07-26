# Manual SQL Migrations & Scripts

This document lists all manual SQL scripts and migrations that require special handling outside of the normal EF Core migration process.

## Overview

Most migrations are handled automatically by Entity Framework Core, but some operations require manual SQL execution:
- Data cleanup scripts (duplicates, orphaned records)
- Index modifications that conflict with existing data
- Database views and stored procedures
- Lookup data seeding

## Manual SQL Files

### 🔧 FixInvalidLogicalOperators.cs
**Location**: `Sentinel/Migrations/FixInvalidLogicalOperators.cs`  
**Type**: Special migration with manual SQL  
**Status**: ⚠️ Not tracked by EF Core  

**Purpose**: Fixes legacy data where `CaseDefinitionCriteria.LogicalOperator` was set to invalid value (0).

**When to Apply**:
- If you encounter errors about invalid LogicalOperator values
- After applying `20260722002830_AddGroupExitOperatorToCriteria` migration
- When importing case definitions from older systems

**SQL**:
```sql
-- Update any criteria with invalid LogicalOperator = 0 to AND (1)
UPDATE CaseDefinitionCriteria 
SET LogicalOperator = 1 
WHERE LogicalOperator = 0 OR LogicalOperator IS NULL;

-- Add check constraint to ensure LogicalOperator is always valid (1=AND, 2=OR, 3=NOT)
ALTER TABLE CaseDefinitionCriteria 
ADD CONSTRAINT CK_CaseDefinitionCriteria_LogicalOperator 
CHECK (LogicalOperator IN (1, 2, 3));
```

**Apply**: Run the SQL in `Up()` method directly against database if needed.

---

### 📄 Remove_Unique_Index_Manual.sql
**Location**: `Sentinel/Migrations/Remove_Unique_Index_Manual.sql`  
**Type**: Index modification  
**Related Migration**: `20260714234516_Remove_HL7Message_Unique_Index_To_Allow_Duplicates`  

**Purpose**: Replaces UNIQUE index on HL7Messages with non-unique index to allow duplicate messages for audit trail.

**When to Apply**:
- If the EF migration fails due to existing duplicates
- If you need to manually fix the index after data corruption

**Changes**:
- Drops: `IX_HL7Messages_MessageControlId_SendingFacility` (UNIQUE)
- Creates: `IX_HL7Messages_MessageControlId_SendingFacility` (NON-UNIQUE)

**Status**: ✅ Usually handled automatically by EF migration, use this only if manual fix needed.

---

### 🧹 Cleanup_Duplicate_HL7Messages.sql
**Location**: `Sentinel/Migrations/Cleanup_Duplicate_HL7Messages.sql`  
**Type**: Data cleanup script  
**Status**: ⚠️ Run BEFORE applying unique index migrations  

**Purpose**: Removes duplicate HL7Messages before applying any unique constraints.

**When to Apply**:
- Before applying `20260506094628_Add_HL7Message_Unique_MessageControlId_Index`
- If you discover duplicate HL7 messages causing issues
- During database maintenance/cleanup

**What It Does**:
1. Identifies duplicates by `MessageControlId` + `SendingFacility`
2. Keeps the FIRST received message (most likely to have been processed)
3. Deletes all subsequent duplicates

**⚠️ Warning**: This permanently deletes duplicate messages. Ensure you have a backup.

**Usage**:
```powershell
# From SQL Server Management Studio or Azure Data Studio
# Open the file and execute against your database
```

---

### 🧹 Cleanup_Duplicate_Patients_Today.sql
**Location**: `Sentinel/Migrations/Cleanup_Duplicate_Patients_Today.sql`  
**Type**: Data cleanup script  
**Status**: ⚠️ Destructive - use with caution  

**Purpose**: Cleans up duplicate patients created on the same day.

**When to Apply**:
- If HL7 processing created duplicate patients due to race conditions
- During troubleshooting of patient matching issues
- As part of database maintenance

**What It Does**:
1. Finds patients created today with same name + DOB
2. Keeps the FIRST patient created
3. **Deletes duplicate patients AND their related data**:
   - Cases
   - Lab Results
   - HL7 Messages
   - Review Queue items

**⚠️ CRITICAL WARNING**: This script CASCADE DELETES data! Always:
- Backup database first
- Review duplicate list before executing
- Test on staging environment

**Scope**: Only affects patients created TODAY (since midnight).

---

### 🔍 Diagnostic_Duplicate_Processing.sql
**Location**: `Sentinel/Migrations/Diagnostic_Duplicate_Processing.sql`  
**Type**: Diagnostic/Investigation script  
**Status**: ✅ Read-only, safe to run  

**Purpose**: Identifies duplicate processing issues without modifying data.

**When to Run**:
- When investigating duplicate patient/case creation
- After HL7 message processing issues
- During performance troubleshooting

**What It Shows**:
1. Duplicate HL7Messages
2. Duplicate patients (created in last hour)
3. Recent HL7 messages and their linked patients
4. Cases with no linked patients
5. Lab results with no linked cases

**No Changes**: This is a diagnostic tool only, makes no modifications.

---

## Migration Strategy

### For Fresh Installations
1. Run `dotnet ef database update` - applies all EF migrations (including healthcare provider seed)
2. Done! Other scripts only needed if issues arise.

### For Existing Installations
1. **Backup database** first
2. Run `dotnet ef database update` - applies pending EF migrations
3. If duplicate issues exist:
   - Run diagnostic script first
   - Run cleanup scripts if needed
4. If LogicalOperator errors occur:
   - Apply `FixInvalidLogicalOperators` SQL manually

### For Troubleshooting
1. Run `Diagnostic_Duplicate_Processing.sql` to identify issues
2. Backup database
3. Run appropriate cleanup script
4. Reprocess failed HL7 messages

---

## Frequently Asked Questions

### Q: Which scripts MUST be run manually?
**A:** Only these require manual execution:
- `FixInvalidLogicalOperators.cs` (if you encounter LogicalOperator errors)
- Cleanup scripts (only if you have duplicate data issues)

All other migrations, including reference data seeding, are handled automatically by `dotnet ef database update`.

### Q: When do I need FixInvalidLogicalOperators?
**A:** Only if:
- You imported case definitions from older systems
- You see errors like "Invalid LogicalOperator value"
- Case definition matching behaves unexpectedly

### Q: Are cleanup scripts destructive?
**A:** YES. Both cleanup scripts delete data:
- `Cleanup_Duplicate_HL7Messages.sql` - deletes duplicate messages
- `Cleanup_Duplicate_Patients_Today.sql` - deletes patients AND all related data

**Always backup first!**

### Q: Can I run these scripts multiple times?
**A:** 
- ✅ Safe to run multiple times:
  - `Diagnostic_Duplicate_Processing.sql` (read-only)
  - `FixInvalidLogicalOperators.cs` (safe if constraint doesn't exist)

- ⚠️ Run carefully:
  - `Cleanup_Duplicate_HL7Messages.sql` (deletes data)
  - `Cleanup_Duplicate_Patients_Today.sql` (deletes data)
  - `Remove_Unique_Index_Manual.sql` (index operations)

### Q: Do I need these for production deployment?
**A:** 
- **Initial deployment**: Just `dotnet ef database update` (includes all seeding)
- **Updating existing**: Usually just `dotnet ef database update`
- **Fixing issues**: Use cleanup/diagnostic scripts as needed

---

## Checklist for Production Deployment

### Pre-Migration
- [ ] Backup database
- [ ] Test migrations on staging
- [ ] Review migration scripts
- [ ] Check for duplicate data (run diagnostic script)

### During Migration
- [ ] Stop application
- [ ] Run `dotnet ef database update --context ApplicationDbContext`
- [ ] (Optional) Run `FixInvalidLogicalOperators` if needed

### Post-Migration
- [ ] Run diagnostic script to verify clean state
- [ ] Start application
- [ ] Test HL7 message processing
- [ ] Verify case creation
- [ ] Check application logs

### If Issues Occur
- [ ] Run diagnostic script to identify problem
- [ ] Run appropriate cleanup script (after backup!)
- [ ] Re-run migrations if needed
- [ ] Test again

---

## Summary Table

| Script | Type | When to Use | Destructive | Idempotent |
|--------|------|-------------|-------------|------------|
| FixInvalidLogicalOperators.cs | Data Fix | LogicalOperator errors | No | Yes* |
| Remove_Unique_Index_Manual.sql | Index Fix | Manual index repair | No | No |
| Cleanup_Duplicate_HL7Messages.sql | Data Cleanup | Before unique constraints | **YES** | Yes |
| Cleanup_Duplicate_Patients_Today.sql | Data Cleanup | Duplicate patient issues | **YES** | Yes |
| Diagnostic_Duplicate_Processing.sql | Investigation | Troubleshooting | No | Yes |

\* Idempotent if constraint doesn't exist; will fail if already applied.

**Note**: The "Healthcare Provider" organization type is now seeded automatically via EF migration `20260724093035_SeedHealthcareProviderOrganizationType`.

---

## Getting Help

If you encounter issues:
1. Run the diagnostic script first
2. Check `DEPLOYMENT.md` for troubleshooting steps
3. Review application logs
4. Contact development team with diagnostic output

## Related Documentation
- `DEPLOYMENT.md` - Full deployment guide
- `Migrations/README.md` - EF Core migration reference
- `MULTIPLEX-FIX-SUMMARY.md` - Recent multiplex feature changes
