# Permissions System - Quick Start

## Step 1: Create and Apply Database Migration

Run these commands in your terminal:

```bash
cd Surveillance-MVP
dotnet ef migrations add AddPermissionsSystem
dotnet ef database update
```

## Step 2: Permissions Are Auto-Seeded

The permissions are automatically seeded when the application starts (already configured in `Program.cs`).

## Step 3: Create Roles and Assign Permissions

1. Navigate to **Settings > Roles**
2. Create roles (e.g., "Admin", "CaseManager", "DataEntry", "ReadOnly")
3. Click **Permissions** next to each role
4. Select the appropriate permissions for each role

### Example Role Setup

**Admin Role**
- All Patient permissions
- All Settings permissions
- All User permissions
- All Audit permissions
- All Report permissions

**Case Manager Role**
- Patient: View, Create, Edit, Search, Merge
- Audit: View
- Settings: View

**Data Entry Role**
- Patient: View, Create, Edit, Search
- Settings: View

**Read Only Role**
- Patient: View, Search
- Audit: View

## Step 4: Assign Users to Roles

1. Navigate to **Settings > Users**
2. Click **Edit** on a user
3. Assign them to appropriate role(s)

## Step 5: Apply Permissions to Pages

### Quick Method: Add to PageModel Class

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class IndexModel : PageModel
    {
        // ... existing code
    }
}
```

### Checking Permissions in Code

```csharp
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using System.Security.Claims;

public class EditModel : PageModel
{
    private readonly IPermissionService _permissionService;
    
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!await _permissionService.HasPermissionAsync(userId!, 
            PermissionModule.Patient, PermissionAction.Edit))
        {
            return Forbid();
        }
        
        // ... rest of code
    }
}
```

### Hiding UI Elements Based on Permissions

```razor
@inject IPermissionService PermissionService
@inject UserManager<ApplicationUser> UserManager

@{
    var userId = UserManager.GetUserId(User);
    var canEdit = await PermissionService.HasPermissionAsync(userId!, 
        PermissionModule.Patient, PermissionAction.Edit);
    var canDelete = await PermissionService.HasPermissionAsync(userId!, 
        PermissionModule.Patient, PermissionAction.Delete);
}

@if (canEdit)
{
    <a asp-page="./Edit" asp-route-id="@Model.Patient.Id" class="btn btn-primary">Edit</a>
}

@if (canDelete)
{
    <a asp-page="./Delete" asp-route-id="@Model.Patient.Id" class="btn btn-danger">Delete</a>
}
```

## Recommended Page Permissions

Apply these attributes to your page models:

```csharp
// Patient Pages
[Authorize(Policy = "Permission.Patient.View")]
public class IndexModel : PageModel { }

[Authorize(Policy = "Permission.Patient.View")]
public class DetailsModel : PageModel { }

[Authorize(Policy = "Permission.Patient.Create")]
public class CreateModel : PageModel { }

[Authorize(Policy = "Permission.Patient.Edit")]
public class EditModel : PageModel { }

[Authorize(Policy = "Permission.Patient.Delete")]
public class DeleteModel : PageModel { }

[Authorize(Policy = "Permission.Patient.Search")]
public class SearchModel : PageModel { }

[Authorize(Policy = "Permission.Patient.Merge")]
public class MergeModel : PageModel { }

// Settings Pages
[Authorize(Policy = "Permission.Settings.View")]
public class SettingsIndexModel : PageModel { }

[Authorize(Policy = "Permission.Settings.Edit")]
public class SettingsEditModel : PageModel { }

// User Management Pages
[Authorize(Policy = "Permission.User.View")]
public class UsersIndexModel : PageModel { }

[Authorize(Policy = "Permission.User.Create")]
public class UserCreateModel : PageModel { }

[Authorize(Policy = "Permission.User.Edit")]
public class UserEditModel : PageModel { }

[Authorize(Policy = "Permission.User.Delete")]
public class UserDeleteModel : PageModel { }

[Authorize(Policy = "Permission.User.ManagePermissions")]
public class UserPermissionsModel : PageModel { }

// Audit Pages
[Authorize(Policy = "Permission.Audit.View")]
public class AuditHistoryModel : PageModel { }
```

## Testing Permissions

1. Create a test user
2. Assign them to the "Read Only" role
3. Log in as that user
4. Try to access create/edit/delete pages - you should see a 403 Forbidden error

## Managing User-Specific Permissions

For exceptions where a specific user needs additional permissions:

1. Navigate to **Settings > Users**
2. Click the **Permissions** button next to the user
3. Select additional permissions (these override role permissions)

## Next Steps

- Apply `[Authorize]` attributes to all your page models
- Update your navigation menus to hide links based on permissions
- Test with different role combinations
- Review the full documentation in `Docs\Granular-Permissions-System-Guide.md`
