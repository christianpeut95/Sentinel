# Debugging Sub-Filter Date Fields Issue

## Problem
When selecting a date field (like "ResultDate") from Lab Results collection sub-filter, it shows string filtering options instead of date-specific operators and a date picker.

## Debugging Steps

### 1. Open Browser Console
Press F12 in your browser and go to the Console tab.

### 2. Reproduce the Issue
1. Go to Reports > Report Builder
2. Select "Cases" as entity type
3. Add some fields (drag from left panel)
4. Click "Add Query" in the Collection Queries section
5. Select "Lab Results" from the Collection dropdown
6. Click "Add Sub-Filter"
7. Select "Result Date" from the field dropdown

### 3. Check Console Output

You should see logs like:
```
[addCollectionSubFilter] Fields: ['TestType', 'TestResult', 'ResultDate', ...]
[addCollectionSubFilter] Metadata: [{fieldPath: 'TestType', dataType: 'String', ...}, {fieldPath: 'ResultDate', dataType: 'DateTime', ...}]
[addCollectionSubFilter] Field: ResultDate, DataType: DateTime, Metadata: {fieldPath: 'ResultDate', dataType: 'DateTime', ...}
[setupSubFilterSmartInput] Setting up smart input for query 1 subfilter 1234567890.123
[SubFilter] Field changed to ResultDate DataType: DateTime Option dataset: {type: 'DateTime'}
```

### 4. What to Look For

**If DataType is 'String' instead of 'DateTime':**
- The metadata is not being loaded correctly
- Check the `/api/reporting/fields/{entityType}/grouped` endpoint response
- Verify that `collectionSubFieldsMetadata` contains the correct data types

**If DataType is 'DateTime' but operators are wrong:**
- The `updateOperators()` function is not handling DateTime correctly
- Check the normalizedType logic in `updateOperators()`

**If DataType is 'DateTime' and operators are correct but input is wrong:**
- The `updateValueInput()` function is not creating the date picker
- Check the `normalizedType` detection in `updateValueInput()`
- Verify that `getDateFilterHTML()` is being called

### 5. Expected Behavior

When you select "Result Date":
1. **Operator dropdown should show:**
   - On Date
   - Not On Date
   - After
   - Before
   - On or After
   - On or Before
   - Between Dates
   - Is Null
   - Is Not Null
   - In Last X Days
   - In Next X Days

2. **Value input should show:**
   - A combined dropdown with preset date options
   - OR a date picker
   - OR custom condition builder

### 6. Common Issues

#### Issue: Metadata is empty
**Solution:** The collection metadata endpoint might not be returning sub-field metadata. Check:
```javascript
const fieldResponse = await fetch(`/api/reporting/fields/${entityType}/grouped`);
const fieldsByCategory = await fieldResponse.json();
```

Make sure `collectionSubFieldsMetadata` is populated in the response.

#### Issue: DataType is 'String' for date fields
**Solution:** The metadata matching logic might be wrong. Check:
```javascript
const metadata = fieldsMetadata.find(m => m.fieldPath === fieldName || m.name === fieldName);
```

The field name in the metadata might not match exactly (e.g., `ResultDate` vs `LabResult.ResultDate`).

#### Issue: Smart filtering not triggering
**Solution:** The event listeners might not be attached. Verify that `setupSubFilterSmartInput()` is being called after the HTML is inserted.

## Files to Check

1. **wwwroot/js/report-builder-collections.js**
   - `addCollectionSubFilter()` - Creates the sub-filter HTML
   - `setupSubFilterSmartInput()` - Attaches event listeners
   - `updateCollectionFields()` - Fetches metadata

2. **wwwroot/js/report-builder.js**
   - `updateOperators()` - Updates operator dropdown based on data type
   - `updateValueInput()` - Creates appropriate input based on data type
   - `getDateFilterHTML()` - Generates date filter UI

3. **Services/Reporting/ReportFieldMetadataService.cs**
   - `GetFieldsByCategoryAsync()` - Returns field metadata
   - Should include `collectionSubFieldsMetadata` with data types

## Testing

After making changes:
1. Stop hot reload / restart the app
2. Hard refresh the browser (Ctrl+F5)
3. Open browser console
4. Follow reproduction steps
5. Check console logs
6. Verify date picker appears for date fields
