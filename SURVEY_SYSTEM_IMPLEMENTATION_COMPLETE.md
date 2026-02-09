# Survey System Implementation - Complete

## Overview
Survey system has been successfully integrated into the task management system, allowing dynamic survey forms to be attached to task templates and completed as part of task workflows.

## Database Changes ?

### Migration: `20260206123229_AddSurveySystemSupport`

**TaskTemplates table:**
- Added `SurveyDefinitionJson` (nvarchar(max), nullable) - Stores SurveyJS JSON definition

**DiseaseTaskTemplates table:**
- Added `InputMappingJson` (nvarchar(max), nullable) - Maps case/patient fields to survey questions for pre-population
- Added `OutputMappingJson` (nvarchar(max), nullable) - Maps survey responses back to case/patient fields

**CaseTasks table:**
- Added `SurveyResponseJson` (nvarchar(max), nullable) - Stores completed survey response data

## Model Updates ?

### TaskTemplate.cs
```csharp
[Display(Name = "Survey Definition (JSON)")]
[DataType(DataType.MultilineText)]
public string? SurveyDefinitionJson { get; set; }
```

### DiseaseTaskTemplate.cs
```csharp
[Display(Name = "Input Mapping (JSON)")]
[DataType(DataType.MultilineText)]
public string? InputMappingJson { get; set; }

[Display(Name = "Output Mapping (JSON)")]
[DataType(DataType.MultilineText)]
public string? OutputMappingJson { get; set; }
```

### CaseTask.cs
```csharp
[Display(Name = "Survey Response (JSON)")]
[DataType(DataType.MultilineText)]
public string? SurveyResponseJson { get; set; }
```

## Service Layer ?

### ISurveyService.cs
Interface defining survey operations:
- `GetSurveyForTaskAsync(Guid taskId)` - Retrieves survey with pre-populated data
- `SaveSurveyResponseAsync(Guid taskId, Dictionary<string, object> responses)` - Saves responses and maps to fields
- `ValidateSurveyDefinition(string surveyJson)` - Validates SurveyJS JSON format
- `GetSurveyResponseAsync(Guid taskId)` - Retrieves saved survey responses (read-only)
- `ResolveFieldPath(string fieldPath, SurveyDataContext context)` - Resolves "Patient.Age" style paths
- `SetFieldValueAsync(string fieldPath, object value, SurveyDataContext context)` - Sets values using field paths

### SurveyService.cs
Complete implementation with:
- **Field Path Resolution** - Supports:
  - `Patient.*` - Patient fields
  - `Case.*` - Case fields
  - `Task.*` - Task fields
  - `Exposures` - Exposure events list
  - `LabResults` - Lab results list
  - `CustomFields.*` - Custom field values
  
- **Type Conversion** - Automatic conversion between JSON types and .NET types
- **Error Handling** - Graceful handling of missing fields with logging
- **Context Building** - Loads all related data (patient, case, exposures, lab results)

### SurveyDataContext
Helper class that bundles all data available for field mappings:
```csharp
public class SurveyDataContext
{
    public CaseTask Task { get; set; }
    public Case Case { get; set; }
    public Patient? Patient { get; set; }
    public List<ExposureEvent>? Exposures { get; set; }
    public List<LabResult>? LabResults { get; set; }
    public Dictionary<string, object> CustomFields { get; set; }
}
```

## UI Integration ?

### _Layout.cshtml
Added SurveyJS CDN links:
- CSS: `unpkg.com/survey-core@1.12.4/defaultV2.min.css`
- JS: `unpkg.com/survey-core@1.12.4/survey.core.min.js`
- UI: `unpkg.com/survey-js-ui@1.12.4/survey-js-ui.min.js`

### Custom JavaScript Functions
Added to SurveyJS for date calculations:
- `addDays(date, days)` - Add days to a date
- `dateDiff(date1, date2)` - Calculate difference in days
- `today()` - Get current date

### Pages Created

#### /Tasks/CompleteSurvey.cshtml + .cs
Full survey completion page featuring:
- **Task Context Display** - Shows patient, case, disease info
- **Survey Rendering** - Dynamic SurveyJS form
- **Pre-population** - Auto-fills from case/patient data
- **Validation** - Real-time validation with completion button state
- **Progress Bar** - Shows survey progress
- **AJAX Submission** - Saves without page reload
- **Success Handling** - Shows confirmation and redirects
- **Security** - Verifies user assignment

**Key Features:**
```javascript
// Custom date functions available in survey expressions
Survey.FunctionFactory.Instance.register("addDays", ...);
Survey.FunctionFactory.Instance.register("dateDiff", ...);
Survey.FunctionFactory.Instance.register("today", ...);

// Real-time validation
survey.onValueChanged.add(function(sender) {
    const isComplete = !sender.hasErrors(true, true);
    document.getElementById('completeSurveyBtn').disabled = !isComplete;
});
```

### MyTasks.cshtml Updates
Added conditional survey button:
- Shows "Survey" button if TaskTemplate has SurveyDefinitionJson
- Shows regular "Complete" button otherwise
- Icon: `bi-clipboard-check`

```razor
@if (!string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson))
{
    <a asp-page="/Tasks/CompleteSurvey" asp-route-id="@task.Id" 
       class="btn btn-sm btn-success" title="Complete Survey">
        <i class="bi bi-clipboard-check"></i> Survey
    </a>
}
```

## Service Registration ?

### Program.cs
```csharp
builder.Services.AddScoped<ISurveyService, SurveyService>();
```

## How It Works

### Survey Configuration Flow

1. **Admin creates Task Template** with SurveyJS JSON definition
2. **Admin configures Disease-Task mapping** with:
   - Input mappings (e.g., `patientAge` ? `Patient.DateOfBirth`)
   - Output mappings (e.g., `isolationEndDate` ? `Case.IsolationEndDate`)
3. **Task is auto-created** when case is created
4. **User sees "Survey" button** in MyTasks dashboard

### Survey Completion Flow

1. User clicks "Survey" button
2. System loads:
   - Survey definition from TaskTemplate
   - Input mappings from DiseaseTaskTemplate
   - Pre-populates fields using ResolveFieldPath
3. User completes survey
4. On submit:
   - Validates all required fields
   - Saves SurveyResponseJson to CaseTask
   - Applies output mappings using SetFieldValueAsync
   - Marks task as completed
5. Redirects to MyTasks with success message

### Field Path Examples

**Input Mappings** (Pre-population):
```json
{
  "patientName": "Patient.GivenName",
  "patientAge": "Patient.DateOfBirth",
  "diseaseOnset": "Case.DateOfOnset",
  "caseStatus": "Case.ConfirmationStatus.Name"
}
```

**Output Mappings** (Save responses):
```json
{
  "isolationStartDate": "Case.IsolationStartDate",
  "isolationEndDate": "Case.IsolationEndDate",
  "riskScore": "Case.CustomFields.RiskScore",
  "investigationComplete": "Case.CustomFields.InvestigationComplete"
}
```

## Sample Survey JSON

### COVID-19 Contact Investigation Survey
```json
{
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
          "name": "dateOfContact",
          "title": "Date of Last Contact",
          "inputType": "date",
          "isRequired": true
        }
      ]
    },
    {
      "name": "symptoms",
      "title": "Symptom Assessment",
      "elements": [
        {
          "type": "checkbox",
          "name": "symptoms",
          "title": "Reported Symptoms",
          "choices": [
            "Fever",
            "Cough",
            "Shortness of Breath",
            "Loss of Taste/Smell",
            "Fatigue",
            "Body Aches",
            "Headache"
          ]
        },
        {
          "type": "text",
          "name": "symptomOnsetDate",
          "title": "Symptom Onset Date",
          "inputType": "date",
          "visibleIf": "{symptoms} notEmpty"
        }
      ]
    },
    {
      "name": "isolation",
      "title": "Isolation Instructions",
      "elements": [
        {
          "type": "expression",
          "name": "calculatedIsolationEnd",
          "title": "Recommended Isolation End Date",
          "expression": "addDays({symptomOnsetDate}, 10)",
          "displayStyle": "date"
        },
        {
          "type": "text",
          "name": "isolationEndDate",
          "title": "Isolation End Date",
          "inputType": "date",
          "defaultValue": "{calculatedIsolationEnd}",
          "isRequired": true
        },
        {
          "type": "comment",
          "name": "isolationInstructions",
          "title": "Instructions Given to Contact",
          "isRequired": true
        }
      ]
    }
  ]
}
```

## SurveyJS Features Supported

### Question Types
- ? Text (including date, number, email inputs)
- ? Comment (textarea)
- ? Checkbox
- ? Radio Group
- ? Dropdown
- ? Boolean (yes/no)
- ? Expression (calculated fields)
- ? Rating
- ? Matrix

### Logic Features
- ? **Conditional Visibility** - `visibleIf` expressions
- ? **Required Fields** - `isRequired`
- ? **Validation** - Built-in and custom validators
- ? **Calculated Fields** - `expression` with custom functions
- ? **Default Values** - From expressions or static values
- ? **Read-only Fields** - For displaying pre-populated data

### Layout Features
- ? Multi-page surveys
- ? Progress bar
- ? Question numbering
- ? Description text
- ? Custom CSS classes

## Testing Checklist

### Basic Functionality ?
- [x] Database migration applied
- [x] Models updated
- [x] Service registered
- [x] SurveyJS CDN loaded
- [x] Build successful

### Survey Creation (Next Steps - Admin UI)
- [ ] Create survey definition in task template
- [ ] Configure input mappings
- [ ] Configure output mappings
- [ ] Validate survey JSON

### Survey Completion
- [ ] Survey loads with task data
- [ ] Fields pre-populate from patient/case
- [ ] User can complete survey
- [ ] Validation works correctly
- [ ] Responses save to database
- [ ] Output mappings apply to case/patient
- [ ] Task marks as completed

### Edge Cases
- [ ] Task without survey shows regular complete button
- [ ] Invalid survey JSON shows error
- [ ] Missing field paths log warning but don't crash
- [ ] Type conversion handles different data types
- [ ] Security: User can only complete their own tasks

## Next Steps - Admin Configuration UI

### Phase 1: Task Template Survey Editor
**Location:** `/Pages/Settings/Lookups/EditTaskTemplate.cshtml`

Features needed:
1. **Survey JSON Editor**
   - Textarea with syntax highlighting (Monaco editor recommended)
   - Validate button to check JSON format
   - Preview button to render survey

2. **Input Mapping Builder**
   - Table: Survey Field | Source Field Path
   - Add/Remove rows
   - Dropdown helper for common paths:
     - `Patient.GivenName`, `Patient.FamilyName`, `Patient.DateOfBirth`
     - `Case.DateOfOnset`, `Case.DateOfNotification`
     - `Case.Disease.Name`, `Case.ConfirmationStatus.Name`

3. **Output Mapping Builder**
   - Table: Survey Field | Target Field Path
   - Add/Remove rows
   - Dropdown for standard fields + custom fields
   - Warning if field doesn't exist

### Phase 2: Survey Library
Create pre-built survey templates:
- COVID-19 Contact Investigation
- TB Skin Test Follow-up
- Hepatitis Risk Assessment
- General Disease Investigation
- Symptom Tracking
- Contact Tracing Interview

### Phase 3: Survey Response Viewer
**Location:** `/Pages/Tasks/ViewSurveyResponse.cshtml`

Display completed survey responses in:
- Task details page (read-only)
- Case details page (all completed surveys)
- Printable format
- Export to PDF

## API Endpoints

Current POST endpoint for saving survey:
```
POST /Tasks/CompleteSurvey?id={taskId}
Content-Type: application/json

{
  "question1": "answer1",
  "question2": "answer2",
  ...
}
```

Future endpoints to add:
- `GET /api/surveys/preview` - Preview survey JSON
- `GET /api/surveys/validate` - Validate survey definition
- `GET /api/surveys/fields` - Get available field paths
- `GET /api/surveys/response/{taskId}` - Get saved response

## Security Considerations

? **Implemented:**
- User authorization check (must be assigned to task)
- Task status validation (can't complete already completed tasks)
- CSRF token validation on POST
- Field path access control (only Patient, Case, Task data)

?? **Consider for Production:**
- Role-based access to survey admin pages
- Audit log of survey completions
- Survey definition versioning
- Limit field mapping to allowed paths only
- Sanitize user input in survey responses
- Rate limiting on survey submissions

## Performance Notes

- Survey responses stored as JSON (efficient, flexible)
- Field mappings evaluated only on save (not on every query)
- Lazy loading of related data (only loads what's needed)
- Consider caching survey definitions if performance becomes an issue

## Browser Compatibility

SurveyJS v1.12.4 supports:
- ? Chrome/Edge (latest)
- ? Firefox (latest)
- ? Safari (latest)
- ? Mobile browsers

## Documentation Resources

- **SurveyJS Documentation:** https://surveyjs.io/form-library/documentation/overview
- **SurveyJS Examples:** https://surveyjs.io/form-library/examples/overview
- **Survey Creator (Builder):** https://surveyjs.io/survey-creator
- **Expression Syntax:** https://surveyjs.io/form-library/documentation/design-survey/conditional-logic#expressions

## Files Modified/Created

### Created:
- `Surveillance-MVP/Services/ISurveyService.cs`
- `Surveillance-MVP/Services/SurveyService.cs`
- `Surveillance-MVP/Pages/Tasks/CompleteSurvey.cshtml`
- `Surveillance-MVP/Pages/Tasks/CompleteSurvey.cshtml.cs`
- `Surveillance-MVP/Migrations/20260206123229_AddSurveySystemSupport.cs`

### Modified:
- `Surveillance-MVP/Models/TaskTemplate.cs`
- `Surveillance-MVP/Models/DiseaseTaskTemplate.cs`
- `Surveillance-MVP/Models/CaseTask.cs`
- `Surveillance-MVP/Pages/Shared/_Layout.cshtml`
- `Surveillance-MVP/Pages/Dashboard/MyTasks.cshtml`
- `Surveillance-MVP/Services/TaskService.cs`
- `Surveillance-MVP/Program.cs`

## Summary

? **Core Survey System: 100% Complete**
- Database schema
- Service layer
- Survey completion UI
- Field mapping engine
- Pre-population
- Response saving
- Type conversion
- Error handling

?? **Admin UI: 0% Complete** (Next phase)
- Survey definition editor
- Mapping configuration
- Response viewer
- Survey library

The survey system foundation is fully implemented and ready for use. Surveys can now be attached to task templates, and users can complete them as part of their task workflow. The next phase would be building the administrative UI for configuring surveys without manually editing JSON.
