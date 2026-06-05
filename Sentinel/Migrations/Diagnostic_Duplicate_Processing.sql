-- =====================================================
-- Diagnostic query to identify duplicate processing issue
-- =====================================================

PRINT '========== DUPLICATE PROCESSING DIAGNOSTIC ==========';
PRINT '';

-- 1. Check for duplicate HL7Messages (should be prevented by unique index now)
PRINT '1. Checking for duplicate HL7Messages...';
SELECT 
    MessageControlId,
    SendingFacility,
    COUNT(*) as Count,
    MIN(ReceivedAt) as FirstReceived,
    MAX(ReceivedAt) as LastReceived
FROM HL7Messages
WHERE MessageControlId IS NOT NULL
  AND MessageControlId <> ''
GROUP BY MessageControlId, SendingFacility
HAVING COUNT(*) > 1;

PRINT '';
PRINT '-------------------------------------------';
PRINT '';

-- 2. Check for patients created within last hour with same name/DOB
PRINT '2. Checking for duplicate patients (created in last hour)...';
SELECT 
    GivenName,
    FamilyName,
    DateOfBirth,
    COUNT(*) as Count,
    STRING_AGG(CAST(Id AS NVARCHAR(MAX)), ', ') as PatientIds,
    STRING_AGG(FriendlyId, ', ') as FriendlyIds,
    MIN(CreatedAt) as FirstCreated,
    MAX(CreatedAt) as LastCreated
FROM Patients
WHERE CreatedAt > DATEADD(HOUR, -1, GETUTCDATE())
  AND IsDeleted = 0
GROUP BY GivenName, FamilyName, DateOfBirth
HAVING COUNT(*) > 1;

PRINT '';
PRINT '-------------------------------------------';
PRINT '';

-- 3. Check recent HL7 messages and their linked patients
PRINT '3. Recent HL7Messages and linked patients...';
SELECT TOP 10
    m.Id as MessageId,
    m.MessageControlId,
    m.ReceivedAt,
    m.ProcessedAt,
    m.Status,
    m.PatientId,
    p.FriendlyId as PatientFriendlyId,
    p.GivenName + ' ' + p.FamilyName as PatientName
FROM HL7Messages m
LEFT JOIN Patients p ON m.PatientId = p.Id
ORDER BY m.ReceivedAt DESC;

PRINT '';
PRINT '-------------------------------------------';
PRINT '';

-- 4. Check for cases created by recent messages
PRINT '4. Cases created from recent HL7 messages...';
SELECT 
    m.MessageControlId,
    m.ReceivedAt,
    c.FriendlyId as CaseId,
    d.Name as DiseaseName,
    lr.FriendlyId as LabResultId,
    p.FriendlyId as PatientId,
    p.GivenName + ' ' + p.FamilyName as PatientName
FROM HL7Messages m
LEFT JOIN LabResults lr ON m.LabResultId = lr.Id
LEFT JOIN Cases c ON lr.CaseId = c.Id
LEFT JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN Patients p ON c.PatientId = p.Id
WHERE m.ReceivedAt > DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY m.ReceivedAt DESC;

PRINT '';
PRINT '========== END DIAGNOSTIC ==========';
