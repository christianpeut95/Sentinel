-- =====================================================================
-- Test Disease Hierarchy Access Control
-- =====================================================================
-- Use this to verify the hierarchy access checking is working correctly
-- =====================================================================

-- 1. Show the disease hierarchy
PRINT 'Disease Hierarchy:';
SELECT 
    d.Id,
    d.Name,
    d.Code,
    d.AccessLevel,
    d.ParentDiseaseId,
    p.Name AS ParentName,
    d.Level
FROM Diseases d
LEFT JOIN Diseases p ON d.ParentDiseaseId = p.Id
WHERE d.Name LIKE '%Salmonella%'
ORDER BY d.PathIds;

PRINT '';
PRINT '---';
PRINT '';

-- 2. Show all role access grants for Salmonella diseases
PRINT 'Role Access Grants for Salmonella:';
SELECT 
    d.Name AS DiseaseName,
    d.AccessLevel,
    r.Name AS RoleName,
    rda.ApplyToChildren,
    rda.InheritedFromDiseaseId,
    pd.Name AS InheritedFromDisease,
    rda.CreatedAt
FROM RoleDiseaseAccess rda
JOIN Diseases d ON rda.DiseaseId = d.Id
JOIN AspNetRoles r ON rda.RoleId = r.Id
LEFT JOIN Diseases pd ON rda.InheritedFromDiseaseId = pd.Id
WHERE d.Name LIKE '%Salmonella%'
ORDER BY d.Name, r.Name;

PRINT '';
PRINT '---';
PRINT '';

-- 3. Show cases by disease
PRINT 'Cases by Salmonella Disease:';
SELECT 
    d.Name AS DiseaseName,
    d.AccessLevel,
    COUNT(c.Id) AS CaseCount
FROM Diseases d
LEFT JOIN Cases c ON d.Id = c.DiseaseId
WHERE d.Name LIKE '%Salmonella%'
GROUP BY d.Id, d.Name, d.AccessLevel
ORDER BY d.Name;

PRINT '';
PRINT '---';
PRINT '';

-- 4. Test Scenario Setup Instructions
PRINT 'To test hierarchy access control:';
PRINT '';
PRINT '1. Make parent "Salmonella" RESTRICTED:';
PRINT '   UPDATE Diseases SET AccessLevel = 1 WHERE Name = ''Salmonella'';';
PRINT '';
PRINT '2. Leave child "Salmonella Typhimurium" as PUBLIC (or RESTRICTED):';
PRINT '   UPDATE Diseases SET AccessLevel = 0 WHERE Name = ''Salmonella Typhimurium'';';
PRINT '';
PRINT '3. Grant access to parent for a test role:';
PRINT '   -- Go to: Disease Access Control -> Manage Role Access';
PRINT '   -- Select "Salmonella"';
PRINT '   -- Grant to a role';
PRINT '';
PRINT '4. Expected Behavior:';
PRINT '   - Users WITHOUT the role: Cannot see Salmonella OR Salmonella Typhimurium cases';
PRINT '   - Users WITH the role: Can see both Salmonella AND Salmonella Typhimurium cases';
PRINT '';
PRINT '5. Verify by logging in as different users and checking Cases list';
