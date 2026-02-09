# Complete Patient Field Audit Logging

## Overview
All patient field changes are now comprehensively logged in the audit system, including:
- ? Standard patient fields (demographics, contact info, address)
- ? Custom field changes
- ? Patient creation events
- ? Old and new values for all changes

## What's Logged

### Patient Creation
When a new patient is created:
```
Action: Modified
Field: Patient Record
Old Value: (empty)
New Value: Created
```

### Standard Patient Field Changes
When any patient field is modified, the system logs:

| Category | Fields Logged |
|----------|---------------|
| **Demographics** | Given Name, Family Name, Date of Birth, Sex at Birth, Gender |
| **Cultural Background** | Country of Birth, Language Spoken at Home, Ethnicity, ATSI Status |
| **Employment** | Occupation |
| **Contact** | Home Phone, Mobile Phone, Email Address |
| **Address** | Address Line, City, State, Postal Code |

### Custom Field Changes
All custom field changes are logged with:
- Field label (prefixed with "Custom Field:")
- Old and new values (or "(empty)")
- User who made the change
- Timestamp and IP address

## Example Audit Log Entries

### Patient Record Created
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Patient Record                          ?
? Old Value: (empty)                             ?
? New Value: Created                             ?
?                                                ?
? Changed By: john.smith@health.gov.au           ?
? Changed At: 15 Jan 2024 14:30                 ?
? IP: 192.168.1.100                              ?
??????????????????????????????????????????????????
```

### Name Change
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Given Name                              ?
? Old Value: John                                ?
? New Value: Jonathan                            ?
?                                                ?
? Changed By: sarah.jones@health.gov.au          ?
? Changed At: 15 Jan 2024 15:45                 ?
? IP: 192.168.1.105                              ?
??????????????????????????????????????????????????
```

### Date of Birth Change
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Date of Birth                           ?
? Old Value: 15 Mar 1985                         ?
? New Value: 15 Apr 1985                         ?
?                                                ?
? Changed By: admin@health.gov.au                ?
? Changed At: 16 Jan 2024 09:15                 ?
? IP: 192.168.1.50                               ?
??????????????????????????????????????????????????
```

### Lookup Field Change (with Display Names)
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Country of Birth                        ?
? Old Value: Australia                           ?
? New Value: New Zealand                         ?
?                                                ?
? Changed By: jane.doe@health.gov.au             ?
? Changed At: 16 Jan 2024 10:30                 ?
? IP: 192.168.1.110                              ?
??????????????????????????????????????????????????
```

### Address Change
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Address Line                            ?
? Old Value: 123 Main St                         ?
? New Value: 456 High St                         ?
?                                                ?
? Changed By: admin@health.gov.au                ?
? Changed At: 16 Jan 2024 11:00                 ?
? IP: 192.168.1.50                               ?
??????????????????????????????????????????????????

??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: City                                    ?
? Old Value: Sydney                              ?
? New Value: Melbourne                           ?
?                                                ?
? Changed By: admin@health.gov.au                ?
? Changed At: 16 Jan 2024 11:00                 ?
? IP: 192.168.1.50                               ?
??????????????????????????????????????????????????
```

### Custom Field Change
```
??????????????????????????????????????????????????
? Action: Modified                                ?
? Field: Custom Field: Smoking Status            ?
? Old Value: Non-smoker                          ?
? New Value: Former smoker                       ?
?                                                ?
? Changed By: john.smith@health.gov.au           ?
? Changed At: 16 Jan 2024 14:20                 ?
? IP: 192.168.1.100                              ?
??????????????????????????????????????????????????
```

## Technical Implementation

### 1. Enhanced IAuditService
Added generic `LogChangeAsync` method:
```csharp
Task LogChangeAsync(
    string entityType, 
    int entityId, 
    string fieldName, 
    string? oldValue, 
    string? newValue, 
    string? userId, 
    string? ipAddress
);
```

This method:
- Only logs if values actually changed (oldValue != newValue)
- Converts null values to "(empty)" for display
- Records user ID and IP address
- Timestamps with UTC

### 2. Patient Edit Page
The Edit page now:
1. Retrieves original patient values before changes
2. Applies user's changes
3. Saves to database
4. Compares old vs new for each field
5. Logs each changed field via `LogPatientChangesAsync()`

### 3. LogPatientChangesAsync Method
This helper method:
- Compares each patient field individually
- For lookup fields (Sex, Gender, Country, etc.):
  - Retrieves display names from related tables
  - Logs human-readable values
- For date fields:
  - Formats as "dd MMM yyyy"
- For text fields:
  - Logs raw string values

### 4. Patient Create Page
When a patient is created:
- Logs a "Patient Record Created" entry
- Custom fields are logged as they're added
- No old values (all show as "(empty)" ? "value")

## Viewing Audit History

### Access Audit Logs
1. Navigate to patient Details page
2. Click "Audit History" button
3. View complete change history

### Audit History Display
The audit history page shows:
- **Timeline view** - Changes grouped by date
- **Color-coded actions** - Modified (blue), Added (green), Deleted (red), Viewed (info)
- **Side-by-side comparison** - Old value (yellow) vs New value (green)
- **User attribution** - Who made each change
- **Timestamps** - When changes occurred
- **IP addresses** - Where changes came from
- **Filter options** - Toggle view events on/off

### Example Timeline View
```
Wednesday, 15 January 2024
?????????????????????????????????????????????

14:30:15  [Modified] Patient Record
          ?????????????????????????????
          ? Old: (empty)? New: Created ?
          ?????????????????????????????
          Created by: john.smith@health.gov.au
          IP: 192.168.1.100

15:45:20  [Modified] Given Name
          ??????????????????????????????
          ? Old: John   ? New: Jonathan ?
          ??????????????????????????????
          Changed by: sarah.jones@health.gov.au
          IP: 192.168.1.105

15:45:22  [Modified] Mobile Phone
          ?????????????????????????????????
          ? Old: (empty)  ? New: 0400123456?
          ?????????????????????????????????
          Changed by: sarah.jones@health.gov.au
          IP: 192.168.1.105
```

## Compliance and Security

### Data Protection
- All changes are permanently logged
- Cannot be deleted or modified
- Provides complete audit trail
- Supports compliance requirements

### Privacy Considerations
- IP addresses are logged for security
- User IDs track who made changes
- Audit logs themselves should be access-controlled
- Consider data retention policies

### Regulatory Compliance
Supports compliance with:
- **HIPAA** (USA) - Complete audit trail required
- **GDPR** (EU) - Data modification tracking
- **Privacy Act** (Australia) - Healthcare data handling
- **NIST Standards** - Audit logging requirements

## Performance Considerations

### Efficient Logging
- Only changed fields are logged (not all fields on every save)
- Lookup display names retrieved in batch where possible
- Asynchronous SaveChangesAsync calls
- Database indexed on EntityType, EntityId, and ChangedAt

### Database Impact
- One audit log row per changed field
- Typical edit: 2-5 audit entries
- Large edit with many fields: 10-20 entries
- Custom fields: Additional entries as needed

### Scalability
- Audit logs can be archived periodically
- Consider partitioning by date
- Index strategy optimized for common queries

## Query Examples

### Get All Changes for a Patient
```csharp
var auditLogs = await _auditService.GetAuditLogsAsync("Patient", patientId);
```

### Get Changes to Specific Field
```csharp
var nameChanges = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
             && a.EntityId == patientId 
             && a.FieldName == "Given Name")
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### Get Changes by User
```csharp
var userChanges = await _auditService.GetAuditLogsByUserAsync(userId, 100);
```

### Get Changes in Date Range
```csharp
var recentChanges = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
             && a.EntityId == patientId
             && a.ChangedAt >= startDate 
             && a.ChangedAt <= endDate)
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

## Field-Specific Handling

### Text Fields
Logged as-is:
- Given Name, Family Name
- Home Phone, Mobile Phone, Email Address
- Address Line, City, State, Postal Code

### Date Fields
Formatted for display:
- Date of Birth: "dd MMM yyyy" format
- Example: "15 Mar 1985"

### Lookup Fields
Display names shown instead of IDs:
- Sex at Birth: "Male", "Female", etc.
- Gender: Display name from Gender table
- Country of Birth: Country name
- Language Spoken at Home: Language name
- Ethnicity: Ethnicity name
- ATSI Status: Status description
- Occupation: Occupation name

### Null/Empty Values
Shown as "(empty)" in audit logs:
- Provides clear indication of no value
- Distinguishes from empty string
- Consistent display across all field types

## Best Practices

### 1. Review Regularly
- Monitor audit logs for data quality issues
- Identify patterns of incorrect entry
- Train users on proper procedures

### 2. Retention Policy
- Define how long to keep audit logs
- Archive old logs to separate storage
- Comply with regulatory requirements

### 3. Access Control
- Limit who can view audit logs
- Consider separate "Audit Reviewer" role
- Protect sensitive audit information

### 4. Investigate Anomalies
- Unusual edit patterns
- Bulk changes from single user
- Changes outside normal business hours
- Multiple rapid edits to same record

### 5. Integration
- Export to SIEM systems
- Generate compliance reports
- Alert on suspicious activity
- Track data quality metrics

## Troubleshooting

### Issue: No audit logs showing for edits
**Check:**
1. Verify IAuditService is injected into EditModel
2. Ensure LogPatientChangesAsync is being called
3. Check database for AuditLogs table
4. Verify SaveChangesAsync is called after logging

### Issue: Old values showing as "(empty)" when they shouldn't
**Check:**
1. originalPatient is loaded before changes
2. Navigation properties are eagerly loaded with Include()
3. Lookup fields are being resolved correctly

### Issue: Too many audit entries
**Possible causes:**
1. Form is posting all fields even unchanged ones
2. EF Core detecting false changes
3. Multiple SaveChangesAsync calls

**Solution:**
- The system only logs if oldValue != newValue
- This prevents duplicate/unnecessary entries

### Issue: Lookup field names not showing
**Check:**
1. Foreign key relationships are properly configured
2. FindAsync is successfully retrieving lookup values
3. Navigation properties exist on Patient model

## Summary

### Complete Audit Trail ?
- **Patient Creation**: Logged with "Patient Record Created"
- **Field Changes**: Every modified field logged with old ? new
- **Custom Fields**: All custom data changes tracked
- **User Attribution**: Who, when, where for every change
- **Display-Friendly**: Dates formatted, lookup names shown, nulls as "(empty)"

### Benefits
- ? **Compliance**: Meets regulatory audit requirements
- ? **Security**: Track unauthorized or suspicious changes
- ? **Data Quality**: Identify and correct data entry issues
- ? **Accountability**: Complete user attribution
- ? **Forensics**: Investigate data discrepancies
- ? **Clinical Context**: Understand patient record evolution

### What's Logged
- ? 15 standard patient fields
- ? All custom field changes
- ? Patient creation events
- ? Lookup display names (not IDs)
- ? Formatted dates
- ? User IDs and IP addresses

All patient data changes now have complete before/after audit trails! ??
