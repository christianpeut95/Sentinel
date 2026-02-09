# Audit Logging - System Reference

## Architecture Overview

```
???????????????????????????????????????????????????????????????
?                         YOUR CODE                            ?
?  (Create/Update/Delete any IAuditable entity)               ?
???????????????????????????????????????????????????????????????
                        ?
                        ?
???????????????????????????????????????????????????????????????
?              ApplicationDbContext.SaveChangesAsync()         ?
?  ????????????????????????????????????????????????????????  ?
?  ? 1. OnBeforeSaveChanges()                             ?  ?
?  ?    - Detect changes via ChangeTracker                ?  ?
?  ?    - Check if entity implements IAuditable           ?  ?
?  ?    - Capture old/new values                          ?  ?
?  ?    - Capture user context (ID, IP, UserAgent)       ?  ?
?  ????????????????????????????????????????????????????????  ?
?                        ?                                     ?
?                        ?                                     ?
?  ????????????????????????????????????????????????????????  ?
?  ? 2. base.SaveChangesAsync()                           ?  ?
?  ?    - Save entity changes to database                 ?  ?
?  ????????????????????????????????????????????????????????  ?
?                        ?                                     ?
?                        ?                                     ?
?  ????????????????????????????????????????????????????????  ?
?  ? 3. OnAfterSaveChanges()                              ?  ?
?  ?    - Create AuditLog entries                         ?  ?
?  ?    - Save audit logs to database                     ?  ?
?  ????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????
                        ?
                        ?
???????????????????????????????????????????????????????????????
?                   AuditLogs Table                            ?
?  - Field-level change history                               ?
?  - Who, What, When, Where                                   ?
???????????????????????????????????????????????????????????????
                        ?
                        ?
???????????????????????????????????????????????????????????????
?                    IAuditService                             ?
?  - Query audit logs                                         ?
?  - Filter by entity, user, date                             ?
???????????????????????????????????????????????????????????????
                        ?
                        ?
???????????????????????????????????????????????????????????????
?              Audit History Pages (UI)                        ?
?  - Timeline view                                            ?
?  - Field comparison                                         ?
?  - User attribution                                         ?
???????????????????????????????????????????????????????????????
```

## Key Components

### 1. IAuditable Interface
```csharp
public interface IAuditable
{
    int Id { get; set; }
}
```
**Purpose**: Marker interface to enable audit logging for a model

### 2. AuditLog Model
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; }      // e.g., "Patient"
    public int EntityId { get; set; }           // e.g., 123
    public string Action { get; set; }          // "Added", "Modified", "Deleted"
    public string FieldName { get; set; }       // e.g., "EmailAddress"
    public string? OldValue { get; set; }       // Previous value
    public string? NewValue { get; set; }       // New value
    public DateTime ChangedAt { get; set; }     // When
    public string ChangedByUserId { get; set; } // Who (User ID)
    public ApplicationUser? ChangedByUser { get; set; }
    public string? IpAddress { get; set; }      // Where from
    public string? UserAgent { get; set; }      // What browser/client
}
```
**Note**: Only actual value changes are logged. If a field is set to the same value it already has, no audit entry is created.

### 3. ApplicationDbContext Override
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var auditEntries = OnBeforeSaveChanges();  // Capture changes
    var result = await base.SaveChangesAsync(cancellationToken);  // Save entity
    await OnAfterSaveChanges(auditEntries);    // Save audit logs
    return result;
}
```
**Smart Detection**: Compares old and new values using `Equals()` before logging to avoid redundant entries.

### 4. IAuditService
```csharp
public interface IAuditService
{
    Task<List<AuditLog>> GetAuditLogsAsync(string entityType, int entityId);
    Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId, int pageSize = 50);
    Task<int> GetAuditLogCountAsync(string entityType, int entityId);
    Task LogViewAsync(string entityType, int entityId, string? userId, string? ipAddress, string? userAgent);
}
```

**LogViewAsync**: Manually creates a "Viewed" audit log entry. Used in page models to track when users access sensitive records.

## Database Schema

```sql
AuditLogs
??? Id (PK)
??? EntityType (indexed with EntityId)
??? EntityId (indexed with EntityType)
??? Action
??? FieldName
??? OldValue
??? NewValue
??? ChangedAt (indexed)
??? ChangedByUserId (FK to AspNetUsers, indexed)
??? IpAddress
??? UserAgent
```

### Indexes for Performance
- `IX_AuditLogs_EntityType_EntityId` - Fast lookups by entity
- `IX_AuditLogs_ChangedAt` - Fast date-based queries
- `IX_AuditLogs_ChangedByUserId` - Fast user-based queries

## Usage Patterns

### Pattern 1: Enable Audit Logging
```csharp
// BEFORE
public class Patient
{
    public int Id { get; set; }
    // ... properties
}

// AFTER
public class Patient : IAuditable  // ? Add this
{
    public int Id { get; set; }
    // ... properties
}
```

### Pattern 2: View Audit History in Page Model
```csharp
public class DetailsModel : PageModel
{
    private readonly IAuditService _auditService;
    
    public DetailsModel(IAuditService auditService)
    {
        _auditService = auditService;
    }
    
    public List<AuditLog> AuditLogs { get; set; }
    
    public async Task OnGetAsync(int id)
    {
        // Get the entity
        // ...
        
        // Get audit history
        AuditLogs = await _auditService.GetAuditLogsAsync("Patient", id);
    }
}
```

### Pattern 3: Display Audit Count
```csharp
public async Task OnGetAsync(int id)
{
    // ... load patient ...
    
    ViewData["AuditCount"] = await _auditService.GetAuditLogCountAsync("Patient", id);
}
```

### Pattern 4: Log View Access
```csharp
public class DetailsModel : PageModel
{
    private readonly IAuditService _auditService;
    
    public DetailsModel(IAuditService auditService)
    {
        _auditService = auditService;
    }
    
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // ... load the entity ...
        
        // Log the view
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        
        await _auditService.LogViewAsync("Patient", id.Value, userId, ipAddress, userAgent);
        
        return Page();
    }
}
```

## Action Types Explained

### "Added" Actions
- Logged when a new entity is created
- Only `NewValue` is populated
- One audit log per field

**Example**:
```
Action: Added
Field: GivenName
New Value: "John"
Old Value: null
```

### "Modified" Actions
- Logged when an existing entity is updated
- Both `OldValue` and `NewValue` are populated
- Only changed fields are logged

**Example**:
```
Action: Modified
Field: EmailAddress
Old Value: "old@example.com"
New Value: "new@example.com"
```

### "Deleted" Actions
- Logged when an entity is deleted
- Only `OldValue` is populated
- One audit log per field

**Example**:
```
Action: Deleted
Field: GivenName
Old Value: "John"
New Value: null
```

### "Viewed" Actions
- Logged when a user accesses a record's details page
- Neither `OldValue` nor `NewValue` are populated
- FieldName is set to "Record"
- Captures user, timestamp, IP address, and user agent

**Example**:
```
Action: Viewed
Field: Record
Changed By: doctor@example.com
Changed At: 2025-01-29 09:15:00 UTC
IP: 192.168.1.50
```

## Service Registration (Program.cs)

```csharp
// Required for audit logging
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
```

## Currently Audited Models

? **Patient** - All fields tracked

### To Add New Audited Models

1. Add `: IAuditable` to your model class
2. Create and apply database migration
3. (Optional) Create audit history page

## Files Reference

### Core Files
- `Models/IAuditable.cs` - Interface for auditable entities
- `Models/AuditLog.cs` - Audit log entity model
- `Data/ApplicationDbContext.cs` - Change tracking implementation
- `Services/IAuditService.cs` - Audit query service

### UI Files
- `Pages/Patients/AuditHistory.cshtml` - Timeline view (reusable)
- `Pages/Patients/AuditHistory.cshtml.cs` - Page model

### Documentation
- `Docs/Audit-Logging-Guide.md` - Complete documentation
- `Docs/Quick-Start-Audit-Logging.md` - Quick start guide
- `Docs/Audit-System-Reference.md` - This file

## Common Queries

### Get all changes to an entity
```csharp
var logs = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" && a.EntityId == patientId)
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get recent changes by user
```csharp
var logs = await _context.AuditLogs
    .Where(a => a.ChangedByUserId == userId)
    .OrderByDescending(a => a.ChangedAt)
    .Take(50)
    .ToListAsync();
```

### Get changes to specific field
```csharp
var logs = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
        && a.EntityId == patientId 
        && a.FieldName == "EmailAddress")
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get all deletions
```csharp
var deletions = await _context.AuditLogs
    .Where(a => a.Action == "Deleted")
    .Include(a => a.ChangedByUser)
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get all views of a patient
```csharp
var views = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
        && a.EntityId == patientId 
        && a.Action == "Viewed")
    .Include(a => a.ChangedByUser)
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get who viewed a patient today
```csharp
var today = DateTime.UtcNow.Date;
var tomorrow = today.AddDays(1);

var todayViews = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
        && a.EntityId == patientId 
        && a.Action == "Viewed"
        && a.ChangedAt >= today
        && a.ChangedAt < tomorrow)
    .Include(a => a.ChangedByUser)
    .Select(a => a.ChangedByUser!.Email)
    .Distinct()
    .ToListAsync();
```

## Performance Tips

1. **Use Indexes**: The system creates indexes automatically
2. **Filter Early**: Always filter by EntityType + EntityId first
3. **Limit Results**: Use `.Take()` for large datasets
4. **Eager Load Users**: Use `.Include(a => a.ChangedByUser)` when needed
5. **Archive Old Logs**: Consider archiving logs older than 2-7 years

## Security Considerations

- ?? Audit logs contain actual data values
- ?? Consider data masking for sensitive fields in displays
- ?? Restrict access to audit history pages
- ?? Include audit logs in GDPR data exports
- ?? Define retention policies based on compliance requirements

## Testing Audit Logging

### Test Create Operation
```csharp
[Fact]
public async Task CreatePatient_CreatesAuditLogs()
{
    // Arrange
    var patient = new Patient { GivenName = "Test", FamilyName = "User" };
    
    // Act
    _context.Patients.Add(patient);
    await _context.SaveChangesAsync();
    
    // Assert
    var logs = await _context.AuditLogs
        .Where(a => a.EntityType == "Patient" && a.EntityId == patient.Id)
        .ToListAsync();
    
    Assert.NotEmpty(logs);
    Assert.Contains(logs, l => l.FieldName == "GivenName" && l.NewValue == "Test");
}
```

## Migration Commands

```bash
# Create migration
dotnet ef migrations add AddAuditLogging

# Apply migration
dotnet ef database update

# Check if applied
dotnet ef migrations list

# Rollback (if needed)
dotnet ef database update PreviousMigrationName
```

## Need More Information?

- **Quick Start**: `/Docs/Quick-Start-Audit-Logging.md`
- **Full Guide**: `/Docs/Audit-Logging-Guide.md`
- **Code Examples**: `Pages/Patients/AuditHistory.cshtml.cs`
