# Sentinel Database Migrations

## Overview
This directory contains Entity Framework Core migrations for the Sentinel application. Migrations are applied sequentially in chronological order based on their timestamp prefix.

## Migration Order
Migrations must be applied in the order listed below. The timestamp prefix (YYYYMMDDHHMMSS) determines the order.

## Special Migrations

### FixInvalidLogicalOperators.cs
This is a **manual SQL migration** that fixes invalid LogicalOperator values in CaseDefinitionCriteria.
- **Not tracked by EF Core**: This migration does not appear in `__EFMigrationsHistory` table
- **Purpose**: Fixes legacy data where LogicalOperator was 0 (invalid) and adds a check constraint
- **When to apply**: After `20260722002830_AddGroupExitOperatorToCriteria` is applied
- **Apply manually**: Run the SQL in this file directly against the database if needed

## Applying Migrations

### Fresh Database Deployment
For a new installation, run:
```powershell
cd Sentinel
dotnet ef database update --context ApplicationDbContext
```

This will apply all migrations from `20260304034303_InitialCreate_Clean` through the latest migration.

### Updating Existing Database
To update an existing database to the latest schema:
```powershell
cd Sentinel
dotnet ef database update --context ApplicationDbContext
```

EF Core will automatically determine which migrations are pending and apply them in order.

### Manual Migration (FixInvalidLogicalOperators)
If you encounter issues with invalid LogicalOperator values, apply this migration manually:
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

## Latest Migrations

### 20260722002830_AddGroupExitOperatorToCriteria
Adds `GroupExitOperator` column to `CaseDefinitionCriteria` table to support nested logical grouping.

### 20260724082854_AddMultiplexLabResultSupport
Adds multiplex lab result support:
- `ParentLabResultId` - Links cloned lab results to their parent
- `IsMultiplexClone` - Flags lab results that are clones
- Self-referencing foreign key for parent/child lab result relationships

When an HL7 result identifies multiple diseases (e.g., Influenza A + RSV from a multiplex test):
- First disease gets the original `LabResult`
- Additional diseases get cloned `LabResult` records
- Each clone links to its own `Case`
- All clones maintain traceability via `ParentLabResultId`

## Troubleshooting

### Error: "Invalid column name 'GroupExitOperator'"
**Cause**: The `AddGroupExitOperatorToCriteria` migration was not applied to the database.

**Solution**:
1. Verify the migration file exists: `Sentinel\Migrations\20260722002830_AddGroupExitOperatorToCriteria.cs`
2. Apply migrations: `dotnet ef database update --context ApplicationDbContext`
3. If the file is missing, restore from git:
   ```powershell
   cd Sentinel
   git show f3e8b8b:Sentinel/Migrations/20260722002830_AddGroupExitOperatorToCriteria.cs > Migrations/20260722002830_AddGroupExitOperatorToCriteria.cs
   git show f3e8b8b:Sentinel/Migrations/20260722002830_AddGroupExitOperatorToCriteria.Designer.cs > Migrations/20260722002830_AddGroupExitOperatorToCriteria.Designer.cs
   dotnet ef database update --context ApplicationDbContext
   ```

### Checking Migration Status
```powershell
# List all migrations and their applied status
dotnet ef migrations list --context ApplicationDbContext

# Check if there are pending model changes not yet in a migration
dotnet ef migrations has-pending-model-changes --context ApplicationDbContext
```

## Creating New Migrations
When you modify entity models, create a new migration:
```powershell
cd Sentinel
dotnet ef migrations add YourMigrationName --context ApplicationDbContext
```

Always review the generated migration file before applying it to ensure it contains only the intended changes.

## Production Deployment Checklist
Before deploying to production:
1. ✅ Backup the database
2. ✅ Test migrations on a staging environment
3. ✅ Review migration scripts for data loss operations (DropColumn, DropTable)
4. ✅ Ensure application is stopped or in maintenance mode
5. ✅ Apply migrations: `dotnet ef database update --context ApplicationDbContext`
6. ✅ Verify critical tables and data after migration
7. ✅ Start application and verify functionality

## Migration Files Location
- **Migration Classes**: `Sentinel\Migrations\*.cs`
- **Designer Files**: `Sentinel\Migrations\*.Designer.cs`
- **Model Snapshot**: `Sentinel\Migrations\ApplicationDbContextModelSnapshot.cs`

The model snapshot represents the current state of your EF model and is updated with each migration.
