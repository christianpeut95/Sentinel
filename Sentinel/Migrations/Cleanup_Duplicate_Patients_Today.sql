-- =====================================================
-- Cleanup duplicate patients created today
-- Keeps the FIRST patient created for each duplicate
-- =====================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '========== CLEANING UP DUPLICATE PATIENTS ==========';
PRINT '';

-- Show duplicates before cleanup
PRINT 'Duplicate patients found:';
SELECT 
    GivenName + ' ' + FamilyName as PatientName,
    DateOfBirth,
    COUNT(*) as DuplicateCount,
    STRING_AGG(FriendlyId, ', ') as FriendlyIds
FROM Patients
WHERE CreatedAt > CAST(GETDATE() AS DATE) -- Today
  AND IsDeleted = 0
GROUP BY GivenName, FamilyName, DateOfBirth
HAVING COUNT(*) > 1;

PRINT '';
PRINT '-------------------------------------------';
PRINT '';

-- Delete duplicate patients (keeping the first one)
-- WARNING: This will also delete their cases and lab results!
PRINT 'Deleting duplicate patients and their data...';

DECLARE @DeletedPatients INT = 0;
DECLARE @DeletedCases INT = 0;
DECLARE @DeletedLabResults INT = 0;
DECLARE @DeletedHL7Messages INT = 0;
DECLARE @DeletedReviewQueue INT = 0;

BEGIN TRANSACTION;

-- Get the IDs of duplicate patients to delete (keep the first created)
SELECT 
    Id,
    GivenName,
    FamilyName,
    DateOfBirth,
    CreatedAt,
    ROW_NUMBER() OVER (
        PARTITION BY GivenName, FamilyName, DateOfBirth 
        ORDER BY CreatedAt ASC, Id ASC
    ) as RowNum
INTO #DuplicatePatients
FROM Patients
WHERE CreatedAt > CAST(GETDATE() AS DATE)
  AND IsDeleted = 0;

-- Keep only duplicates to delete
SELECT Id 
INTO #PatientsToDelete 
FROM #DuplicatePatients 
WHERE RowNum > 1;

-- Get lab results to delete
SELECT Id 
INTO #LabResultsToDelete 
FROM LabResults 
WHERE PatientId IN (SELECT Id FROM #PatientsToDelete);

-- Delete HL7Messages FIRST (they reference LabResults)
UPDATE HL7Messages
SET LabResultId = NULL
WHERE LabResultId IN (SELECT Id FROM #LabResultsToDelete);
SET @DeletedHL7Messages = @@ROWCOUNT;

-- Delete lab results (they reference Cases)
DELETE FROM LabResults
WHERE PatientId IN (SELECT Id FROM #PatientsToDelete);
SET @DeletedLabResults = @@ROWCOUNT;

-- Delete ReviewQueue entries (they reference Cases)
DELETE FROM ReviewQueue
WHERE CaseId IN (SELECT Id FROM Cases WHERE PatientId IN (SELECT Id FROM #PatientsToDelete));
SET @DeletedReviewQueue = @@ROWCOUNT;

-- Then delete cases
DELETE FROM Cases 
WHERE PatientId IN (SELECT Id FROM #PatientsToDelete);
SET @DeletedCases = @@ROWCOUNT;

-- Soft delete the duplicate patients
UPDATE Patients
SET IsDeleted = 1, DeletedAt = GETUTCDATE()
WHERE Id IN (SELECT Id FROM #PatientsToDelete);
SET @DeletedPatients = @@ROWCOUNT;

DROP TABLE #DuplicatePatients;
DROP TABLE #PatientsToDelete;
DROP TABLE #LabResultsToDelete;

COMMIT TRANSACTION;

PRINT '';
PRINT CONCAT('✅ Unlinked ', @DeletedHL7Messages, ' HL7 messages');
PRINT CONCAT('✅ Deleted ', @DeletedLabResults, ' lab results');
PRINT CONCAT('✅ Deleted ', @DeletedReviewQueue, ' review queue entries');
PRINT CONCAT('✅ Deleted ', @DeletedCases, ' cases');
PRINT CONCAT('✅ Deleted ', @DeletedPatients, ' duplicate patients');
PRINT '';

-- Verify no duplicates remain
DECLARE @RemainingDuplicates INT;
SELECT @RemainingDuplicates = COUNT(*)
FROM (
    SELECT GivenName, FamilyName, DateOfBirth
    FROM Patients
    WHERE CreatedAt > CAST(GETDATE() AS DATE)
      AND IsDeleted = 0
    GROUP BY GivenName, FamilyName, DateOfBirth
    HAVING COUNT(*) > 1
) AS Dupes;

IF @RemainingDuplicates = 0
BEGIN
    PRINT '✅ SUCCESS: All duplicate patients have been removed.';
END
ELSE
BEGIN
    PRINT '⚠️ WARNING: Some duplicates still remain. Review manually.';
END

PRINT '';
PRINT '========== CLEANUP COMPLETE ==========';
