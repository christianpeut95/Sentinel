# ?? Survey Field Mapping Reference - Copy & Paste Guide

Quick reference for mapping survey fields to database. Copy and paste these into your Field Mappings tab.

---

## ?? How to Use This Guide

### For Question Text Variables:
1. Copy the variable name (e.g., `patientName`)
2. In Survey Designer, type `{patientName}` in your question text
3. In Field Mappings tab ? Input Mappings, paste the mapping

### For Survey Answers:
1. Copy the field path (e.g., `Case.DateOfOnset`)
2. In Field Mappings tab ? Output Mappings, select from dropdown or paste

---

## ?? Patient Information

### Input Mappings (for question text)
```
patientName ? Patient.GivenName
patientFirstName ? Patient.GivenName
patientLastName ? Patient.FamilyName
patientFullName ? Patient.FullName
patientAge ? Patient.Age
patientDOB ? Patient.DateOfBirth
patientPhone ? Patient.Phone
patientEmail ? Patient.Email
patientAddress ? Patient.Address
patientCity ? Patient.City
patientState ? Patient.State
patientZip ? Patient.PostalCode
patientGender ? Patient.Gender
```

### Output Mappings (for saving answers)
```
Case.Patient.GivenName
Case.Patient.FamilyName
Case.Patient.DateOfBirth
Case.Patient.Phone
Case.Patient.Email
Case.Patient.Address
Case.Patient.City
Case.Patient.State
Case.Patient.PostalCode
```

---

## ?? Case Information

### Input Mappings
```
caseNumber ? Case.FriendlyId
dateOfOnset ? Case.DateOfOnset
diagnosisDate ? Case.DateOfDiagnosis
notificationDate ? Case.DateOfNotification
isolationStartDate ? Case.IsolationStartDate
isolationEndDate ? Case.IsolationEndDate
isolationEnd ? Case.IsolationEndDate
riskLevel ? Case.RiskLevel
outcome ? Case.Outcome
outcomeDate ? Case.OutcomeDate
caseNotes ? Case.Notes
```

### Output Mappings
```
Case.DateOfOnset
Case.DateOfDiagnosis
Case.DateOfNotification
Case.IsolationStartDate
Case.IsolationEndDate
Case.Outcome
Case.OutcomeDate
Case.RiskLevel
Case.Notes
```

---

## ??? Exposure Information

### Input Mappings
```
exposureDate ? ExposureEvent.ExposureStartDate
exposureEndDate ? ExposureEvent.ExposureEndDate
locationName ? Location.Name
locationAddress ? Location.Address
eventName ? Event.Name
eventDate ? Event.StartDate
foodItem ? ExposureEvent.FoodItemDescription
exposureType ? ExposureEvent.ExposureType
```

### Output Mappings
```
Exposures[0].ExposureStartDate
Exposures[0].ExposureEndDate
Exposures[0].ExposureType
Exposures[0].Description
Exposures[0].FoodItemDescription
Exposures[0].FreeTextLocation
Exposures[0].Notes
```

---

## ?? Lab Results

### Output Mappings
```
LabResults[0].TestResult
LabResults[0].ResultDate
LabResults[0].LabName
LabResults[0].SpecimenType
LabResults[0].Notes
```

---

## ?? Custom Fields

### Pattern
```
Case.CustomFields.YourFieldName
```

### Common Examples
```
Case.CustomFields.FoodExposure
Case.CustomFields.TravelHistory
Case.CustomFields.VaccinationStatus
Case.CustomFields.SymptomSeverity
Case.CustomFields.Hospitalized
Case.CustomFields.ICUAdmission
Case.CustomFields.ContactTracingComplete
Case.CustomFields.InterviewComplete
Case.CustomFields.RiskAssessment
```

---

## ?? Example Mappings

### Food History Survey

**Input Mappings:**
```json
{
  "patientName": "Patient.GivenName",
  "caseNumber": "Case.FriendlyId",
  "isolationEndDate": "Case.IsolationEndDate"
}
```

**Output Mappings:**
```json
{
  "lastMealDate": "Case.DateOfOnset",
  "restaurantName": "Exposures[0].FreeTextLocation",
  "foodEaten": "Exposures[0].FoodItemDescription",
  "symptoms": "Case.Notes"
}
```

**Example Question Text:**
```
"Hello {patientName}, Case #{caseNumber}. 
You need to isolate until {isolationEndDate}. 
When did you last eat out?"
```

---

### Contact Investigation Survey

**Input Mappings:**
```json
{
  "patientName": "Patient.GivenName",
  "dateOfOnset": "Case.DateOfOnset",
  "riskLevel": "Case.RiskLevel"
}
```

**Output Mappings:**
```json
{
  "hadCloseContacts": "Case.CustomFields.HasCloseContacts",
  "contactCount": "Case.CustomFields.ContactCount",
  "contactTracingNeeded": "Case.CustomFields.ContactTracingRequired"
}
```

---

### Symptom Screening Survey

**Input Mappings:**
```json
{
  "patientName": "Patient.GivenName",
  "patientAge": "Patient.Age"
}
```

**Output Mappings:**
```json
{
  "hasFever": "Case.CustomFields.HasFever",
  "feverTemp": "Case.CustomFields.FeverTemperature",
  "hasCough": "Case.CustomFields.HasCough",
  "hasBreathingDifficulty": "Case.CustomFields.BreathingDifficulty",
  "symptomOnsetDate": "Case.DateOfOnset"
}
```

---

## ?? Quick Tips

### ? DO:
- Use camelCase: `patientName` not `patient_name`
- Keep variable names simple and memorable
- Map variables even if unused (no harm)
- Test in preview before saving

### ? DON'T:
- Use spaces in variable names: `{patient name}` ?
- Use special characters: `{patient-name}` ?
- Forget the curly braces in question text: `patientName` ?
- Map questions that collect new data to Input Mappings ?

---

## ?? Finding Field Names

### For Standard Fields:
Look at the "Common Database Fields" reference card in the Field Mappings tab.

### For Custom Fields:
1. Go to **Settings ? Diseases**
2. Edit your disease
3. Scroll to **Custom Field Definitions**
4. Copy the field name exactly
5. Use pattern: `Case.CustomFields.YourFieldName`

---

## ?? Mobile-Friendly Version

### Top 10 Most Common Mappings:

**Input (for text):**
```
{patientName} ? Patient.GivenName
{dateOfOnset} ? Case.DateOfOnset
{isolationEndDate} ? Case.IsolationEndDate
{caseNumber} ? Case.FriendlyId
{patientPhone} ? Patient.Phone
```

**Output (for answers):**
```
symptomDate ? Case.DateOfOnset
phoneNumber ? Patient.Phone
hadFever ? Case.CustomFields.HasFever
isolated ? Case.CustomFields.IsIsolating
notes ? Case.Notes
```

---

## ?? Testing Your Mappings

### 1. Test Variables in Question Text:
```
Question: "Hello {patientName}, you were exposed on {exposureDate}"

Expected: "Hello John Doe, you were exposed on Feb 1, 2026"
If shows: "Hello {patientName}, you were exposed on {exposureDate}"
? Variable not mapped in Input Mappings!
```

### 2. Test Output Mappings:
- Complete survey with test data
- Check Case Details page
- Verify data appears in correct fields
- Check Custom Fields section if using those

---

## ?? Save This Page!

**Bookmark this page** or keep it open while building surveys. You can:
- Copy variable names directly
- Reference field paths
- Check mapping examples
- Verify syntax

---

## ?? Troubleshooting

### "Variable shows as {variableName} in survey"
? Not mapped in **Input Mappings** tab

### "Survey answer didn't save to database"
? Not mapped in **Output Mappings** tab or wrong field path

### "Can't find custom field"
? Define it first in Disease settings ? Custom Field Definitions

### "Array fields not working (Exposures[0])"
? Make sure exposure exists in case, or use regular fields instead

---

**Last Updated:** February 7, 2026  
**Quick Access:** Keep this page bookmarked when building surveys!
