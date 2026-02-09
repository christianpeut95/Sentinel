# Granular Permissions System - Implementation Guide

## Overview

A comprehensive permissions system has been implemented to provide fine-grained access control for your surveillance application. This system allows you to define permissions at both the role and user level.

## Features

### Permission Modules
- **Patient**: Control access to patient records
- **Settings**: Control access to system configuration
- **Audit**: Control access to audit logs
- **User**: Control access to user management
- **Report**: Control access to reports and analytics

### Permission Actions
- **View**: Read-only access
- **Create**: Ability to create new records
- **Edit**: Ability to modify existing records
- **Delete**: Ability to delete records
- **Search**: Ability to search and filter records
- **Merge**: Ability to merge duplicate records
- **Export**: Ability to export data
- **Import**: Ability to import data
- **ManagePermissions**: Ability to manage permissions

## Setup Instructions

### 1. Create and Apply Migration

```bash
# Navigate to your project directory
cd Surveillance-MVP

# Create a new migration
dotnet ef migrations add AddPermissionsSystem

# Apply the migration to the database
dotnet ef database update
```

### 2. Seed Permissions

Add this code to your `Program.cs` after `app.Run();`:

```csharp
// Before app.Run(), add:
using (var scope = app.Services.CreateScope())
{
    await Surveillance_MVP.Extensions.PermissionSeeder.SeedPermissionsAsync(scope.ServiceProvider);
}
```

### 3. Using Permissions in Code

#### In Razor Pages (PageModel)

```csharp
using Microsoft.AspNetCore.Authorization;
using Surveillance_MVP.Models;
using Surveillance_MVP.Extensions;

// Require permission at class level
[Authorize(Policy = "Permission.Patient.View")]
public class IndexModel : PageModel
{
    // ...
}

// Or check permission programmatically
public class EditModel : PageModel
{
    private readonly IPermissionService _permissionService;
    
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!await _permissionService.HasPermissionAsync(userId, 
            PermissionModule.Patient, PermissionAction.Edit))
        {
            return Forbid();
        }
        
        // ... rest of your code
    }
}
```

#### In Razor Views

```razor
@inject IPermissionService PermissionService
@inject UserManager<ApplicationUser> UserManager

@{
    var userId = UserManager.GetUserId(User);
    var canEdit = await PermissionService.HasPermissionAsync(userId, 
        PermissionModule.Patient, PermissionAction.Edit);
}

@if (canEdit)
{
    <a asp-page="./Edit" asp-route-id="@Model.Patient.Id" class="btn btn-primary">Edit</a>
}
```

## Management Pages

### Role Permissions
Navigate to: **Settings > Roles > [Select Role] > Permissions**

Here you can grant or revoke permissions for entire roles. All users assigned to that role will inherit these permissions.

### User-Specific Permissions
Navigate to: **Settings > Users > [Select User] > Permissions**

Here you can grant additional permissions to specific users. These permissions override role-based permissions.

## Permission Hierarchy

1. **User-Specific Permissions** (highest priority)
   - Permissions directly assigned to a user
   - These override role permissions

2. **Role-Based Permissions**
   - Permissions assigned to roles
   - All users in the role inherit these permissions

## Default Permissions

The system seeds the following permissions:

### Patient Module
- Patient.View
- Patient.Create
- Patient.Edit
- Patient.Delete
- Patient.Search
- Patient.Merge
- Patient.Export
- Patient.Import

### Settings Module
- Settings.View
- Settings.Create
- Settings.Edit
- Settings.Delete
- Settings.Import

### Audit Module
- Audit.View
- Audit.Export

### User Module
- User.View
- User.Create
- User.Edit
- User.Delete
- User.ManagePermissions

### Report Module
- Report.View
- Report.Export

## Applying Permissions to Existing Pages

### Example: Protecting Patient Index Page

**File**: `Surveillance-MVP\Pages\Patients\Index.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class IndexModel : PageModel
    {
        // ... rest of your code
    }
}
```

### Example: Protecting Patient Create Page

```csharp
[Authorize(Policy = "Permission.Patient.Create")]
public class CreateModel : PageModel
{
    // ... rest of your code
}
```

### Example: Protecting Patient Edit Page

```csharp
[Authorize(Policy = "Permission.Patient.Edit")]
public class EditModel : PageModel
{
    // ... rest of your code
}
```

## Best Practices

1. **Assign permissions to roles, not individual users** whenever possible
2. **Use user-specific permissions** only for exceptions
3. **Test permission changes** in a development environment first
4. **Document custom permissions** if you add new modules or actions
5. **Regular audit** of user permissions to ensure appropriate access

## Troubleshooting

### Permission denied errors
- Verify the user has the correct role assigned
- Check that the role has the necessary permission
- Ensure migrations have been applied
- Confirm permissions were seeded

### Permissions not working
- Clear browser cache and cookies
- Re-login to refresh claims
- Verify the policy name matches the format: `Permission.Module.Action`

## Adding Custom Permissions

To add new permissions:

1. Update `PermissionModule` or `PermissionAction` enum in `Models/Permission.cs`
2. Add to the seeder in `Extensions/PermissionSeeder.cs`
3. Create and apply a new migration
4. Apply the permission to your pages using `[Authorize(Policy = "Permission.Module.Action")]`

## API Reference

### IPermissionService Methods

```csharp
// Check if user has permission
Task<bool> HasPermissionAsync(string userId, PermissionModule module, PermissionAction action)
Task<bool> HasPermissionAsync(string userId, string permissionKey)

// Get permissions
Task<List<Permission>> GetUserPermissionsAsync(string userId)
Task<List<Permission>> GetRolePermissionsAsync(string roleId)
Task<List<Permission>> GetAllPermissionsAsync()

// Manage user permissions
Task GrantPermissionToUserAsync(string userId, int permissionId)
Task RevokePermissionFromUserAsync(string userId, int permissionId)

// Manage role permissions
Task GrantPermissionToRoleAsync(string roleId, int permissionId)
Task RevokePermissionFromRoleAsync(string roleId, int permissionId)

// Get permission matrix
Task<Dictionary<PermissionModule, List<PermissionAction>>> GetUserPermissionMatrixAsync(string userId)
```
