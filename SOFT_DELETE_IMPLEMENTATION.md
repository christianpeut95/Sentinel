# Soft Delete Implementation

This document describes the soft delete functionality implemented for Cases, Patients, Lab Results, and Notes.

## Overview

Soft delete allows records to be marked as deleted without actually removing them from the database. This provides:
- **Data retention** for compliance and audit purposes
- **Recovery capability** for accidentally deleted records
- **Audit trail** of deletions with timestamps and user information

## Implementation Details

### 1. ISoftDeletable Interface

Located in `Models\ISoftDeletable.cs`, this interface defines the required properties for soft delete:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedByUserId { get; set; }
}
```

### 2. Models Supporting Soft Delete

The following models implement `ISoftDeletable`:
- **Patient** - `Models\Patient.cs`
- **Case** - `Models\Case.cs`
- **LabResult** - `Models\LabResult.cs`
- **Note** - `Models\Note.cs`

Each model includes:
- `IsDeleted` - Boolean flag indicating soft delete status
- `DeletedAt` - Timestamp of deletion
- `DeletedByUserId` - User ID who performed the deletion

### 3. Database Configuration

In `Data\ApplicationDbContext.cs`, global query filters automatically exclude soft-deleted records:

```csharp
builder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
builder.Entity<Case>().HasQueryFilter(c => !c.IsDeleted);
builder.Entity<LabResult>().HasQueryFilter(lr => !lr.IsDeleted);
builder.Entity<Note>().HasQueryFilter(n => !n.IsDeleted);
```

### 4. Soft Delete Helper Methods

The `ApplicationDbContext` provides helper methods for soft delete operations:

#### SoftDeleteAsync
```csharp
public async Task<bool> SoftDeleteAsync<T>(T entity) where T : class, ISoftDeletable
```
Marks an entity as deleted, sets the deletion timestamp, and records the user who deleted it.

#### RestoreAsync
```csharp
public async Task<bool> RestoreAsync<T>(T entity) where T : class, ISoftDeletable
```
Restores a soft-deleted entity by clearing the deletion flags.

#### IncludeDeleted
```csharp
public IQueryable<T> IncludeDeleted<T>() where T : class, ISoftDeletable
```
Returns a query that includes soft-deleted records (bypasses query filters).

#### OnlyDeleted
```csharp
public IQueryable<T> OnlyDeleted<T>() where T : class, ISoftDeletable
```
Returns a query that only includes soft-deleted records.

### 5. Authorization

Delete operations are protected by permission-based authorization:

- **Cases**: `Permission.Case.Delete`
- **Patients**: `Permission.Patient.Delete`

These permissions are enforced using the `[Authorize]` attribute on the Delete page models:

```csharp
[Authorize(Policy = "Permission.Case.Delete")]
public class DeleteModel : PageModel
```

### 6. Delete Pages Updated

The following delete pages have been updated to use soft delete:

#### Pages\Cases\Delete.cshtml.cs
- Added authorization: `[Authorize(Policy = "Permission.Case.Delete")]`
- Changed from `_context.Cases.Remove(Case)` to `await _context.SoftDeleteAsync(Case)`
- Maintains audit logging

#### Pages\Patients\Delete.cshtml.cs
- Added authorization: `[Authorize(Policy = "Permission.Patient.Delete")]`
- Changed from `_context.Patients.Remove(patient)` to `await _context.SoftDeleteAsync(patient)`
- Maintains error handling and success messages

### 7. Inline Delete Handlers

For Lab Results and Notes, delete handlers are available on the detail pages:

#### Pages\Cases\Details.cshtml.cs
- `OnPostDeleteLabResultAsync` - Soft deletes a lab result
- `OnPostDeleteNoteAsync` - Soft deletes a note linked to a case

#### Pages\Patients\Details.cshtml.cs
- `OnPostDeleteNoteAsync` - Soft deletes a note linked to a patient

All handlers:
- Use `await _context.SoftDeleteAsync(entity)`
- Log audit changes
- Show success/error messages via TempData
- Redirect back to the details page

## Usage Examples

### Soft Delete a Case
```csharp
var caseEntity = await _context.Cases.FindAsync(id);
await _context.SoftDeleteAsync(caseEntity);
```

### Restore a Deleted Patient
```csharp
var patient = await _context.IncludeDeleted<Patient>().FirstOrDefaultAsync(p => p.Id == id);
await _context.RestoreAsync(patient);
```

### Query Only Deleted Records
```csharp
var deletedCases = await _context.OnlyDeleted<Case>().ToListAsync();
```

### Include Deleted Records in a Query
```csharp
var allPatients = await _context.IncludeDeleted<Patient>().ToListAsync();
```

## Permission Requirements

Users must have the appropriate delete permission to perform delete operations:

1. **Case.Delete** - Required to delete cases
2. **Patient.Delete** - Required to delete patients

These permissions are managed through the application's role-based permission system.

## Audit Trail

All delete operations are automatically logged through the existing audit system:
- Entity type and ID
- User who performed the deletion
- Timestamp
- IP address
- Original values preserved in audit logs

## Database Migration

No new migration is required as the soft delete infrastructure (ISoftDeletable interface, model properties, query filters, and helper methods) was already in place. The changes only updated the application logic to use soft delete instead of hard delete.

## Benefits

1. **Data Recovery** - Accidentally deleted records can be restored
2. **Compliance** - Maintains complete data history for regulatory requirements
3. **Audit Trail** - Full tracking of who deleted what and when
4. **Referential Integrity** - Related records remain intact
5. **Performance** - Deleted records still benefit from indexes
6. **Security** - Only authorized users can delete records

## Future Enhancements

Potential improvements for the soft delete system:

1. **Admin Interface** - Create a page to view and restore deleted records
2. **Permanent Delete** - Add ability for admins to permanently delete old soft-deleted records
3. **Cascade Soft Delete** - Automatically soft delete related child records
4. **Delete Warnings** - Show warnings when deleting records with related data
5. **Bulk Operations** - Support for soft deleting multiple records at once
6. **Scheduled Cleanup** - Background job to permanently delete old soft-deleted records

## Testing

To test the soft delete functionality:

1. Create test records (Patient, Case, Lab Result, Note)
2. Delete them using the delete pages/handlers
3. Verify they no longer appear in normal queries
4. Use `IncludeDeleted<T>()` to verify they still exist in the database
5. Check audit logs for deletion records
6. Verify unauthorized users cannot access delete functionality
