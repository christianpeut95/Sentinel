# How to Use the Symptom Tracking System

## For Administrators

### Managing Symptoms

1. **Navigate to Symptoms**
   - Go to Settings ? Laboratory Lookups ? Symptoms
   - Or visit: `/Settings/Lookups/Symptoms`

2. **Add a New Symptom**
   - Click "Add Symptom" button
   - Fill in:
     - **Name** (required): e.g., "Chest Pain"
     - **Code** (optional): Internal code, e.g., "CHEST_PAIN"
     - **Export Code** (optional): External reporting code, e.g., "SYM026"
     - **Description** (optional): Additional details about the symptom
     - **Sort Order**: Number for display order (lower = appears first)
     - **Is Active**: Check to make symptom available
   - Click "Create"

3. **Edit an Existing Symptom**
   - In the symptom list, click the pencil icon
   - Modify fields as needed
   - Click "Save Changes"

4. **Delete a Symptom**
   - Edit the symptom
   - Click "Delete" button at bottom
   - Confirm deletion
   - Note: This is a soft delete - the symptom is hidden but data is preserved

5. **Special Note: "Other" Symptom**
   - Pre-created symptom with code "OTHER"
   - Always appears last (sort order 999)
   - Used when patient has an unlisted symptom
   - **Do not delete this symptom**

### Associating Symptoms with Diseases (Optional, Future Enhancement)

This improves data entry by showing relevant symptoms first:

```sql
-- Example: Associate common flu symptoms with Influenza
-- Find your disease ID first
SELECT Id, Name FROM Diseases WHERE Name LIKE '%Influenza%';

-- Then associate symptoms
INSERT INTO DiseaseSymptoms (DiseaseId, SymptomId, IsCommon, SortOrder)
SELECT 
    '12345678-1234-1234-1234-123456789012', -- Your Disease GUID
    s.Id,
    1, -- IsCommon = true
    s.SortOrder
FROM Symptoms s
WHERE s.Code IN ('FEVER', 'COUGH', 'MYALGIA', 'HEADACHE', 'FATIGUE');
```

## For Clinical Users (Future)

### Recording Symptoms for a Case

_Note: This UI is not yet implemented. Instructions below are for when it's added._

1. **When Creating or Editing a Case**
   - Navigate to the "Symptoms" section
   - You'll see a list of common symptoms (if disease is selected)

2. **Select Symptoms**
   - Check the box next to each symptom the patient has
   - For each selected symptom, provide:
     - **Onset Date**: When did the symptom start?
     - **Severity**: Mild, Moderate, or Severe
     - **Notes** (optional): Any additional details

3. **Using "Other" for Unlisted Symptoms**
   - If patient has a symptom not on the list:
   - Check "Other"
   - Enter symptom description in the text box
   - Provide onset date and severity as usual

4. **Multiple Symptoms**
   - You can select as many symptoms as needed
   - Each symptom can have a different onset date
   - This helps track disease progression

5. **Optional Field**
   - Symptoms are completely optional
   - Skip if not collecting symptom data for this case type
   - Example: Shingles surveillance might not need detailed symptoms

### Viewing Symptoms

_On the Case Details page:_
- See all recorded symptoms
- View onset dates and severity
- See progression timeline

## For Epidemiologists / Analysts

### Querying Symptom Data

**Example Queries:**

```sql
-- Cases with a specific symptom
SELECT c.FriendlyId, c.DateOfOnset, s.Name AS Symptom, cs.OnsetDate
FROM Cases c
JOIN CaseSymptoms cs ON c.Id = cs.CaseId
JOIN Symptoms s ON cs.SymptomId = s.Id
WHERE s.Code = 'FEVER'
  AND c.IsDeleted = 0
  AND cs.IsDeleted = 0;

-- Symptom frequency by disease
SELECT d.Name AS Disease, s.Name AS Symptom, COUNT(*) AS CaseCount
FROM Cases c
JOIN CaseSymptoms cs ON c.Id = cs.CaseId
JOIN Symptoms s ON cs.SymptomId = s.Id
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE c.IsDeleted = 0
  AND cs.IsDeleted = 0
GROUP BY d.Name, s.Name
ORDER BY d.Name, CaseCount DESC;

-- Symptom onset to case notification delay
SELECT 
    c.FriendlyId,
    cs.OnsetDate AS SymptomOnset,
    c.DateOfNotification,
    DATEDIFF(day, cs.OnsetDate, c.DateOfNotification) AS DaysDelay,
    s.Name AS Symptom
FROM Cases c
JOIN CaseSymptoms cs ON c.Id = cs.CaseId
JOIN Symptoms s ON cs.SymptomId = s.Id
WHERE cs.OnsetDate IS NOT NULL
  AND c.DateOfNotification IS NOT NULL
  AND c.IsDeleted = 0
  AND cs.IsDeleted = 0
ORDER BY DaysDelay DESC;

-- Most common symptom combinations
SELECT 
    STRING_AGG(s.Name, ', ') WITHIN GROUP (ORDER BY s.Name) AS SymptomCombination,
    COUNT(DISTINCT c.Id) AS CaseCount
FROM Cases c
JOIN CaseSymptoms cs ON c.Id = cs.CaseId
JOIN Symptoms s ON cs.SymptomId = s.Id
WHERE c.IsDeleted = 0
  AND cs.IsDeleted = 0
GROUP BY c.Id
HAVING COUNT(cs.Id) >= 2
ORDER BY CaseCount DESC;
```

### Export Considerations

- **Export Codes**: Map symptoms to external classification systems
  - SNOMED CT
  - ICD-10
  - State/National reporting codes
- Edit symptoms to add your organization's export codes
- These codes appear in data exports

## Best Practices

### For Administrators
1. ? Keep symptom names clear and consistent
2. ? Use standard medical terminology
3. ? Maintain export code mappings
4. ? Don't delete the "Other" symptom
5. ? Order symptoms logically (most common first)
6. ? Keep active symptoms to a reasonable number (~20-30)

### For Clinical Users
1. ? Record onset dates accurately - critical for analysis
2. ? Use "Other" sparingly - suggest adding common unlisted symptoms to admin
3. ? Be consistent with severity assessment
4. ? Add notes for unusual presentations
5. ? It's OK to leave symptoms blank if not applicable

### For Data Quality
1. ? Onset date should be ? case notification date
2. ? Severity should be documented for key symptoms
3. ? Use "Other" text field descriptively
4. ? Review symptom data completeness regularly
5. ? Train staff on consistent symptom documentation

## Common Scenarios

### Scenario 1: Foodborne Illness Case
**Disease:** Salmonella  
**Symptoms to record:**
- ?? Diarrhea (onset date, severity)
- ?? Abdominal Pain (onset date, severity)
- ?? Fever (onset date, severity)
- ?? Nausea or Vomiting (onset date, severity)

### Scenario 2: Respiratory Illness
**Disease:** COVID-19  
**Symptoms to record:**
- ?? Fever (onset date, severity)
- ?? Cough (onset date, severity)
- ?? Shortness of Breath (onset date, severity)
- ?? Fatigue (onset date, severity)
- ?? Loss of Taste or Smell (onset date, severity)

### Scenario 3: Neurological Presentation
**Disease:** Meningitis  
**Symptoms to record:**
- ?? Headache (onset date, severity: likely "Severe")
- ?? Fever (onset date, severity)
- ?? Confusion (onset date, severity)
- ?? Other: "Neck stiffness" (describe in text field)

### Scenario 4: Asymptomatic Case
**Disease:** Any  
**Symptoms to record:**
- Leave blank or add note: "Asymptomatic at time of testing"

## Troubleshooting

**Q: I can't see the Symptoms option in Settings**  
A: Check that you have the ManagePermissions permission. Contact your administrator.

**Q: How do I add a symptom that's not on the list?**  
A: Use the "Other" option and describe the symptom in the text field. Let your administrator know if it's commonly used.

**Q: Can I record symptoms retrospectively?**  
A: Yes, when editing a case, you can add symptoms with their historical onset dates.

**Q: What if I don't know the exact onset date?**  
A: Leave the onset date blank. It's better to leave it blank than guess incorrectly.

**Q: Should I record all symptoms or just the main ones?**  
A: Record the clinically significant symptoms. Use your judgment - document what's relevant for the disease and public health response.

**Q: Can symptoms be deleted from a case?**  
A: Yes (soft delete). The data is preserved for audit purposes but won't appear in active views.

## Need Help?

- **Technical Issues**: Contact your IT administrator
- **Clinical Questions**: Refer to your organization's symptom documentation guidelines
- **Missing Symptoms**: Request new symptoms through your administrator
- **Data Analysis**: Refer to the example queries above or contact your epidemiology team
