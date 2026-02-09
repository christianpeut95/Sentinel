# Audit Logging System

## Overview
The Audit Logging system automatically tracks all changes made to auditable entities in the application. It records field-level changes including who made the change, when it was made, and what was changed (old value vs new value).

## Features

### Automatic Change Tracking
- **Field-Level Tracking**: Every field change is recorded separately
- **Action Types**: Tracks Add, Modify, Delete, and View operations
- **User Tracking**: Records which user made the change or viewed the record
- **Timestamp**: UTC timestamp of when the change occurred
- **IP Address**: Captures the IP address of the user making the change
- **User Agent**: Records the browser/client information
- **Smart Change Detection**: Only logs fields where values actually changed (ignores setting a field to its existing value)

### Audit Information Captured
For each change, the system records:
- **Entity Type**: The model being changed (e.g., "Patient")
- **Entity ID**: The ID of the specific record
- **Action**: Add, Modified, or Deleted
- **Field Name**: The specific property that changed
- **Old Value**: The previous value (for Modified and Deleted)
- **New Value**: The new value (for Added and Modified)
- **Changed By**: User ID and email of who made the change (nullable - will be null for system operations)
- **Changed At**: UTC timestamp
- **IP Address**: Request IP address
- **User Agent**: Browser/client information

## How It Works

### Architecture

#### 1. IAuditable Interface
Any model that implements `IAuditable` will automatically have all changes tracked:

```csharp
public interface IAuditable
{
    int Id { get; set; }
}
```

#### 2. ApplicationDbContext Override
The `SaveChangesAsync` method is overridden to intercept changes before and after saving:

- **OnBeforeSaveChanges**: Captures the state of entities before saving
- **OnAfterSaveChanges**: Creates audit log entries and saves them

#### 3. Automatic Detection
The system uses EF Core's `ChangeTracker` to automatically detect:
- New entities (Added)
- Modified properties (Modified)
- Deleted entities (Deleted)

Additionally, the system manually logs:
- View operations (Viewed) - when someone accesses a patient's details page

**Smart Change Detection**: The system compares old and new values before logging. If a field is set to the same value it already has (e.g., updating a record but not actually changing a field), no audit log entry is created for that field. This prevents unnecessary audit log entries and keeps the audit trail focused on actual changes.

**View Tracking**: Every time a user accesses a patient's details page, a "Viewed" action is logged with the user's ID, IP address, and timestamp. This helps track who has accessed sensitive patient information for compliance and security purposes.

### Enabling Audit Logging for a Model

To enable audit logging for any model, simply implement the `IAuditable` interface:

```csharp
public class Patient : IAuditable
{
    public int Id { get; set; }
    // ... other properties
}
```

That's it! No additional code is required. All changes will be automatically tracked.

## Viewing Audit Logs

### Patient Audit History Page
Navigate to a patient's details page and click the **"Audit History"** button to view all changes made to that patient.

**URL**: `/Patients/AuditHistory?id={patientId}`

### Timeline View
The audit history is displayed as a timeline showing:
- **Grouped by Date**: Changes are grouped by day
- **Chronological Order**: Most recent changes first
- **Visual Indicators**: Color-coded badges for different action types
  - Green: Added
  - Blue: Modified
  - Red: Deleted
  - Light Blue: Viewed
- **Field-Level Details**: Each field change is shown separately
- **User Information**: Who made the change and when
- **Value Comparison**: Old value vs new value for modifications
- **Filter Toggle**: Show or hide view events to focus on data changes

## Using the Audit Service

### IAuditService Interface
The `IAuditService` provides methods to query and create audit logs:

```csharp
public interface IAuditService
{
    // Get all audit logs for a specific entity
    Task<List<AuditLog>> GetAuditLogsAsync(string entityType, int entityId);
    
    // Get recent audit logs by a specific user
    Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId, int pageSize = 50);
    
    // Get count of audit logs for an entity
    Task<int> GetAuditLogCountAsync(string entityType, int entityId);
    
    // Manually log a view operation
    Task LogViewAsync(string entityType, int entityId, string? userId, string? ipAddress, string? userAgent);
}
```

### Example Usage in a Razor Page

```csharp
public class AuditHistoryModel : PageModel
{
    private readonly IAuditService _auditService;

    public AuditHistoryModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public List<AuditLog> AuditLogs { get; set; }

    public async Task OnGetAsync(int id)
    {
        AuditLogs = await _auditService.GetAuditLogsAsync("Patient", id);
    }
}
```

## Database Schema

### AuditLog Table
```sql
CREATE TABLE AuditLogs (
    Id INT PRIMARY KEY IDENTITY,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    FieldName NVARCHAR(100) NOT NULL,
    OldValue NVARCHAR(MAX) NULL,
    NewValue NVARCHAR(MAX) NULL,
    ChangedAt DATETIME2 NOT NULL,
    ChangedByUserId NVARCHAR(450) NOT NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    
    FOREIGN KEY (ChangedByUserId) REFERENCES AspNetUsers(Id)
)

-- Indexes for performance
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_ChangedAt ON AuditLogs(ChangedAt);
CREATE INDEX IX_AuditLogs_ChangedByUserId ON AuditLogs(ChangedByUserId);
```

## Adding Audit Logging to New Models

### Step 1: Implement IAuditable
```csharp
public class YourModel : IAuditable
{
    public int Id { get; set; }
    // ... your properties
}
```

### Step 2: That's It!
The system will automatically track all changes to this model. No additional configuration is required.

### Step 3 (Optional): Create an Audit History Page
If you want a dedicated audit history page for your model:

1. Create a new Razor Page (e.g., `YourModel/AuditHistory.cshtml`)
2. Use the `IAuditService` to retrieve audit logs
3. Display them using the timeline view or create your own display

Example:
```csharp
public class AuditHistoryModel : PageModel
{
    private readonly IAuditService _auditService;

    public AuditHistoryModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public List<AuditLog> AuditLogs { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        
        AuditLogs = await _auditService.GetAuditLogsAsync("YourModel", id.Value);
        return Page();
    }
}
```

## Configuration

### Registering Services
The audit service is registered in `Program.cs`:

```csharp
// Audit logging service
builder.Services.AddScoped<IAuditService, AuditService>();

// Required for capturing user context in audit logs
builder.Services.AddHttpContextAccessor();
```

### Database Context
The `ApplicationDbContext` requires `IHttpContextAccessor` to capture user information:

```csharp
public ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor httpContextAccessor) : base(options)
{
    _httpContextAccessor = httpContextAccessor;
}
```

## Performance Considerations

### Smart Change Detection
The system uses value comparison to prevent unnecessary audit logs:
- **Before Logging**: Compares `OldValue` and `NewValue` using `Equals()`
- **Skip Unchanged**: If values are identical, no audit log is created
- **Benefit**: Reduces database storage and improves query performance

**Example Scenario**:
```csharp
// User updates a patient but doesn't change the email
patient.EmailAddress = "same@example.com";  // Already "same@example.com"
patient.City = "Melbourne";  // Changed from "Sydney"

// Result: Only City change is logged, EmailAddress is skipped
```

This is especially helpful when:
- Forms are submitted without changes to all fields
- Programmatic updates set multiple fields (some unchanged)
- Background processes update records with partial changes

### Indexes
The system creates indexes on frequently queried columns:
- `EntityType` and `EntityId` (composite index)
- `ChangedAt` (for date-based queries)
- `ChangedByUserId` (for user-based queries)

### Bulk Changes
When bulk operations create many audit logs, they are inserted in a single batch for better performance.

### Storage
Audit logs can grow large over time. Consider implementing:
- **Archival Strategy**: Move old audit logs to archive tables
- **Retention Policy**: Delete audit logs older than X years (based on compliance requirements)
- **Compression**: Compress old audit log data

## Security and Compliance

### Data Retention
The audit system helps meet compliance requirements by:
- Recording all data changes
- Tracking who made changes
- Providing a complete audit trail
- Capturing client information (IP, User Agent)

### Access Control
- Audit logs should only be visible to authorized users
- Consider implementing role-based access to audit history pages
- Audit logs themselves are NOT audited (to prevent infinite loops)

### Data Privacy
- Audit logs store actual field values (both old and new)
- Consider data masking for sensitive fields in audit log displays
- Ensure audit logs are included in data export/deletion requests (GDPR compliance)

## Troubleshooting

### Audit Logs Not Being Created

**Problem**: Changes are saved but no audit logs appear.

**Solutions**:
1. Verify the model implements `IAuditable`
2. Check that `IHttpContextAccessor` is registered in `Program.cs`
3. Ensure the database migration has been applied
4. Check for exceptions in application logs

### "An error occurred while saving the entity changes"

**Problem**: Getting database errors when updating entities after adding audit logging.

**Solution**: This was fixed in migration `MakeAuditUserIdNullable`. Ensure you've applied all migrations:
```bash
dotnet ef database update
```

The `ChangedByUserId` field is now nullable to handle cases where the user cannot be determined.

### "System" User in Audit Logs

**Problem**: Some audit logs show "System" as the user or have no user.

**Explanation**: When the system cannot determine the current user (e.g., during background jobs, migrations, or console applications), the `ChangedByUserId` field will be null and displayed as "System" in the UI.

**This is expected behavior** for operations that don't have an authenticated user context.

### Performance Issues

**Problem**: Saving entities is slower than before.

**Solutions**:
1. Verify indexes are created on `AuditLogs` table
2. Consider archiving old audit logs
3. For bulk operations, consider temporarily disabling audit logging if appropriate

## Future Enhancements

Potential improvements:
- **Audit Log Search**: Global search across all audit logs
- **Audit Report Generation**: Export audit logs to PDF/Excel
- **Change Comparison View**: Side-by-side comparison of entity versions
- **Selective Field Auditing**: Attribute to exclude specific fields from auditing
- **Audit Log Retention Policies**: Automatic archival and cleanup
- **Real-time Notifications**: Alert on suspicious changes
- **Rollback Functionality**: Restore previous values from audit logs
- **Data Masking**: Automatically mask sensitive field values in audit display
- **Compliance Reports**: Pre-built reports for compliance audits

## Example Audit Log Entries

### When a Patient is Created
```
Action: Added
Field: GivenName, New Value: "John"
Field: FamilyName, New Value: "Smith"
Field: DateOfBirth, New Value: "1990-01-15"
...
Changed By: user@example.com
Changed At: 2025-01-29 14:30:00 UTC
```

### When a Patient is Updated
```
Action: Modified
Field: EmailAddress
Old Value: "old@example.com"
New Value: "new@example.com"
Changed By: admin@example.com
Changed At: 2025-01-29 15:45:00 UTC
IP Address: 192.168.1.100
```

### When a Patient is Deleted
```
Action: Deleted
Field: GivenName, Old Value: "John"
Field: FamilyName, Old Value: "Smith"
...
Changed By: admin@example.com
Changed At: 2025-01-29 16:00:00 UTC
```

### When a Patient Record is Viewed
```
Action: Viewed
Field: Record
Changed By: doctor@example.com
Changed At: 2025-01-29 09:15:00 UTC
IP Address: 192.168.1.50
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)...
```

## View Tracking

### What is Tracked
Every time a user accesses a patient's details page, the system automatically logs:
- **Who** viewed the record (User ID and email)
- **When** the record was viewed (UTC timestamp)
- **From where** the access occurred (IP address and browser/device info)

### Why Track Views
- **Compliance**: HIPAA and other regulations often require tracking who accesses patient records
- **Security**: Detect unauthorized access patterns
- **Accountability**: Maintain a complete audit trail of all interactions with patient data
- **Investigation**: Review access history when investigating potential data breaches

### Filtering View Events
View events can generate a lot of audit log entries. The audit history page includes a toggle to:
- **Show View Events**: See all interactions with the record
- **Hide View Events**: Focus only on data changes (Add/Modify/Delete)

This allows users to quickly review actual changes while still maintaining a complete audit trail.

### Privacy Considerations
View tracking logs do NOT capture:
- What fields were viewed
- How long the page was viewed
- Whether any data was copied or exported

Only the fact that the record was accessed is logged.

## Related Documentation

- **Advanced Patient Search Guide**: `/Docs/Advanced-Patient-Search-Guide.md`
- **ANZSCO Upload Guide**: `/Docs/ANZSCO-Upload-Guide.md`

## Support

For issues or questions about the audit logging system:
1. Check the application logs for errors
2. Verify database migration has been applied: `dotnet ef database update`
3. Review this documentation
4. Contact the development team
