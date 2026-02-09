# View Tracking Feature for Audit Logging

## Overview
The audit logging system has been extended to track when users view patient records. Every time someone accesses a patient's details page, a "Viewed" action is automatically logged with full user and access context.

## What Was Added

### 1. **LogViewAsync Method** in IAuditService
A new method to manually create view audit logs:

```csharp
Task LogViewAsync(string entityType, int entityId, string? userId, string? ipAddress, string? userAgent);
```

### 2. **Automatic View Logging** in Patient Details Page
The `OnGetAsync` method now logs every page view:

```csharp
// Log the view action
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
var userAgent = Request.Headers["User-Agent"].ToString();

await _auditService.LogViewAsync("Patient", patient.Id, userId, ipAddress, userAgent);
```

### 3. **Enhanced Audit History UI**
- **"Viewed" Badge**: Light blue badge with eye icon
- **View Count**: Displays separate counts for data changes vs views
- **Filter Toggle**: Show/hide view events to focus on data changes
- **Visual Styling**: View events are slightly faded to distinguish from data changes

### 4. **Updated Documentation**
All documentation has been updated to include view tracking:
- Action types now include "Viewed"
- Usage patterns for implementing view tracking
- Query examples for retrieving view logs
- Compliance and security benefits explained

## Why Track Views?

### Compliance
- **HIPAA**: Requires tracking access to protected health information (PHI)
- **GDPR**: Demonstrates accountability for data access
- **Audit Requirements**: Many healthcare regulations mandate access logging

### Security
- **Detect Unauthorized Access**: Identify unusual access patterns
- **Investigation**: Review who accessed a record during a specific timeframe
- **Accountability**: Create a complete trail of all interactions with sensitive data

### Operational Benefits
- **Usage Analytics**: Understand how often records are accessed
- **Staff Monitoring**: Ensure appropriate access to patient information
- **Incident Response**: Quickly identify who had access during a security incident

## What Gets Logged

For each view, the system captures:
- ? **User ID**: Who accessed the record
- ? **Timestamp**: When the access occurred (UTC)
- ? **IP Address**: Where the access came from
- ? **User Agent**: Browser/device information
- ? **Entity Type**: "Patient"
- ? **Entity ID**: The specific patient ID
- ? **Action**: "Viewed"

What is **NOT** logged:
- ? Which fields were viewed
- ? How long the page was viewed
- ? Whether data was copied or exported
- ? Mouse movements or interactions

## Using the Filter

### Show All Events
By default, the audit history shows all events including views.

**Badge Display:**
- `5 data changes` (Add/Modify/Delete)
- `12 views` (Record access)

### Hide View Events
Toggle off "Show View Events" to focus only on data changes.

**Result:**
- Timeline shows only Add, Modify, and Delete actions
- View events are hidden but remain in the database
- Total count adjusts to show filtered results

## Visual Indicators

### View Events in Timeline
- **Icon**: Eye icon (???)
- **Badge Color**: Light blue (`bg-info`)
- **Opacity**: Slightly faded (70% opacity)
- **Background**: Light blue background (`#f0f8ff`)
- **Message**: "Patient record was accessed"

### Data Change Events
- **Added**: Green with plus icon
- **Modified**: Blue with pencil icon
- **Deleted**: Red with trash icon

## Performance Considerations

### Storage Impact
View tracking will increase audit log entries significantly:
- **Example**: A patient viewed 50 times creates 50 view logs
- **Data Changes**: Typically much fewer entries
- **Recommendation**: Consider archiving old view logs after 1-2 years

### Query Performance
- Views are indexed by EntityType, EntityId, and ChangedAt
- Filtering views in queries is efficient
- Use the ShowViews toggle to reduce UI load for heavily-viewed records

### Archival Strategy
```sql
-- Example: Archive view logs older than 2 years
INSERT INTO AuditLogsArchive
SELECT * FROM AuditLogs
WHERE Action = 'Viewed' AND ChangedAt < DATEADD(year, -2, GETDATE());

DELETE FROM AuditLogs
WHERE Action = 'Viewed' AND ChangedAt < DATEADD(year, -2, GETDATE());
```

## Adding View Tracking to Other Models

To add view tracking to other entity details pages:

### Step 1: Inject IAuditService
```csharp
public class DetailsModel : PageModel
{
    private readonly IAuditService _auditService;
    
    public DetailsModel(IAuditService auditService)
    {
        _auditService = auditService;
    }
}
```

### Step 2: Log the View
```csharp
public async Task<IActionResult> OnGetAsync(int? id)
{
    // ... load your entity ...
    
    // Log the view
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
    var userAgent = Request.Headers["User-Agent"].ToString();
    
    await _auditService.LogViewAsync("YourEntityType", id.Value, userId, ipAddress, userAgent);
    
    return Page();
}
```

### Step 3: Done!
Views are now tracked automatically. Update your audit history page to handle "Viewed" actions.

## Query Examples

### Get All Views for a Patient
```csharp
var views = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" && a.EntityId == patientId && a.Action == "Viewed")
    .Include(a => a.ChangedByUser)
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get Unique Users Who Viewed a Patient
```csharp
var viewers = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" && a.EntityId == patientId && a.Action == "Viewed")
    .Select(a => a.ChangedByUserId)
    .Distinct()
    .ToListAsync();
```

### Get View Count by User
```csharp
var viewsByUser = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" && a.EntityId == patientId && a.Action == "Viewed")
    .GroupBy(a => a.ChangedByUserId)
    .Select(g => new { UserId = g.Key, ViewCount = g.Count() })
    .ToListAsync();
```

### Find Patients Viewed Today
```csharp
var today = DateTime.UtcNow.Date;
var tomorrow = today.AddDays(1);

var patientsViewedToday = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
        && a.Action == "Viewed"
        && a.ChangedAt >= today 
        && a.ChangedAt < tomorrow)
    .Select(a => a.EntityId)
    .Distinct()
    .ToListAsync();
```

## Privacy and Legal Considerations

### What to Tell Users
Consider adding a notice to your application:
> "For security and compliance purposes, all access to patient records is logged and monitored."

### Data Retention
View logs may be considered part of the medical record in some jurisdictions:
- Check your local regulations
- Define a retention policy (typically 2-7 years)
- Include view logs in data exports for GDPR/HIPAA requests

### Access Control
- Ensure only authorized users can view audit history
- Consider role-based restrictions on audit log access
- Protect view logs from unauthorized deletion or modification

## Testing View Tracking

### Manual Test
1. Navigate to a patient's details page
2. Click "Audit History"
3. You should see a "Viewed" entry with:
   - Light blue badge
   - Eye icon
   - Your email address
   - Current timestamp
   - Your IP address

### Verify Filter
1. Toggle "Show View Events" off
2. View events should disappear
3. Data change count should remain the same
4. Toggle back on to see view events again

### Check Multiple Views
1. View the same patient 3 times
2. Check audit history
3. Should see 3 separate "Viewed" entries
4. Each with different timestamps

## Files Modified

1. **Services/IAuditService.cs**
   - Added `LogViewAsync` method to interface and implementation

2. **Pages/Patients/Details.cshtml.cs**
   - Added IAuditService injection
   - Added view logging in OnGetAsync

3. **Pages/Patients/AuditHistory.cshtml**
   - Added "Viewed" action badge and icon
   - Added filter toggle for view events
   - Added visual styling for view entries

4. **Pages/Patients/AuditHistory.cshtml.cs**
   - Added ShowViews property and filtering logic
   - Added ViewCount and DataChangeCount properties

5. **Documentation**
   - Updated all audit logging guides
   - Added view tracking sections
   - Added query examples

## Future Enhancements

Potential improvements:
- **Bulk View Reports**: Generate reports of access patterns
- **Suspicious Access Alerts**: Alert when unusual access patterns detected
- **Access Heatmap**: Visualize which records are accessed most
- **Time-based Reports**: Show views by hour/day/week
- **Department Tracking**: Group views by user department
- **Export Access Logs**: Download view logs for compliance audits
- **Real-time Notifications**: Alert record owners when their record is accessed

## Related Documentation

- **Main Guide**: `/Docs/Audit-Logging-Guide.md`
- **Quick Start**: `/Docs/Quick-Start-Audit-Logging.md`
- **System Reference**: `/Docs/Audit-System-Reference.md`
- **Fix Documentation**: `/Docs/Audit-Logging-Fix.md`
