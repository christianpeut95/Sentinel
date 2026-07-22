-- Diagnostic Script: Analyze Case Definition for Orion with Nested Groups
-- This script helps understand why (Marker A AND Marker B) OR Marker C is not matching

-- Step 1: Find active Orion case definitions
PRINT '========================================';
PRINT 'Step 1: Active Orion Case Definitions';
PRINT '========================================';

SELECT 
    cd.Id,
    cd.Name,
    cd.Status,
    cd.EnableAutoEvaluation,
    d.Name AS DiseaseName,
    cs.Name AS ConfirmationStatus
FROM CaseDefinitions cd
LEFT JOIN Diseases d ON cd.DiseaseId = d.Id
LEFT JOIN CaseStatuses cs ON cd.ConfirmationStatusId = cs.Id
WHERE d.Name LIKE '%Orion%'
    AND cd.Status = 1 -- Current
    AND cd.EnableAutoEvaluation = 1;

-- Step 2: Show the criteria structure
PRINT '';
PRINT '========================================';
PRINT 'Step 2: Criteria Structure (Tree View)';
PRINT '========================================';

DECLARE @CaseDefId INT;
SELECT TOP 1 @CaseDefId = cd.Id
FROM CaseDefinitions cd
LEFT JOIN Diseases d ON cd.DiseaseId = d.Id
WHERE d.Name LIKE '%Orion%'
    AND cd.Status = 1
    AND cd.EnableAutoEvaluation = 1
ORDER BY cd.Id DESC;

PRINT 'Case Definition ID: ' + CAST(@CaseDefId AS VARCHAR(10));
PRINT '';

-- Show all criteria with hierarchy
SELECT 
    cdc.Id,
    cdc.ParentCriteriaId,
    cdc.CriterionType,
    cdc.LogicalOperator,
    cdc.GroupExitOperator,
    cdc.GroupNumber,
    cdc.DisplayOrder,
    cdc.DisplayText,
    cdc.AcceptablePathogensJson,
    cdc.AcceptableTestMethodsJson,
    cdc.AcceptableSpecimenTypesJson,
    cdc.AcceptableResultsJson,
    CASE 
        WHEN cdc.ParentCriteriaId IS NULL THEN 'ROOT'
        ELSE 'CHILD OF ' + CAST(cdc.ParentCriteriaId AS VARCHAR(10))
    END AS TreeLevel
FROM CaseDefinitionCriteria cdc
WHERE cdc.CaseDefinitionId = @CaseDefId
ORDER BY 
    ISNULL(cdc.ParentCriteriaId, 0),
    cdc.DisplayOrder;

-- Step 3: Analyze the HL7 messages
PRINT '';
PRINT '========================================';
PRINT 'Step 3: HL7 Message Analysis';
PRINT '========================================';

-- Message 1: A + B positive
PRINT 'Message 1: A + B positive (f34d0dfc-3a2a-4c57-f535-08dee790a601)';
SELECT 
    lr.Id AS LabResultId,
    lr.FriendlyId AS LabResultFriendlyId,
    lr.SpecimenTypeId,
    st.Name AS SpecimenTypeName,
    m.Id AS MarkerId,
    m.TestCode,
    m.PathogenId,
    p.Name AS PathogenName,
    m.TestMethodId,
    tm.Name AS TestMethodName,
    m.TestResultId,
    tr.Name AS TestResultName
FROM HL7Messages msg
LEFT JOIN LabResults lr ON msg.LabResultId = lr.Id
LEFT JOIN SpecimenTypes st ON lr.SpecimenTypeId = st.Id
LEFT JOIN LabResultMarkers m ON lr.Id = m.LabResultId
LEFT JOIN Pathogens p ON m.PathogenId = p.Id
LEFT JOIN TestMethods tm ON m.TestMethodId = tm.Id
LEFT JOIN TestResults tr ON m.TestResultId = tr.Id
WHERE msg.Id = 'f34d0dfc-3a2a-4c57-f535-08dee790a601';

PRINT '';

-- Message 2: C positive only
PRINT 'Message 2: C positive only (6f8d4305-aa99-4d39-f536-08dee790a601)';
SELECT 
    lr.Id AS LabResultId,
    lr.FriendlyId AS LabResultFriendlyId,
    lr.SpecimenTypeId,
    st.Name AS SpecimenTypeName,
    m.Id AS MarkerId,
    m.TestCode,
    m.PathogenId,
    p.Name AS PathogenName,
    m.TestMethodId,
    tm.Name AS TestMethodName,
    m.TestResultId,
    tr.Name AS TestResultName
FROM HL7Messages msg
LEFT JOIN LabResults lr ON msg.LabResultId = lr.Id
LEFT JOIN SpecimenTypes st ON lr.SpecimenTypeId = st.Id
LEFT JOIN LabResultMarkers m ON lr.Id = m.LabResultId
LEFT JOIN Pathogens p ON m.PathogenId = p.Id
LEFT JOIN TestMethods tm ON m.TestMethodId = tm.Id
LEFT JOIN TestResults tr ON m.TestResultId = tr.Id
WHERE msg.Id = '6f8d4305-aa99-4d39-f536-08dee790a601';

-- Step 4: Check if pathogens/methods/results match the criteria
PRINT '';
PRINT '========================================';
PRINT 'Step 4: Matching Analysis';
PRINT '========================================';

-- For each criterion, check if the markers would match
SELECT 
    cdc.Id AS CriterionId,
    cdc.DisplayText,
    cdc.AcceptablePathogensJson,
    cdc.AcceptableTestMethodsJson,
    cdc.AcceptableResultsJson,
    cdc.ParentCriteriaId,
    cdc.LogicalOperator,
    cdc.GroupExitOperator
FROM CaseDefinitionCriteria cdc
WHERE cdc.CaseDefinitionId = @CaseDefId
    AND cdc.CriterionType = 1 -- Laboratory
ORDER BY cdc.DisplayOrder;

PRINT '';
PRINT '========================================';
PRINT 'Step 5: Check Disease Configuration';
PRINT '========================================';

-- Check if disease has HL7 matching config
SELECT 
    dhmc.*
FROM DiseaseHL7MatchingConfigs dhmc
WHERE dhmc.DiseaseId = (
    SELECT cd.DiseaseId
    FROM CaseDefinitions cd
    WHERE cd.Id = @CaseDefId
);
