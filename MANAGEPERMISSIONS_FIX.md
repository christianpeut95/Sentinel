# Settings.ManagePermissions - Missing Permission Fix

## Problem
The Disease Access Control pages require `Permission.Settings.ManagePermissions`, but this permission didn't exist in the permission seed data.

## Solution Applied

### 1. Updated Permission Seeder ?
**File:** `Surveillance-MVP\Extensions\PermissionSeeder.cs`

Added the missing permission:
```csharp
new Permission
{
    Module = PermissionModule.Settings,
    Action = PermissionAction.ManagePermissions,
    Name = "Settings.ManagePermissions",
    Description = "Manage roles, permissions, and disease access control"
}
```

### 2. SQL Scripts Created ?

Two SQL files have been created to add this permission to your database:

#### Option 1: Full Featured Script
**File:** `Add_Settings_ManagePermissions.sql`
- Adds the permission
- Includes 3 options for granting to roles
- Includes verification queries
- Includes helper queries to view roles and permissions

#### Option 2: Quick Script
**File:** `Quick_Add_ManagePermissions.sql`
- Simplified version for quick execution
- Just add permission and grant to your role

## How to Execute

### Step 1: Add the Permission to Database

Run either script against your database. The simplest approach:

1. **Open SQL Server Management Studio** or your SQL tool
2. **Connect to your database**
3. **Copy and run this:**

```sql
-- Add the permission
INSERT INTO Permissions (Module, Action, Name, Description)
VALUES (2, 8, 'Settings.ManagePermissions', 'Manage roles, permissions, and disease access control');
```

### Step 2: Grant to Your Role

#### Method A: By Role Name (Easiest)

```sql
-- Replace 'Administrator' with your role name
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
SELECT 
    r.Id,
    p.Id,
    GETUTCDATE()
FROM AspNetRoles r
CROSS JOIN Permissions p
WHERE r.Name = 'Administrator'  -- CHANGE THIS
  AND p.Name = 'Settings.ManagePermissions';
```

#### Method B: By Role ID

```sql
-- First, find your role ID
SELECT Id, Name FROM AspNetRoles;

-- Then grant (replace 'your-role-id-here' with actual ID)
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
VALUES (
    'your-role-id-here',
    (SELECT Id FROM Permissions WHERE Name = 'Settings.ManagePermissions'),
    GETUTCDATE()
);
```

### Step 3: Verify

```sql
-- Check if permission exists
SELECT * FROM Permissions WHERE Name = 'Settings.ManagePermissions';

-- Check if it's granted to your role
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName
FROM RolePermissions rp
JOIN AspNetRoles r ON rp.RoleId = r.Id
JOIN Permissions p ON rp.PermissionId = p.Id
WHERE p.Name = 'Settings.ManagePermissions';
```

### Step 4: Refresh Your Session

1. **Log out** from the application
2. **Log back in**
3. Navigate to Settings ? Disease Access Control
4. Should work now! ?

## Future Runs

The updated `PermissionSeeder.cs` will automatically add this permission when:
- The application starts up
- New databases are created
- Migrations are run

Existing databases need the manual SQL insert (one time only).

## What This Permission Controls

`Settings.ManagePermissions` grants access to:
- ? Disease Access Control (all pages)
  - `/Settings/DiseaseAccess/Index`
  - `/Settings/DiseaseAccess/ManageRoles`
  - `/Settings/DiseaseAccess/ManageUsers`
  - `/Settings/DiseaseAccess/ViewGrants`
- ? Role permission management pages
- ? User permission management pages

## Troubleshooting

### Permission not working after SQL insert?
- Clear browser cookies
- Restart the application
- Check user is in the correct role: `SELECT * FROM AspNetUserRoles WHERE UserId = 'your-user-id'`

### Don't know your role name?
```sql
SELECT r.Name, COUNT(ur.UserId) AS UserCount
FROM AspNetRoles r
LEFT JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
GROUP BY r.Id, r.Name
ORDER BY r.Name;
```

### Don't know your user ID?
```sql
SELECT Id, UserName, Email 
FROM AspNetUsers 
WHERE Email = 'your-email@example.com';
```

### Check what roles you're in:
```sql
SELECT r.Name AS RoleName
FROM AspNetUserRoles ur
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE ur.UserId = 'your-user-id';
```

## Permission Details

| Property | Value |
|----------|-------|
| **Name** | Settings.ManagePermissions |
| **Module** | Settings (enum value: 2) |
| **Action** | ManagePermissions (enum value: 8) |
| **Description** | Manage roles, permissions, and disease access control |
| **Pattern** | Permission.Settings.ManagePermissions |

## Related Permissions

Other Settings permissions that exist:
- `Settings.View` - View settings
- `Settings.Edit` - Edit settings
- `Settings.Create` - Create lookup tables
- `Settings.Delete` - Delete lookup tables
- `Settings.Import` - Import lookup data
- `Settings.ManageCustomFields` - Manage custom fields
- `Settings.ManageCustomLookups` - Manage custom lookup tables
- `Settings.ManageSystemLookups` - Manage system lookups
- `Settings.ManageOrganization` - Manage organization settings
- `Settings.ManagePermissions` - **? This one (NEW)** Manage roles & permissions

## Status

? **Seeder Updated** - Will auto-add in future
? **SQL Scripts Created** - Ready to run
? **Build Successful** - Code compiles
? **Database Update Required** - Run SQL script once
? **Session Refresh Required** - Log out/in after SQL
