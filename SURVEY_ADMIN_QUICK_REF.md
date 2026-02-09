# Survey Admin UI - Quick Reference Card

## ?? Quick Start

### Configure a Survey (3 Steps)

#### Step 1: Add Survey to Task Template (2 minutes)
```
Settings ? Task Templates ? [Select Template] ? Edit
   ?
Survey Configuration Tab
   ?
Paste JSON ? Validate ? Preview ? Save
```

#### Step 2: Configure Mappings per Disease (1 minute)
```
Settings ? Diseases ? [Select Disease] ? Edit
   ?
Tasks & Surveys Tab ? [Expand Task]
   ?
Add Input/Output Mappings ? Save
```

#### Step 3: Test
```
Create Case ? Go to My Tasks ? Click Survey Button
```

---

## ?? Survey JSON Template

### Minimal Survey
```json
{
  "title": "Survey Title",
  "elements": [
    {
      "type": "text",
      "name": "fieldName",
      "title": "Question Text",
      "isRequired": true
    }
  ]
}
```

### Multi-Page Survey
```json
{
  "title": "Survey Title",
  "showProgressBar": "top",
  "pages": [
    {
      "name": "page1",
      "title": "Page 1",
      "elements": [...]
    },
    {
      "name": "page2",
      "title": "Page 2",
      "elements": [...]
    }
  ]
}
```

---

## ?? Common Question Types

```json
// Text input
{"type": "text", "name": "name", "title": "Your Name"}

// Text area
{"type": "comment", "name": "notes", "title": "Notes"}

// Date picker
{"type": "text", "inputType": "date", "name": "date", "title": "Date"}

// Number input
{"type": "text", "inputType": "number", "name": "age", "title": "Age"}

// Dropdown
{"type": "dropdown", "name": "status", "choices": ["Active", "Inactive"]}

// Radio buttons (single choice)
{"type": "radiogroup", "name": "choice", "choices": ["Yes", "No"]}

// Checkboxes (multiple choice)
{"type": "checkbox", "name": "symptoms", "choices": ["Fever", "Cough"]}

// Yes/No toggle
{"type": "boolean", "name": "confirm", "title": "Confirmed?"}

// Read-only calculated field
{"type": "expression", "name": "calc", "expression": "today()"}
```

---

## ??? Field Path Reference

### Input Mappings (Pre-populate FROM)
```json
{
  "surveyField": "Patient.GivenName",
  "caseId": "Case.FriendlyId",
  "onsetDate": "Case.DateOfOnset",
  "disease": "Case.Disease.Name",
  "phone": "Patient.MobilePhone",
  "email": "Patient.EmailAddress"
}
```

### Output Mappings (Save responses TO)
```json
{
  "isolationStart": "Case.IsolationStartDate",
  "isolationEnd": "Case.IsolationEndDate",
  "customField": "Case.CustomFields.RiskLevel",
  "anotherField": "Case.CustomFields.ContactType"
}
```

---

## ?? Custom Date Functions

Use in expressions:
```json
// Current date
{"expression": "today()"}

// Add 10 days
{"expression": "addDays({dateField}, 10)"}

// Days between dates
{"expression": "dateDiff({startDate}, {endDate})"}

// Set default to 3 days from now
{"defaultValue": "addDays(today(), 3)"}
```

---

## ?? Advanced Features

### Conditional Display
```json
{
  "type": "text",
  "name": "temperature",
  "title": "Temperature",
  "visibleIf": "{hasFever} = true"
}
```

### Required Fields
```json
{
  "type": "text",
  "name": "required",
  "isRequired": true
}
```

### Read-Only Fields
```json
{
  "type": "text",
  "name": "readOnly",
  "readOnly": true
}
```

### Default Values
```json
{
  "type": "text",
  "name": "field",
  "defaultValue": "today()"
}
```

### Min/Max for Numbers
```json
{
  "type": "text",
  "inputType": "number",
  "min": 0,
  "max": 150
}
```

---

## ?? Common Mistakes

### ? Don't Do This
```json
// Missing comma
{
  "type": "text"
  "name": "field"
}

// Extra comma
{
  "type": "text",
  "name": "field",
}

// Single quotes (use double quotes)
{'type': 'text'}

// Missing quotes on keys
{type: "text"}
```

### ? Do This
```json
{
  "type": "text",
  "name": "field"
}
```

---

## ??? Troubleshooting

| Problem | Solution |
|---------|----------|
| JSON validation error | Use "Format JSON" button, check for missing commas |
| Survey doesn't pre-populate | Check Input Mappings field paths are correct |
| Responses not saving | Check Output Mappings field paths exist |
| Survey button not showing | Ensure TaskTemplate has SurveyDefinitionJson |
| Custom field not saving | Create custom field first in Settings ? Custom Fields |

---

## ?? Resources

- **SurveyJS Examples**: https://surveyjs.io/form-library/examples/overview/reactjs
- **SurveyJS Documentation**: https://surveyjs.io/form-library/documentation/overview
- **Question Types**: https://surveyjs.io/form-library/documentation/api-reference/question
- **Conditions**: https://surveyjs.io/form-library/documentation/design-survey/conditional-logic

---

## ?? Pro Tips

1. **Always validate** JSON before saving
2. **Use Preview** to test your survey before deploying
3. **Format JSON** for readability
4. **Start simple** - add one question at a time
5. **Test with real case** before rolling out to users
6. **Use Custom Fields** for disease-specific data that doesn't fit standard fields
7. **Read-only fields** are great for showing context (patient name, case number)
8. **Progressive disclosure** - use visibleIf to show questions conditionally

---

## ?? Example Scenarios

### Contact Investigation
```json
{
  "title": "Contact Investigation",
  "elements": [
    {"type": "text", "name": "contactDate", "inputType": "date", "isRequired": true},
    {"type": "dropdown", "name": "contactType", "choices": ["Household", "Workplace"]},
    {"type": "checkbox", "name": "symptoms", "choices": ["Fever", "Cough"]},
    {"type": "comment", "name": "notes"}
  ]
}
```

### Daily Symptom Screen
```json
{
  "title": "Daily Symptom Check",
  "elements": [
    {"type": "boolean", "name": "hasFever", "title": "Do you have fever?"},
    {"type": "text", "name": "temp", "inputType": "number", "visibleIf": "{hasFever} = true"},
    {"type": "radiogroup", "name": "feeling", "choices": ["Good", "Fair", "Poor"]}
  ]
}
```

---

## ? Checklist Before Going Live

- [ ] Survey JSON validated successfully
- [ ] Survey previewed and looks correct
- [ ] Input mappings configured (if needed)
- [ ] Output mappings configured (if needed)
- [ ] Custom fields created (if using CustomFields.*)
- [ ] Tested with a real case
- [ ] Pre-population working correctly
- [ ] Responses saving to correct fields
- [ ] All required questions marked as required
- [ ] Conditional logic tested
- [ ] Instructions clear for end users

---

## ?? You're Ready!

Your survey system is configured and ready to use. Users will see the **Survey** button in their **My Tasks** dashboard for tasks with surveys attached.

**Need Help?** Check the full documentation in `SURVEY_ADMIN_UI_COMPLETE.md`
