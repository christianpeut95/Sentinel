-- Add "Healthcare Provider" OrganizationType for ordering providers
-- This allows HL7 messages to automatically create/match ordering provider organizations

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Name = 'Healthcare Provider')
BEGIN
    INSERT INTO OrganizationTypes (Name, IsActive)
    VALUES ('Healthcare Provider', 1);

    PRINT '✅ Added "Healthcare Provider" organization type';
END
ELSE
BEGIN
    PRINT 'ℹ️ "Healthcare Provider" organization type already exists';
END

-- Show all organization types for reference
PRINT '';
PRINT 'Current OrganizationTypes:';
SELECT Id, Name, IsActive 
FROM OrganizationTypes 
ORDER BY Name;
