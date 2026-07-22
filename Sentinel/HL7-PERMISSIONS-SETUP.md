# HL7 Test Generator - Permission Setup

## Changes Made

### 1. **Added New Permission Module & Actions**
**File:** `Models/Permission.cs`

Added to `PermissionModule` enum:
- `HL7` - New module for HL7-specific permissions

Added to `PermissionAction` enum:
- `Configure` - For HL7 configuration management
- `Process` - For HL7 message processing
- `GenerateTestFiles` - For test file generation

### 2. **Updated Authorization on Generate Test Files Page**
**File:** `Components/Pages/Settings/HL7/GenerateTestFiles.razor`

Changed from:
```csharp
@attribute [Authorize(Roles = "Administrator")]
```

To:
```csharp
@attribute [Authorize(Policy = "Permission.HL7.GenerateTestFiles")]
```

This uses the existing permission-based authorization system instead of role-based.

### 3. **Created Database Migration**
**Migration:** `20260625100551_AddHL7Permissions`

The migration was created and applied successfully to update the enum values in the database.

### 4. **Manual Seeding Script** (Optional)
**File:** `Scripts/SeedHL7Permissions.sql`

A SQL script was created that can manually seed the HL7 permissions if needed. However, this is **not required** because:

## Automatic Permission Seeding

The `PermissionSeedService.SeedAsync()` method runs on every application startup and:

1. **Automatically enumerates all enum combinations** - It loops through all values in `PermissionModule` × `PermissionAction` and creates permissions for each combination
2. **Creates missing permissions** - Any new permissions are automatically added to the database
3. **Assigns permissions to roles** - Admin and Surveillance Manager roles get assigned their respective permissions

### Default Role Assignments

Based on `PermissionSeedService.cs`:

**Admin Role** - Gets ALL permissions (including all HL7 permissions):
- `HL7.View`
- `HL7.Create`
- `HL7.Edit`
- `HL7.Delete`
- `HL7.Configure`
- `HL7.Process`
- `HL7.GenerateTestFiles`
- ... and all other HL7 actions

**Surveillance Manager Role** - Gets all permissions except audit deletion (includes HL7):
- All HL7 permissions (same as Admin)

**Other Roles** - Do not get HL7 permissions by default

## What Happens on Next App Start

When you next start the application:

1. ✅ `PermissionSeedService.SeedAsync()` runs
2. ✅ Detects new `HL7` module permissions
3. ✅ Creates ~20 new permission records (HL7 × all actions)
4. ✅ Assigns all HL7 permissions to Admin role
5. ✅ Assigns all HL7 permissions to Surveillance Manager role
6. ✅ Generate Test Files page is now accessible to users with those roles

## Testing the Permission

1. **Log in as Admin or Surveillance Manager**
2. **Navigate to:** Settings → HL7 Integration → Generate Test Files
3. **Expected Result:** Page loads successfully
4. **If using another role:** You'll get a 403 Forbidden or access denied message

## Granting Permission to Other Roles

To grant the `HL7.GenerateTestFiles` permission to other roles:

### Option 1: Via UI (if permission management UI exists)
1. Go to Settings → Roles → [Role Name] → Permissions
2. Find "HL7" section
3. Check "GenerateTestFiles"
4. Save

### Option 2: Via Database
```sql
-- Get the role ID
DECLARE @RoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'YourRoleName');

-- Get the permission ID
DECLARE @PermissionId INT = (SELECT Id FROM Permissions WHERE Module = 9 AND Action = 18);

-- Assign the permission
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted)
VALUES (@RoleId, @PermissionId, 1);
```

### Option 3: Update PermissionSeedService.cs
Add the role to the seeding logic in `SeedRolePermissionsAsync()` method.

## Permission Key Format

The permission system uses this format:
```
Permission.{Module}.{Action}
```

For the test file generator:
```
Permission.HL7.GenerateTestFiles
```

This integrates with the existing `PermissionPolicyProvider` which parses these policy names and creates authorization requirements.

## Verification

To verify permissions were seeded correctly:

```sql
-- Check if HL7 module permissions exist
SELECT * FROM Permissions WHERE Module = 9;

-- Check role assignments for HL7 permissions
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    rp.IsGranted
FROM RolePermissions rp
INNER JOIN AspNetRoles r ON rp.RoleId = r.Id
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE p.Module = 9
ORDER BY r.Name, p.Name;
```

## Summary

✅ **No manual SQL needed** - Permission seeding is automatic on app startup  
✅ **Admin role has access** - By default through automatic role assignment  
✅ **Surveillance Manager has access** - By default through automatic role assignment  
✅ **Permission-based authorization** - More granular than role-based  
✅ **Database migration applied** - Enum changes are persisted  
✅ **Build successful** - Everything compiles correctly  

---

**Next Steps:**
1. Restart the application to trigger automatic permission seeding
2. Log in as Admin or Surveillance Manager
3. Navigate to the Generate Test Files page
4. Start creating test HL7 messages!

*Last Updated: 2026-01-25*
