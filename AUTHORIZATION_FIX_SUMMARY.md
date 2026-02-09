# Authorization Policy Fix - Disease Access Control

## Issue
The Disease Access Control pages were created with an incorrect authorization policy:
- **Used:** `Permission.Settings.ManageRoles` 
- **Error:** `The AuthorizationPolicy named: 'Permission.Settings.ManageRoles' was not found.`

## Root Cause
The application uses a custom permission-based authorization system with the pattern:
```
Permission.{Module}.{Action}
```

Where:
- **Module** must be a valid `PermissionModule` enum value
- **Action** must be a valid `PermissionAction` enum value

Available `PermissionAction` values:
- View
- Create
- Edit
- Delete
- Search
- Merge
- Export
- Import
- **ManagePermissions** ?
- ManageCustomFields
- ManageCustomLookups
- ManageSystemLookups
- ManageOrganization
- ResetPassword

`ManageRoles` is **not** a valid action.

## Solution Applied
Changed all Disease Access Control pages to use the correct authorization policy:
- **Now Using:** `Permission.Settings.ManagePermissions` ?

## Files Updated

### 1. Index.cshtml.cs
```csharp
[Authorize(Policy = "Permission.Settings.ManagePermissions")]
public class IndexModel : PageModel
```

### 2. ManageRoles.cshtml.cs
```csharp
[Authorize(Policy = "Permission.Settings.ManagePermissions")]
public class ManageRolesModel : PageModel
```

### 3. ManageUsers.cshtml.cs
```csharp
[Authorize(Policy = "Permission.Settings.ManagePermissions")]
public class ManageUsersModel : PageModel
```

### 4. ViewGrants.cshtml.cs
```csharp
[Authorize(Policy = "Permission.Settings.ManagePermissions")]
public class ViewGrantsModel : PageModel
```

### Documentation Updates
- Updated `DISEASE_ACCESS_PAGES_SUMMARY.md`
- Updated `DISEASE_ACCESS_QUICK_GUIDE.md`

## How to Test

1. **Stop and restart your application** (Hot reload may not pick up authorization policy changes)

2. **Ensure your test user has the permission:**
   - Navigate to Settings ? Roles ? [Your Role] ? Grant Permissions
   - Grant: **Permission.Settings.ManagePermissions**

3. **Access the Disease Access Control pages:**
   - Navigate to Settings ? Disease Access Control
   - Should load without the authorization error

## Understanding the Permission System

Your application uses a custom authorization system defined in:
- `Authorization/PermissionPolicyProvider.cs` - Dynamically creates policies for Permission.* patterns
- `Authorization/PermissionHandler.cs` - Checks if user has the required permission
- `Services/IPermissionService.cs` - Service that checks permissions in the database

The system checks:
1. Does the user have a `RolePermission` for this Module+Action?
2. Does the user have a `UserPermission` override for this Module+Action?

## Available Permission Policies

Based on your enums, these are valid authorization policies:

**Patient Module:**
- `Permission.Patient.View`
- `Permission.Patient.Create`
- `Permission.Patient.Edit`
- `Permission.Patient.Delete`
- `Permission.Patient.Search`
- `Permission.Patient.Merge`

**Case Module:**
- `Permission.Case.View`
- `Permission.Case.Create`
- `Permission.Case.Edit`
- `Permission.Case.Delete`
- `Permission.Case.Export`

**Settings Module:**
- `Permission.Settings.ManagePermissions` ? **Used for Disease Access Control**
- `Permission.Settings.ManageCustomFields`
- `Permission.Settings.ManageCustomLookups`
- `Permission.Settings.ManageSystemLookups`
- `Permission.Settings.ManageOrganization`

**User Module:**
- `Permission.User.View`
- `Permission.User.Create`
- `Permission.User.Edit`
- `Permission.User.ResetPassword`

**Audit Module:**
- `Permission.Audit.View`
- `Permission.Audit.Export`

**Report Module:**
- `Permission.Report.View`
- `Permission.Report.Export`

## Status: ? FIXED

Build successful. Application should now work correctly when restarted.
