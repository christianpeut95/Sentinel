# Grant All Permissions Fix

## Problem
When clicking "Grant All Permissions" in the role permissions page, a `DbUpdateException` was thrown with the error:
```
An error occurred while saving the entity changes. See the inner exception for details.
```

## Root Cause
The `OnPostGrantAllAsync` method was calling `_permissionService.GrantPermissionToRoleAsync()` in a loop, which saved changes to the database after **each permission**. This caused issues because:

1. **Duplicate Key Violations**: The `RolePermission` table has a composite primary key on `(RoleId, PermissionId)`. If a permission already existed and the method tried to add it again, it would violate the unique constraint.

2. **Multiple SaveChanges Calls**: Calling `SaveChangesAsync()` multiple times in a loop is inefficient and can cause race conditions or tracking issues in Entity Framework.

3. **Context Tracking Issues**: The EF Core change tracker might not have been properly updated between iterations, leading to attempts to insert duplicate records.

## Solution
Changed the bulk operations to:

1. **Query existing permissions first** to avoid duplicates
2. **Add only new permissions** to the context
3. **Update existing permissions** to ensure `IsGranted = true`
4. **Save all changes in a single batch** at the end

### Methods Fixed

#### 1. `OnPostGrantAllAsync` - Grant all permissions to a role
**Before:**
```csharp
foreach (var permission in allPermissions)
{
    await _permissionService.GrantPermissionToRoleAsync(roleId, permission.Id);
    // ?? SaveChanges called inside the service method on each iteration
}
```

**After:**
```csharp
var existingPermissions = await _context.RolePermissions
    .Where(rp => rp.RoleId == roleId)
    .Select(rp => rp.PermissionId)
    .ToHashSetAsync();

foreach (var permission in allPermissions)
{
    if (!existingPermissions.Contains(permission.Id))
    {
        _context.RolePermissions.Add(new RolePermission { ... });
    }
    else
    {
        // Update existing to ensure IsGranted = true
    }
}

await _context.SaveChangesAsync(); // ? Single save at the end
```

#### 2. `OnPostGrantModuleAsync` - Grant all permissions in a module to a role
Applied the same pattern as above but filtered by module.

#### 3. `OnPostRevokeAllAsync` - Revoke all permissions from a role
**Before:**
```csharp
foreach (var permission in grantedPermissions)
{
    await _permissionService.RevokePermissionFromRoleAsync(roleId, permission.Id);
    // ?? SaveChanges called inside the service method on each iteration
}
```

**After:**
```csharp
var grantedPermissions = await _context.RolePermissions
    .Where(rp => rp.RoleId == roleId)
    .ToListAsync();

_context.RolePermissions.RemoveRange(grantedPermissions);
await _context.SaveChangesAsync(); // ? Single save at the end
```

#### 4. `OnPostRevokeModuleAsync` - Revoke all permissions in a module from a role
Applied the same pattern as above but filtered by module.

## Benefits

1. **No More DbUpdateException**: Properly handles existing permissions
2. **Better Performance**: Single database transaction instead of multiple
3. **Atomic Operations**: All changes succeed or fail together
4. **Reduced Database Round-trips**: More efficient bulk operations

## Testing
After applying this fix:
1. Stop the debugger and restart the application to apply the changes
2. Navigate to a role's permissions page
3. Click "Grant All Permissions" - should work without errors
4. Click "Revoke All Permissions" - should work without errors
5. Test module-level grant/revoke operations

## Files Modified
- `Surveillance-MVP\Pages\Settings\Roles\Permissions.cshtml.cs`
