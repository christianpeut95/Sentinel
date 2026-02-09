-- ========================================================================
-- Seed Exposure Tracking Configuration for Common Diseases
-- ========================================================================
-- This script configures exposure tracking settings for common diseases
-- Run after AddExposureTrackingEnhancements migration
-- ========================================================================

-- Overseas Acquired Diseases
-- These diseases require travel history and country selection

UPDATE Diseases
SET 
    ExposureTrackingMode = 3, -- OverseasAcquired
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Please specify the country/countries visited during the likely exposure period. Travel history is mandatory for this disease.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 0, -- Travel only
    ExposureDataGracePeriodDays = 14,
    RequiredLocationTypeIds = NULL
WHERE Name IN ('Malaria', 'Dengue', 'Yellow Fever', 'Zika Virus', 'Chikungunya');

-- Locally Acquired with Specific Location Requirements
-- Requires detailed location tracking

UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Please document all locations visited 10-21 days before symptom onset. Focus on food establishments, water sources, and swimming locations.',
    RequireGeographicCoordinates = 1,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 21,
    RequiredLocationTypeIds = NULL
WHERE Name IN ('Salmonella', 'Campylobacter', 'Cryptosporidiosis', 'Giardiasis', 'Listeriosis');

-- Measles - Highly specific location tracking needed
UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Document ALL locations visited 7-21 days before rash onset. Measles is highly contagious - complete exposure history is critical for contact tracing.',
    RequireGeographicCoordinates = 1,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 7, -- Urgent for contact tracing
    RequiredLocationTypeIds = NULL
WHERE Name = 'Measles';

-- Tuberculosis - Complex exposure tracking
UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 1, -- Often residential/household transmission
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 1,
    ExposureGuidanceText = 'Document household contacts and places of regular attendance (work, school, healthcare facilities). TB exposure period can be months to years.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 30,
    RequiredLocationTypeIds = NULL
WHERE Name = 'Tuberculosis';

-- Legionnaires Disease - Water system tracking critical
UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Focus on locations with water systems 2-10 days before symptom onset: hotels, spas, pools, cooling towers, fountains. Include any travel.',
    RequireGeographicCoordinates = 1,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 14,
    RequiredLocationTypeIds = NULL
WHERE Name = 'Legionellosis' OR Name LIKE '%Legionnaires%';

-- Locally Acquired - Soft Reminders
-- Exposure data is helpful but not critical

UPDATE Diseases
SET 
    ExposureTrackingMode = 1, -- LocallyAcquired
    DefaultToResidentialAddress = 1,
    AlwaysPromptForLocation = 0,
    SyncWithPatientAddressUpdates = 1,
    ExposureGuidanceText = 'Residential address is pre-filled. Update if exposure likely occurred elsewhere.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = NULL, -- No urgency
    RequiredLocationTypeIds = NULL
WHERE Name IN ('Ross River Virus', 'Barmah Forest Virus', 'Q Fever');

-- Influenza - Optional but track for surveillance
UPDATE Diseases
SET 
    ExposureTrackingMode = 0, -- Optional
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 0,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Exposure tracking is optional for influenza unless part of an outbreak investigation.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = NULL,
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%Influenza%' OR Name LIKE '%Flu%';

-- COVID-19 - Can be domestic or overseas
UPDATE Diseases
SET 
    ExposureTrackingMode = 1, -- LocallyAcquired
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Document high-risk exposure settings (household, healthcare, congregate settings) and any travel history in the 14 days before symptom onset.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 7,
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%COVID%' OR Name LIKE '%SARS-CoV-2%';

-- Hepatitis A - Food/water/travel
UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Document all food sources, restaurants, and travel 15-50 days before symptom onset. Pay special attention to shellfish, uncooked foods, and international travel.',
    RequireGeographicCoordinates = 1,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 14,
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%Hepatitis A%';

-- Pertussis (Whooping Cough) - Contact tracing critical
UPDATE Diseases
SET 
    ExposureTrackingMode = 1, -- LocallyAcquired
    DefaultToResidentialAddress = 1,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 1,
    ExposureGuidanceText = 'Document household, school, and workplace contacts. Exposure period is 6-20 days before symptom onset.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 7, -- Urgent for infant contacts
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%Pertussis%' OR Name LIKE '%Whooping%';

-- Meningococcal Disease - Urgent contact tracing
UPDATE Diseases
SET 
    ExposureTrackingMode = 1, -- LocallyAcquired
    DefaultToResidentialAddress = 1,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 1,
    ExposureGuidanceText = 'URGENT: Document close contacts immediately. Focus on household, intimate contacts, and enclosed spaces in the 10 days before onset.',
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 1, -- Extremely urgent
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%Meningococcal%';

-- Shigellosis - Foodborne/waterborne
UPDATE Diseases
SET 
    ExposureTrackingMode = 2, -- LocalSpecificRegion
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 1,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = 'Document food sources, restaurants, childcare attendance, and swimming locations 1-7 days before symptom onset.',
    RequireGeographicCoordinates = 1,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = 14,
    RequiredLocationTypeIds = NULL
WHERE Name LIKE '%Shigell%';

-- Generic defaults for unspecified diseases
-- Keep most at Optional to avoid disrupting existing workflows
UPDATE Diseases
SET 
    ExposureTrackingMode = 0, -- Optional
    DefaultToResidentialAddress = 0,
    AlwaysPromptForLocation = 0,
    SyncWithPatientAddressUpdates = 0,
    ExposureGuidanceText = NULL,
    RequireGeographicCoordinates = 0,
    AllowDomesticAcquisition = 1,
    ExposureDataGracePeriodDays = NULL,
    RequiredLocationTypeIds = NULL
WHERE ExposureTrackingMode IS NULL;

PRINT 'Exposure tracking configuration seeded successfully';
