-- Backfill NoSurveillanceItem flag for existing messages
-- This script identifies messages that were processed as "NoSurveillance" 
-- before the NoSurveillanceItem column was added

-- First, let's see how many messages need backfilling
SELECT COUNT(*) as MessagesToBackfill
FROM HL7Messages
WHERE Status = 1 -- ProcessedSuccessfully
  AND NoSurveillanceItem = 0
  AND RequiresManualReview = 0
  AND ManualReviewCompleted = 0
  AND (
	ProcessingNotes LIKE '%not in surveillance list%'
	OR ProcessingNotes LIKE '%No surveillance configuration%'
	OR ProcessingNotes LIKE '%NoSurveillance%'
	OR (ProcessedAt IS NOT NULL AND PatientId IS NULL AND CaseId IS NULL)
  );

-- Preview the messages that will be updated
SELECT 
	Id,
	MessageControlId,
	Status,
	ProcessingNotes,
	ReceivedAt,
	ProcessedAt
FROM HL7Messages
WHERE Status = 1 -- ProcessedSuccessfully
  AND NoSurveillanceItem = 0
  AND RequiresManualReview = 0
  AND ManualReviewCompleted = 0
  AND (
	ProcessingNotes LIKE '%not in surveillance list%'
	OR ProcessingNotes LIKE '%No surveillance configuration%'
	OR ProcessingNotes LIKE '%NoSurveillance%'
	OR (ProcessedAt IS NOT NULL AND PatientId IS NULL AND CaseId IS NULL)
  )
ORDER BY ReceivedAt DESC;

-- UNCOMMMENT THE FOLLOWING TO APPLY THE BACKFILL:
/*
BEGIN TRANSACTION;

UPDATE HL7Messages
SET NoSurveillanceItem = 1
WHERE Status = 1 -- ProcessedSuccessfully
  AND NoSurveillanceItem = 0
  AND RequiresManualReview = 0
  AND ManualReviewCompleted = 0
  AND (
	ProcessingNotes LIKE '%not in surveillance list%'
	OR ProcessingNotes LIKE '%No surveillance configuration%'
	OR ProcessingNotes LIKE '%NoSurveillance%'
	OR (ProcessedAt IS NOT NULL AND PatientId IS NULL AND CaseId IS NULL)
  );

SELECT @@ROWCOUNT as RowsUpdated;

-- Review the changes before committing
SELECT 
	Id,
	MessageControlId,
	NoSurveillanceItem,
	ProcessingNotes
FROM HL7Messages
WHERE NoSurveillanceItem = 1
ORDER BY ReceivedAt DESC;

-- If everything looks good, commit:
-- COMMIT;

-- If something looks wrong, rollback:
-- ROLLBACK;
*/
