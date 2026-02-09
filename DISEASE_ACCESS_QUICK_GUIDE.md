# Disease Access Control - Quick Reference

## URLs

| Page | URL | Purpose |
|------|-----|---------|
| Index | `/Settings/DiseaseAccess/Index` | Overview of all diseases and their access status |
| Manage Roles | `/Settings/DiseaseAccess/ManageRoles` | Grant/revoke disease access to roles |
| Manage Users | `/Settings/DiseaseAccess/ManageUsers` | Grant/revoke temporary access to users |
| View Grants | `/Settings/DiseaseAccess/ViewGrants` | Comprehensive view of all access grants |
| Edit Disease | `/Settings/Diseases/Edit?id={diseaseId}` | Edit disease including access level |

## Access Levels

| Level | Description | Who Can Access |
|-------|-------------|----------------|
| **Public** | Default access level | Everyone |
| **Restricted** | Requires explicit permission | Only users with role or user grants |

## Common Tasks

### Make a Disease Restricted
1. Go to Settings ? Diseases ? Edit [disease]
2. Change "Access Level" to "Restricted"
3. Save changes
4. Grant access to roles via Disease Access Control ? Manage Role Access

### Grant Permanent Role Access
1. Go to Disease Access Control ? Manage Role Access
2. Select the restricted disease from the left panel
3. Select a role from the dropdown
4. Click "Grant Access"

### Grant Temporary User Access
1. Go to Disease Access Control ? Manage User Access
2. Select any disease from the left panel
3. Select a user from the dropdown
4. Optionally set an expiration date
5. Optionally add a reason
6. Click "Grant Access"

### View All Grants for a Disease
1. Go to Disease Access Control ? View All Grants
2. Find the disease in the list
3. See all role and user grants in side-by-side tables

### Revoke Access
**For Roles:**
- Navigate to Manage Role Access ? Select disease ? Click "Revoke" button

**For Users:**
- Navigate to Manage User Access ? Select disease ? Click "Revoke" button

## Key Features

? **Role-based access** - Grant access to entire roles
? **User-specific access** - Override role access for individual users
? **Temporary access** - Set expiration dates for user grants
? **Audit trail** - Track who granted access and when
? **Reason tracking** - Document why user access was granted
? **Expired grant handling** - Visual indication of expired user grants

## Visual Indicators

| Badge | Meaning |
|-------|---------|
| ?? Public | Disease is accessible to everyone |
| ?? Restricted | Disease requires explicit access |
| ?? EXPIRED | User access has expired |
| ?? Count badge | Number of active grants |

## Important Notes

?? **Role grants** are permanent (no expiration)
?? **User grants** can have optional expiration dates
?? Only **restricted diseases** can have role grants
?? **Public diseases** can still have user grants (for tracking purposes)
?? User-specific access **overrides** role-based access
?? Expired grants are shown but must be manually removed

## Authorization Required

All Disease Access Control pages require:
- Policy: `Permission.Settings.ManagePermissions`
- Users without this permission cannot access these pages

## Service Methods Used

```csharp
// Grant role access
await _diseaseAccessService.GrantDiseaseAccessToRoleAsync(roleId, diseaseId, currentUserId);

// Revoke role access
await _diseaseAccessService.RevokeDiseaseAccessFromRoleAsync(roleId, diseaseId);

// Grant user access
await _diseaseAccessService.GrantDiseaseAccessToUserAsync(userId, diseaseId, currentUserId, expiresAt, reason);

// Revoke user access
await _diseaseAccessService.RevokeDiseaseAccessFromUserAsync(userId, diseaseId);

// Check if user can access disease
bool canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, diseaseId);

// Get all accessible disease IDs for a user
List<Guid> accessibleIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);
```

## File Locations

```
Surveillance-MVP/Pages/Settings/DiseaseAccess/
??? Index.cshtml
??? Index.cshtml.cs
??? ManageRoles.cshtml
??? ManageRoles.cshtml.cs
??? ManageUsers.cshtml
??? ManageUsers.cshtml.cs
??? ViewGrants.cshtml
??? ViewGrants.cshtml.cs

Surveillance-MVP/Pages/Settings/Diseases/
??? Edit.cshtml (updated with AccessLevel dropdown)

Surveillance-MVP/Pages/Settings/
??? Index.cshtml (updated with Disease Access Control link)
```

## Database Tables

| Table | Purpose |
|-------|---------|
| `Diseases` | Stores `AccessLevel` field |
| `RoleDiseaseAccess` | Stores role-to-disease grants |
| `UserDiseaseAccess` | Stores user-to-disease grants with expiration |

## Typical Workflow

1. **Setup Phase**
   - Create diseases via Settings ? Diseases
   - Mark sensitive diseases as "Restricted"

2. **Access Management Phase**
   - Grant access to appropriate roles (permanent)
   - Grant temporary access to users as needed

3. **Monitoring Phase**
   - Use View All Grants to see who has access
   - Review expired grants periodically
   - Revoke access when no longer needed

4. **Maintenance Phase**
   - Clean up expired grants
   - Adjust role grants as organization changes
   - Update disease access levels as needed
