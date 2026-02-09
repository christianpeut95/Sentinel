# Disease Access Control - Quick Reference

## Common Tasks

### Mark a Disease as Restricted
```sql
UPDATE Diseases 
SET AccessLevel = 1  -- 1 = Restricted
WHERE Code = 'HIV';
```

### Grant Role Access to Disease
```csharp
await _diseaseAccessService.GrantDiseaseAccessToRoleAsync(
    roleId: "role-id-here",
    diseaseId: hivDiseaseId,
    grantedByUserId: User.FindFirstValue(ClaimTypes.NameIdentifier)
);
```

### Grant Temporary User Access (Outbreak Worker)
```csharp
await _diseaseAccessService.GrantDiseaseAccessToUserAsync(
    userId: "user-id-here",
    diseaseId: choleraDiseaseId,
    grantedByUserId: User.FindFirstValue(ClaimTypes.NameIdentifier),
    expiresAt: DateTime.UtcNow.AddMonths(3),
    reason: "Cholera outbreak response team"
);
```

### Revoke Access
```csharp
// Revoke from role
await _diseaseAccessService.RevokeDiseaseAccessFromRoleAsync(roleId, diseaseId);

// Revoke from user
await _diseaseAccessService.RevokeDiseaseAccessFromUserAsync(userId, diseaseId);
```

### Check User Access
```csharp
var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, diseaseId);
```

### Get All Accessible Diseases for User
```csharp
var diseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);
```

### Remove Expired Access
```csharp
await _diseaseAccessService.RemoveExpiredAccessAsync();
```

## SQL Queries

### Find All Restricted Diseases
```sql
SELECT Id, Name, Code, AccessLevel
FROM Diseases
WHERE AccessLevel = 1;
```

### View Role Access Grants
```sql
SELECT 
    r.Name AS RoleName,
    d.Name AS DiseaseName,
    rda.IsAllowed,
    rda.CreatedAt,
    u.Email AS GrantedBy
FROM RoleDiseaseAccess rda
JOIN AspNetRoles r ON rda.RoleId = r.Id
JOIN Diseases d ON rda.DiseaseId = d.Id
LEFT JOIN AspNetUsers u ON rda.CreatedByUserId = u.Id
ORDER BY d.Name, r.Name;
```

### View User Access Grants
```sql
SELECT 
    u.Email AS UserEmail,
    d.Name AS DiseaseName,
    uda.IsAllowed,
    uda.CreatedAt,
    uda.ExpiresAt,
    uda.Reason,
    g.Email AS GrantedBy
FROM UserDiseaseAccess uda
JOIN AspNetUsers u ON uda.UserId = u.Id
JOIN Diseases d ON uda.DiseaseId = d.Id
LEFT JOIN AspNetUsers g ON uda.GrantedByUserId = g.Id
ORDER BY uda.ExpiresAt, d.Name;
```

### Find Expired Access
```sql
SELECT 
    u.Email,
    d.Name AS Disease,
    uda.ExpiresAt,
    uda.Reason
FROM UserDiseaseAccess uda
JOIN AspNetUsers u ON uda.UserId = u.Id
JOIN Diseases d ON uda.DiseaseId = d.Id
WHERE uda.ExpiresAt IS NOT NULL 
  AND uda.ExpiresAt <= GETUTCDATE()
ORDER BY uda.ExpiresAt DESC;
```

### Check What Diseases a User Can Access
```sql
DECLARE @UserId NVARCHAR(450) = 'user-id-here';

-- Public diseases
SELECT Id, Name, 'Public' AS AccessType
FROM Diseases
WHERE AccessLevel = 0

UNION

-- User-specific grants
SELECT d.Id, d.Name, 'User Grant' AS AccessType
FROM UserDiseaseAccess uda
JOIN Diseases d ON uda.DiseaseId = d.Id
WHERE uda.UserId = @UserId 
  AND uda.IsAllowed = 1
  AND (uda.ExpiresAt IS NULL OR uda.ExpiresAt > GETUTCDATE())

UNION

-- Role-based grants
SELECT d.Id, d.Name, 'Role: ' + r.Name AS AccessType
FROM RoleDiseaseAccess rda
JOIN Diseases d ON rda.DiseaseId = d.Id
JOIN AspNetRoles r ON rda.RoleId = r.Id
JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId 
  AND rda.IsAllowed = 1

ORDER BY Name;
```

## Example Scenarios

### Scenario 1: Setup HIV Specialist Role
```sql
-- 1. Get role ID
DECLARE @RoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'HIV Specialist');

-- 2. Get HIV disease ID
DECLARE @HIVDiseaseId UNIQUEIDENTIFIER = (SELECT Id FROM Diseases WHERE Code = 'HIV');

-- 3. Mark HIV as restricted
UPDATE Diseases SET AccessLevel = 1 WHERE Id = @HIVDiseaseId;

-- 4. Grant access to role
INSERT INTO RoleDiseaseAccess (RoleId, DiseaseId, IsAllowed, CreatedAt)
VALUES (@RoleId, @HIVDiseaseId, 1, GETUTCDATE());
```

### Scenario 2: Temporary Outbreak Worker
```sql
-- 1. Get user ID
DECLARE @UserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'temp.worker@example.com');

-- 2. Get disease ID
DECLARE @DiseaseId UNIQUEIDENTIFIER = (SELECT Id FROM Diseases WHERE Name = 'Cholera');

-- 3. Mark disease as restricted
UPDATE Diseases SET AccessLevel = 1 WHERE Id = @DiseaseId;

-- 4. Grant temporary access (expires in 3 months)
INSERT INTO UserDiseaseAccess (UserId, DiseaseId, IsAllowed, CreatedAt, ExpiresAt, Reason)
VALUES (
    @UserId, 
    @DiseaseId, 
    1, 
    GETUTCDATE(), 
    DATEADD(MONTH, 3, GETUTCDATE()),
    'Cholera outbreak response - Q2 2025'
);
```

### Scenario 3: Cleanup Expired Access
```sql
-- Remove expired access
DELETE FROM UserDiseaseAccess
WHERE ExpiresAt IS NOT NULL 
  AND ExpiresAt <= GETUTCDATE();
```

## Troubleshooting

### User Can't See Restricted Disease Cases
1. Check disease access level: `SELECT AccessLevel FROM Diseases WHERE Id = '...'`
2. Check user's role grants: Query `RoleDiseaseAccess` for user's roles
3. Check user-specific grants: Query `UserDiseaseAccess` for user
4. Check expiration: Ensure `ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE()`

### All Users See Restricted Cases
- Disease is probably marked as Public (AccessLevel = 0)
- Change to Restricted: `UPDATE Diseases SET AccessLevel = 1 WHERE ...`

### Access Grant Not Working
- Check foreign keys are correct (RoleId/UserId and DiseaseId)
- Verify `IsAllowed = 1`
- For user grants, check expiration hasn't passed
- Clear any caches (if implemented)

## Integration with Existing Permissions

Disease access is ADDITIVE to action permissions:

| Action Permission | Disease Access | Result |
|------------------|----------------|--------|
| ? Case.View | ? Has Access | ? Can View |
| ? Case.View | ? No Access | ? Forbidden |
| ? No Case.View | ? Has Access | ? Unauthorized |
| ? No Case.View | ? No Access | ? Unauthorized |

**Both conditions must be true** for access to be granted.
