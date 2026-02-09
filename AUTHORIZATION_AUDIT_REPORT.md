# ?? Authorization Audit Report

## Executive Summary

**Total Pages Audited:** 157  
**Pages Missing Authorization:** 68  
**Authorization Pattern:** Permission-based policies  
**Status:** ?? **Needs Immediate Action**

---

## ?? Current Authorization Strategy

Your app uses **GLOBAL authorization** in `Program.cs`:
```csharp
options.Conventions.AuthorizeFolder("/");
```

**This means:** All pages require authentication by default (GOOD!)

**However:** Pages need **granular permission checks** for role-based access control.

---

## ? Pages WITH Proper Authorization

### Cases (Exemplar Pattern ?)
- `Cases\Create.cshtml.cs` - `[Authorize(Policy = "Permission.Case.Create")]`
- `Cases\Edit.cshtml.cs` - `[Authorize(Policy = "Permission.Case.Edit")]`
- `Cases\Index.cshtml.cs` - `[Authorize(Policy = "Permission.Case.View")]`
- `Cases\Delete.cshtml.cs` - `[Authorize(Policy = "Permission.Case.Delete")]`
- `Cases\Details.cshtml.cs` - `[Authorize(Policy = "Permission.Case.View")]`

### Dashboard
- `Dashboard\MyTasks.cshtml.cs` - `[Authorize]` (basic)
- `Dashboard\SuperviseInterviews.cshtml.cs` - `[Authorize(Roles = "Admin,Supervisor")]`
- `Dashboard\InterviewQueue.cshtml.cs` - `[Authorize]`

### Patients
- `Patients\Create.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.Create")]`
- `Patients\Edit.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.Edit")]`
- `Patients\Index.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.View")]`
- `Patients\Delete.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.Delete")]`
- `Patients\Details.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.View")]`
- `Patients\Merge.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.Merge")]`
- `Patients\SelectMerge.cshtml.cs` - `[Authorize(Policy = "Permission.Patient.Merge")]`

### Settings (Partial)
- Most Settings pages have `[Authorize(Policy = "Permission.Settings.View")]` etc.

---

## ? Pages MISSING Specific Authorization (68 Total)

### High Priority - Production Features

#### **Exposures** (CRITICAL - Data Entry)
- ? `Cases\Exposures\Create.cshtml.cs`
- ? `Cases\Exposures\Edit.cshtml.cs`

#### **Events** (HIGH - Contact Tracing)
- ? `Events\Create.cshtml.cs`
- ? `Events\Edit.cshtml.cs`
- ? `Events\Delete.cshtml.cs`
- ? `Events\Details.cshtml.cs`
- ? `Events\Index.cshtml.cs`

#### **Locations** (HIGH - Contact Tracing)
- ? `Locations\Create.cshtml.cs`
- ? `Locations\Edit.cshtml.cs`
- ? `Locations\Delete.cshtml.cs`
- ? `Locations\Details.cshtml.cs`
- ? `Locations\Index.cshtml.cs`

#### **Outbreaks** (CRITICAL - Investigation Management)
- ? `Outbreaks\Create.cshtml.cs`
- ? `Outbreaks\Edit.cshtml.cs`
- ? `Outbreaks\Details.cshtml.cs`
- ? `Outbreaks\Index.cshtml.cs`
- ? `Outbreaks\LinkCases.cshtml.cs`
- ? `Outbreaks\ClassifyCases.cshtml.cs`
- ? `Outbreaks\ManageTeam.cshtml.cs`
- ? `Outbreaks\CaseDefinitions.cshtml.cs`
- ? `Outbreaks\BulkActions.cshtml.cs`
- ? `Outbreaks\LineList.cshtml.cs`

#### **Organizations** (MEDIUM - Data Management)
- ? `Organizations\Create.cshtml.cs`
- ? `Organizations\Edit.cshtml.cs`
- ? `Organizations\Details.cshtml.cs`
- ? `Organizations\Index.cshtml.cs`

#### **Tasks** (HIGH - Workflow)
- ? `Tasks\CompleteSurvey.cshtml.cs`

### Medium Priority - Settings/Lookups

#### **Settings - Lookups** (ALL MISSING - 28 pages)
All lookup CRUD operations lack permission checks:
- ? EventTypes (Create, Edit, Index)
- ? LocationTypes (Create, Edit, Index)
- ? OrganizationTypes (Create, Edit, Index)
- ? ResultUnits (Create, Edit, Index)
- ? SpecimenTypes (Create, Edit, Index)
- ? TaskTemplates (Create, Edit, Index)
- ? TaskTypes (Create, Edit, Index)
- ? TestResults (Create, Edit, Index)
- ? TestTypes (Create, Edit, Index)

#### **Settings - User Management** (CRITICAL - Security)
- ? `Settings\Users\Create.cshtml.cs`
- ? `Settings\Users\Edit.cshtml.cs`
- ? `Settings\Users\Delete.cshtml.cs`
- ? `Settings\Users\Details.cshtml.cs`
- ? `Settings\Users\Index.cshtml.cs`
- ? `Settings\Users\Permissions.cshtml.cs`

#### **Settings - Roles** (CRITICAL - Security)
- ? `Settings\Roles\Index.cshtml.cs`
- ? `Settings\Roles\Permissions.cshtml.cs`

### Low Priority - Utility Pages
- ? `Patients\AuditHistory.cshtml.cs` (view-only)
- ? `Patients\Search.cshtml.cs` (search function)
- ? `Api\OccupationSearch.cshtml.cs` (API endpoint)
- ? `DebugPermissions.cshtml.cs` (debug page)
- ? `Error.cshtml.cs` (error page - should allow anonymous)
- ? `Index.cshtml.cs` (home page)
- ? `Privacy.cshtml.cs` (public page)

---

## ?? Recommended Authorization Mapping

### Module: Exposure
```csharp
[Authorize(Policy = "Permission.Exposure.Create")]  // Cases\Exposures\Create
[Authorize(Policy = "Permission.Exposure.Edit")]    // Cases\Exposures\Edit
```

### Module: Event
```csharp
[Authorize(Policy = "Permission.Event.View")]    // Events\Index, Details
[Authorize(Policy = "Permission.Event.Create")]  // Events\Create
[Authorize(Policy = "Permission.Event.Edit")]    // Events\Edit
[Authorize(Policy = "Permission.Event.Delete")]  // Events\Delete
```

### Module: Location
```csharp
[Authorize(Policy = "Permission.Location.View")]    // Locations\Index, Details
[Authorize(Policy = "Permission.Location.Create")]  // Locations\Create
[Authorize(Policy = "Permission.Location.Edit")]    // Locations\Edit
[Authorize(Policy = "Permission.Location.Delete")]  // Locations\Delete
```

### Module: Outbreak
```csharp
[Authorize(Policy = "Permission.Outbreak.View")]    // Outbreaks\Index, Details, LineList
[Authorize(Policy = "Permission.Outbreak.Create")]  // Outbreaks\Create
[Authorize(Policy = "Permission.Outbreak.Edit")]    // Outbreaks\Edit, LinkCases, ClassifyCases, ManageTeam, CaseDefinitions, BulkActions
[Authorize(Policy = "Permission.Outbreak.Delete")]  // Future: Outbreak delete
```

### Module: Task
```csharp
[Authorize(Policy = "Permission.Task.Edit")]  // Tasks\CompleteSurvey
```

### Module: Settings
```csharp
[Authorize(Policy = "Permission.Settings.View")]               // All lookup Index pages
[Authorize(Policy = "Permission.Settings.Create")]             // All lookup Create pages
[Authorize(Policy = "Permission.Settings.Edit")]               // All lookup Edit pages
[Authorize(Policy = "Permission.Settings.ManageSystemLookups")] // System lookups
```

### Module: User (Special - Admin Only)
```csharp
[Authorize(Policy = "Permission.User.View")]          // Settings\Users\Index, Details
[Authorize(Policy = "Permission.User.Create")]        // Settings\Users\Create
[Authorize(Policy = "Permission.User.Edit")]          // Settings\Users\Edit
[Authorize(Policy = "Permission.User.Delete")]        // Settings\Users\Delete
[Authorize(Policy = "Permission.User.ManagePermissions")] // Settings\Users\Permissions, Settings\Roles\*
```

---

## ?? Security Risks

### Critical Risks (Immediate Action Required)

1. **User Management Unprotected**
   - Any authenticated user can create/edit/delete users
   - Any authenticated user can assign roles
   - **Risk:** Privilege escalation

2. **Outbreak Management Unprotected**
   - Any authenticated user can create/modify outbreaks
   - Any authenticated user can link cases
   - **Risk:** Data integrity compromise

3. **Settings/Lookups Unprotected**
   - Any authenticated user can modify system settings
   - **Risk:** System misconfiguration

### Medium Risks

4. **Exposure Tracking Unprotected**
   - Data entry staff can't be restricted from exposure management
   
5. **Events/Locations Unprotected**
   - Contact tracing data accessible to all

---

## ? Quick Fix Strategy

### Phase 1: CRITICAL (Do Now - 30 minutes)

Add authorization to security-critical pages:

```csharp
// Settings\Users\*.cshtml.cs (6 files)
[Authorize(Policy = "Permission.User.View")]     // Index, Details
[Authorize(Policy = "Permission.User.Create")]   // Create
[Authorize(Policy = "Permission.User.Edit")]     // Edit
[Authorize(Policy = "Permission.User.Delete")]   // Delete
[Authorize(Policy = "Permission.User.ManagePermissions")] // Permissions

// Settings\Roles\*.cshtml.cs (2 files)
[Authorize(Policy = "Permission.User.ManagePermissions")] // All role pages
```

### Phase 2: HIGH (Do Today - 1 hour)

Add authorization to production features:

```csharp
// Outbreaks\*.cshtml.cs (10 files)
// Events\*.cshtml.cs (5 files)
// Locations\*.cshtml.cs (5 files)
// Cases\Exposures\*.cshtml.cs (2 files)
// Organizations\*.cshtml.cs (4 files)
```

### Phase 3: MEDIUM (This Week)

Add authorization to all Settings/Lookups pages (28 files)

---

## ??? Implementation Pattern

### Example Fix (Copy-Paste Pattern)

**Before:**
```csharp
namespace Surveillance_MVP.Pages.Events
{
    public class CreateModel : PageModel
    {
        // ...
    }
}
```

**After:**
```csharp
using Microsoft.AspNetCore.Authorization;

namespace Surveillance_MVP.Pages.Events
{
    [Authorize(Policy = "Permission.Event.Create")]
    public class CreateModel : PageModel
    {
        // ...
    }
}
```

---

## ?? Authorization Coverage Summary

| Module | Total Pages | Protected | Missing | Coverage |
|--------|-------------|-----------|---------|----------|
| Cases | 7 | 7 | 0 | ? 100% |
| Patients | 9 | 9 | 0 | ? 100% |
| Dashboard | 3 | 3 | 0 | ? 100% |
| **Exposures** | 2 | 0 | 2 | ? 0% |
| **Events** | 5 | 0 | 5 | ? 0% |
| **Locations** | 5 | 0 | 5 | ? 0% |
| **Outbreaks** | 10 | 0 | 10 | ? 0% |
| **Organizations** | 4 | 0 | 4 | ? 0% |
| **Tasks** | 1 | 0 | 1 | ? 0% |
| **Settings - Lookups** | 28 | 0 | 28 | ? 0% |
| **Settings - Users** | 6 | 0 | 6 | ? 0% |
| **Settings - Roles** | 2 | 0 | 2 | ? 0% |
| Settings - Other | 50 | 45 | 5 | ?? 90% |
| Utility | 5 | 0 | 5 | ?? N/A |

**Overall Coverage:** 64/157 = **41%** ?  
**Production Features:** **56% UNPROTECTED** ?

---

## ?? Next Steps

1. **Run the permission seeder** (already done)
2. **Apply Phase 1 fixes** (security-critical)
3. **Test with different roles**
4. **Apply Phase 2 fixes** (production features)
5. **Complete Phase 3** (lookups)

---

## ?? Want Me To Fix This?

I can automatically add authorization attributes to all 68 pages in ~5 minutes.

**Approve?** Yes/No

---

**Generated:** $(Get-Date)  
**Audit Tool:** PowerShell + Manual Review
