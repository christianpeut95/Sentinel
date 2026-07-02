-- ========================================
-- FIX: HL7 Field Resolution Issues
-- ========================================
-- Issue: Specimen Type (258500001 - Nasopharyngeal swab) not resolving
-- Cause: SNOMED code missing from SpecimenTypes table
-- 
-- This script will:
-- 1. Add SNOMED code to Nasopharyngeal Swab specimen type
-- 2. Verify the fix
-- 3. Show current disease text matching configuration
-- ========================================

-- STEP 1: Fix Nasopharyngeal Swab SNOMED Code
PRINT '========================================';
PRINT 'STEP 1: Adding SNOMED code to Nasopharyngeal Swab';
PRINT '========================================';
PRINT '';

-- First, check current state
PRINT 'Current state:';
SELECT 
    Id,
    Name,
    SnomedCode AS [SNOMED Code (Before)],
    Hl7Code AS [HL7 Code],
    LoincSystemCode AS [LOINC Code],
    IsActive
FROM SpecimenTypes
WHERE Name LIKE '%Nasopharyngeal%'
   OR SnomedCode = '258500001';

PRINT '';
PRINT 'Updating SNOMED code...';

-- Update the record
UPDATE SpecimenTypes
SET SnomedCode = '258500001',
    ModifiedAt = GETUTCDATE()
WHERE Name = 'Nasopharyngeal swab'
  AND (SnomedCode IS NULL OR SnomedCode = '' OR SnomedCode != '258500001');

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' row(s) updated';
PRINT '';

-- Verify the update
PRINT 'Updated state:';
SELECT 
    Id,
    Name,
    SnomedCode AS [SNOMED Code (After)],
    Hl7Code AS [HL7 Code],
    LoincSystemCode AS [LOINC Code],
    IsActive
FROM SpecimenTypes
WHERE Name LIKE '%Nasopharyngeal%'
   OR SnomedCode = '258500001';

PRINT '';
PRINT '✅ Nasopharyngeal Swab SNOMED code updated';
PRINT '';

-- ========================================
-- STEP 2: Check Disease Text Matching Configuration
-- ========================================
PRINT '========================================';
PRINT 'STEP 2: Disease Text Matching Configuration';
PRINT '========================================';
PRINT '';
PRINT 'Current text fallback settings for Influenza diseases:';

SELECT 
    d.Name AS [Disease Name],
    CASE WHEN dm.SpecimenType_UseTextFallback = 1 THEN 'Yes' ELSE 'No' END AS [Specimen Text Fallback],
    CASE WHEN dm.Pathogen_UseTextFallback = 1 THEN 'Yes' ELSE 'No' END AS [Pathogen Text Fallback],
    CASE WHEN dm.TestMethod_UseTextFallback = 1 THEN 'Yes' ELSE 'No' END AS [TestMethod Text Fallback],
    CASE WHEN dm.TestResult_UseTextFallback = 1 THEN 'Yes' ELSE 'No' END AS [TestResult Text Fallback]
FROM Diseases d
LEFT JOIN DiseaseHL7MatchingConfigs dm ON d.Id = dm.DiseaseId
WHERE d.Name LIKE '%Influenza%'
   OR d.Name LIKE '%COVID%'
ORDER BY d.Name;

PRINT '';
PRINT '========================================';
PRINT 'OPTIONAL: Enable Text Fallback (Recommended)';
PRINT '========================================';
PRINT 'Text fallback allows matching by name if codes are not found.';
PRINT 'Uncomment the following lines to enable:';
PRINT '';

/*
-- Enable text fallback for Influenza A and B
UPDATE DiseaseHL7MatchingConfigs
SET SpecimenType_UseTextFallback = 1,
    Pathogen_UseTextFallback = 1,
    TestMethod_UseTextFallback = 1,
    ModifiedAt = GETUTCDATE()
WHERE DiseaseId IN (
    SELECT Id FROM Diseases WHERE Name LIKE '%Influenza%'
);

PRINT 'Text fallback enabled for Influenza diseases';
*/

-- ========================================
-- STEP 3: Verify Complete Configuration
-- ========================================
PRINT '';
PRINT '========================================';
PRINT 'STEP 3: Configuration Verification';
PRINT '========================================';
PRINT '';

-- Check if Influenza A has required case definitions
PRINT 'Influenza A Case Definitions:';
SELECT 
    cd.Name AS [Case Definition],
    cd.IsActive,
    cs.Name AS [Confirmation Status]
FROM CaseDefinitions cd
JOIN Diseases d ON cd.DiseaseId = d.Id
LEFT JOIN ConfirmationStatuses cs ON cd.ConfirmationStatusId = cs.Id
WHERE d.Name = 'Influenza A'
ORDER BY cd.IsActive DESC, cd.Name;

PRINT '';
PRINT 'Nasopharyngeal Swab in Case Definitions:';
SELECT 
    d.Name AS [Disease],
    cd.Name AS [Case Definition],
    st.Name AS [Specimen Type]
FROM CaseDefinitionSpecimenTypes cdst
JOIN CaseDefinitions cd ON cdst.CaseDefinitionId = cd.Id
JOIN Diseases d ON cd.DiseaseId = d.Id
JOIN SpecimenTypes st ON cdst.SpecimenTypeId = st.Id
WHERE st.Name LIKE '%Nasopharyngeal%'
   OR st.SnomedCode = '258500001'
ORDER BY d.Name, cd.Name;

PRINT '';
PRINT '========================================';
PRINT 'COMPLETE!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. ✅ Nasopharyngeal Swab now has SNOMED code 258500001';
PRINT '2. If specimen is still not resolving, enable text fallback (see OPTIONAL section above)';
PRINT '3. Re-process your HL7 file to test the fix';
PRINT '';
