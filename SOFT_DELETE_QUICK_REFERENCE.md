# Quick Reference: Soft Delete Usage

## For Developers

### Using Soft Delete in Code

```csharp
// Soft delete an entity
var patient = await _context.Patients.FindAsync(id);
await _context.SoftDeleteAsync(patient);

// Restore a soft-deleted entity
var deletedCase = await _context.OnlyDeleted<Case>()
    .FirstOrDefaultAsync(c => c.Id == id);
await _context.RestoreAsync(deletedCase);

// Query including deleted records
var allPatients = await _context.IncludeDeleted<Patient>()
    .ToListAsync();

// Query only deleted records
var deletedNotes = await _context.OnlyDeleted<Note>()
    .Where(n => n.CreatedAt > DateTime.Now.AddDays(-30))
    .ToListAsync();
```

### Adding Soft Delete to New Models

1. Implement `ISoftDeletable`:
```csharp
public class YourModel : ISoftDeletable
{
    public Guid Id { get; set; }
    // ... other properties ...
    
    // Soft Delete Properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
}
```

2. Add global query filter in `ApplicationDbContext.OnModelCreating`:
```csharp
builder.Entity<YourModel>().HasQueryFilter(m => !m.IsDeleted);
```

3. Use `SoftDeleteAsync()` in your delete handlers:
```csharp
public async Task<IActionResult> OnPostDeleteAsync(Guid id)
{
    var entity = await _context.YourModels.FindAsync(id);
    if (entity != null)
    {
        await _context.SoftDeleteAsync(entity);
        TempData["SuccessMessage"] = "Record deleted successfully.";
    }
    return RedirectToPage("./Index");
}
```

## For Administrators

### Accessing Deleted Records

1. Navigate to **Settings** from the main menu
2. Scroll down to the **Data Management** section
3. Click on **Deleted Records**
4. Use the tabs to view different types of deleted records:
   - **Patients** - Deleted patient records
   - **Cases** - Deleted case records
   - **Lab Results** - Deleted lab results
   - **Notes** - Deleted notes

### Restoring Deleted Records

1. Find the record you want to restore
2. Click the **Restore** button
3. Confirm the action
4. The record will be restored and appear in normal queries

### Permanently Deleting Records

?? **Warning: This action cannot be undone!**

1. Find the record you want to permanently delete
2. Click the **Delete Forever** button
3. Confirm the action in the warning dialog
4. The record will be permanently removed from the database

### Best Practices

1. **Review Before Permanent Delete** - Always review the record and its related data before permanently deleting
2. **Regular Cleanup** - Periodically review old deleted records and permanently delete them if no longer needed
3. **Backup First** - Take a database backup before performing bulk permanent deletions
4. **Check Dependencies** - Consider whether restoring a parent record requires restoring child records too

## Permissions Required

| Action | Required Permission |
|--------|-------------------|
| Delete Case | `Permission.Case.Delete` |
| Delete Patient | `Permission.Patient.Delete` |
| Delete Lab Result | Inherited from Case permissions |
| Delete Note | Inherited from parent entity permissions |
| View Deleted Records | `Permission.Settings.ManageCustomFields` |
| Restore Records | `Permission.Settings.ManageCustomFields` |
| Permanent Delete | `Permission.Settings.ManageCustomFields` |

## Troubleshooting

### "I can't see deleted records in the normal views"
This is by design. Soft-deleted records are automatically excluded from normal queries. Use the Deleted Records admin page to view them.

### "I accidentally permanently deleted a record"
Unfortunately, permanent deletion cannot be undone. You'll need to restore from a database backup if available.

### "How do I delete multiple records at once?"
Currently, records must be deleted individually. Bulk operations may be added in a future update.

### "Can I search or filter deleted records?"
The current implementation shows the most recent 100 deleted records per type. Advanced search/filter functionality may be added in a future update.

## API Reference

### ApplicationDbContext Methods

#### `SoftDeleteAsync<T>(T entity)`
- **Purpose**: Mark an entity as deleted without removing it from the database
- **Parameters**: Entity implementing `ISoftDeletable`
- **Returns**: `Task<bool>` - True if successful
- **Example**:
  ```csharp
  await _context.SoftDeleteAsync(patient);
  ```

#### `RestoreAsync<T>(T entity)`
- **Purpose**: Restore a soft-deleted entity
- **Parameters**: Entity implementing `ISoftDeletable`
- **Returns**: `Task<bool>` - True if successful
- **Example**:
  ```csharp
  await _context.RestoreAsync(deletedCase);
  ```

#### `IncludeDeleted<T>()`
- **Purpose**: Get a queryable that includes soft-deleted records
- **Returns**: `IQueryable<T>`
- **Example**:
  ```csharp
  var allCases = await _context.IncludeDeleted<Case>().ToListAsync();
  ```

#### `OnlyDeleted<T>()`
- **Purpose**: Get a queryable that only includes soft-deleted records
- **Returns**: `IQueryable<T>`
- **Example**:
  ```csharp
  var deletedRecords = await _context.OnlyDeleted<Patient>()
      .OrderByDescending(p => p.DeletedAt)
      .ToListAsync();
  ```

## Support

For questions or issues with soft delete functionality:
1. Check the technical documentation: `SOFT_DELETE_IMPLEMENTATION.md`
2. Review the completion summary: `SOFT_DELETE_COMPLETION_SUMMARY.md`
3. Contact your system administrator
