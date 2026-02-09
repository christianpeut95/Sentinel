-- ========================================================================
-- Seed Task Template Configuration for Common Diseases
-- ========================================================================
-- This script configures task templates and assigns them to diseases
-- Run after AddTaskManagementSystem migration
-- ========================================================================

-- ========================================================================
-- TASK TEMPLATES
-- ========================================================================

-- Measles: Isolation Task
DECLARE @MeaslesIsolationTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromOnset, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @MeaslesIsolationTaskId,
    'Measles Isolation',
    'Patient must remain in isolation for the entire infectious period',
    0, -- Isolation
    2, -- High
    0, -- OnCaseCreation
    1, -- Case
    4, -- 4 days from onset
    0, -- FromSymptomOnset
    0, -- Not recurring
    'Remain in isolation until 4 days after rash onset. Do not attend work, school, childcare, or public places. Avoid contact with susceptible individuals, especially pregnant women and immunocompromised persons.',
    0, -- Patient
    0, -- Inherit
    1, -- Active
    GETUTCDATE()
);

-- Measles: Contact Tracing Urgent Task
DECLARE @MeaslesContactTracingTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @MeaslesContactTracingTaskId,
    'Urgent Contact Tracing',
    'Identify and contact all exposed individuals immediately',
    6, -- ContactTracing
    3, -- Urgent
    0, -- OnCaseCreation
    1, -- Case
    1, -- 1 day from notification
    1, -- FromNotificationDate
    0,
    'URGENT: Document all contacts from 4 days before rash onset until isolation. Focus on household, workplace/school, healthcare, and public transport exposures. Measles is highly contagious.',
    1, -- Investigator
    0,
    1,
    GETUTCDATE()
);

-- Meningococcal: Prophylactic Antibiotics for Close Contacts
DECLARE @MeningoProphylaxisTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, RequiresEvidence, IsActive, CreatedAt
)
VALUES (
    @MeningoProphylaxisTaskId,
    'Prophylactic Antibiotics',
    'Close contacts must receive prophylactic antibiotics immediately',
    1, -- Medication
    3, -- Urgent
    1, -- OnContactIdentification
    2, -- Contact
    0, -- Immediately
    1, -- FromNotificationDate
    0,
    'Take prescribed prophylactic antibiotics as directed (typically Rifampicin, Ciprofloxacin, or Ceftriaxone). Contact your doctor or public health nurse if you experience any adverse effects. Upload prescription confirmation.',
    0, -- Patient
    0,
    1, -- Requires evidence
    1,
    GETUTCDATE()
);

-- COVID-19: Daily Symptom Monitoring for Contacts
DECLARE @CovidSymptomCheckTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromContact, DueCalculationMethod, IsRecurring, RecurrencePattern, RecurrenceDurationDays,
    Instructions, AssignmentType, InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @CovidSymptomCheckTaskId,
    'Daily Symptom Check',
    'Monitor for COVID-19 symptoms daily during quarantine period',
    2, -- Monitoring
    2, -- High
    1, -- OnContactIdentification
    2, -- Contact
    0,
    2, -- FromContactDate
    1, -- Is recurring
    0, -- Daily
    14, -- 14 days duration
    'Check temperature twice daily (morning and evening). Monitor for fever, cough, shortness of breath, loss of taste/smell, fatigue, or other COVID-19 symptoms. Report any symptoms immediately to public health.',
    0, -- Patient
    0,
    1,
    GETUTCDATE()
);

-- Tuberculosis: Contact Investigation
DECLARE @TbContactInvestigationTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @TbContactInvestigationTaskId,
    'TB Contact Investigation',
    'Comprehensive contact investigation for household and close contacts',
    6, -- ContactTracing
    2, -- High
    2, -- OnLabConfirmation (when confirmed as active TB)
    1, -- Case
    7, -- 7 days from confirmation
    1,
    0,
    'Conduct comprehensive contact investigation. Identify all household contacts and close contacts (>8 hours cumulative contact). Arrange TB screening (TST/IGRA) and chest X-rays for identified contacts. Document occupational exposures.',
    1, -- Investigator
    0,
    1,
    GETUTCDATE()
);

-- Salmonella: Food History Questionnaire
DECLARE @SalmonellaFoodHistoryTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @SalmonellaFoodHistoryTaskId,
    'Detailed Food History Questionnaire',
    'Complete comprehensive food history for outbreak investigation',
    3, -- Survey
    2, -- High
    0, -- OnCaseCreation
    1, -- Case
    7, -- 7 days from notification
    1,
    0,
    'Document all food consumed in the 3 days before symptom onset. Pay special attention to: raw/undercooked eggs, poultry, ground beef, unpasteurized dairy, fresh produce. Include restaurants, takeaway, home-prepared meals, and shared foods.',
    1, -- Investigator
    0, -- Inherit to all Salmonella subtypes
    1,
    GETUTCDATE()
);

-- Legionellosis: Water Exposure Investigation
DECLARE @LegionellaWaterInvestigationTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @LegionellaWaterInvestigationTaskId,
    'Water System Exposure Investigation',
    'Investigate all water system exposures in the 2-10 days before symptom onset',
    6, -- ContactTracing
    2, -- High
    0, -- OnCaseCreation
    1, -- Case
    5,
    1,
    0,
    'Document exposure to water systems 2-10 days before symptom onset. Focus on: hotels/accommodation, spas/hot tubs, pools, cooling towers, decorative fountains, medical/dental procedures. Document travel, work, and home water systems.',
    1, -- Investigator
    0,
    1,
    GETUTCDATE()
);

-- Pertussis: Identify Vulnerable Contacts
DECLARE @PertussisVulnerableContactsTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @PertussisVulnerableContactsTaskId,
    'Identify Vulnerable Contacts',
    'Urgent identification of infant and pregnant contacts',
    6, -- ContactTracing
    3, -- Urgent
    0, -- OnCaseCreation
    1, -- Case
    1, -- 1 day
    1,
    0,
    'URGENT: Identify all contacts with infants <12 months and pregnant women (especially 3rd trimester). Document household, workplace, childcare, and healthcare contacts. Infants are at high risk of severe disease.',
    1, -- Investigator
    0,
    1,
    GETUTCDATE()
);

-- Generic: Outbreak Questionnaire (Manual trigger)
DECLARE @GenericOutbreakQuestionnaireTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromNotification, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @GenericOutbreakQuestionnaireTaskId,
    'Outbreak Investigation Questionnaire',
    'Administer outbreak-specific questionnaire',
    3, -- Survey
    2, -- High
    5, -- Manual
    1, -- Case
    5,
    1,
    0,
    'Administer the outbreak-specific questionnaire. Document all exposures relevant to the current outbreak investigation. Follow outbreak control team instructions.',
    1, -- Investigator
    1, -- NoInheritance - only assigned manually
    1,
    GETUTCDATE()
);

-- Generic: Isolation for Infectious Period
DECLARE @GenericIsolationTaskId UNIQUEIDENTIFIER = NEWID();
INSERT INTO TaskTemplates (
    Id, Name, Description, Category, DefaultPriority, TriggerType, ApplicableToType,
    DueDaysFromOnset, DueCalculationMethod, IsRecurring, Instructions, AssignmentType,
    InheritanceBehavior, IsActive, CreatedAt
)
VALUES (
    @GenericIsolationTaskId,
    'Isolation During Infectious Period',
    'Patient must isolate during infectious period',
    0, -- Isolation
    2, -- High
    0, -- OnCaseCreation
    1, -- Case
    NULL, -- Disease-specific
    0,
    0,
    'Remain in isolation as directed by public health. Do not attend work, school, or public places while infectious. Follow disease-specific guidance on isolation duration.',
    0, -- Patient
    0,
    1,
    GETUTCDATE()
);

PRINT 'Task templates created successfully';

-- ========================================================================
-- DISEASE-TASK ASSIGNMENTS
-- ========================================================================

-- Measles
DECLARE @MeaslesId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name = 'Measles');
IF @MeaslesId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @MeaslesId, @MeaslesIsolationTaskId, 0, 0, 1, 1, 0, 0, 1, 1, GETUTCDATE()),
        (NEWID(), @MeaslesId, @MeaslesContactTracingTaskId, 0, 0, 1, 1, 0, 0, 2, 1, GETUTCDATE());
    PRINT 'Measles tasks assigned';
END

-- Meningococcal Disease
DECLARE @MeningoId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name LIKE '%Meningococcal%');
IF @MeningoId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @MeningoId, @MeningoProphylaxisTaskId, 0, 1, 1, 0, 1, 0, 1, 1, GETUTCDATE());
    PRINT 'Meningococcal tasks assigned';
END

-- COVID-19
DECLARE @CovidId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name LIKE '%COVID%');
IF @CovidId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @CovidId, @CovidSymptomCheckTaskId, 0, 0, 1, 0, 1, 0, 1, 1, GETUTCDATE());
    PRINT 'COVID-19 tasks assigned';
END

-- Tuberculosis
DECLARE @TbId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name = 'Tuberculosis');
IF @TbId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @TbId, @TbContactInvestigationTaskId, 0, 1, 1, 0, 0, 1, 1, 1, GETUTCDATE());
    PRINT 'Tuberculosis tasks assigned';
END

-- Salmonella (Parent) - Will cascade to all Salmonella subtypes
DECLARE @SalmonellaId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name = 'Salmonella' AND ParentDiseaseId IS NULL);
IF @SalmonellaId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @SalmonellaId, @SalmonellaFoodHistoryTaskId, 0, 1, 1, 1, 0, 0, 1, 1, GETUTCDATE());
    
    -- Propagate to children
    DECLARE @SalmonellaChildren TABLE (DiseaseId UNIQUEIDENTIFIER);
    INSERT INTO @SalmonellaChildren
    SELECT Id FROM Diseases WHERE PathIds LIKE '%/' + CAST(@SalmonellaId AS VARCHAR(36)) + '/%';
    
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, InheritedFromDiseaseId, ApplyToChildren, AllowChildOverride, DisplayOrder, IsActive, CreatedAt)
    SELECT NEWID(), DiseaseId, @SalmonellaFoodHistoryTaskId, 1, @SalmonellaId, 0, 1, 1, 1, GETUTCDATE()
    FROM @SalmonellaChildren;
    
    PRINT 'Salmonella tasks assigned and propagated to subtypes';
END

-- Legionellosis
DECLARE @LegionellaId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name = 'Legionellosis' OR Name LIKE '%Legionnaires%');
IF @LegionellaId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @LegionellaId, @LegionellaWaterInvestigationTaskId, 0, 0, 1, 1, 0, 0, 1, 1, GETUTCDATE());
    PRINT 'Legionellosis tasks assigned';
END

-- Pertussis
DECLARE @PertussisId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name LIKE '%Pertussis%' OR Name LIKE '%Whooping%');
IF @PertussisId IS NOT NULL
BEGIN
    INSERT INTO DiseaseTaskTemplates (Id, DiseaseId, TaskTemplateId, IsInherited, ApplyToChildren, AllowChildOverride, AutoCreateOnCaseCreation, AutoCreateOnContactCreation, AutoCreateOnLabConfirmation, DisplayOrder, IsActive, CreatedAt)
    VALUES 
        (NEWID(), @PertussisId, @PertussisVulnerableContactsTaskId, 0, 0, 1, 1, 0, 0, 1, 1, GETUTCDATE());
    PRINT 'Pertussis tasks assigned';
END

PRINT 'Disease-task assignments completed successfully';

-- ========================================================================
-- EXAMPLE: Child Disease Override
-- ========================================================================
-- If you want Salmonella Typhi to have different instructions:
/*
DECLARE @SalmonellaTyphiId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Diseases WHERE Name = 'Salmonella Typhi');
IF @SalmonellaTyphiId IS NOT NULL
BEGIN
    UPDATE DiseaseTaskTemplates
    SET 
        OverrideInstructions = 'Focus on food and water sources during international travel. Salmonella Typhi is primarily travel-related and transmitted through contaminated water.',
        OverridePriority = 3, -- Urgent
        ModifiedAt = GETUTCDATE()
    WHERE DiseaseId = @SalmonellaTyphiId 
      AND TaskTemplateId = @SalmonellaFoodHistoryTaskId
      AND IsInherited = 1;
    
    PRINT 'Salmonella Typhi override applied';
END
*/
