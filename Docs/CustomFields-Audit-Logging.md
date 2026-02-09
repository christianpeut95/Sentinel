# Custom Field Audit Logging

## Overview
Custom field changes are now automatically logged in the audit system. When a user creates or updates custom field values for a patient, each change is recorded with the old and new values.

## What's Logged

### Field Change Information
- **Field Name**: The custom field label (prefixed with "Custom Field:")
- **Old Value**: The previous value (or "(empty)" if no value existed)
- **New Value**: The updated value (or "(empty)" if cleared)
- **Changed By**: The user who made the change
- **Changed At**: Timestamp of the change
- **IP Address**: The IP address of the user making the change

### Format Examples

**Text/Number/Email/Phone Fields:**
```
Field: Custom Field: Smoking Status
Old Value: Non-smoker
New Value: Former smoker
```

**Date Fields:**
```
Field: Custom Field: Last Vaccination Date
Old Value: (empty)
New Value: 15 Jan 2024
```

**Checkbox Fields:**
```
Field: Custom Field: Has Travel History
Old Value: No
New Value: Yes
```

**Dropdown Fields:**
```
Field: Custom Field: Risk Category
Old Value: Low Risk
New Value: High Risk
```

## How It Works

### 1. Change Detection
When custom fields are saved, the service:
1. Retrieves the old values before saving
2. Saves the new values
3. Compares old vs new for each field
4. Logs only the fields that actually changed

### 2. Audit Log Creation
For each changed field, an audit log entry is created with:
- `EntityType`: "Patient"
- `EntityId`: The patient's ID
- `Action`: "Updated"
- `FieldName`: "Custom Field: [Field Label]"
- `OldValue`: Previous value or "(empty)"
- `NewValue`: New value or "(empty)"

### 3. User Tracking
The audit log captures:
- User ID from the authenticated session
- IP address from the HTTP request
- Timestamp in UTC

## Viewing Custom Field Audit History

### In Patient Audit History Page
1. Navigate to a patient's details page
2. Click "Audit History" button
3. View all changes including custom fields
4. Custom field changes show as:
   - **Action**: Updated
   - **Field**: Custom Field: [Field Label]
   - **Old ? New**: Shows the value change

### Audit Log Appearance
```
???????????????????????????????????????????????????????????????
? Changed: 15 Jan 2024 14:30                                  ?
? By: john.smith@health.gov.au                                ?
? IP: 192.168.1.100                                          ?
?                                                             ?
? Action: Updated                                             ?
? Field: Custom Field: Smoking Status                         ?
? Old Value: Non-smoker                                       ?
? New Value: Former smoker                                    ?
???????????????????????????????????????????????????????????????
```

## Technical Implementation

### IAuditService Enhancement
Added new method:
```csharp
Task LogCustomFieldChangeAsync(
    int patientId, 
    string fieldLabel, 
    string? oldValue, 
    string? newValue, 
    string? userId, 
    string? ipAddress
);
```

### PatientCustomFieldService Changes
Updated `SavePatientFieldValuesAsync` to:
1. Accept `userId` and `ipAddress` parameters
2. Retrieve old values using `GetPatientFieldDisplayValuesAsync()`
3. Track new display values for each field type
4. Log changes via `IAuditService.LogCustomFieldChangeAsync()`

### Display Value Formatting
Different field types are formatted for audit display:

| Field Type | Format Example |
|------------|----------------|
| Text/TextArea/Email/Phone | Raw value |
| Number | Numeric string |
| Date | "dd MMM yyyy" format |
| Checkbox | "Yes" or "No" |
| Dropdown | Display text from lookup table |

## Benefits

### 1. Compliance and Accountability
- Complete audit trail of all patient data changes
- Regulatory compliance (HIPAA, GDPR, etc.)
- Ability to track who changed what and when

### 2. Data Quality
- Identify patterns of incorrect data entry
- Track corrections and updates
- Review history when investigating data issues

### 3. Security
- Detect unauthorized changes
- Monitor suspicious activity
- Support forensic investigations

### 4. Clinical Workflow
- Review patient care timeline
- Understand data evolution
- Support clinical decision-making with history

## Examples of Audit Logs

### Example 1: Initial Data Entry (Create)
When a patient is first created with custom fields:
```
Custom Field: Smoking Status
Old Value: (empty)
New Value: Non-smoker

Custom Field: Has Allergies
Old Value: (empty)
New Value: Yes

Custom Field: Risk Level
Old Value: (empty)
New Value: Low Risk
```

### Example 2: Update Existing Data (Edit)
When custom fields are modified:
```
Custom Field: Smoking Status
Old Value: Non-smoker
New Value: Former smoker

Custom Field: Last Assessment Date
Old Value: 10 Jan 2024
New Value: 15 Jan 2024
```

### Example 3: Clearing Data
When a custom field value is removed:
```
Custom Field: Emergency Contact
Old Value: John Doe (555-1234)
New Value: (empty)
```

### Example 4: Multiple Changes in One Edit
All changes in a single edit session are logged:
```
[2024-01-15 14:30:00]
- Custom Field: Smoking Status: Non-smoker ? Former smoker
- Custom Field: Has Travel History: No ? Yes
- Custom Field: Risk Level: Low Risk ? Medium Risk
```

## Audit Log Query Examples

### View All Custom Field Changes for a Patient
```csharp
var customFieldAudits = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
             && a.EntityId == patientId
             && a.FieldName.StartsWith("Custom Field:"))
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### View Changes to a Specific Custom Field
```csharp
var fieldAudits = await _context.AuditLogs
    .Where(a => a.EntityType == "Patient" 
             && a.EntityId == patientId
             && a.FieldName == "Custom Field: Smoking Status")
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

### View All Custom Field Changes by a User
```csharp
var userCustomFieldChanges = await _context.AuditLogs
    .Where(a => a.ChangedByUserId == userId
             && a.FieldName.StartsWith("Custom Field:"))
    .OrderByDescending(a => a.ChangedAt)
    .ToListAsync();
```

## Performance Considerations

### Optimization
- Only fields that actually change are logged
- Audit logs written asynchronously
- No impact on save performance
- Bulk operations possible if needed

### Database Impact
- One audit log row per changed field
- Indexed on EntityType, EntityId, and ChangedAt
- Regular archival recommended for long-term storage

## Configuration

### Disabling Custom Field Auditing
If needed, auditing can be disabled by:
1. Modifying `SavePatientFieldValuesAsync` to skip audit calls
2. Using a configuration flag to conditionally log

### Custom Audit Rules
Future enhancements could include:
- Audit only specific custom fields
- Different audit levels (summary vs detailed)
- Retention policies per field type
- Anonymization for sensitive fields

## Troubleshooting

### Issue: Audit Logs Not Appearing
**Check:**
1. Ensure `IAuditService` is properly injected into `PatientCustomFieldService`
2. Verify database has `AuditLogs` table
3. Check that userId and ipAddress are being passed correctly
4. Look for exceptions in SaveChangesAsync

### Issue: Old Values Showing as "(empty)" When They Shouldn't
**Check:**
1. Verify `GetPatientFieldDisplayValuesAsync()` is working correctly
2. Check that field definitions are properly loaded
3. Ensure lookup table relationships are intact for dropdowns

### Issue: Wrong User Shown in Audit Log
**Check:**
1. Verify `User.FindFirstValue(ClaimTypes.NameIdentifier)` returns correct user
2. Check authentication is working properly
3. Ensure user context is available in Razor Page

## Best Practices

### 1. Regular Review
- Periodically review audit logs for data quality
- Monitor for unusual patterns
- Train staff on proper data entry

### 2. Retention Policy
- Define how long to keep audit logs
- Archive old logs to separate storage
- Comply with regulatory requirements

### 3. Access Control
- Limit who can view audit logs
- Consider adding audit log viewer role
- Protect sensitive audit data

### 4. Integration
- Consider exporting audit logs to SIEM
- Integrate with alerting systems
- Generate compliance reports

## Future Enhancements

Potential improvements:
1. **Batch Change Detection**: Group multiple field changes into single session
2. **Change Reasons**: Allow users to add notes explaining changes
3. **Rollback Functionality**: Restore previous values from audit history
4. **Advanced Filtering**: Filter audit logs by field, user, date range
5. **Export Capability**: Export audit logs to CSV/Excel
6. **Real-time Notifications**: Alert on specific field changes
7. **Compliance Reports**: Generate audit reports for regulators

## Summary

Custom field audit logging provides:
- ? Complete change tracking
- ? Regulatory compliance
- ? Data quality assurance
- ? Security monitoring
- ? Clinical context
- ? Forensic capabilities

All custom field changes are now automatically logged alongside standard patient data changes, providing a complete audit trail for patient records.
