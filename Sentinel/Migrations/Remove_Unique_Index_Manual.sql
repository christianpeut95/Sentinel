-- Migration: Remove_HL7Message_Unique_Index_To_Allow_Duplicates
-- Purpose: Allow duplicate HL7 messages for audit trail purposes
-- Date: 2026-07-14

-- 1. Drop the existing UNIQUE index
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HL7Messages_MessageControlId_SendingFacility' AND object_id = OBJECT_ID('dbo.HL7Messages'))
BEGIN
    PRINT 'Dropping unique index IX_HL7Messages_MessageControlId_SendingFacility...'
    DROP INDEX [IX_HL7Messages_MessageControlId_SendingFacility] ON [dbo].[HL7Messages]
    PRINT 'Index dropped successfully.'
END
ELSE
BEGIN
    PRINT 'Index IX_HL7Messages_MessageControlId_SendingFacility does not exist, skipping drop.'
END
GO

-- 2. Create a new NON-UNIQUE index for query performance
PRINT 'Creating non-unique index IX_HL7Messages_MessageControlId_SendingFacility...'
CREATE NONCLUSTERED INDEX [IX_HL7Messages_MessageControlId_SendingFacility]
    ON [dbo].[HL7Messages] ([MessageControlId], [SendingFacility])
    WHERE [MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL
GO

PRINT 'Migration completed successfully.'
PRINT 'HL7Messages table now allows duplicate MessageControlId + SendingFacility combinations.'
GO

-- Verify the new index
SELECT 
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.HL7Messages')
    AND i.name = 'IX_HL7Messages_MessageControlId_SendingFacility'
ORDER BY ic.key_ordinal
GO
