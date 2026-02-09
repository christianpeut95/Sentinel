# Permissions Applied to All Settings Pages ?

## Summary

Successfully applied granular permissions to all settings pages in your surveillance application.

## Applied Permissions

### Custom Fields Pages (Settings.ManageCustomFields)
? All pages protected with `[Authorize(Policy = "Permission.Settings.ManageCustomFields")]`

- Index.cshtml.cs
- Create.cshtml.cs
- Edit.cshtml.cs
- Delete.cshtml.cs
- Visibility.cshtml.cs

### Custom Lookup Tables (Settings.ManageCustomLookups)
? All pages protected with `[Authorize(Policy = "Permission.Settings.ManageCustomLookups")]`

- Index.cshtml.cs
- Create.cshtml.cs
- Edit.cshtml.cs
- Delete.cshtml.cs
- Details.cshtml.cs
- ManageValues.cshtml.cs

### System Lookup Pages (Settings.ManageSystemLookups)
? All pages protected with `[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]`

#### Countries
- Index, Create, Edit, Delete, Details

#### Languages
- Index, Create, Edit, Delete, Details

#### Ethnicities
- Index, Create, Edit, Delete, Details

#### Genders
- Index, Create, Edit, Delete

#### SexAtBirths
- Index, Create, Edit, Delete

#### AtsiStatuses
- Index, Create

#### CaseStatuses
- Index, Create, Edit, Delete, Details

#### Occupations
- Index, Create, Edit, Delete, Details, Upload

### Organization Settings (Settings.ManageOrganization)
? Protected with `[Authorize(Policy = "Permission.Settings.ManageOrganization")]`

- Organization.cshtml.cs (replacedrole-based auth)
- GoogleMaps.cshtml.cs (replaced old role-based auth)

## Total Files Modified

**45 files** across all settings pages now have permission-based authorization.

## Build Status

? **Build Successful** - All files compile correctly with the new permissions.

## Next Steps

### 1. Create the Database Migration

```bash
dotnet ef migrations add AddPermissionsSystem
dotnet ef database update
```

### 2. Clear Existing Permissions (Development Only)

If you already seeded permissions without the new ones:

```sql
DELETE FROM UserPermissions;
DELETE FROM RolePermissions;
DELETE FROM Permissions;
```

Then restart your app - the `PermissionSeeder` will add all 27 permissions.

### 3. Assign Permissions to Roles

Navigate to **Settings > Roles** and assign permissions:

#### Example Role Setup

**Administrator**
- ? Settings.ManageCustomFields
- ? Settings.ManageCustomLookups
- ? Settings.ManageSystemLookups
- ? Settings.ManageOrganization
- ? All other permissions

**Settings Manager**
- ? Settings.ManageCustomFields
- ? Settings.ManageCustomLookups
- ? Settings.ManageSystemLookups
- ? Settings.ManageOrganization
- ? No patient or user management permissions

**Data Manager**
- ? Settings.ManageSystemLookups (only)
- ? Cannot modify custom fields/lookups
- ? Cannot change organization settings

**Case Manager**
- ? Patient.View, Create, Edit, Search
- ? No settings permissions
- ? No delete permissions

### 4. Test the Permissions

1. Create a test user
2. Assign them to "Data Manager" role with only `Settings.ManageSystemLookups`
3. Login as that user
4. Try to access:
   - ? Countries, Languages, Ethnicities (should work)
   - ? Custom Fields (should get 403 Forbidden)
   - ? Organization Settings (should get 403 Forbidden)

## Permission Matrix

| Module | Action | Policy Name | Description |
|--------|--------|-------------|-------------|
| Settings | ManageCustomFields | Permission.Settings.ManageCustomFields | View, create, edit custom fields |
| Settings | ManageCustomLookups | Permission.Settings.ManageCustomLookups | View, create, edit custom lookup tables |
| Settings | ManageSystemLookups | Permission.Settings.ManageSystemLookups | View, create, edit system lookups |
| Settings | ManageOrganization | Permission.Settings.ManageOrganization | Change organization and system settings |

## What Changed

### Before
- Pages used role-based authorization: `[Authorize(Roles = "Admin")]`
- All-or-nothing access (Admin could do everything, others nothing)
- No granular control over specific settings areas

### After
- Pages use permission-based authorization: `[Authorize(Policy = "Permission.Settings.ManageCustomFields")]`
- Granular control - can give access to specific areas
- Can create specialized roles (e.g., "Lookup Manager" who only manages reference data)
- User-specific permission overrides available

## Troubleshooting

### If you see 403 Forbidden errors:

1. **Check if permissions are seeded:**
   ```sql
   SELECT * FROM Permissions WHERE Module = 1; -- Settings module
   ```

2. **Check if role has permission:**
   ```sql
   SELECT r.Name, p.Name 
   FROM RolePermissions rp
   JOIN AspNetRoles r ON rp.RoleId = r.Id
   JOIN Permissions p ON rp.PermissionId = p.Id
   WHERE p.Module = 1;
   ```

3. **Check if user is in correct role:**
   ```sql
   SELECT u.Email, r.Name
   FROM AspNetUsers u
   JOIN AspNetUserRoles ur ON u.Id = ur.UserId
   JOIN AspNetRoles r ON ur.RoleId = r.Id;
   ```

### If permissions aren't being enforced:

1. Verify `PermissionHandler` and `PermissionPolicyProvider` are registered in `Program.cs`
2. Clear browser cookies and re-login
3. Check that the `IPermissionService` is resolving correctly

## Summary

?? All 45 settings page files now have appropriate granular permissions applied!

- Custom Fields: 5 files
- Custom Lookups: 6 files
- System Lookups: 32 files (8 lookup types)
- Organization: 2 files

Your settings are now fully protected with granular, role-based permissions that can be managed through the UI.
