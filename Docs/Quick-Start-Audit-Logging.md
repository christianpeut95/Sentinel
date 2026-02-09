# Quick Start: Adding Audit Logging to Your Models

This guide shows you how to add automatic audit logging to any model in just one step.

## Step 1: Implement IAuditable

Simply add `: IAuditable` to your model class:

```csharp
using Surveillance_MVP.Models;

namespace YourNamespace
{
    public class YourModel : IAuditable
    {
        public int Id { get; set; }  // Required by IAuditable
        
        // Your other properties...
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        // etc.
    }
}
```

## That's All!

Once your model implements `IAuditable`, the system will **automatically** track:
- ? When records are created (all fields)
- ? When fields are modified (old value ? new value)
- ? When records are deleted (all field values)
- ? Who made the change (user ID and email)
- ? When it happened (UTC timestamp)
- ? Where it came from (IP address and browser)
- ? Only actual changes (ignores setting a field to the same value)

## No Additional Code Required

The `ApplicationDbContext` automatically intercepts all `SaveChanges` operations and creates audit log entries for any entity that implements `IAuditable`.

## Viewing Audit Logs

### Option 1: Using the Audit Service

Inject `IAuditService` into your page model:

```csharp
public class YourPageModel : PageModel
{
    private readonly IAuditService _auditService;

    public YourPageModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public List<AuditLog> AuditLogs { get; set; }

    public async Task OnGetAsync(int id)
    {
        // Get all audit logs for this entity
        AuditLogs = await _auditService.GetAuditLogsAsync("YourModel", id);
    }
}
```

### Option 2: Create a Dedicated Audit History Page

Copy and adapt the existing Patient Audit History page:
1. Copy `Pages/Patients/AuditHistory.cshtml` and `.cshtml.cs`
2. Update the model name and route
3. Add navigation links to your details page

## Complete Example: Adding Audit Logging to a "Case" Model

### 1. Create Your Model with IAuditable

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Models
{
    public class Case : IAuditable
    {
        public int Id { get; set; }  // Required by IAuditable

        [Required]
        [Display(Name = "Case Number")]
        public string CaseNumber { get; set; }

        [Display(Name = "Patient")]
        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Display(Name = "Reported Date")]
        public DateTime? ReportedDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        // ... other properties
    }
}
```

### 2. Add to DbContext

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // ... existing code ...
    
    public DbSet<Case> Cases { get; set; }  // Add this line
    
    // ... rest of the code ...
}
```

### 3. Create Migration

```bash
dotnet ef migrations add AddCaseModel
dotnet ef database update
```

### 4. That's It - Audit Logging is Now Active!

Every change to any `Case` record will now be automatically logged to the `AuditLogs` table.

### 5. (Optional) Add Audit History Page

Create `Pages/Cases/AuditHistory.cshtml.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Cases
{
    public class AuditHistoryModel : PageModel
    {
        private readonly IAuditService _auditService;

        public AuditHistoryModel(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public int CaseId { get; set; }
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            CaseId = id.Value;
            AuditLogs = await _auditService.GetAuditLogsAsync("Case", CaseId);
            return Page();
        }
    }
}
```

And create the view file (copy from `Pages/Patients/AuditHistory.cshtml` and update the model references).

## What Gets Tracked?

### For New Records (Action: "Added")
Every field is logged with its initial value:
```
Field: CaseNumber, New Value: "CASE-001"
Field: Status, New Value: "Open"
Field: ReportedDate, New Value: "2025-01-29"
```

### For Modified Records (Action: "Modified")
Only changed fields are logged with both old and new values:
```
Field: Status
Old Value: "Open"
New Value: "Closed"
```

### For Deleted Records (Action: "Deleted")
All fields are logged with their final values:
```
Field: CaseNumber, Old Value: "CASE-001"
Field: Status, Old Value: "Closed"
```

## Best Practices

### ? DO:
- Implement `IAuditable` on models that need audit tracking
- Use the entity name consistently when querying audit logs
- Consider pagination when displaying audit logs for entities with many changes

### ? DON'T:
- Implement `IAuditable` on lookup tables (Countries, Languages, etc.) unless required
- Implement `IAuditable` on the `AuditLog` model itself (creates infinite loop)
- Store sensitive data without considering audit log retention policies

## Querying Audit Logs

### Get all changes for a specific entity
```csharp
var logs = await _auditService.GetAuditLogsAsync("Case", caseId);
```

### Get recent changes by a user
```csharp
var userLogs = await _auditService.GetAuditLogsByUserAsync(userId, pageSize: 100);
```

### Count changes for an entity
```csharp
var count = await _auditService.GetAuditLogCountAsync("Case", caseId);
```

### Direct LINQ queries (advanced)
```csharp
var recentChanges = await _context.AuditLogs
    .Include(a => a.ChangedByUser)
    .Where(a => a.EntityType == "Case" 
        && a.ChangedAt >= DateTime.UtcNow.AddDays(-7))
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

## Troubleshooting

### Changes aren't being logged
1. ? Verify your model implements `IAuditable`
2. ? Check that the model has an `Id` property of type `int`
3. ? Ensure the database migration has been applied
4. ? Verify `IHttpContextAccessor` is registered in `Program.cs`

### Can't see audit logs in the database
- Check the `AuditLogs` table exists: `SELECT * FROM AuditLogs`
- Verify the migration was applied: `dotnet ef migrations list`
- Check application logs for errors during save operations

## Need More Help?

See the full documentation: `/Docs/Audit-Logging-Guide.md`
