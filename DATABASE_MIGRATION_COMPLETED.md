# Database Migration Applied - Disease Access Control

## ? Migration Successfully Completed

**Date:** February 3, 2026  
**Migration:** AddDiseaseAccessControl  
**Status:** Applied Successfully

## Changes Applied to Database

### 1. **Diseases Table**
- ? Added `AccessLevel` column (int, default: 0)
  - 0 = Public (default)
  - 1 = Restricted

### 2. **RoleDiseaseAccess Table** (NEW)
Created table with following structure:
```sql
CREATE TABLE [RoleDiseaseAccess] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RoleId] NVARCHAR(450) NOT NULL,
    [DiseaseId] UNIQUEIDENTIFIER NOT NULL,
    [IsAllowed] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] NVARCHAR(450) NULL,
    CONSTRAINT FK_RoleDiseaseAccess_AspNetRoles FOREIGN KEY ([RoleId]) 
        REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_RoleDiseaseAccess_Diseases FOREIGN KEY ([DiseaseId]) 
        REFERENCES [Diseases] ([Id]) ON DELETE CASCADE,
    CONSTRAINT FK_RoleDiseaseAccess_AspNetUsers FOREIGN KEY ([CreatedByUserId]) 
        REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
```

**Indexes Created:**
- ? `IX_RoleDiseaseAccess_RoleId_DiseaseId` (UNIQUE)
- ? `IX_RoleDiseaseAccess_DiseaseId`
- ? `IX_RoleDiseaseAccess_CreatedByUserId`

### 3. **UserDiseaseAccess Table** (NEW)
Created table with following structure:
```sql
CREATE TABLE [UserDiseaseAccess] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [DiseaseId] UNIQUEIDENTIFIER NOT NULL,
    [IsAllowed] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [ExpiresAt] DATETIME2(7) NULL,
    [GrantedByUserId] NVARCHAR(450) NULL,
    [Reason] NVARCHAR(500) NULL,
    CONSTRAINT FK_UserDiseaseAccess_AspNetUsers_UserId FOREIGN KEY ([UserId]) 
        REFERENCES [AspNetUsers] ([Id]) ON DELETE RESTRICT,
    CONSTRAINT FK_UserDiseaseAccess_Diseases FOREIGN KEY ([DiseaseId]) 
        REFERENCES [Diseases] ([Id]) ON DELETE RESTRICT,
    CONSTRAINT FK_UserDiseaseAccess_AspNetUsers_GrantedBy FOREIGN KEY ([GrantedByUserId]) 
        REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
```

**Indexes Created:**
- ? `IX_UserDiseaseAccess_UserId_DiseaseId` (UNIQUE)
- ? `IX_UserDiseaseAccess_DiseaseId`
- ? `IX_UserDiseaseAccess_ExpiresAt`
- ? `IX_UserDiseaseAccess_GrantedByUserId`

## Configuration Changes

### Fixed Cascade Delete Conflicts
Changed delete behavior to avoid SQL Server cascade path conflicts:
- `UserDiseaseAccess.UserId` ? `ON DELETE RESTRICT` (was CASCADE)
- `UserDiseaseAccess.DiseaseId` ? `ON DELETE RESTRICT` (was CASCADE)

This prevents multiple cascade paths through the AspNetUsers table.

## Verification

? **Build Status:** Successful  
? **Migration Applied:** 20260203102408_AddDiseaseAccessControl  
? **Database:** aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d  
? **All Tables Created:** RoleDiseaseAccess, UserDiseaseAccess  
? **All Indexes Created:** 7 total indexes  
? **Foreign Keys:** All constraints applied successfully

## Next Steps - Quick Start Guide

### 1. Mark Sensitive Diseases as Restricted
```sql
-- Example: Mark HIV as restricted
UPDATE Diseases 
SET AccessLevel = 1 
WHERE Code = 'HIV';

-- Mark multiple diseases
UPDATE Diseases 
SET AccessLevel = 1 
WHERE Code IN ('HIV', 'HEP_B', 'HEP_C', 'SYPHILIS', 'GONORRHEA');
```

### 2. Grant Role Access
```sql
-- Grant HIV Specialist role access to HIV
DECLARE @RoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'HIV Specialist');
DECLARE @HIVId UNIQUEIDENTIFIER = (SELECT Id FROM Diseases WHERE Code = 'HIV');

INSERT INTO RoleDiseaseAccess (RoleId, DiseaseId, IsAllowed, CreatedAt)
VALUES (@RoleId, @HIVId, 1, GETUTCDATE());
```

### 3. Grant Temporary User Access (Outbreak Worker)
```sql
-- Grant 3-month access to outbreak worker
DECLARE @UserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'worker@example.com');
DECLARE @DiseaseId UNIQUEIDENTIFIER = (SELECT Id FROM Diseases WHERE Name = 'Cholera');

INSERT INTO UserDiseaseAccess (UserId, DiseaseId, IsAllowed, CreatedAt, ExpiresAt, Reason)
VALUES (
    @UserId, 
    @DiseaseId, 
    1, 
    GETUTCDATE(), 
    DATEADD(MONTH, 3, GETUTCDATE()),
    'Cholera outbreak response - Q2 2025'
);
```

### 4. Test the System

#### Test Case 1: Public Disease (Should Work)
1. Try to view any case with a public disease (default)
2. All users with Case.View permission should see it

#### Test Case 2: Restricted Disease Without Access (Should Block)
1. Mark a disease as restricted: `UPDATE Diseases SET AccessLevel = 1 WHERE Code = 'HIV'`
2. Try to view a case with that disease
3. Should return 403 Forbidden if user doesn't have access

#### Test Case 3: Restricted Disease With Role Access (Should Work)
1. Grant role access: Insert into `RoleDiseaseAccess`
2. User in that role should now see the case

#### Test Case 4: Temporary User Access (Should Expire)
1. Grant user access with expiration date
2. User should see case before expiration
3. After expiration, access should be denied

### 5. Query Examples

#### Check What Diseases a User Can Access
```sql
DECLARE @UserId NVARCHAR(450) = 'user-id-here';

-- Public diseases
SELECT Id, Name, 'Public' AS AccessType
FROM Diseases
WHERE AccessLevel = 0

UNION

-- User-specific grants (not expired)
SELECT d.Id, d.Name, 'User Grant' AS AccessType
FROM UserDiseaseAccess uda
JOIN Diseases d ON uda.DiseaseId = d.Id
WHERE uda.UserId = @UserId 
  AND uda.IsAllowed = 1
  AND (uda.ExpiresAt IS NULL OR uda.ExpiresAt > GETUTCDATE())

UNION

-- Role-based grants
SELECT d.Id, d.Name, 'Role: ' + r.Name AS AccessType
FROM RoleDiseaseAccess rda
JOIN Diseases d ON rda.DiseaseId = d.Id
JOIN AspNetRoles r ON rda.RoleId = r.Id
JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId 
  AND rda.IsAllowed = 1;
```

#### View All Access Grants
```sql
-- Role grants
SELECT 
    r.Name AS RoleName,
    d.Name AS DiseaseName,
    rda.CreatedAt,
    u.Email AS GrantedBy
FROM RoleDiseaseAccess rda
JOIN AspNetRoles r ON rda.RoleId = r.Id
JOIN Diseases d ON rda.DiseaseId = d.Id
LEFT JOIN AspNetUsers u ON rda.CreatedByUserId = u.Id
ORDER BY d.Name;

-- User grants
SELECT 
    u.Email AS UserEmail,
    d.Name AS DiseaseName,
    uda.ExpiresAt,
    uda.Reason,
    g.Email AS GrantedBy
FROM UserDiseaseAccess uda
JOIN AspNetUsers u ON uda.UserId = u.Id
JOIN Diseases d ON uda.DiseaseId = d.Id
LEFT JOIN AspNetUsers g ON uda.GrantedByUserId = g.Id
ORDER BY uda.ExpiresAt;
```

#### Clean Up Expired Access
```sql
-- View expired access
SELECT 
    u.Email,
    d.Name AS Disease,
    uda.ExpiresAt,
    uda.Reason
FROM UserDiseaseAccess uda
JOIN AspNetUsers u ON uda.UserId = u.Id
JOIN Diseases d ON uda.DiseaseId = d.Id
WHERE uda.ExpiresAt IS NOT NULL 
  AND uda.ExpiresAt <= GETUTCDATE();

-- Delete expired access
DELETE FROM UserDiseaseAccess
WHERE ExpiresAt IS NOT NULL 
  AND ExpiresAt <= GETUTCDATE();
```

## Rollback (If Needed)

If you need to rollback this migration:

```powershell
cd Surveillance-MVP
dotnet ef database update <previous-migration-name>
```

Or remove the migration entirely:
```powershell
cd Surveillance-MVP
dotnet ef migrations remove
```

## Support & Documentation

For more information, see:
- `DISEASE_ACCESS_CONTROL.md` - Complete architecture guide
- `DISEASE_ACCESS_QUICK_REFERENCE.md` - Quick reference
- `DISEASE_ACCESS_IMPLEMENTATION_SUMMARY.md` - Implementation details

## Summary

? **Database updated successfully**  
? **3 new database objects created** (1 column, 2 tables)  
? **7 indexes created** for optimal performance  
? **All foreign key constraints applied**  
? **System ready for testing**  
? **Zero breaking changes** to existing functionality

**The Disease Access Control system is now live and ready to use!** ??

All existing diseases default to Public (AccessLevel = 0), so no immediate action is required. You can start marking diseases as restricted and granting access as needed.
