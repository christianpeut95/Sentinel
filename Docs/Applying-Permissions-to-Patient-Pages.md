# Applying Permissions to Patient Pages

This guide shows you exactly how to protect your Patient pages with the new permission system.

## Step 1: Update Patient Index Page

**File**: `Surveillance-MVP\Pages\Patients\Index.cshtml.cs`

Add the `[Authorize]` attribute at the top of your class:

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class IndexModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 2: Update Patient Details Page

**File**: `Surveillance-MVP\Pages\Patients\Details.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class DetailsModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 3: Update Patient Create Page

**File**: `Surveillance-MVP\Pages\Patients\Create.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Create")]
    public class CreateModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 4: Update Patient Edit Page

**File**: `Surveillance-MVP\Pages\Patients\Edit.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Edit")]
    public class EditModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 5: Update Patient Delete Page

**File**: `Surveillance-MVP\Pages\Patients\Delete.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Delete")]
    public class DeleteModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 6: Update Patient Search Page

**File**: `Surveillance-MVP\Pages\Patients\Search.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Search")]
    public class SearchModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 7: Update Patient Merge Pages

**File**: `Surveillance-MVP\Pages\Patients\SelectMerge.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Merge")]
    public class SelectMergeModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

**File**: `Surveillance-MVP\Pages\Patients\Merge.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Merge")]
    public class MergeModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 8: Update Patient Audit History Page

**File**: `Surveillance-MVP\Pages\Patients\AuditHistory.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Audit.View")]
    public class AuditHistoryModel : PageModel
    {
        // ... existing code unchanged
    }
}
```

## Step 9: Update Patient Index View (Hide Action Buttons)

**File**: `Surveillance-MVP\Pages\Patients\Index.cshtml`

At the top of the file, add:

```razor
@inject Surveillance_MVP.Helpers.PermissionHelper PermissionHelper
```

Then update the action buttons section:

```razor
@{
    var canCreate = await PermissionHelper.CanCreatePatients();
    var canEdit = await PermissionHelper.CanEditPatients();
    var canDelete = await PermissionHelper.CanDeletePatients();
    var canSearch = await PermissionHelper.CanSearchPatients();
    var canMerge = await PermissionHelper.CanMergePatients();
}

@if (canCreate)
{
    <a asp-page="Create" class="btn btn-primary">
        <i class="bi bi-plus-circle"></i> Create New
    </a>
}

@if (canSearch)
{
    <a asp-page="Search" class="btn btn-secondary">
        <i class="bi bi-search"></i> Advanced Search
    </a>
}

@if (canMerge)
{
    <a asp-page="SelectMerge" class="btn btn-warning">
        <i class="bi bi-arrow-left-right"></i> Merge Patients
    </a>
}
```

And in your table rows:

```razor
<td>
    @if (canEdit)
    {
        <a asp-page="./Edit" asp-route-id="@item.Id" class="btn btn-sm btn-primary">Edit</a>
    }
    <a asp-page="./Details" asp-route-id="@item.Id" class="btn btn-sm btn-info">Details</a>
    @if (canDelete)
    {
        <a asp-page="./Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">Delete</a>
    }
</td>
```

## Step 10: Update Patient Details View

**File**: `Surveillance-MVP\Pages\Patients\Details.cshtml`

```razor
@inject Surveillance_MVP.Helpers.PermissionHelper PermissionHelper

@{
    var canEdit = await PermissionHelper.CanEditPatients();
    var canDelete = await PermissionHelper.CanDeletePatients();
}

<div>
    @if (canEdit)
    {
        <a asp-page="./Edit" asp-route-id="@Model.Patient.Id" class="btn btn-warning">Edit</a>
    }
    @if (canDelete)
    {
        <a asp-page="./Delete" asp-route-id="@Model.Patient.Id" class="btn btn-danger">Delete</a>
    }
    <a asp-page="./Index" class="btn btn-secondary">Back to List</a>
</div>
```

## Testing Your Implementation

1. **Create test roles:**
   - Admin (all permissions)
   - CaseManager (View, Create, Edit, Search)
   - ReadOnly (View only)

2. **Create test users:**
   - admin@test.com ? Admin role
   - manager@test.com ? CaseManager role
   - viewer@test.com ? ReadOnly role

3. **Test scenarios:**
   - Login as admin@test.com ? Should see all buttons and access all pages
   - Login as manager@test.com ? Should NOT see Delete or Merge buttons
   - Login as viewer@test.com ? Should ONLY see Details button, no Create/Edit/Delete
   - Try to access `/Patients/Edit/1` as viewer ? Should get 403 Forbidden

## Summary

You've now:
- ? Protected all Patient pages with appropriate permissions
- ? Hidden UI elements based on user permissions
- ? Prevented unauthorized access at the page level
- ? Applied the same pattern to your other modules (Settings, Audit, etc.)

## Next Steps

Apply the same pattern to:
- Settings pages (`Permission.Settings.*`)
- User management pages (`Permission.User.*`)
- Audit pages (`Permission.Audit.*`)
- Any custom pages you create
