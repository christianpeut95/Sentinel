# Permission Seeder Duplicate Key Fix

## Problem
When restarting the application after the initial permission seeder update, the following error occurred:

```
SqlException: Cannot insert duplicate key row in object 'dbo.Permissions' 
with unique index 'IX_Permissions_Module_Action'. 
The duplicate key value is (1, 0).
```

## Root Cause

The database has a **unique constraint** on the combination of `Module` and `Action` columns in the `Permissions` table:

```csharp
builder.Entity<Permission>()
    .HasIndex(p => new { p.Module, p.Action })
    .IsUnique();
```

The permission seeder was checking for existing permissions by the `Name` field:

```csharp
var existingKeys = existingPermissions.Select(p => p.Name).ToHashSet();
var permissionsToAdd = definedPermissions
    .Where(p => !existingKeys.Contains(p.Name))
    .ToList();
```

This check didn't align with the database constraint, causing attempts to insert permissions with duplicate `(Module, Action)` combinations even though their `Name` values might differ.

## Solution

Updated the seeder to check for existing permissions using the same criteria as the database constraint - the combination of `Module` and `Action`:

```csharp
// Get existing permissions from database
var existingPermissions = await context.Permissions.ToListAsync();
var existingCombinations = existingPermissions
    .Select(p => new { p.Module, p.Action })
    .ToHashSet();

// Find permissions that need to be added (check by Module+Action combination)
var permissionsToAdd = definedPermissions
    .Where(p => !existingCombinations.Contains(new { p.Module, p.Action }))
    .ToList();
```

### How It Works

1. **Fetch existing permissions** from the database
2. **Create a HashSet** of anonymous objects containing `Module` and `Action` properties
3. **Filter defined permissions** by checking if their `(Module, Action)` combination already exists
4. **Add only new permissions** that don't have existing `(Module, Action)` combinations

This ensures the check matches the database constraint exactly, preventing duplicate key violations.

## Technical Details

### Anonymous Type HashSet Comparison

The code uses anonymous types for comparison:

```csharp
var existingCombinations = existingPermissions
    .Select(p => new { p.Module, p.Action })
    .ToHashSet();

permissionsToAdd = definedPermissions
    .Where(p => !existingCombinations.Contains(new { p.Module, p.Action }))
    .ToList();
```

Anonymous types in C# have built-in structural equality, meaning two anonymous objects with the same property names and values are considered equal. This makes them perfect for this type of comparison.

### Database Constraint

The unique index ensures no two permissions can have the same module and action:

- ? `(Patient, View)` and `(Patient, Create)` - Allowed (different actions)
- ? `(Patient, View)` and `(Case, View)` - Allowed (different modules)
- ? `(Patient, View)` and `(Patient, View)` - **Not allowed** (duplicate combination)

## Files Changed

- `Extensions/PermissionSeeder.cs` - Updated duplicate detection logic

## Testing

After this fix:

1. **Initial seed** - Creates all permissions successfully
2. **Subsequent restarts** - Detects existing permissions correctly and skips them
3. **New permission additions** - Only adds permissions with new `(Module, Action)` combinations

## Related Documentation

- See `Docs/Lookup-Permissions-Fix.md` for the complete permission system fixes
