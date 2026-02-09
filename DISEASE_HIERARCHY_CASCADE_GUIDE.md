# Disease Access Control - Hierarchical Permissions & Cascading

## Overview

The Disease Access Control system has been enhanced with support for hierarchical disease structures and cascading permissions. This allows administrators to grant access to parent diseases that automatically applies to all child diseases, preventing the need to manage permissions for each disease individually.

## New Features

### 1. **Collapsible Disease Hierarchy**
- Diseases are now displayed in a tree structure showing parent-child relationships
- Expandable/collapsible nodes for diseases with children
- Visual indication of hierarchy level with indentation
- Badge showing count of child diseases

### 2. **Cascading Permissions**
- Grant access to a parent disease and have it automatically apply to all children
- Two options when granting:
  - **Direct Access Only**: Permission applies only to the selected disease
  - **Apply to Children**: Permission cascades to all current and future child diseases

### 3. **Inherited Access Protection**
- Child diseases that inherit access from a parent cannot have separate permissions granted directly
- Inherited permissions are clearly marked with badges and visual distinction
- Inherited grants can only be removed by revoking the parent permission

## Database Changes

### New Fields Added

**RoleDiseaseAccess table:**
```sql
ApplyToChildren BIT NOT NULL DEFAULT 0
InheritedFromDiseaseId UNIQUEIDENTIFIER NULL
```

**UserDiseaseAccess table:**
```sql
ApplyToChildren BIT NOT NULL DEFAULT 0
InheritedFromDiseaseId UNIQUEIDENTIFIER NULL
```

### Migration SQL

Run `Add_Disease_Hierarchy_Support.sql` to add these columns to your existing database.

## How It Works

### Role Access Cascade Example

1. **Admin grants access to "Salmonella" with "Apply to Children" checked**
   - Creates direct grant for "Salmonella" with `ApplyToChildren = true`
   - Automatically creates inherited grants for:
     - "Salmonella Typhimurium"
     - "Salmonella Enteritidis"
     - All other Salmonella child diseases

2. **Attempting to grant direct access to "Salmonella Typhimurium"**
   - System detects inherited access from parent
   - Grant is blocked with error message
   - Admin must revoke parent cascade or work at parent level

3. **Revoking parent access**
   - When parent access is revoked, all inherited child access is automatically removed
   - Confirmation dialog warns about cascade effect

### User Access Cascade Example

Same logic applies to user-specific access:
- Temporary access can be granted with expiration dates
- Cascade option available if disease has children
- Inherited user access cannot be directly modified

## UI Components

### 1. Disease List with Hierarchy
**File**: `_DiseaseHierarchyNode.cshtml`

Features:
- Recursive partial view for tree rendering
- Collapse/expand functionality with Bootstrap
- Visual indicators for restricted diseases
- Works on both ManageRoles and ManageUsers pages

### 2. Grant Form Enhancements

**Manage Role Access page:**
- Checkbox: "Apply to all child diseases" (shown only when disease has children)
- Helper text explaining cascade behavior
- Count of affected child diseases shown in success message

**Manage User Access page:**
- Same cascade checkbox
- Works alongside expiration dates and reasons
- Cascade status shown in grants table

### 3. Grants Table

**New columns:**
- **Type**: Shows if grant is "Direct" or "Inherited"
- Badge indicators:
  - ?? Direct - Granted directly to this disease
  - ?? Inherited - Cascaded from parent disease
  - ?? + Children - This grant cascades to children

**Visual distinction:**
- Inherited grants have light blue row highlighting
- "Inherited" badge with down-arrow icon
- Parent disease name shown for inherited grants (roles only)

### 4. Action Restrictions

**For inherited grants:**
- Revoke button replaced with lock icon and "Inherited" text
- Cannot be directly revoked
- Tooltip/message directs to parent disease

**For cascade grants:**
- Revoke confirmation includes warning about child impact
- Example: "This will also revoke access from all child diseases"

## Service Layer Enhancements

### IDiseaseAccessService - New Methods

```csharp
// Updated signature with cascade parameter
Task GrantDiseaseAccessToRoleAsync(string roleId, Guid diseaseId, string grantedByUserId, bool applyToChildren = false);

Task GrantDiseaseAccessToUserAsync(string userId, Guid diseaseId, string grantedByUserId, DateTime? expiresAt = null, string? reason = null, bool applyToChildren = false);

// New helper methods
Task<bool> HasInheritedAccessAsync(string roleId, Guid diseaseId);
Task<bool> HasInheritedUserAccessAsync(string userId, Guid diseaseId);
Task<List<Guid>> GetAllChildDiseaseIdsAsync(Guid parentDiseaseId);
```

### Implementation Logic

1. **Granting with Cascade:**
   ```csharp
   - Create direct grant for parent disease with ApplyToChildren = true
   - Get all descendant disease IDs recursively
   - For each child:
     - Create inherited grant with InheritedFromDiseaseId = parentDiseaseId
     - Mark as not directly editable
   ```

2. **Revoking with Cascade:**
   ```csharp
   - Remove direct grant from parent
   - Query all grants where InheritedFromDiseaseId = parentDiseaseId
   - Remove all inherited grants
   ```

3. **Checking Access:**
   ```csharp
   - Public diseases: Always accessible
   - Check direct grant OR inherited grant
   - Inherited grant checked by InheritedFromDiseaseId
   ```

## User Workflows

### Grant Cascading Access to a Role

1. Navigate to **Settings ? Disease Access Control ? Manage Role Access**
2. Select a parent disease from the hierarchical list (e.g., "Salmonella")
3. Select a role from the dropdown
4. **Check** "Apply to all child diseases"
5. Click **Grant Access**
6. Success message shows count of affected diseases
7. All child diseases now show inherited access

### Grant Direct Access (No Cascade)

1. Navigate to **Settings ? Disease Access Control ? Manage Role Access**
2. Select any disease (parent or child)
3. Select a role from the dropdown
4. **Leave unchecked** "Apply to all child diseases"
5. Click **Grant Access**
6. Only the selected disease is granted

### View Inherited Access

1. Navigate to a child disease that has inherited access
2. Grants table shows:
   - Blue-highlighted row
   - "Inherited" badge
   - Parent disease name (for roles)
   - Lock icon instead of Revoke button
3. Click parent disease name link to manage at parent level

### Revoke Cascading Access

1. Navigate to the parent disease
2. Find the role/user grant with "+ Children" badge
3. Click **Revoke**
4. Confirm dialog warns about child impact
5. Parent and all child access removed

## Page Model Enhancements

### ManageRolesModel / ManageUsersModel

**New Properties:**
```csharp
[BindProperty]
public bool ApplyToChildren { get; set; }

public List<DiseaseHierarchyNode> RestrictedDiseases/AllDiseases { get; set; }
public bool SelectedDiseaseHasChildren { get; set; }
```

**New Grant Class Properties:**
```csharp
public class RoleAccessGrant
{
    // ... existing properties ...
    public bool ApplyToChildren { get; set; }
    public bool IsInherited { get; set; }
    public Guid? InheritedFromDiseaseId { get; set; }
    public string? InheritedFromDiseaseName { get; set; }
}
```

### Disease Hierarchy Model

```csharp
public class DiseaseHierarchyNode
{
    public Disease Disease { get; set; }
    public List<DiseaseHierarchyNode> Children { get; set; }
    public int Level { get; set; }
}
```

## Visual Indicators Reference

| Badge/Icon | Meaning |
|------------|---------|
| ?? Direct | Permission granted directly to this disease |
| ?? Inherited | Permission inherited from parent disease |
| ?? + Children | Permission cascades to child diseases |
| ?? Inherited (lock) | Cannot revoke - inherited from parent |
| ?? Restricted | Disease requires explicit permission |
| ? | Collapsed node (has children) |
| ? | Expanded node (showing children) |
| ?? Badge | Count of child diseases |

## Best Practices

### When to Use Cascade

? **Use cascade when:**
- Managing a disease family (e.g., all Salmonella subtypes)
- You want consistent access across all variations
- New child diseases are frequently added
- Simplifying permission management

? **Don't use cascade when:**
- Some child diseases need different access levels
- Temporary/specific access to one subtype
- Fine-grained control is required

### Managing Large Hierarchies

1. **Start at the top level**: Grant access to category-level diseases
2. **Use cascade sparingly**: Only cascade when all children truly need same access
3. **Document exceptions**: Use the "Reason" field for special cases
4. **Regular audits**: Use View All Grants page to review cascade impacts

### Troubleshooting

**Can't grant access to a disease:**
- Check if it has inherited access from a parent
- Navigate to parent disease to manage
- Or revoke parent's cascade and grant individually

**Unexpected access:**
- Check for inherited grants in ViewGrants page
- Look for cascade indicators (+ Children badge)
- Review parent disease permissions

**Need to exclude one child:**
- Don't use cascade on parent
- Grant individually to each child except the one to exclude

## Migration Checklist

- [ ] Run `Add_Disease_Hierarchy_Support.sql`
- [ ] Verify new columns exist in both tables
- [ ] Test hierarchy display on ManageRoles page
- [ ] Test cascade grant for a parent disease
- [ ] Verify inherited grants appear on child diseases
- [ ] Test revoke with cascade
- [ ] Check View All Grants page shows cascade info
- [ ] Verify inherited grants cannot be directly modified

## API Usage Examples

### Check if disease has children before showing cascade option

```csharp
var disease = await _context.Diseases
    .Include(d => d.SubDiseases)
    .FirstOrDefaultAsync(d => d.Id == diseaseId);
    
bool hasChildren = disease?.SubDiseases.Any() ?? false;
```

### Get all descendants of a disease

```csharp
var allChildIds = await _diseaseAccessService.GetAllChildDiseaseIdsAsync(parentDiseaseId);
Console.WriteLine($"This disease has {allChildIds.Count} descendants");
```

### Check if access is inherited

```csharp
bool isInherited = await _diseaseAccessService.HasInheritedAccessAsync(roleId, diseaseId);

if (isInherited)
{
    // Show message: Cannot grant directly - already inherited from parent
}
```

## Files Modified/Created

**Models:**
- ?? `RoleDiseaseAccess.cs` - Added cascade fields
- ?? `UserDiseaseAccess.cs` - Added cascade fields

**Services:**
- ?? `IDiseaseAccessService.cs` - New method signatures
- ?? `DiseaseAccessService.cs` - Cascade logic implementation

**Pages:**
- ?? `ManageRoles.cshtml.cs` - Hierarchy building & cascade support
- ?? `ManageRoles.cshtml` - Hierarchy display & cascade UI
- ?? `ManageUsers.cshtml.cs` - Hierarchy building & cascade support
- ?? `ManageUsers.cshtml` - Hierarchy display & cascade UI
- ? `_DiseaseHierarchyNode.cshtml` - NEW: Recursive partial view

**SQL:**
- ? `Add_Disease_Hierarchy_Support.sql` - NEW: Migration script

## Testing Scenarios

1. **Scenario: Grant cascade to parent with 3 children**
   - Expected: 4 total grants (1 parent + 3 children)
   - Verify: All children show "Inherited" badge

2. **Scenario: Try to grant directly to inherited child**
   - Expected: Error message preventing grant
   - Verify: System detects InheritedFromDiseaseId

3. **Scenario: Revoke cascade from parent**
   - Expected: Parent + all 3 children lose access
   - Verify: Inherited grants removed from database

4. **Scenario: Add new child to parent with cascade**
   - Expected: New child automatically gets inherited access
   - Note: Requires re-granting parent or service enhancement

5. **Scenario: Hierarchy display with 3 levels**
   - Expected: Proper indentation and collapse/expand
   - Verify: JavaScript toggling works

## Future Enhancements

Potential additions:
- Auto-apply cascade to newly created child diseases
- Bulk grant wizard for multiple diseases
- Permission inheritance visualization diagram
- Export hierarchy with permission mappings
- Audit log for cascade operations
- "Break inheritance" option for exceptions

## Support

For issues or questions:
- Check `DISEASE_ACCESS_QUICK_GUIDE.md` for usage
- Review `DISEASE_ACCESS_PAGES_SUMMARY.md` for architecture
- Database issues: Run verification queries in SQL script
- UI issues: Check browser console for JavaScript errors
