-- ==========================================
-- CASE DEFINITION STRUCTURE REPORT
-- Orion Case Definition (ID=11)
-- ==========================================

PRINT '';
PRINT '========================================';
PRINT 'CASE DEFINITION: Orion A and B Confirmed (ID=11)';
PRINT '========================================';
PRINT '';

-- Show all criteria with hierarchy indicators
SELECT 
    CASE 
        WHEN ParentCriteriaId IS NULL THEN '▶ ROOT [' + CAST(Id AS VARCHAR) + ']'
        ELSE '    └─ CHILD [' + CAST(Id AS VARCHAR) + ']'
    END AS TreePosition,
    'Order=' + CAST(DisplayOrder AS VARCHAR) AS [Order],
    CASE CriterionType
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'Clinical'
        WHEN 2 THEN 'Laboratory'
        WHEN 3 THEN 'Epidemiological'
        WHEN 4 THEN 'Demographic'
        WHEN 5 THEN 'Custom'
        ELSE CAST(CriterionType AS VARCHAR)
    END AS [Type],
    CASE LogicalOperator
        WHEN 1 THEN 'AND'
        WHEN 2 THEN 'OR'
        WHEN 3 THEN 'NOT'
        ELSE CAST(LogicalOperator AS VARCHAR)
    END AS InternalLogic,
    CASE 
        WHEN GroupExitOperator IS NULL THEN '(none)'
        WHEN GroupExitOperator = 1 THEN 'AND'
        WHEN GroupExitOperator = 2 THEN 'OR'
        WHEN GroupExitOperator = 3 THEN 'NOT'
        ELSE CAST(GroupExitOperator AS VARCHAR)
    END AS ExitOperator,
    DisplayText
FROM CaseDefinitionCriteria
WHERE CaseDefinitionId = 11
ORDER BY 
    ISNULL(ParentCriteriaId, 0),
    DisplayOrder;

PRINT '';
PRINT '========================================';
PRINT 'CRITERION DETAILS';
PRINT '========================================';

-- Criterion 19 Details
PRINT '';
PRINT '--- CRITERION [19] ---';
SELECT 
    Id,
    ParentCriteriaId,
    CriterionType,
    LogicalOperator,
    GroupExitOperator,
    DisplayText,
    AcceptablePathogensJson,
    AcceptableSpecimenTypesJson,
    AcceptableTestMethodsJson,
    AcceptableResultsJson
FROM CaseDefinitionCriteria
WHERE Id = 19;

-- Decode Pathogen GUIDs
PRINT '';
PRINT 'Acceptable Pathogens for Criterion [19]:';
SELECT p.Id, p.Name, p.LOINCCode
FROM Pathogens p
WHERE p.Id IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptablePathogensJson FROM CaseDefinitionCriteria WHERE Id = 19)
    )
);

-- Decode Specimen Types
PRINT '';
PRINT 'Acceptable Specimen Types for Criterion [19]:';
SELECT st.Id, st.Name
FROM SpecimenTypes st
WHERE st.Id IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptableSpecimenTypesJson FROM CaseDefinitionCriteria WHERE Id = 19)
    )
);

-- Decode Results
PRINT '';
PRINT 'Acceptable Results for Criterion [19]:';
SELECT tr.Name
FROM TestResults tr
WHERE tr.Name IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptableResultsJson FROM CaseDefinitionCriteria WHERE Id = 19)
    )
);

PRINT '';
PRINT '--- CRITERION [20] ---';
SELECT 
    Id,
    ParentCriteriaId,
    CriterionType,
    LogicalOperator,
    GroupExitOperator,
    DisplayText,
    AcceptablePathogensJson,
    AcceptableSpecimenTypesJson,
    AcceptableTestMethodsJson,
    AcceptableResultsJson
FROM CaseDefinitionCriteria
WHERE Id = 20;

-- Decode Pathogen GUIDs
PRINT '';
PRINT 'Acceptable Pathogens for Criterion [20]:';
SELECT p.Id, p.Name, p.LOINCCode
FROM Pathogens p
WHERE p.Id IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptablePathogensJson FROM CaseDefinitionCriteria WHERE Id = 20)
    )
);

-- Decode Specimen Types
PRINT '';
PRINT 'Acceptable Specimen Types for Criterion [20]:';
SELECT st.Id, st.Name
FROM SpecimenTypes st
WHERE st.Id IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptableSpecimenTypesJson FROM CaseDefinitionCriteria WHERE Id = 20)
    )
);

-- Decode Results
PRINT '';
PRINT 'Acceptable Results for Criterion [20]:';
SELECT tr.Name
FROM TestResults tr
WHERE tr.Name IN (
    SELECT value 
    FROM OPENJSON(
        (SELECT AcceptableResultsJson FROM CaseDefinitionCriteria WHERE Id = 20)
    )
);

PRINT '';
PRINT '========================================';
PRINT 'EVALUATION LOGIC INTERPRETATION';
PRINT '========================================';
PRINT '';
PRINT 'Current Structure:';
PRINT '[19] Orion Marker A (ROOT)';
PRINT '  LogicalOperator = 1 (AND) <- applies to combining with children';
PRINT '  GroupExitOperator = NULL <- NO operator to connect to next root!';
PRINT '  └─ [20] Orion Marker B (CHILD)';
PRINT '      LogicalOperator = 2 (OR) <- children use this for internal logic';
PRINT '';
PRINT 'How TreeBasedCriteriaEvaluator interprets this:';
PRINT '1. [19] is ROOT and has children, so it becomes a GROUP';
PRINT '2. Children internal operator = LogicalOperator of first child = OR';
PRINT '3. Group evaluates: (A OR B) because child has OR operator';
PRINT '4. No second ROOT criterion, so evaluation stops';
PRINT '';
PRINT 'Expected Structure for (A AND B) OR C:';
PRINT '[ROOT 1] Group Container';
PRINT '  LogicalOperator = 1 (AND) <- children use AND internally';
PRINT '  GroupExitOperator = 2 (OR) <- connects to next root with OR';
PRINT '  ├─ [CHILD 1] Marker A';
PRINT '  │   LogicalOperator = 1 (AND)';
PRINT '  └─ [CHILD 2] Marker B';
PRINT '      LogicalOperator = 1 (AND)';
PRINT '[ROOT 2] Marker C';
PRINT '  LogicalOperator = 1 (AND)';
PRINT '';

PRINT '';
PRINT '========================================';
PRINT 'MISSING MARKER C CHECK';
PRINT '========================================';
PRINT '';
PRINT 'Checking if Marker C criterion exists in ANY case definition:';
SELECT 
    cd.Id AS CaseDefId,
    cd.Name AS CaseDefName,
    cdc.Id AS CriterionId,
    cdc.DisplayText
FROM CaseDefinitionCriteria cdc
INNER JOIN CaseDefinitions cd ON cdc.CaseDefinitionId = cd.Id
WHERE cdc.AcceptablePathogensJson LIKE '%3b16e496-9c93-4f8f-9aae-01427fb1d932%' -- Orion Marker C GUID
ORDER BY cd.Id;

PRINT '';
PRINT 'If no results above, Marker C has NOT been added to any case definition.';
PRINT '';
