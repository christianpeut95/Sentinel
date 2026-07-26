-- Diagnostic query for 614dba04-b3bf-4c16-18bc-08dee9e75fc0
-- Run this in SQL Server Management Studio or Azure Data Studio

SELECT 
	Id,
	MessageControlId,
	Status,
	RequiresManualReview,
	ManualReviewCompleted,
	NoSurveillanceItem,
	ReviewOutcome,
	ProcessingNotes,
	ErrorMessage,
	ReceivedAt
FROM HL7Messages
WHERE Id = '614dba04-b3bf-4c16-18bc-08dee9e75fc0';

-- If the message has RequiresManualReview=0 and NoSurveillanceItem=0 but should be in review queue,
-- you can fix it with one of these:

-- Option 1: If it's a manual review case (error/conflict)
-- UPDATE HL7Messages 
-- SET RequiresManualReview = 1 
-- WHERE Id = '614dba04-b3bf-4c16-18bc-08dee9e75fc0';

-- Option 2: If it's a NoSurveillance case (processed but no case created)
-- UPDATE HL7Messages 
-- SET NoSurveillanceItem = 1 
-- WHERE Id = '614dba04-b3bf-4c16-18bc-08dee9e75fc0';
