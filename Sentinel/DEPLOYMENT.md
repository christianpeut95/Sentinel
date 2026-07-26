# Sentinel Deployment Guide

## Prerequisites
- .NET 10 SDK installed
- SQL Server (2019 or later recommended)
- Visual Studio 2026 or VS Code (optional, for development)

## Initial Deployment (Fresh Installation)

### 1. Database Setup
Create an empty database in SQL Server:
```sql
CREATE DATABASE Sentinel;
GO
```

### 2. Configure Connection String
Edit `Sentinel/appsettings.json` and update the connection string:
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=YOUR_SERVER;Database=Sentinel;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 3. Apply Migrations
Run the migration script:
```powershell
cd Sentinel
.\Apply-Migrations.ps1
```

Or manually:
```powershell
cd Sentinel
dotnet ef database update --context ApplicationDbContext
```

### 4. Verify Schema
After migrations complete, verify critical tables exist:
- `Cases`
- `LabResults` (with `ParentLabResultId` and `IsMultiplexClone` columns)
- `CaseDefinitionCriteria` (with `GroupExitOperator` column)
- `HL7Messages`
- `Patients`

### 5. Start Application
```powershell
cd Sentinel
dotnet run
```

Or publish for production:
```powershell
cd Sentinel
dotnet publish -c Release -o ../publish
```

## Updating Existing Installation

### 1. Backup Database
**CRITICAL**: Always backup before applying migrations:
```sql
BACKUP DATABASE Sentinel 
TO DISK = 'C:\Backups\Sentinel_PreMigration_YYYYMMDD.bak'
WITH FORMAT, INIT, NAME = 'Sentinel Full Backup';
GO
```

### 2. Stop Application
Ensure the application is stopped or in maintenance mode.

### 3. Pull Latest Code
```powershell
git pull origin master
```

### 4. Apply Migrations
```powershell
cd Sentinel
.\Apply-Migrations.ps1
```

### 5. Restart Application
Start the application and verify functionality.

## Migration History

### Critical Migrations to Verify

#### 20260722002830_AddGroupExitOperatorToCriteria
**Purpose**: Adds support for nested logical grouping in case definition criteria.

**Column Added**: `CaseDefinitionCriteria.GroupExitOperator (int, nullable)`

**If Missing**: 
- Error: "Invalid column name 'GroupExitOperator'"
- Solution: Re-apply migrations from git commit `f3e8b8b`

#### 20260724082854_AddMultiplexLabResultSupport
**Purpose**: Enables multiplex HL7 results to create separate lab results per identified disease.

**Changes**:
- Added `LabResults.ParentLabResultId` (Guid, nullable)
- Added `LabResults.IsMultiplexClone` (bit, default false)
- Added self-referencing foreign key `FK_LabResults_LabResults_ParentLabResultId`
- Added index `IX_LabResults_ParentLabResultId`

**Behavior**:
- When a multiplex test identifies multiple diseases (e.g., Flu A + RSV):
  - First disease → original LabResult
  - Additional diseases → cloned LabResults
  - Each clone linked to its own Case
  - Clones track parent via `ParentLabResultId`

#### FixInvalidLogicalOperators.cs
**Purpose**: Manual SQL migration to fix legacy data.

**Not tracked by EF**: This migration must be applied manually if needed.

**Apply if**: You see errors about invalid LogicalOperator values (0).

```sql
UPDATE CaseDefinitionCriteria 
SET LogicalOperator = 1 
WHERE LogicalOperator = 0 OR LogicalOperator IS NULL;

ALTER TABLE CaseDefinitionCriteria 
ADD CONSTRAINT CK_CaseDefinitionCriteria_LogicalOperator 
CHECK (LogicalOperator IN (1, 2, 3));
```

## Troubleshooting

### "Invalid column name 'GroupExitOperator'"
**Cause**: Migration `20260722002830_AddGroupExitOperatorToCriteria` not applied.

**Solution**:
```powershell
cd Sentinel
# Restore migration from git if missing
git show f3e8b8b:Sentinel/Migrations/20260722002830_AddGroupExitOperatorToCriteria.cs > Migrations/20260722002830_AddGroupExitOperatorToCriteria.cs
git show f3e8b8b:Sentinel/Migrations/20260722002830_AddGroupExitOperatorToCriteria.Designer.cs > Migrations/20260722002830_AddGroupExitOperatorToCriteria.Designer.cs

# Apply migrations
dotnet ef database update --context ApplicationDbContext
```

### "Cannot insert duplicate key in object 'dbo.LabResults'"
**Cause**: FriendlyId generation issue or race condition.

**Solution**: Check `ApplicationDbContext.GenerateLabResultFriendlyIds()` method. It uses `UPDLOCK` to prevent races.

### Multiplex Results Only Creating One Case
**Cause**: Old code before multiplex support was implemented.

**Solution**: Ensure you're on commit `7/24/2026` or later with the multiplex cloning logic in `CaseMatchingService.ProcessLabResultAsync`.

### Build Errors After Update
**Cause**: Missing dependencies or incompatible .NET version.

**Solution**:
```powershell
cd Sentinel
dotnet restore
dotnet clean
dotnet build
```

## Rollback Procedure

If a migration fails:

### 1. Restore Database Backup
```sql
USE master;
GO
ALTER DATABASE Sentinel SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO
RESTORE DATABASE Sentinel 
FROM DISK = 'C:\Backups\Sentinel_PreMigration_YYYYMMDD.bak'
WITH REPLACE;
GO
ALTER DATABASE Sentinel SET MULTI_USER;
GO
```

### 2. Revert Code
```powershell
git checkout <previous-working-commit>
```

### 3. Investigate Issue
Review migration scripts and error messages before attempting again.

## Production Deployment Checklist

- [ ] Database backup completed and verified
- [ ] Migrations tested on staging environment
- [ ] Application stopped or in maintenance mode
- [ ] Connection string configured correctly
- [ ] .NET 10 runtime installed on server
- [ ] SQL Server accessible from application server
- [ ] Database user has necessary permissions (db_owner recommended for migrations)
- [ ] Migrations applied successfully
- [ ] Schema validated (critical tables and columns exist)
- [ ] Application started successfully
- [ ] Core functionality tested (patient search, case creation, HL7 processing)
- [ ] Logs reviewed for errors

## Post-Deployment Validation

### 1. Test HL7 Processing
Upload a test HL7 message and verify:
- Message parses correctly
- Lab result created
- Case(s) created/linked
- For multiplex results: separate lab results per disease

### 2. Test Case Definitions
- Create a test case definition
- Verify criteria builder works
- Test case matching logic

### 3. Monitor Logs
```powershell
# View latest application logs
Get-Content Sentinel\Logs\*.log -Tail 100 -Wait
```

## Support

For issues not covered in this guide:
1. Check `Sentinel/Migrations/README.md` for migration-specific details
2. Review application logs in `Sentinel/Logs/`
3. Check git commit history for recent changes
4. Contact development team

## Migration File Maintenance

### Creating New Migrations
```powershell
cd Sentinel
dotnet ef migrations add YourMigrationName --context ApplicationDbContext
```

### Removing Last Migration (if not applied)
```powershell
dotnet ef migrations remove --context ApplicationDbContext
```

### Generating SQL Script
```powershell
# Generate script from specific migration to latest
dotnet ef migrations script 20260724082854_AddMultiplexLabResultSupport --context ApplicationDbContext --idempotent --output migration-script.sql
```

### Checking Pending Migrations
```powershell
dotnet ef migrations list --context ApplicationDbContext
```

The `(Pending)` marker indicates migrations not yet applied to the database.
