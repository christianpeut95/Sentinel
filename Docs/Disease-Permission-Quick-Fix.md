# Disease Permission - Quick Fix

## The Problem

You can't access Disease management pages because the `Settings.ManageSystemLookups` permission hasn't been granted to your user or role.

## Quick Fix (Choose One)

### ? Option 1: Grant Permission via UI (Recommended)

1. Log in as an administrator
2. Go to **Settings ? Roles**
3. Click on your role (e.g., "Administrator")
4. Click **"Manage Permissions"**
5. Scroll to the **Settings** section
6. Check: **? ManageSystemLookups**
7. Click **Save**
8. Log out and log back in

### ? Option 2: Quick SQL Fix

If you can't access the UI, run this SQL directly:

```sql
-- Check if permission exists
SELECT * FROM Permissions 
WHERE Module = 2 AND Action = 11;  -- Settings.ManageSystemLookups

-- Grant to all roles (temporarily for admin access)
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM AspNetRoles r
CROSS JOIN Permissions p
WHERE p.Module = 2 AND p.Action = 11
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
);

-- Or grant directly to your user
INSERT INTO UserPermissions (UserId, PermissionId, IsAllowed)
SELECT 'YOUR-USER-ID-GUID', p.Id, 1
FROM Permissions p
WHERE p.Module = 2 AND p.Action = 11
AND NOT EXISTS (
    SELECT 1 FROM UserPermissions up 
    WHERE up.UserId = 'YOUR-USER-ID-GUID' AND up.PermissionId = p.Id
);
```

Replace `YOUR-USER-ID-GUID` with your actual user ID from the `AspNetUsers` table.

## Verify It's Working

1. Navigate to **Settings**
2. Look for **Diseases** in the "Lookup Tables" section
3. Click on **Diseases**
4. You should see the Disease management page

## What This Permission Controls

The `Settings.ManageSystemLookups` permission allows you to manage:
- Countries, Languages, Ethnicities
- ATSI Statuses, Sex at Birth, Genders  
- Occupations, Case Statuses
- **Diseases** ? New!

## Still Not Working?

1. **Clear browser cache** and reload
2. **Log out and log back in** to refresh permissions
3. **Restart the application** to ensure permissions are seeded
4. Check the database to verify permissions exist:

```sql
-- View all permissions
SELECT * FROM Permissions ORDER BY Module, Action;

-- View your user's permissions
SELECT p.Name, up.IsAllowed
FROM UserPermissions up
JOIN Permissions p ON up.PermissionId = p.Id
WHERE up.UserId = 'YOUR-USER-ID-GUID';

-- View role permissions
SELECT r.Name, p.Name as Permission
FROM RolePermissions rp
JOIN AspNetRoles r ON rp.RoleId = r.Id
JOIN Permissions p ON rp.PermissionId = p.Id
ORDER BY r.Name, p.Name;
```

## Need Help?

See `Docs/Disease-Permission-Setup.md` for detailed setup instructions.
