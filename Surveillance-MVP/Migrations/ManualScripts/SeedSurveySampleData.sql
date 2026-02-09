-- ========================================================================
-- Survey System Sample Data
-- ========================================================================
-- This script provides sample survey configurations for testing
-- ========================================================================

-- Example 1: COVID-19 Contact Investigation Survey
-- ========================================================================

-- First, find or create a COVID-19 disease task template
-- Replace the GUID values with actual IDs from your database

DECLARE @TaskTemplateId uniqueidentifier = (
    SELECT TOP 1 Id FROM TaskTemplates WHERE Name LIKE '%Contact%Investigation%'
);

DECLARE @DiseaseId uniqueidentifier = (
    SELECT TOP 1 Id FROM Diseases WHERE Name LIKE '%COVID%19%'
);

-- Add survey definition to task template
UPDATE TaskTemplates
SET SurveyDefinitionJson = N'{
  "title": "COVID-19 Contact Investigation",
  "description": "Investigation form for COVID-19 case contacts",
  "showProgressBar": "top",
  "progressBarType": "pages",
  "pages": [
    {
      "name": "contactInfo",
      "title": "Contact Information",
      "elements": [
        {
          "type": "text",
          "name": "patientName",
          "title": "Patient Name",
          "isRequired": true,
          "readOnly": true
        },
        {
          "type": "text",
          "name": "caseNumber",
          "title": "Case Number",
          "readOnly": true
        },
        {
          "type": "text",
          "name": "dateOfContact",
          "title": "Date of Last Contact with Case",
          "inputType": "date",
          "isRequired": true
        },
        {
          "type": "dropdown",
          "name": "contactType",
          "title": "Type of Contact",
          "isRequired": true,
          "choices": [
            "Household Member",
            "Close Contact (< 6 feet for 15+ min)",
            "Casual Contact",
            "Healthcare Exposure"
          ]
        },
        {
          "type": "text",
          "name": "contactPhone",
          "title": "Contact Phone Number",
          "inputType": "tel"
        }
      ]
    },
    {
      "name": "symptoms",
      "title": "Symptom Assessment",
      "elements": [
        {
          "type": "boolean",
          "name": "hasSymptoms",
          "title": "Is the contact currently experiencing any COVID-19 symptoms?",
          "isRequired": true
        },
        {
          "type": "checkbox",
          "name": "symptomsList",
          "title": "Which symptoms is the contact experiencing?",
          "visibleIf": "{hasSymptoms} = true",
          "choices": [
            "Fever or chills",
            "Cough",
            "Shortness of breath",
            "Fatigue",
            "Muscle or body aches",
            "Headache",
            "Loss of taste or smell",
            "Sore throat",
            "Congestion or runny nose",
            "Nausea or vomiting",
            "Diarrhea"
          ]
        },
        {
          "type": "text",
          "name": "symptomOnsetDate",
          "title": "When did symptoms start?",
          "inputType": "date",
          "visibleIf": "{hasSymptoms} = true",
          "isRequired": true
        },
        {
          "type": "expression",
          "name": "daysSinceOnset",
          "title": "Days Since Symptom Onset",
          "expression": "dateDiff({symptomOnsetDate}, today())",
          "visibleIf": "{hasSymptoms} = true"
        }
      ]
    },
    {
      "name": "testing",
      "title": "Testing Recommendation",
      "elements": [
        {
          "type": "boolean",
          "name": "testRecommended",
          "title": "Is testing recommended for this contact?",
          "isRequired": true,
          "defaultValue": true
        },
        {
          "type": "text",
          "name": "recommendedTestDate",
          "title": "Recommended Test Date",
          "inputType": "date",
          "visibleIf": "{testRecommended} = true",
          "defaultValue": "today()"
        },
        {
          "type": "comment",
          "name": "testingInstructions",
          "title": "Testing Instructions Provided",
          "visibleIf": "{testRecommended} = true",
          "placeholder": "e.g., Testing location, what to bring, when to expect results"
        }
      ]
    },
    {
      "name": "isolation",
      "title": "Isolation/Quarantine Instructions",
      "elements": [
        {
          "type": "expression",
          "name": "calculatedQuarantineEnd",
          "title": "Calculated Quarantine End Date (10 days from last contact)",
          "expression": "addDays({dateOfContact}, 10)",
          "displayStyle": "date"
        },
        {
          "type": "text",
          "name": "quarantineEndDate",
          "title": "Recommended Quarantine End Date",
          "inputType": "date",
          "defaultValue": "{calculatedQuarantineEnd}",
          "isRequired": true
        },
        {
          "type": "comment",
          "name": "quarantineInstructions",
          "title": "Quarantine Instructions Provided to Contact",
          "isRequired": true,
          "placeholder": "Isolation location, support needs, monitoring instructions"
        },
        {
          "type": "boolean",
          "name": "understoodInstructions",
          "title": "Contact confirmed understanding of quarantine instructions",
          "isRequired": true
        }
      ]
    },
    {
      "name": "followup",
      "title": "Follow-up Plan",
      "elements": [
        {
          "type": "boolean",
          "name": "requiresFollowup",
          "title": "Does this contact require follow-up?",
          "isRequired": true,
          "defaultValue": true
        },
        {
          "type": "text",
          "name": "followupDate",
          "title": "Follow-up Contact Date",
          "inputType": "date",
          "visibleIf": "{requiresFollowup} = true",
          "isRequired": true,
          "defaultValue": "addDays(today(), 3)"
        },
        {
          "type": "dropdown",
          "name": "followupMethod",
          "title": "Follow-up Method",
          "visibleIf": "{requiresFollowup} = true",
          "choices": ["Phone Call", "Email", "Text Message", "In-Person Visit"]
        },
        {
          "type": "comment",
          "name": "additionalNotes",
          "title": "Additional Notes",
          "placeholder": "Any other relevant information about this contact investigation"
        }
      ]
    }
  ]
}'
WHERE Id = @TaskTemplateId;

PRINT '? Survey definition added to task template';

-- Configure input mappings for pre-population
IF EXISTS (
    SELECT 1 FROM DiseaseTaskTemplates 
    WHERE TaskTemplateId = @TaskTemplateId AND DiseaseId = @DiseaseId
)
BEGIN
    UPDATE DiseaseTaskTemplates
    SET InputMappingJson = N'{
      "patientName": "Patient.GivenName",
      "caseNumber": "Case.FriendlyId",
      "contactPhone": "Patient.MobilePhone"
    }'
    WHERE TaskTemplateId = @TaskTemplateId 
      AND DiseaseId = @DiseaseId;
    
    PRINT '? Input mappings configured';
END
ELSE
BEGIN
    PRINT '? DiseaseTaskTemplate not found - create it first';
END

-- Configure output mappings to save responses
IF EXISTS (
    SELECT 1 FROM DiseaseTaskTemplates 
    WHERE TaskTemplateId = @TaskTemplateId AND DiseaseId = @DiseaseId
)
BEGIN
    UPDATE DiseaseTaskTemplates
    SET OutputMappingJson = N'{
      "dateOfContact": "Case.LastContactDate",
      "quarantineEndDate": "Case.QuarantineEndDate",
      "requiresFollowup": "Case.RequiresFollowup"
    }'
    WHERE TaskTemplateId = @TaskTemplateId 
      AND DiseaseId = @DiseaseId;
    
    PRINT '? Output mappings configured';
END

PRINT '';
PRINT 'COVID-19 Contact Investigation survey configured successfully!';
PRINT '';

-- ========================================================================
-- Example 2: Simple Symptom Screening Survey
-- ========================================================================

DECLARE @SymptomTaskId uniqueidentifier = (
    SELECT TOP 1 Id FROM TaskTemplates WHERE Name LIKE '%Symptom%Screen%'
);

IF @SymptomTaskId IS NOT NULL
BEGIN
    UPDATE TaskTemplates
    SET SurveyDefinitionJson = N'{
      "title": "Daily Symptom Screening",
      "description": "Daily health check for monitored individuals",
      "elements": [
        {
          "type": "text",
          "name": "screeningDate",
          "title": "Screening Date",
          "inputType": "date",
          "defaultValue": "today()",
          "isRequired": true,
          "readOnly": true
        },
        {
          "type": "boolean",
          "name": "hasFever",
          "title": "Do you have a fever (?100.4°F / 38°C)?",
          "isRequired": true
        },
        {
          "type": "text",
          "name": "temperature",
          "title": "Temperature (°F)",
          "inputType": "number",
          "visibleIf": "{hasFever} = true",
          "min": 95,
          "max": 110
        },
        {
          "type": "checkbox",
          "name": "symptoms",
          "title": "Are you experiencing any of the following symptoms?",
          "choices": [
            "Cough",
            "Shortness of breath",
            "Difficulty breathing",
            "Fatigue",
            "Body aches",
            "Headache",
            "Sore throat",
            "Loss of taste or smell"
          ]
        },
        {
          "type": "radiogroup",
          "name": "overallHealth",
          "title": "How would you rate your overall health today?",
          "isRequired": true,
          "choices": [
            "Excellent - No symptoms",
            "Good - Minor symptoms, manageable",
            "Fair - Symptoms affecting daily activities",
            "Poor - Severe symptoms, need medical attention"
          ]
        },
        {
          "type": "boolean",
          "name": "needsAssistance",
          "title": "Do you need any assistance or have concerns?",
          "isRequired": true
        },
        {
          "type": "comment",
          "name": "concerns",
          "title": "Please describe your concerns",
          "visibleIf": "{needsAssistance} = true",
          "isRequired": true
        }
      ]
    }'
    WHERE Id = @SymptomTaskId;
    
    PRINT '? Daily symptom screening survey configured';
END

-- ========================================================================
-- Example 3: Risk Assessment Survey
-- ========================================================================

DECLARE @RiskTaskId uniqueidentifier = (
    SELECT TOP 1 Id FROM TaskTemplates WHERE Name LIKE '%Risk%Assessment%'
);

IF @RiskTaskId IS NOT NULL
BEGIN
    UPDATE TaskTemplates
    SET SurveyDefinitionJson = N'{
      "title": "Risk Assessment",
      "description": "Assess risk factors for disease transmission",
      "pages": [
        {
          "name": "demographics",
          "title": "Demographics & Risk Factors",
          "elements": [
            {
              "type": "text",
              "name": "age",
              "title": "Age",
              "inputType": "number",
              "isRequired": true
            },
            {
              "type": "checkbox",
              "name": "comorbidities",
              "title": "Pre-existing Health Conditions",
              "choices": [
                "Diabetes",
                "Heart Disease",
                "Chronic Lung Disease",
                "Immunocompromised",
                "Obesity (BMI ?30)",
                "Chronic Kidney Disease",
                "Cancer (active treatment)"
              ]
            },
            {
              "type": "radiogroup",
              "name": "vaccinationStatus",
              "title": "Vaccination Status",
              "isRequired": true,
              "choices": [
                "Fully vaccinated + booster",
                "Fully vaccinated",
                "Partially vaccinated",
                "Not vaccinated"
              ]
            }
          ]
        },
        {
          "name": "exposure",
          "title": "Exposure Assessment",
          "elements": [
            {
              "type": "radiogroup",
              "name": "exposureSetting",
              "title": "Primary Exposure Setting",
              "isRequired": true,
              "choices": [
                "Household",
                "Workplace",
                "Healthcare Facility",
                "School/Childcare",
                "Social Gathering",
                "Travel",
                "Unknown"
              ]
            },
            {
              "type": "radiogroup",
              "name": "exposureDuration",
              "title": "Duration of Exposure",
              "isRequired": true,
              "choices": [
                "< 15 minutes",
                "15-30 minutes",
                "30-60 minutes",
                "> 1 hour",
                "Multiple hours/days"
              ]
            },
            {
              "type": "boolean",
              "name": "maskWorn",
              "title": "Was a mask worn during exposure?",
              "isRequired": true
            },
            {
              "type": "boolean",
              "name": "ventilatedSpace",
              "title": "Was the space well-ventilated?",
              "isRequired": true
            }
          ]
        },
        {
          "name": "riskCalculation",
          "title": "Risk Assessment Result",
          "elements": [
            {
              "type": "html",
              "name": "riskInfo",
              "html": "<p><strong>Risk Score Calculation:</strong></p><ul><li>Age ?65 or comorbidities: Higher risk</li><li>Unvaccinated: Higher risk</li><li>Household/prolonged exposure: Higher risk</li><li>No mask + poor ventilation: Higher risk</li></ul>"
            },
            {
              "type": "dropdown",
              "name": "overallRisk",
              "title": "Overall Risk Assessment",
              "isRequired": true,
              "choices": [
                "Low Risk",
                "Moderate Risk",
                "High Risk",
                "Very High Risk"
              ]
            },
            {
              "type": "comment",
              "name": "riskJustification",
              "title": "Risk Assessment Justification",
              "isRequired": true,
              "placeholder": "Explain the risk level determination"
            },
            {
              "type": "comment",
              "name": "recommendedActions",
              "title": "Recommended Actions",
              "isRequired": true,
              "placeholder": "What interventions or follow-up are recommended?"
            }
          ]
        }
      ]
    }'
    WHERE Id = @RiskTaskId;
    
    PRINT '? Risk assessment survey configured';
END

PRINT '';
PRINT '========================================================================';
PRINT 'Survey System Sample Data Installation Complete';
PRINT '========================================================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Assign tasks with surveys to users';
PRINT '2. Navigate to "My Tasks" dashboard';
PRINT '3. Click "Survey" button on tasks with surveys';
PRINT '4. Complete the survey and submit';
PRINT '5. Check CaseTasks.SurveyResponseJson for saved data';
PRINT '';
PRINT 'To view completed surveys:';
PRINT 'SELECT Title, SurveyResponseJson FROM CaseTasks WHERE SurveyResponseJson IS NOT NULL';
PRINT '';
