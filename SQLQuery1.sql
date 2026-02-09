SELECT 
    ee.Id,
    ee.CaseId,
    ee.FreeTextLocation,
    ee.IsDefaultedFromResidentialAddress,
    ee.Description,
    ee.CreatedDate
FROM ExposureEvents ee
WHERE ee.CaseId = '8fe7b962-ee05-4602-88aa-ddafa126abe2'