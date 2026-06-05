-- =====================================================
-- Cleanup script for duplicate HL7Messages
-- Run this BEFORE applying the unique index migration
-- =====================================================

PRINT 'Starting cleanup of duplicate HL7Messages...';

-- Step 1: Identify duplicates
SELECT 
    MessageControlId, 
    SendingFacility, 
    COUNT(*) as DuplicateCount,
    MIN(ReceivedAt) as FirstReceived,
    MAX(ReceivedAt) as LastReceived
FROM HL7Messages
WHERE MessageControlId IS NOT NULL 
  AND SendingFacility IS NOT NULL
GROUP BY MessageControlId, SendingFacility
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;

PRINT '----------------------------------------------------';
PRINT 'Duplicate messages found above. Proceeding with cleanup...';
PRINT '----------------------------------------------------';

-- Step 2: Delete duplicate HL7Messages, keeping the FIRST one received
-- (The first one is most likely to have been processed successfully)
WITH DuplicateMessages AS (
    SELECT 
        Id,
        MessageControlId,
        SendingFacility,
        ReceivedAt,
        ROW_NUMBER() OVER (
            PARTITION BY MessageControlId, SendingFacility 
            ORDER BY ReceivedAt ASC, Id ASC
        ) as RowNum
    FROM HL7Messages
    WHERE MessageControlId IS NOT NULL 
      AND SendingFacility IS NOT NULL
)
DELETE FROM HL7Messages
WHERE Id IN (
    SELECT Id 
    FROM DuplicateMessages 
    WHERE RowNum > 1
);

PRINT CONCAT('Deleted ', @@ROWCOUNT, ' duplicate HL7Message records');

-- Step 3: Verify no duplicates remain
DECLARE @RemainingDuplicates INT;
SELECT @RemainingDuplicates = COUNT(*)
FROM (
    SELECT MessageControlId, SendingFacility
    FROM HL7Messages
    WHERE MessageControlId IS NOT NULL 
      AND SendingFacility IS NOT NULL
    GROUP BY MessageControlId, SendingFacility
    HAVING COUNT(*) > 1
) AS Dupes;

IF @RemainingDuplicates = 0
BEGIN
    PRINT '✅ SUCCESS: All duplicates have been removed. You can now apply the unique index migration.';
END
ELSE
BEGIN
    PRINT '⚠️ WARNING: Some duplicates still remain. Review and resolve manually.';
END

PRINT 'Cleanup complete.';
