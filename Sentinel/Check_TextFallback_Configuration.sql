-- Optional: Enable text fallback for specimen type matching
-- This allows matching by text name if SNOMED code is not found

-- Check current disease text matching configuration
SELECT 
    d.Name AS DiseaseName,
    dm.SpecimenType_UseTextFallback,
    dm.Pathogen_UseTextFallback,
    dm.TestMethod_UseTextFallback,
    dm.TestResult_UseTextFallback
FROM Diseases d
LEFT JOIN DiseaseHL7MatchingConfigs dm ON d.Id = dm.DiseaseId
WHERE d.Name LIKE '%Influenza%'
   OR d.Name LIKE '%COVID%'
ORDER BY d.Name;

-- If you want to enable text fallback for specimen types (optional):
-- UPDATE DiseaseHL7MatchingConfigs
-- SET SpecimenType_UseTextFallback = 1
-- WHERE DiseaseId IN (SELECT Id FROM Diseases WHERE Name LIKE '%Influenza%');
