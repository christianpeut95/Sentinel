-- =============================================
-- Add Chronic Hepatitis B Disease and Case Definition
-- =============================================
-- This script adds:
-- 1. Chronic Hepatitis B disease
-- 2. Case definition with 3 required lab criteria (HBsAg+, Anti-HBs-, Anti-HBc+)
-- =============================================

-- Variables for lookups (must query from database)
DECLARE @DiseaseId UNIQUEIDENTIFIER = NEWID();
DECLARE @CaseDefinitionId INT;
DECLARE @ConfirmationStatusId INT;
DECLARE @DiseaseCategoryId UNIQUEIDENTIFIER;

-- Get Confirmed status
SELECT @ConfirmationStatusId = Id FROM CaseStatuses WHERE Name = 'Confirmed';

-- Get Sexually Transmitted Infections category
SELECT @DiseaseCategoryId = Id FROM DiseaseCategories WHERE Name = 'Sexually Transmitted Infections';

-- Get pathogen IDs (from seeded data)
DECLARE @HBsAgPathogenId UNIQUEIDENTIFIER;
DECLARE @AntiHBsPathogenId UNIQUEIDENTIFIER;
DECLARE @AntiHBcTotalPathogenId UNIQUEIDENTIFIER;

SELECT @HBsAgPathogenId = Id FROM Pathogens WHERE LOINCCode = '5196-1'; -- HBsAg
SELECT @AntiHBsPathogenId = Id FROM Pathogens WHERE LOINCCode = '5193-8'; -- Anti-HBs
SELECT @AntiHBcTotalPathogenId = Id FROM Pathogens WHERE LOINCCode = '13952-2'; -- Anti-HBc Total

-- Get specimen type ID for Serum
DECLARE @SerumSpecimenId INT;
SELECT @SerumSpecimenId = Id FROM SpecimenTypes WHERE Name = 'Serum';

-- Get test method ID for Immunoassay
DECLARE @ImmunoassayMethodId INT;
SELECT @ImmunoassayMethodId = Id FROM TestMethods WHERE Name = 'Immunoassay';

-- =============================================
-- 1. Add Disease
-- =============================================
IF NOT EXISTS (SELECT 1 FROM Diseases WHERE Name = 'Chronic Hepatitis B')
BEGIN
    INSERT INTO Diseases (
        Id,
        Name,
        Code,
        ExportCode,
        Description,
        DiseaseCategoryId,
        IsActive,
        PathIds,
        Level,
        DisplayOrder,
        AccessLevel,
        ExposureTrackingMode,
        DefaultToResidentialAddress,
        AlwaysPromptForLocation,
        SyncWithPatientAddressUpdates,
        AddressReviewWindowBeforeDays,
        AddressReviewWindowAfterDays,
        CheckJurisdictionCrossing,
        JurisdictionFieldsToCheck,
        InheritAddressSettingsFromParent,
        RequireGeographicCoordinates,
        AllowDomesticAcquisition,
        ReviewGroupingWindowHours,
        ReviewAutoQueueLabResults,
        ReviewAutoQueueExposures,
        ReviewAutoQueueContacts,
        ReviewAutoQueueConfirmationChanges,
        ReviewAutoQueueDiseaseChanges,
        ReviewAutoQueueClinicalNotifications,
        ReviewAutoQueueNewCases,
        ReviewDefaultPriority,
        CreatedAt
    )
    VALUES (
        @DiseaseId,
        'Chronic Hepatitis B',
        'B18.1',
        'HEPB_CHR',
        'Chronic viral hepatitis B infection characterized by persistent HBsAg positivity, absence of anti-HBs, and presence of anti-HBc antibodies',
        @DiseaseCategoryId,
        1, -- IsActive
        CAST(@DiseaseId AS NVARCHAR(36)), -- PathIds
        0, -- Level (top level)
        1, -- DisplayOrder
        0, -- AccessLevel = Public
        1, -- ExposureTrackingMode = Optional
        0, -- DefaultToResidentialAddress
        0, -- AlwaysPromptForLocation
        0, -- SyncWithPatientAddressUpdates
        30, -- AddressReviewWindowBeforeDays
        90, -- AddressReviewWindowAfterDays
        1, -- CheckJurisdictionCrossing
        '1,2,3', -- JurisdictionFieldsToCheck
        1, -- InheritAddressSettingsFromParent
        0, -- RequireGeographicCoordinates
        1, -- AllowDomesticAcquisition
        6, -- ReviewGroupingWindowHours
        1, -- ReviewAutoQueueLabResults
        0, -- ReviewAutoQueueExposures
        0, -- ReviewAutoQueueContacts
        1, -- ReviewAutoQueueConfirmationChanges
        1, -- ReviewAutoQueueDiseaseChanges
        0, -- ReviewAutoQueueClinicalNotifications
        0, -- ReviewAutoQueueNewCases
        1, -- ReviewDefaultPriority
        GETUTCDATE()
    );

    PRINT 'Added Disease: Chronic Hepatitis B';
END
ELSE
BEGIN
    -- Update existing disease ID
    SELECT @DiseaseId = Id FROM Diseases WHERE Name = 'Chronic Hepatitis B';
    PRINT 'Disease "Chronic Hepatitis B" already exists';
END

-- =============================================
-- 2. Add Case Definition
-- =============================================
IF NOT EXISTS (SELECT 1 FROM CaseDefinitions WHERE DiseaseId = @DiseaseId AND Name = 'Chronic Hepatitis B - Laboratory Confirmed')
BEGIN
    INSERT INTO CaseDefinitions (
        DiseaseId,
        Name,
        Status,
        ConfirmationStatusId,
        DateActiveFrom,
        DateActiveTo,
        ApplyToChildDiseases,
        AllowAutoClassification,
        EnableAutoEvaluation,
        CreateReviewQueueOnChange,
        CreateReviewQueueOnSuggestion,
        CreatedAt
    )
    VALUES (
        @DiseaseId,
        'Chronic Hepatitis B - Laboratory Confirmed',
        1, -- Current
        @ConfirmationStatusId,
        '2026-01-01',
        NULL,
        0, -- ApplyToChildDiseases = false
        0, -- AllowAutoClassification = false
        1, -- EnableAutoEvaluation = true
        0, -- CreateReviewQueueOnChange = false
        1, -- CreateReviewQueueOnSuggestion = true
        GETUTCDATE()
    );

    SET @CaseDefinitionId = SCOPE_IDENTITY();
    PRINT 'Added Case Definition: Chronic Hepatitis B - Laboratory Confirmed (ID: ' + CAST(@CaseDefinitionId AS NVARCHAR(10)) + ')';
END
ELSE
BEGIN
    SELECT @CaseDefinitionId = Id FROM CaseDefinitions WHERE DiseaseId = @DiseaseId AND Name = 'Chronic Hepatitis B - Laboratory Confirmed';
    PRINT 'Case Definition already exists (ID: ' + CAST(@CaseDefinitionId AS NVARCHAR(10)) + ')';
END

-- =============================================
-- 3. Add Lab Criteria
-- =============================================

-- Criterion 1: HBsAg Positive (REQUIRED)
IF NOT EXISTS (
    SELECT 1 FROM CaseDefinitionCriteria 
    WHERE CaseDefinitionId = @CaseDefinitionId 
    AND CanonicalPathogenId = @HBsAgPathogenId
)
BEGIN
    INSERT INTO CaseDefinitionCriteria (
        CaseDefinitionId,
        CriterionType,
        IsRequired,
        LogicalOperator,
        GroupNumber,
        CanonicalPathogenId,
        AcceptablePathogensJson,
        CanonicalSpecimenTypeId,
        AcceptableSpecimenTypesJson,
        CanonicalTestMethodId,
        AcceptableTestMethodsJson,
        BiomarkerStoragePreference,
        Description,
        DisplayText,
        DisplayOrder,
        CreatedAt
    )
    VALUES (
        @CaseDefinitionId,
        0, -- Laboratory
        1, -- IsRequired = true
        0, -- AND
        1, -- GroupNumber
        @HBsAgPathogenId,
        '["' + CAST(@HBsAgPathogenId AS NVARCHAR(36)) + '"]', -- JSON array of GUID
        @SerumSpecimenId,
        '[' + CAST(@SerumSpecimenId AS NVARCHAR(10)) + ']',
        @ImmunoassayMethodId,
        '[' + CAST(@ImmunoassayMethodId AS NVARCHAR(10)) + ']',
        1, -- StoreCanonicalId
        'HBsAg Positive (Hepatitis B Surface Antigen detected)',
        'HBsAg Positive (Hepatitis B Surface Antigen detected)',
        1,
        GETUTCDATE()
    );

    PRINT 'Added Criterion 1: HBsAg Positive';
END

-- Criterion 2: Anti-HBs Negative (REQUIRED)
IF NOT EXISTS (
    SELECT 1 FROM CaseDefinitionCriteria 
    WHERE CaseDefinitionId = @CaseDefinitionId 
    AND CanonicalPathogenId = @AntiHBsPathogenId
)
BEGIN
    INSERT INTO CaseDefinitionCriteria (
        CaseDefinitionId,
        CriterionType,
        IsRequired,
        LogicalOperator,
        GroupNumber,
        CanonicalPathogenId,
        AcceptablePathogensJson,
        CanonicalSpecimenTypeId,
        AcceptableSpecimenTypesJson,
        CanonicalTestMethodId,
        AcceptableTestMethodsJson,
        BiomarkerStoragePreference,
        Description,
        DisplayText,
        DisplayOrder,
        CreatedAt
    )
    VALUES (
        @CaseDefinitionId,
        0, -- Laboratory
        1, -- IsRequired = true
        0, -- AND
        1, -- GroupNumber
        @AntiHBsPathogenId,
        '["' + CAST(@AntiHBsPathogenId AS NVARCHAR(36)) + '"]', -- JSON array of GUID
        @SerumSpecimenId,
        '[' + CAST(@SerumSpecimenId AS NVARCHAR(10)) + ']',
        @ImmunoassayMethodId,
        '[' + CAST(@ImmunoassayMethodId AS NVARCHAR(10)) + ']',
        1, -- StoreCanonicalId
        'Anti-HBs Negative (Hepatitis B Surface Antibody NOT detected - indicates no immunity)',
        'Anti-HBs Negative (Hepatitis B Surface Antibody NOT detected - indicates no immunity)',
        2,
        GETUTCDATE()
    );

    PRINT 'Added Criterion 2: Anti-HBs Negative';
END

-- Criterion 3: Anti-HBc Total Positive (REQUIRED)
IF NOT EXISTS (
    SELECT 1 FROM CaseDefinitionCriteria 
    WHERE CaseDefinitionId = @CaseDefinitionId 
    AND CanonicalPathogenId = @AntiHBcTotalPathogenId
)
BEGIN
    INSERT INTO CaseDefinitionCriteria (
        CaseDefinitionId,
        CriterionType,
        IsRequired,
        LogicalOperator,
        GroupNumber,
        CanonicalPathogenId,
        AcceptablePathogensJson,
        CanonicalSpecimenTypeId,
        AcceptableSpecimenTypesJson,
        CanonicalTestMethodId,
        AcceptableTestMethodsJson,
        BiomarkerStoragePreference,
        Description,
        DisplayText,
        DisplayOrder,
        CreatedAt
    )
    VALUES (
        @CaseDefinitionId,
        0, -- Laboratory
        1, -- IsRequired = true
        0, -- AND
        1, -- GroupNumber
        @AntiHBcTotalPathogenId,
        '["' + CAST(@AntiHBcTotalPathogenId AS NVARCHAR(36)) + '"]', -- JSON array of GUID
        @SerumSpecimenId,
        '[' + CAST(@SerumSpecimenId AS NVARCHAR(10)) + ']',
        @ImmunoassayMethodId,
        '[' + CAST(@ImmunoassayMethodId AS NVARCHAR(10)) + ']',
        1, -- StoreCanonicalId
        'Anti-HBc Total Positive (Hepatitis B Core Antibody detected - indicates past or current infection)',
        'Anti-HBc Total Positive (Hepatitis B Core Antibody detected - indicates past or current infection)',
        3,
        GETUTCDATE()
    );

    PRINT 'Added Criterion 3: Anti-HBc Total Positive';
END

PRINT '';
PRINT '========================================';
PRINT 'Chronic Hepatitis B setup complete!';
PRINT '========================================';
PRINT 'Disease ID: ' + CAST(@DiseaseId AS NVARCHAR(36));
PRINT 'Case Definition ID: ' + CAST(@CaseDefinitionId AS NVARCHAR(10));
PRINT '';
PRINT 'IMPORTANT: This case definition requires ALL three criteria:';
PRINT '  1. HBsAg POSITIVE';
PRINT '  2. Anti-HBs NEGATIVE';
PRINT '  3. Anti-HBc Total POSITIVE';
PRINT '';
PRINT 'Note: For HL7 processing, Anti-HBs negative results must be';
PRINT 'properly detected as "Negative" or "Not Detected" in the';
PRINT 'normalized result value for correct matching.';
