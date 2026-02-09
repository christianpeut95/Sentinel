# Survey System - Quick Reference

## Core Concept
Attach dynamic forms (surveys) to task templates. When tasks are created, users can complete surveys that pre-populate from case data and save responses back to case/patient fields.

## Quick Start - Manual Configuration

### 1. Add Survey to Task Template
```sql
UPDATE TaskTemplates
SET SurveyDefinitionJson = '{
  "title": "Contact Investigation",
  "elements": [
    {
      "type": "text",
      "name": "contactDate",
      "title": "Date of Contact",
      "inputType": "date",
      "isRequired": true
    },
    {
      "type": "checkbox",
      "name": "symptoms",
      "title": "Symptoms",
      "choices": ["Fever", "Cough", "Fatigue"]
    }
  ]
}'
WHERE Name = 'Contact Investigation';
```

### 2. Configure Input Mappings (Pre-population)
```sql
UPDATE DiseaseTaskTemplates
SET InputMappingJson = '{
  "patientName": "Patient.GivenName",
  "caseOnset": "Case.DateOfOnset"
}'
WHERE TaskTemplateId = '...' AND DiseaseId = '...';
```

### 3. Configure Output Mappings (Save responses)
```sql
UPDATE DiseaseTaskTemplates
SET OutputMappingJson = '{
  "contactDate": "Case.LastContactDate",
  "followupRequired": "Case.RequiresFollowup"
}'
WHERE TaskTemplateId = '...' AND DiseaseId = '...';
```

## Field Path Reference

### Available Root Objects
- `Patient.*` - Patient demographics
- `Case.*` - Case information
- `Task.*` - Current task details
- `Exposures` - List of exposure events
- `LabResults` - List of lab results

### Common Patient Fields
```
Patient.GivenName
Patient.FamilyName
Patient.DateOfBirth
Patient.HomePhone
Patient.MobilePhone
Patient.EmailAddress
Patient.AddressLine
```

### Common Case Fields
```
Case.FriendlyId
Case.DateOfOnset
Case.DateOfNotification
Case.Disease.Name
Case.ConfirmationStatus.Name
Case.Type
```

### Custom Fields
```
Case.CustomFields.{FieldName}
Patient.CustomFields.{FieldName}
```

## SurveyJS Cheat Sheet

### Basic Question Types
```json
{
  "type": "text",
  "name": "fieldName",
  "title": "Question Text",
  "isRequired": true
}

{
  "type": "checkbox",
  "name": "symptoms",
  "title": "Select all that apply",
  "choices": ["Option 1", "Option 2", "Option 3"]
}

{
  "type": "radiogroup",
  "name": "yesNo",
  "title": "Yes or No?",
  "choices": ["Yes", "No"]
}

{
  "type": "comment",
  "name": "notes",
  "title": "Additional notes"
}
```

### Conditional Logic
```json
{
  "type": "text",
  "name": "symptomDate",
  "title": "When did symptoms start?",
  "inputType": "date",
  "visibleIf": "{hasSymptoms} = 'Yes'"
}
```

### Calculated Fields
```json
{
  "type": "expression",
  "name": "isolationEnd",
  "title": "Isolation End Date",
  "expression": "addDays({symptomOnset}, 10)",
  "displayStyle": "date"
}
```

### Custom Date Functions
Available in expressions:
- `addDays(date, days)` - Add days to a date
- `dateDiff(date1, date2)` - Days between dates
- `today()` - Current date

Example:
```json
"expression": "dateDiff({symptomOnset}, today())"
```

## Complete Survey Example

```json
{
  "title": "COVID-19 Contact Tracing",
  "showProgressBar": "top",
  "pages": [
    {
      "name": "contact",
      "title": "Contact Details",
      "elements": [
        {
          "type": "text",
          "name": "patientName",
          "title": "Patient Name",
          "readOnly": true
        },
        {
          "type": "text",
          "name": "contactDate",
          "title": "Date of Last Contact",
          "inputType": "date",
          "isRequired": true
        },
        {
          "type": "dropdown",
          "name": "contactType",
          "title": "Type of Contact",
          "choices": ["Household", "Close Contact", "Casual Contact"],
          "isRequired": true
        }
      ]
    },
    {
      "name": "symptoms",
      "title": "Symptom Screening",
      "elements": [
        {
          "type": "boolean",
          "name": "hasSymptoms",
          "title": "Are you experiencing any symptoms?",
          "isRequired": true
        },
        {
          "type": "checkbox",
          "name": "symptomList",
          "title": "Which symptoms?",
          "visibleIf": "{hasSymptoms} = true",
          "choices": [
            "Fever",
            "Cough",
            "Shortness of Breath",
            "Loss of Taste or Smell",
            "Fatigue",
            "Body Aches"
          ]
        },
        {
          "type": "text",
          "name": "symptomOnset",
          "title": "When did symptoms start?",
          "inputType": "date",
          "visibleIf": "{hasSymptoms} = true",
          "isRequired": true
        }
      ]
    },
    {
      "name": "instructions",
      "title": "Isolation Instructions",
      "elements": [
        {
          "type": "expression",
          "name": "recommendedIsolationEnd",
          "title": "Recommended Isolation Period",
          "expression": "addDays({symptomOnset}, 10)",
          "displayStyle": "date",
          "visibleIf": "{hasSymptoms} = true"
        },
        {
          "type": "text",
          "name": "isolationEndDate",
          "title": "Isolation End Date",
          "inputType": "date",
          "defaultValue": "{recommendedIsolationEnd}",
          "isRequired": true
        },
        {
          "type": "comment",
          "name": "instructionsGiven",
          "title": "Instructions Provided to Contact",
          "isRequired": true
        },
        {
          "type": "boolean",
          "name": "requiresFollowup",
          "title": "Requires Follow-up Call?",
          "defaultValue": false
        }
      ]
    }
  ]
}
```

## Workflow

1. **User opens "My Tasks" dashboard**
2. **User sees task with survey button** (clipboard icon)
3. **User clicks "Survey" button**
4. **Survey loads with pre-populated data** from patient/case
5. **User completes survey questions**
6. **Validation happens in real-time**
7. **User clicks "Complete Survey"**
8. **System saves:**
   - Survey responses to `CaseTask.SurveyResponseJson`
   - Mapped fields to Case/Patient
   - Task status to Completed
9. **User redirected to dashboard** with success message

## Troubleshooting

### Survey doesn't show "Survey" button
- Check `TaskTemplate.SurveyDefinitionJson` is not null
- Rebuild/restart app to ensure include statement loads

### Survey shows but doesn't pre-populate
- Check `DiseaseTaskTemplate.InputMappingJson` exists
- Verify field paths are correct (case-sensitive)
- Check browser console for JavaScript errors
- Check application logs for field resolution warnings

### Survey saves but fields don't update
- Check `DiseaseTaskTemplate.OutputMappingJson` exists
- Verify target field paths exist and are writable
- Check application logs for field mapping warnings
- Ensure field names in survey match mapping keys exactly

### Validation Errors
```
Survey.FunctionFactory is not defined
```
- Ensure SurveyJS scripts load before custom code
- Check CDN links in _Layout.cshtml

## Useful SQL Queries

### Find tasks with surveys
```sql
SELECT tt.Name, tt.SurveyDefinitionJson
FROM TaskTemplates tt
WHERE tt.SurveyDefinitionJson IS NOT NULL;
```

### Find completed surveys
```sql
SELECT 
    ct.Title,
    c.FriendlyId AS CaseNumber,
    p.GivenName + ' ' + p.FamilyName AS PatientName,
    ct.CompletedAt,
    ct.SurveyResponseJson
FROM CaseTasks ct
INNER JOIN Cases c ON ct.CaseId = c.Id
INNER JOIN Patients p ON c.PatientId = p.Id
WHERE ct.SurveyResponseJson IS NOT NULL
ORDER BY ct.CompletedAt DESC;
```

### View survey responses for a case
```sql
SELECT 
    ct.Title,
    ct.CompletedAt,
    JSON_VALUE(ct.SurveyResponseJson, '$.contactDate') AS ContactDate,
    JSON_VALUE(ct.SurveyResponseJson, '$.hasSymptoms') AS HasSymptoms
FROM CaseTasks ct
WHERE ct.CaseId = '{caseId}'
    AND ct.SurveyResponseJson IS NOT NULL;
```

## Architecture

```
TaskTemplate
  ?? SurveyDefinitionJson (SurveyJS format)
  ?? DiseaseTaskTemplates
       ?? InputMappingJson (field ? survey question)
       ?? OutputMappingJson (survey response ? field)

CaseTask (created from template)
  ?? SurveyResponseJson (completed responses)

Survey Completion Flow:
  1. Load TaskTemplate.SurveyDefinitionJson
  2. Apply DiseaseTaskTemplate.InputMappingJson for pre-population
  3. User completes survey
  4. Save to CaseTask.SurveyResponseJson
  5. Apply DiseaseTaskTemplate.OutputMappingJson to update case/patient
```

## Service Methods

```csharp
// Get survey with pre-populated data
var surveyData = await _surveyService.GetSurveyForTaskAsync(taskId);
// Returns: SurveyDefinitionJson + PrePopulatedData dictionary

// Save survey response
await _surveyService.SaveSurveyResponseAsync(taskId, responses);
// Saves to SurveyResponseJson AND applies output mappings

// Get saved response (read-only)
var response = await _surveyService.GetSurveyResponseAsync(taskId);
// Returns: Dictionary<string, object>

// Validate survey JSON
bool isValid = _surveyService.ValidateSurveyDefinition(surveyJson);
```

## Next: Admin UI Development

Priority pages to build:
1. **Task Template Survey Editor** - Visual editor for survey JSON
2. **Mapping Configuration** - UI for input/output field mappings
3. **Survey Response Viewer** - Display completed surveys in case details
4. **Survey Library** - Pre-built templates for common diseases

See `SURVEY_SYSTEM_IMPLEMENTATION_COMPLETE.md` for detailed implementation plan.
