# New Permissions Added - Settings & Users

## Summary

Added 5 new granular permissions for better control over settings and user management features.

## New Permissions

### Settings Module (4 new permissions)

#### 1. Settings.ManageCustomFields
- **Action**: `ManageCustomFields`
- **Description**: View, create, and edit custom fields
- **Use Cases**: 
  - Access to `/Settings/CustomFields/*` pages
  - Create/edit/delete custom field definitions
  - Manage custom field visibility

**Apply to pages:**
```csharp
[Authorize(Policy = "Permission.Settings.ManageCustomFields")]
public class CustomFieldsIndexModel : PageModel { }
```

#### 2. Settings.ManageCustomLookups
- **Action**: `ManageCustomLookups`
- **Description**: View, create, and edit custom lookup tables
- **Use Cases**:
  - Access to `/Settings/LookupTables/*` pages
  - Create/edit/delete user-defined lookup tables
  - Manage lookup values

**Apply to pages:**
```csharp
[Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
public class LookupTablesIndexModel : PageModel { }
```

#### 3. Settings.ManageSystemLookups
- **Action**: `ManageSystemLookups`
- **Description**: View, create, and edit system lookup tables (countries, languages, etc.)
- **Use Cases**:
  - Access to system lookup pages:
    - `/Settings/Countries/*`
    - `/Settings/Languages/*`
    - `/Settings/Ethnicities/*`
    - `/Settings/Genders/*`
    - `/Settings/SexAtBirths/*`
    - `/Settings/AtsiStatuses/*`
    - `/Settings/CaseStatuses/*`
    - `/Settings/Occupations/*`

**Apply to pages:**
```csharp
[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class CountriesIndexModel : PageModel { }

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class LanguagesIndexModel : PageModel { }
```

#### 4. Settings.ManageOrganization
- **Action**: `ManageOrganization`
- **Description**: Change organization and system settings
- **Use Cases**:
  - Access to `/Settings/Organization` page
  - Edit organization name, country, locale
  - Configure system-wide settings
  - Google Maps API settings

**Apply to pages:**
```csharp
[Authorize(Policy = "Permission.Settings.ManageOrganization")]
public class OrganizationModel : PageModel { }

[Authorize(Policy = "Permission.Settings.ManageOrganization")]
public class GoogleMapsModel : PageModel { }
```

### User Module (1 new permission)

#### 5. User.ResetPassword
- **Action**: `ResetPassword`
- **Description**: Reset user passwords
- **Use Cases**:
  - Reset passwords for other users
  - Force password change functionality
  - Help users who forgot passwords

**Apply to pages/methods:**
```csharp
[Authorize(Policy = "Permission.User.ResetPassword")]
public async Task<IActionResult> OnPostResetPasswordAsync(string userId)
{
    // Reset password logic
}
```

## Migration Required

After making these changes, you need to:

### 1. Delete Existing Permissions (Development Only)
```sql
DELETE FROM UserPermissions;
DELETE FROM RolePermissions;
DELETE FROM Permissions;
```

### 2. Restart Your Application
The `PermissionSeeder` will automatically seed all 27 permissions (22 existing + 5 new).

### 3. Or Create a New Migration
If you prefer to keep existing permission assignments:

```bash
dotnet ef migrations add AddNewSettingsAndUserPermissions
dotnet ef database update
```

Then manually insert the new permissions:
```sql
INSERT INTO Permissions (Module, Action, Name, Description) VALUES
(1, 10, 'Settings.ManageCustomFields', 'View, create, and edit custom fields'),
(1, 11, 'Settings.ManageCustomLookups', 'View, create, and edit custom lookup tables'),
(1, 12, 'Settings.ManageSystemLookups', 'View, create, and edit system lookup tables (countries, languages, etc.)'),
(1, 13, 'Settings.ManageOrganization', 'Change organization and system settings'),
(3, 14, 'User.ResetPassword', 'Reset user passwords');
```

## Using in Views

```razor
@inject Surveillance_MVP.Helpers.PermissionHelper PermissionHelper

@if (await PermissionHelper.CanManageCustomFields())
{
    <a asp-page="/Settings/CustomFields/Index" class="btn btn-primary">Manage Custom Fields</a>
}

@if (await PermissionHelper.CanManageSystemLookups())
{
    <a asp-page="/Settings/Countries/Index" class="btn btn-primary">Manage Countries</a>
}

@if (await PermissionHelper.CanManageOrganization())
{
    <a asp-page="/Settings/Organization" class="btn btn-primary">Organization Settings</a>
}

@if (await PermissionHelper.CanResetPasswords())
{
    <button onclick="resetPassword('@user.Id')" class="btn btn-warning">Reset Password</button>
}
```

## Recommended Role Setup

### Administrator Role
- ? All permissions including the new ones

### Settings Manager Role
- ? Settings.ManageCustomFields
- ? Settings.ManageCustomLookups
- ? Settings.ManageSystemLookups
- ? Settings.ManageOrganization
- ? No patient permissions
- ? No user management permissions

### Data Manager Role
- ? Settings.ManageSystemLookups (for maintaining reference data)
- ? Settings.ManageCustomFields (restricted)
- ? Settings.ManageCustomLookups (restricted)
- ? Settings.ManageOrganization (restricted)

### User Manager Role
- ? User.View
- ? User.Create
- ? User.Edit
- ? User.Delete
- ? User.ResetPassword
- ? User.ManagePermissions

### Help Desk Role
- ? User.View
- ? User.ResetPassword
- ? User.Create (restricted)
- ? User.Delete (restricted)
- ? User.ManagePermissions (restricted)

## Pages to Protect

### Custom Fields Pages
```csharp
// Surveillance-MVP\Pages\Settings\CustomFields\Index.cshtml.cs
[Authorize(Policy = "Permission.Settings.ManageCustomFields")]

// All CustomFields pages: Create, Edit, Delete, Visibility
```

### Custom Lookup Tables Pages
```csharp
// Surveillance-MVP\Pages\Settings\LookupTables\Index.cshtml.cs
[Authorize(Policy = "Permission.Settings.ManageCustomLookups")]

// All LookupTables pages: Create, Edit, Delete, Details, ManageValues
```

### System Lookup Pages
```csharp
// Apply to all these page folders:
// - Settings/Countries/*
// - Settings/Languages/*
// - Settings/Ethnicities/*
// - Settings/Genders/*
// - Settings/SexAtBirths/*
// - Settings/AtsiStatuses/*
// - Settings/CaseStatuses/*
// - Settings/Occupations/*

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
```

### Organization Settings Pages
```csharp
// Surveillance-MVP\Pages\Settings\Organization.cshtml.cs
[Authorize(Policy = "Permission.Settings.ManageOrganization")]

// Surveillance-MVP\Pages\Settings\GoogleMaps.cshtml.cs
[Authorize(Policy = "Permission.Settings.ManageOrganization")]
```

## Total Permissions Now

- **Patient**: 8 permissions
- **Settings**: 9 permissions (5 original + 4 new)
- **Audit**: 2 permissions
- **User**: 6 permissions (5 original + 1 new)
- **Report**: 2 permissions

**Total: 27 permissions**

## Next Steps

1. ? Run migration or clear and reseed permissions
2. ? Assign new permissions to appropriate roles
3. ? Apply `[Authorize]` attributes to the settings pages
4. ? Update navigation menus to hide/show based on permissions
5. ? Test with different role configurations
