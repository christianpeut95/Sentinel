# Lookup Permissions Fix

## Problem
The lookup-related permissions were not appearing on the permissions management pages, preventing administrators from assigning permissions to manage system lookups (countries, languages, etc.) and custom lookup tables.

## Root Causes

### 1. Missing Authorization Service Registration
The custom permission authorization system was not registered in `Program.cs`. Even though the code existed, it wasn't being used:
- `PermissionPolicyProvider` - Creates authorization policies dynamically
- `PermissionHandler` - Evaluates permission requirements

### 2. Permission Seeder Not Adding New Permissions
The `PermissionSeeder` would skip adding any permissions if it found existing permissions in the database. This meant new permission types (like `ManageSystemLookups`, `ManageCustomLookups`, `ManageCustomFields`) weren't being added to existing installations.

### 3. Permission Display Names
Permission action names were displayed as raw enum values (e.g., "ManageSystemLookups") instead of user-friendly names (e.g., "Manage System Lookups").

## Fixes Applied

### 1. Registered Authorization Services in Program.cs
```csharp
// Authorization with custom permission policy provider
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
```

### 2. Updated PermissionSeeder to Add New Permissions
Changed the seeder to check for missing permissions and add them, rather than skipping entirely if any permissions exist. **Important:** The check is done by Module+Action combination, which matches the unique constraint in the database:

```csharp
// Get all defined permission combinations
var definedPermissions = GetDefinedPermissions();

// Get existing permissions from database
var existingPermissions = await context.Permissions.ToListAsync();
var existingCombinations = existingPermissions
    .Select(p => new { p.Module, p.Action })
    .ToHashSet();

// Find permissions that need to be added (check by Module+Action combination)
var permissionsToAdd = definedPermissions
    .Where(p => !existingCombinations.Contains(new { p.Module, p.Action }))
    .ToList();

if (permissionsToAdd.Any())
{
    await context.Permissions.AddRangeAsync(permissionsToAdd);
    await context.SaveChangesAsync();
}
```

This approach respects the database's unique index on `(Module, Action)` and prevents duplicate key violations.

### 3. Added Friendly Display Names
Added a helper method to both Role and User permission pages to display human-readable names:

```csharp
private string GetFriendlyActionName(PermissionAction action)
{
    return action switch
    {
        PermissionAction.ManageCustomFields => "Manage Custom Fields",
        PermissionAction.ManageCustomLookups => "Manage Custom Lookups",
        PermissionAction.ManageSystemLookups => "Manage System Lookups",
        // ... other mappings
    };
}
```

### 4. Added Case Module Icon
Added the missing icon for the Case module in permission displays.

## Available Lookup Permissions

After restarting the application, the following permissions will now be available under the **Settings** module:

1. **View** - View settings and configuration
2. **Edit** - Edit settings and configuration
3. **Create** - Create new lookup tables and configurations
4. **Delete** - Delete lookup tables and configurations
5. **Import** - Import lookup data (e.g., occupations)
6. **Manage Custom Fields** - View, create, and edit custom fields
7. **Manage Custom Lookups** - View, create, and edit custom lookup tables
8. **Manage System Lookups** - View, create, and edit system lookup tables (countries, languages, genders, etc.)
9. **Manage Organization** - Change organization and system settings

## Files Changed

1. `Program.cs` - Added authorization service registrations
2. `Extensions/PermissionSeeder.cs` - Updated to add missing permissions incrementally
3. `Pages/Settings/Roles/Permissions.cshtml` - Added friendly names and Case icon
4. `Pages/Settings/Users/Permissions.cshtml` - Added friendly names and Case icon

## Testing Steps

1. **Stop the application if running**
2. **Start the application** - The permission seeder will automatically run and add any missing permissions
3. **Navigate to Settings > Roles & Permissions**
4. **Click "Manage Permissions" on any role**
5. **Verify the Settings section shows:**
   - Manage Custom Fields
   - Manage Custom Lookups
   - Manage System Lookups
   - Manage Organization
6. **Grant appropriate permissions to roles**
7. **Test accessing lookup pages** like:
   - `/Settings/Countries/Index`
   - `/Settings/Genders/Index`
   - `/Settings/LookupTables/Index`

## Permission Usage Examples

### System Lookup Pages
Pages like Countries, Languages, Genders, etc. use:
```csharp
[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
```

### Custom Lookup Tables
Custom lookup table management uses:
```csharp
[Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
```

### Custom Fields
Custom field management uses:
```csharp
[Authorize(Policy = "Permission.Settings.ManageCustomFields")]
```

## Next Steps

1. **Assign Permissions**: Go to Settings > Roles & Permissions and grant the appropriate permissions to your admin roles
2. **Test Access**: Verify that users with the permissions can now access and edit lookup tables
3. **Review Security**: Ensure only authorized users have these permissions as they control critical system data

## Notes

- The permission seeder runs automatically on application startup
- Existing permissions are preserved - only missing ones are added
- Permission assignments to roles and users are not affected by the update
- Users will need to log out and back in for new permissions to take effect
