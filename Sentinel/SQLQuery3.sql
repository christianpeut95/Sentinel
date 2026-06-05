-- Clear the message so you can reprocess it
DELETE FROM HL7Messages WHERE MessageControlId = 'MSG212436';
DELETE FROM HL7MessageSegments WHERE HL7MessageId NOT IN (SELECT Id FROM HL7Messages);