# Case Definition Criteria Builder - User Guide

## Quick Start

### Adding Criteria to a Case Definition

1. **Open the Case Definitions page**
   - Navigate to an outbreak
   - Click "Case Definitions" tab

2. **Create or Edit a Definition**
   - Click "Create" for a new definition (Confirmed, Probable, Suspect, or Not a Case)
   - OR click "Edit" on an existing definition

3. **Add Criteria Rules**
   - In the "Specific Criteria" section, click **"Add Criterion"**
   - A modal will open

4. **Configure the Criterion**
   - **Select Custom Field**: Choose the field you want to create a rule for
   - **Select Operator**: Choose how to compare (Equals, Contains, Greater Than, etc.)
   - **Enter/Select Value**:
     - For **lookup fields** (like "Smoking Status"): Select from dropdown
     - For **text fields**: Type the value
     - For **number fields**: Enter a number
     - For **date fields**: Pick a date
     - For **boolean fields**: Select Yes/No
   - **Logic Operator**: Choose AND or OR (how this rule combines with others)

5. **Add the Criterion**
   - Click "Add Criterion" button
   - The rule appears in the criteria list

6. **Review Your Criteria**
   - Each criterion shows as a card
   - Logic operators (AND/OR) are displayed as badges
   - Click the trash icon to remove a criterion

7. **Save the Definition**
   - Click "Save Definition" to save everything

---

## Example Scenarios

### Example 1: Simple Single Criterion
**Scenario:** Confirmed cases must have "Lab Result" = "Positive"

1. Add Criterion
2. Custom Field: "Lab Result"
3. Operator: "Equals"
4. Value: Select "Positive" from dropdown
5. Click "Add Criterion"
6. Save Definition

**Result:**
```
[Lab Result] Equals [Positive]
```

---

### Example 2: Multiple Criteria with AND
**Scenario:** Probable cases must have "Fever" = "Yes" AND "Cough" = "Yes"

1. **First Criterion:**
   - Custom Field: "Fever"
   - Operator: "Equals"
   - Value: "Yes"
   - Click "Add Criterion"

2. **Second Criterion:**
   - Custom Field: "Cough"
   - Operator: "Equals"
   - Value: "Yes"
   - Logic Operator: "AND"
   - Click "Add Criterion"

**Result:**
```
[Fever] Equals [Yes]
AND [Cough] Equals [Yes]
```

---

### Example 3: Multiple Criteria with OR
**Scenario:** Suspect cases have "Contact with Case" = "Yes" OR "Travel to Affected Area" = "Yes"

1. **First Criterion:**
   - Custom Field: "Contact with Case"
   - Operator: "Equals"
   - Value: "Yes"
   - Click "Add Criterion"

2. **Second Criterion:**
   - Custom Field: "Travel to Affected Area"
   - Operator: "Equals"
   - Value: "Yes"
   - Logic Operator: "OR"
   - Click "Add Criterion"

**Result:**
```
[Contact with Case] Equals [Yes]
OR [Travel to Affected Area] Equals [Yes]
```

---

### Example 4: Age Range
**Scenario:** Definition applies to people over 18 years old

1. Add Criterion
2. Custom Field: "Age"
3. Operator: "Greater Than"
4. Value: 18
5. Click "Add Criterion"

**Result:**
```
[Age] Greater Than [18]
```

---

### Example 5: Complex Logic
**Scenario:** Confirmed if (Lab Positive OR Rapid Test Positive) AND (Fever OR Cough)

Since the current system doesn't support nested groups, you would need to create 4 separate definitions or use the following approach:

**Option 1: Most Restrictive**
```
[Lab Result] Equals [Positive]
AND [Fever] Equals [Yes]
```

**Option 2: Separate Definitions**
Create multiple versions with different combinations.

---

## Operator Guide

### For Lookup/Dropdown Fields
- **Equals**: Field must match the selected value exactly
- **Not Equals**: Field must NOT match the selected value
- **In List**: Field matches any of multiple values (future feature)
- **Is Present**: Field has any value (not empty)
- **Is Absent**: Field is empty/null

### For Text Fields
- **Equals**: Exact match (case-sensitive)
- **Not Equals**: Does not match exactly
- **Contains**: Text appears anywhere in the field
- **Does Not Contain**: Text does NOT appear in the field
- **Is Present**: Field has any text
- **Is Absent**: Field is empty

### For Number Fields
- **Equals**: Exact number match
- **Not Equals**: Not equal to number
- **Greater Than**: Number is larger than specified
- **Less Than**: Number is smaller than specified
- **Between**: Number falls within range (future feature)
- **Is Present**: Field has a value
- **Is Absent**: Field is empty

### For Date Fields
- **Equals**: Same date
- **Not Equals**: Different date
- **Greater Than**: After this date
- **Less Than**: Before this date
- **Between**: Date within range (future feature)
- **Is Present**: Date exists
- **Is Absent**: No date entered

### For Boolean (Yes/No) Fields
- **Equals**: Matches Yes or No
- **Not Equals**: Opposite of Yes or No
- **Is Present**: Has a value
- **Is Absent**: Not set

---

## Tips & Best Practices

### ✅ DO:
- Use clear, specific field names
- Test your criteria with a few sample cases
- Document complex criteria in the "Notes" field
- Use "Is Present" when you just need to know a field exists
- Combine related criteria with AND
- Use OR for alternative qualifying conditions

### ❌ DON'T:
- Create overly complex criteria (keep it simple)
- Forget to specify logic operators (AND/OR)
- Use "Contains" for exact matches (use "Equals" instead)
- Create duplicate criteria
- Mix too many OR conditions (creates confusion)

---

## Troubleshooting

### Problem: Custom field not showing in dropdown
**Solution:** 
- Ensure the field is marked as "Active"
- Ensure "Show on Case Form" is enabled
- Refresh the page

### Problem: Lookup values not appearing
**Solution:**
- Verify the lookup table has active values
- Check that the custom field is linked to a lookup table
- Refresh the page

### Problem: Can't save definition
**Solution:**
- Fill in all required fields (marked with *)
- Ensure at least the "Definition Name" is filled
- Check browser console for errors

### Problem: Criteria not showing after save
**Solution:**
- Edit the definition
- Re-add the criteria
- Make sure to click "Save Definition"

---

## Keyboard Shortcuts

- **Tab**: Navigate between fields
- **Enter**: Submit modal (when focused on a button)
- **Esc**: Close modal

---

## Related Pages

- **Custom Fields Management**: `/Settings/CustomFields`
- **Lookup Tables**: `/Settings/LookupTables`
- **Outbreak Details**: `/Outbreaks/Details/{id}`
- **Case Classification**: `/Outbreaks/ClassifyCases/{id}`

---

## Support

For technical issues or questions:
1. Check the browser console (F12) for errors
2. Review the implementation documentation
3. Contact your system administrator

---

**Version:** 1.0  
**Last Updated:** 2026-04-27
