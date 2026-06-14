# Case Definition Criteria Builder - Implementation Summary

## Overview
Enhanced the case definition builder to properly handle custom fields with lookup tables. The system now provides a user-friendly interface for adding criteria rules with proper validation.

---

## Changes Made

### 1. **CustomFieldsController.cs** - Added New API Endpoints

#### `GET /api/CustomFields/{id}`
- Retrieves detailed information about a specific custom field
- Includes lookup table values if the field has a lookup table
- Returns:
  - Field metadata (id, name, label, type)
  - Whether it has a lookup table
  - All active lookup values (id, value, displayText)

#### `GET /api/CustomFields/ForCaseDefinitions`
- Gets all active custom fields available for case forms
- Returns simplified field list for dropdown population
- Ordered by display order and label

**Example Response:**
```json
{
  "id": 123,
  "name": "smoking_status",
  "label": "Smoking Status",
  "fieldType": "Dropdown",
  "lookupTableId": 45,
  "hasLookupTable": true,
  "lookupValues": [
    {
      "id": 1,
      "value": "current",
      "displayText": "Current Smoker"
    },
    {
      "id": 2,
      "value": "former",
      "displayText": "Former Smoker"
    },
    {
      "id": 3,
      "value": "never",
      "displayText": "Never Smoked"
    }
  ]
}
```

---

### 2. **CaseDefinitions.cshtml** - Enhanced UI

#### Replaced JSON Textarea with Visual Criteria Builder
- **Before:** Plain textarea requiring manual JSON input
- **After:** Interactive criteria builder with cards and buttons

#### New Components:
1. **Criteria Builder Section**
   - Visual display of all criteria rules
   - "Add Criterion" button
   - Individual criterion cards with delete buttons
   - Shows logic operators (AND/OR) between criteria

2. **Add Criterion Modal**
   - Custom field dropdown (populated dynamically)
   - Operator dropdown (filtered by field type)
   - Dynamic input field:
     - **Lookup fields:** Dropdown with lookup values
     - **Number fields:** Number input
     - **Date fields:** Date picker
     - **Boolean fields:** Yes/No dropdown
     - **Text fields:** Text input
   - Logic operator selector (AND/OR)

---

### 3. **JavaScript Implementation**

#### Key Functions:

**`loadCustomFields()`**
- Loads available custom fields on page load
- Caches them for quick access

**`openAddCriterionModal()`**
- Opens the modal to add a new criterion
- Populates custom field dropdown

**`loadCustomFieldWithLookup(customFieldId)`**
- Fetches custom field details from API
- If field has lookup table, displays dropdown with values
- Otherwise shows appropriate input type

**`updateOperatorOptions(fieldType, hasLookup)`**
- Dynamically updates operator dropdown based on field type
- **Lookup fields:** Equals, Not Equals, In List, Is Present, Is Absent
- **Number/Date fields:** Equals, Not Equals, Greater Than, Less Than, Between, Is Present, Is Absent
- **Boolean fields:** Equals, Not Equals, Is Present, Is Absent
- **Text fields:** Equals, Not Equals, Contains, Does Not Contain, Is Present, Is Absent

**`addCriterion()`**
- Validates form inputs
- Creates criterion object with proper data types
- Adds to criteria list
- Updates display

**`updateCriteriaDisplay()`**
- Renders criteria list as visual cards
- Updates hidden JSON field for form submission
- Shows "No criteria" message when empty

**`removeCriterion(index)`**
- Removes criterion from list
- Re-adjusts logic operators

---

## How It Works

### User Flow:

1. **Open Case Definition Modal**
   - User clicks "Create" or "Edit" on any classification card
   - Modal opens with empty or pre-filled form

2. **Add Criteria**
   - Click "Add Criterion" button
   - Add Criterion modal opens

3. **Select Custom Field**
   - Choose from dropdown of available custom fields
   - System detects if field has lookup table
   - Loads lookup values if applicable

4. **Configure Criterion**
   - Select appropriate operator (filtered by field type)
   - Choose/enter value:
     - **Lookup field:** Select from dropdown
     - **Other types:** Enter appropriate value
   - Select logic operator (AND/OR) if not first criterion

5. **Save Criterion**
   - Criterion is added to visual list
   - JSON is updated in hidden field
   - Modal closes

6. **Save Definition**
   - Submit form with all criteria
   - Criteria JSON is saved to database

---

## Data Structure

### Criterion Object:
```javascript
{
  type: "CustomField",
  customFieldId: 123,
  customFieldLabel: "Smoking Status",
  operator: 1,              // Integer from ComparisonOperator enum
  operatorText: "Equals",
  input: "1",               // Lookup value ID or actual value
  logicOperator: "AND"      // null for first criterion
}
```

### Complete Criteria JSON:
```json
{
  "criteria": [
    {
      "type": "CustomField",
      "customFieldId": 123,
      "customFieldLabel": "Smoking Status",
      "operator": 1,
      "operatorText": "Equals",
      "input": "1",
      "logicOperator": null
    },
    {
      "type": "CustomField",
      "customFieldId": 456,
      "customFieldLabel": "Age",
      "operator": 5,
      "operatorText": "Greater Than",
      "input": "18",
      "logicOperator": "AND"
    }
  ]
}
```

---

## ComparisonOperator Enum Values

| Value | Operator | Use Case |
|-------|----------|----------|
| 1 | Equals | Exact match |
| 2 | Not Equals | Exclusion |
| 3 | Contains | Text substring search |
| 4 | Does Not Contain | Text exclusion |
| 5 | Greater Than | Numeric/date comparison |
| 6 | Less Than | Numeric/date comparison |
| 7 | Between | Range queries |
| 8 | In List | Multiple values |
| 9 | Is Present | Field has value |
| 10 | Is Absent | Field is null/empty |

---

## Validation

### Client-Side:
- Custom field selection required
- Operator selection required
- Input value required (except for "Is Present" / "Is Absent")
- Proper data types enforced by input type

### Server-Side:
The existing model validation handles:
- Required `input` field (when operator needs it)
- Valid `operator` integer value (1-10)
- Valid `customFieldId` reference

---

## Error Handling

### Fixed Issues:
1. ✅ **"The input field is required"** - Now properly included in all criterion objects
2. ✅ **"JSON value could not be converted to ComparisonOperator"** - Now sends integer values (1-10) instead of strings
3. ✅ **Lookup fields showing text input** - Now shows dropdown with lookup values

### Error Messages:
- API failures show user-friendly alerts
- Form validation prevents submission of incomplete data
- Console logs errors for debugging

---

## Testing Checklist

### Test Scenarios:

- [ ] **Load page** - Custom fields populate correctly
- [ ] **Open Add Criterion modal** - Dropdown shows all case custom fields
- [ ] **Select lookup field** - Dropdown shows lookup values
- [ ] **Select text field** - Shows text input
- [ ] **Select number field** - Shows number input
- [ ] **Select date field** - Shows date picker
- [ ] **Select boolean field** - Shows Yes/No dropdown
- [ ] **Operator filtering** - Operators change based on field type
- [ ] **Add criterion** - Criterion appears in list
- [ ] **Add multiple criteria** - Logic operators (AND/OR) work
- [ ] **Remove criterion** - Criterion is removed, list updates
- [ ] **Edit definition** - Existing criteria load correctly
- [ ] **Save definition** - Criteria JSON saves to database
- [ ] **Validation** - Empty fields prevent submission

---

## Future Enhancements

### Possible Improvements:
1. **Drag-and-drop reordering** of criteria
2. **Criterion groups** (nested AND/OR logic)
3. **Between operator** - Show two input fields
4. **In List operator** - Multi-select dropdown
5. **Preview query** - Show SQL/LINQ equivalent
6. **Test criteria** - Run against existing cases
7. **Criterion templates** - Save common patterns
8. **Import/export** - Share definitions between outbreaks

---

## Files Modified

1. ✅ `Controllers\CustomFieldsController.cs`
   - Added `GetCustomField(id)` endpoint
   - Added `GetCustomFieldsForCaseDefinitions()` endpoint

2. ✅ `Pages\Outbreaks\CaseDefinitions.cshtml`
   - Replaced textarea with visual criteria builder
   - Added "Add Criterion" modal
   - Added comprehensive JavaScript implementation

---

## Dependencies

- **Bootstrap 5** - Modal and form components
- **Bootstrap Icons** - UI icons
- **Fetch API** - AJAX requests
- **System.Text.Json** - Server-side JSON serialization

---

## Compatibility

- ✅ .NET 10
- ✅ Blazor projects
- ✅ Modern browsers (ES6+ JavaScript)
- ✅ Razor Pages architecture

---

**Status:** ✅ **Ready for Testing**

**Build Status:** ✅ **Successful**

**Implementation Date:** 2026-04-27
