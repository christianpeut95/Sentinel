# ?? Authorization Quick Fix Guide

## Current Status
- ? Permissions seeded (196 permissions)
- ? Roles created (5 roles with assignments)
- ? **68 pages missing authorization attributes**

---

## ?? CRITICAL - Fix Now (5 minutes)

### User Management (6 files)

```powershell
# Add to ALL Settings\Users\*.cshtml.cs files
using Microsoft.AspNetCore.Authorization;
```

| File | Authorization |
|------|---------------|
| `Settings\Users\Index.cshtml.cs` | `[Authorize(Policy = "Permission.User.View")]` |
| `Settings\Users\Details.cshtml.cs` | `[Authorize(Policy = "Permission.User.View")]` |
| `Settings\Users\Create.cshtml.cs` | `[Authorize(Policy = "Permission.User.Create")]` |
| `Settings\Users\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.User.Edit")]` |
| `Settings\Users\Delete.cshtml.cs` | `[Authorize(Policy = "Permission.User.Delete")]` |
| `Settings\Users\Permissions.cshtml.cs` | `[Authorize(Policy = "Permission.User.ManagePermissions")]` |

### Role Management (2 files)

| File | Authorization |
|------|---------------|
| `Settings\Roles\Index.cshtml.cs` | `[Authorize(Policy = "Permission.User.ManagePermissions")]` |
| `Settings\Roles\Permissions.cshtml.cs` | `[Authorize(Policy = "Permission.User.ManagePermissions")]` |

---

## ?? HIGH PRIORITY - Fix Today (30 minutes)

### Exposures (2 files)
| File | Authorization |
|------|---------------|
| `Cases\Exposures\Create.cshtml.cs` | `[Authorize(Policy = "Permission.Exposure.Create")]` |
| `Cases\Exposures\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.Exposure.Edit")]` |

### Events (5 files)
| File | Authorization |
|------|---------------|
| `Events\Index.cshtml.cs` | `[Authorize(Policy = "Permission.Event.View")]` |
| `Events\Details.cshtml.cs` | `[Authorize(Policy = "Permission.Event.View")]` |
| `Events\Create.cshtml.cs` | `[Authorize(Policy = "Permission.Event.Create")]` |
| `Events\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.Event.Edit")]` |
| `Events\Delete.cshtml.cs` | `[Authorize(Policy = "Permission.Event.Delete")]` |

### Locations (5 files)
| File | Authorization |
|------|---------------|
| `Locations\Index.cshtml.cs` | `[Authorize(Policy = "Permission.Location.View")]` |
| `Locations\Details.cshtml.cs` | `[Authorize(Policy = "Permission.Location.View")]` |
| `Locations\Create.cshtml.cs` | `[Authorize(Policy = "Permission.Location.Create")]` |
| `Locations\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.Location.Edit")]` |
| `Locations\Delete.cshtml.cs` | `[Authorize(Policy = "Permission.Location.Delete")]` |

### Outbreaks (10 files)
| File | Authorization |
|------|---------------|
| `Outbreaks\Index.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.View")]` |
| `Outbreaks\Details.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.View")]` |
| `Outbreaks\LineList.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.View")]` |
| `Outbreaks\Create.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Create")]` |
| `Outbreaks\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |
| `Outbreaks\LinkCases.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |
| `Outbreaks\ClassifyCases.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |
| `Outbreaks\ManageTeam.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |
| `Outbreaks\CaseDefinitions.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |
| `Outbreaks\BulkActions.cshtml.cs` | `[Authorize(Policy = "Permission.Outbreak.Edit")]` |

### Organizations (4 files)
| File | Authorization |
|------|---------------|
| `Organizations\Index.cshtml.cs` | `[Authorize(Policy = "Permission.Settings.View")]` |
| `Organizations\Details.cshtml.cs` | `[Authorize(Policy = "Permission.Settings.View")]` |
| `Organizations\Create.cshtml.cs` | `[Authorize(Policy = "Permission.Settings.Create")]` |
| `Organizations\Edit.cshtml.cs` | `[Authorize(Policy = "Permission.Settings.Edit")]` |

### Tasks (1 file)
| File | Authorization |
|------|---------------|
| `Tasks\CompleteSurvey.cshtml.cs` | `[Authorize(Policy = "Permission.Task.Edit")]` |

---

## ?? MEDIUM PRIORITY - Settings/Lookups (28 files)

All Settings\Lookups pages follow this pattern:

```csharp
[Authorize(Policy = "Permission.Settings.View")]               // Index pages
[Authorize(Policy = "Permission.Settings.Create")]             // Create pages
[Authorize(Policy = "Permission.Settings.Edit")]               // Edit pages
[Authorize(Policy = "Permission.Settings.ManageSystemLookups")] // For system-critical lookups
```

### Lookup Types (9 types × ~3 pages each)
- EventTypes
- LocationTypes
- OrganizationTypes
- ResultUnits
- SpecimenTypes
- TaskTemplates
- TaskTypes
- TestResults
- TestTypes

---

## ?? Copy-Paste Templates

### Template 1: View Permission
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.YourModule
{
    [Authorize(Policy = "Permission.Module.View")]
    public class IndexModel : PageModel
```

### Template 2: Create Permission
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.YourModule
{
    [Authorize(Policy = "Permission.Module.Create")]
    public class CreateModel : PageModel
```

### Template 3: Edit Permission
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.YourModule
{
    [Authorize(Policy = "Permission.Module.Edit")]
    public class EditModel : PageModel
```

### Template 4: Delete Permission
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.YourModule
{
    [Authorize(Policy = "Permission.Module.Delete")]
    public class DeleteModel : PageModel
```

---

## ? Testing Authorization

### Test Each Role

1. **Admin** - Should access everything
2. **Surveillance Manager** - Should access all except delete audit logs
3. **Surveillance Officer** - Should access cases, patients, tasks, outbreaks
4. **Data Entry** - Should only access patient/case view/create/edit
5. **Contact Tracer** - Should access tasks, surveys, exposures

### Quick Test Script
```csharp
// Settings > Users > Details > [Pick User] > Check permissions granted
// Then login as that user and try accessing restricted pages
```

---

## ?? Automated Fix Available

I can automatically fix all 68 pages in ~5 minutes using multi_replace_string_in_file tool.

**Want me to proceed?** Yes/No

---

**Priority Order:**
1. ? **Now:** User/Role management (8 files) - Security risk
2. ?? **Today:** Production features (27 files) - Data integrity
3. ?? **This Week:** Lookups (28 files) - Configuration control
4. ?? **Later:** Utility pages (5 files) - Low risk
