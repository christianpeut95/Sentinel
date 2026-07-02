-- Fix: Add SNOMED code to Nasopharyngeal Swab specimen type
-- This allows the HL7 message SPM-4 field (258500001^Nasopharyngeal swab^SCT) to resolve correctly

UPDATE SpecimenTypes
SET SnomedCode = '258500001'
WHERE Name = 'Nasopharyngeal swab'
  AND (SnomedCode IS NULL OR SnomedCode = '');

-- Verify the update
SELECT 
    Id,
    Name,
    SnomedCode,
    Hl7Code,
    LoincSystemCode,
    IsActive
FROM SpecimenTypes
WHERE Name LIKE '%Nasopharyngeal%'
   OR SnomedCode = '258500001';
