# Disease Management - Permission Setup

## Issue

Disease management pages require the `Settings.ManageSystemLookups` permission, but this permission hasn't been granted to your user or role yet.

## Solution

Grant the `Settings.ManageSystemLookups` permission to your user or role:

### Option 1: Grant to a Role (Recommended)

1. Navigate to **Settings ? Roles**
2. Select your role (e.g., "Administrator")
3. Click **"Manage Permissions"**
4. In the **Settings** section, check:
   - ? **ManageSystemLookups** - "View, create, and edit system lookup tables (countries, languages, etc.)"
5. Click **Save**

### Option 2: Grant to a Specific User

1. Navigate to **Settings ? Users**
2. Click **Details** on your user
3. Click **"Manage User Permissions"**
4. In the **Settings** section, check:
   - ? **ManageSystemLookups**
5. Click **Save**

## What This Permission Controls

The `Settings.ManageSystemLookups` permission grants access to:

- ? **Countries** (view, create, edit, delete)
- ? **Languages** (view, create, edit, delete)
- ? **Ethnicities** (view, create, edit, delete)
- ? **ATSI Statuses** (view, create, edit, delete)
- ? **Sex at Birth** (view, create, edit, delete)
- ? **Genders** (view, create, edit, delete)
- ? **Occupations** (view, create, edit, delete, upload)
- ? **Case Statuses** (view, create, edit, delete)
- ? **Diseases** (view, create, edit, delete) ? **NEW!**

## Alternative: Create a New "Disease.Manage" Permission

If you want separate permissions for Disease management:

### 1. Add Disease-specific permissions to PermissionSeeder

Add these to `Extensions/PermissionSeeder.cs`:

```csharp
// Disease Permissions
new Permission
{
    Module = PermissionModule.Settings,
    Action = PermissionAction.ManageDiseases,
    Name = "Settings.ManageDiseases",
    Description = "View, create, and edit disease hierarchy"
},
```

### 2. Add the new PermissionAction

Add to `Models/Permission.cs`:

```csharp
public enum PermissionAction
{
    // ... existing actions
    ManageDiseases  // Add this
}
```

### 3. Update Disease pages

Change from:
```csharp
[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
```

To:
```csharp
[Authorize(Policy = "Permission.Settings.ManageDiseases")]
```

### 4. Restart the application

The new permission will be seeded automatically on startup.

## Recommended Approach

**Use Option 1 (grant ManageSystemLookups permission)** because:

- ? Diseases are system lookups like Countries, Languages, etc.
- ? Consistent with existing permission structure
- ? No code changes needed
- ? Works immediately

Only create separate permissions if you need to grant Disease management to users who shouldn't manage other system lookups.

## Verification

After granting the permission:

1. Log out and log back in (or wait for permission cache to refresh)
2. Navigate to **Settings ? Diseases**
3. You should now see the Disease management page
4. You can create, edit, and delete diseases

## Quick Check

**Can't see Diseases link in Settings?**
- Make sure you have `Settings.View` permission to see the Settings page

**Can see Diseases link but get "Access Denied"?**
- You need the `Settings.ManageSystemLookups` permission

**Just granted permission but still getting "Access Denied"?**
- Log out and log back in to refresh your permission cache
