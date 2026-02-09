# Disease Access Control System

## Overview
The Disease Access Control System provides granular, disease-level permissions for case data. This allows you to restrict access to sensitive disease cases (e.g., HIV, STIs) while keeping most disease data publicly accessible.

## Architecture

### Key Components

#### 1. **DiseaseAccessLevel Enum**
- `Public (0)`: Default. Everyone with Case.View permission can see these cases
- `Restricted (1)`: Requires explicit disease access grant

#### 2. **RoleDiseaseAccess Table**
- Grants disease access to entire roles (permanent)
- Use for: HIV specialists, STI coordinators, etc.
- One-to-many: Role ? Diseases

#### 3. **UserDiseaseAccess Table**
- Grants disease access to specific users (can be temporary)
- Use for: Outbreak response workers, temporary staff
- Supports expiration dates for automatic access revocation
- Includes reason field for audit trail

### Database Schema

```sql
Diseases
??? AccessLevel (int) - 0=Public, 1=Restricted

RoleDiseaseAccess
??? Id (PK)
??? RoleId (FK ? AspNetRoles)
??? DiseaseId (FK ? Diseases)
??? IsAllowed (bit)
??? CreatedAt (datetime2)
??? CreatedByUserId (FK ? AspNetUsers, ON DELETE SET NULL)

UserDiseaseAccess
??? Id (PK)
??? UserId (FK ? AspNetUsers, ON DELETE RESTRICT)
??? DiseaseId (FK ? Diseases, ON DELETE RESTRICT)
??? IsAllowed (bit)
??? CreatedAt (datetime2)
??? ExpiresAt (datetime2, nullable)
??? GrantedByUserId (FK ? AspNetUsers, ON DELETE SET NULL)
??? Reason (nvarchar(500), nullable)
```

**Note:** UserDiseaseAccess uses `ON DELETE RESTRICT` to avoid SQL Server cascade path conflicts. If you need to delete users or diseases, manually remove related UserDiseaseAccess records first.

## Usage Scenarios

### Scenario 1: Most Diseases (Public Access)
**Example:** Measles, Tuberculosis, COVID-19

1. Keep `Disease.AccessLevel = Public` (default)
2. No additional configuration needed
3. Anyone with `Case.View` permission can see these cases

### Scenario 2: Sensitive Diseases (Restricted Access)
**Example:** HIV, Hepatitis, Syphilis

1. Set `Disease.AccessLevel = Restricted`
2. Grant access to specific roles via `RoleDiseaseAccess`:
   ```csharp
   await _diseaseAccessService.GrantDiseaseAccessToRoleAsync(
       roleId: "hivSpecialistRoleId",
       diseaseId: hivDiseaseId,
       grantedByUserId: currentUserId
   );
   ```

### Scenario 3: Outbreak Response (Temporary User Access)
**Example:** Cholera outbreak - bring in temporary workers

1. Set `Disease.AccessLevel = Restricted` for Cholera
2. Grant temporary access to outbreak workers:
   ```csharp
   await _diseaseAccessService.GrantDiseaseAccessToUserAsync(
       userId: tempWorkerId,
       diseaseId: choleraDiseaseId,
       grantedByUserId: currentUserId,
       expiresAt: DateTime.UtcNow.AddMonths(3), // Auto-expire after outbreak
       reason: "Cholera outbreak response team - Q2 2025"
   );
   ```
3. Access automatically revokes after expiration date

## Service Methods

### IDiseaseAccessService

```csharp
// Check access
Task<bool> CanAccessDiseaseAsync(string userId, Guid diseaseId)
Task<List<Guid>> GetAccessibleDiseaseIdsAsync(string userId)

// Grant access
Task GrantDiseaseAccessToRoleAsync(string roleId, Guid diseaseId, string grantedByUserId)
Task GrantDiseaseAccessToUserAsync(string userId, Guid diseaseId, string grantedByUserId, 
                                    DateTime? expiresAt = null, string? reason = null)

// Revoke access
Task RevokeDiseaseAccessFromRoleAsync(string roleId, Guid diseaseId)
Task RevokeDiseaseAccessFromUserAsync(string userId, Guid diseaseId)

// Query access
Task<List<string>> GetRolesWithDiseaseAccessAsync(Guid diseaseId)
Task<List<string>> GetUsersWithDiseaseAccessAsync(Guid diseaseId)

// Maintenance
Task RemoveExpiredAccessAsync()
```

## Access Check Logic

The system checks access in this order:

1. **Disease is Public?** ? Allow (no further checks)
2. **User-specific access?** ? Check `UserDiseaseAccess` (not expired)
3. **Role-based access?** ? Check user's roles in `RoleDiseaseAccess`
4. **Default:** Deny for restricted diseases

## Implementation in Pages

### Case Index (List)
```csharp
var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

Cases = await _context.Cases
    .Where(c => c.Disease == null || 
               c.Disease.AccessLevel == DiseaseAccessLevel.Public || 
               accessibleDiseaseIds.Contains(c.DiseaseId!.Value))
    .ToListAsync();
```

### Case Details/Edit/Delete
```csharp
// After loading case
if (caseEntity.DiseaseId.HasValue)
{
    var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, caseEntity.DiseaseId.Value);
    if (!canAccess)
        return Forbid();
}
```

### Case Create
```csharp
// Filter disease dropdown
var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

ViewData["DiseaseId"] = new SelectList(
    await _context.Diseases
        .Where(d => d.IsActive && 
                   (d.AccessLevel == DiseaseAccessLevel.Public || 
                    accessibleDiseaseIds.Contains(d.Id)))
        .ToListAsync(),
    "Id", "Name");
```

## Security Features

### 1. **Defense in Depth**
- Disease access is checked IN ADDITION TO action permissions
- User needs both `Case.View` AND disease access

### 2. **User Overrides Role**
- User-specific grants override role-based access
- Allows for exceptions without changing role configuration

### 3. **Automatic Expiration**
- Temporary access expires automatically
- Run `RemoveExpiredAccessAsync()` periodically (e.g., daily background job)

### 4. **Audit Trail**
- All grants include `CreatedByUserId` and `CreatedAt`
- User grants include `Reason` field for compliance

## Performance Considerations

### Optimized for Public Diseases
- Short-circuit check for `AccessLevel == Public`
- 90%+ of queries avoid extra lookups

### Caching Strategy (Future Enhancement)
Consider caching `GetAccessibleDiseaseIdsAsync` results:
```csharp
// Cache for 15 minutes
var cacheKey = $"DiseaseAccess:{userId}";
var accessibleIds = await _cache.GetOrCreateAsync(cacheKey, async entry => {
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
    return await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);
});
```

## Admin UI (To Be Implemented)

### Disease Management
**Path:** `/Settings/Diseases/Edit/{id}`

Add field:
```html
<div class="form-group">
    <label asp-for="Disease.AccessLevel"></label>
    <select asp-for="Disease.AccessLevel" class="form-control">
        <option value="0">Public (Everyone can access)</option>
        <option value="1">Restricted (Requires explicit grant)</option>
    </select>
</div>
```

### Disease Access Management
**Path:** `/Settings/DiseaseAccess` (new page)

Features:
- View all restricted diseases
- Assign roles to diseases
- Assign users to diseases (with expiration)
- View current access grants
- Revoke access

## Migration

Run the migration script:
```powershell
# Apply migration
sqlcmd -S (localdb)\mssqllocaldb -d SurveillanceMVP -i Migrations/Add_Disease_Access_Control.sql
```

Or use Entity Framework:
```powershell
# Generate migration
Add-Migration AddDiseaseAccessControl

# Apply migration
Update-Database
```

## Testing Checklist

- [ ] Public disease: All users can view cases
- [ ] Restricted disease: Unauthorized users cannot view
- [ ] Role grant: All users in role can view restricted disease
- [ ] User grant: Specific user can view restricted disease
- [ ] Expiration: Expired user grant denies access
- [ ] Case Index: Filters out restricted cases for unauthorized users
- [ ] Case Details: Returns Forbid() for unauthorized disease access
- [ ] Case Create: Dropdown only shows accessible diseases
- [ ] Case Edit: Returns Forbid() for unauthorized disease access
- [ ] Case Delete: Returns Forbid() for unauthorized disease access

## Future Enhancements

1. **Background Job**: Auto-cleanup expired access daily
2. **Admin UI**: Full disease access management pages
3. **Notifications**: Alert users before access expires
4. **Audit Reports**: Who accessed which sensitive diseases
5. **Bulk Operations**: Grant access to multiple users/diseases at once
6. **Disease Groups**: Assign access to disease categories (e.g., all STIs)
