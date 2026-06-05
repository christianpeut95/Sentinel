-- Check if HL7 messages have ProcessingNotes
SELECT TOP 10
    MessageControlId,
    Status,
    ProcessedAt,
    CASE 
        WHEN ProcessingNotes IS NULL THEN 'NULL'
        WHEN LEN(ProcessingNotes) = 0 THEN 'EMPTY'
        ELSE 'HAS DATA (' + CAST(LEN(ProcessingNotes) AS VARCHAR) + ' chars)'
    END AS ProcessingNotesStatus,
    LEFT(ProcessingNotes, 200) AS ProcessingNotesPreview,
    ErrorMessage
FROM HL7Messages
ORDER BY ReceivedAt DESC;

-- Count of messages with and without ProcessingNotes
SELECT 
    CASE 
        WHEN ProcessingNotes IS NULL OR LEN(ProcessingNotes) = 0 THEN 'No ProcessingNotes'
        ELSE 'Has ProcessingNotes'
    END AS Category,
    COUNT(*) AS MessageCount
FROM HL7Messages
GROUP BY 
    CASE 
        WHEN ProcessingNotes IS NULL OR LEN(ProcessingNotes) = 0 THEN 'No ProcessingNotes'
        ELSE 'Has ProcessingNotes'
    END;
