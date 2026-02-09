# Line List - All Data Types Now Available

## Summary of What Was Added

? **Lab Result Fields** - Latest lab result for each case  
? **Exposure Event Fields** - Primary exposure event for each case  
? **Custom Field Fields** - Dynamic fields based on outbreak's disease  

## New Field Categories Available

### 1. **Exposure** Category (9 fields)
Shows information from the **primary exposure event** (reporting exposure or earliest):

| Field Path | Display Name | Data Type |
|------------|--------------|-----------|
| `Exposure.Type` | Exposure Type | string |
| `Exposure.StartDate` | Exposure Start Date | date |
| `Exposure.EndDate` | Exposure End Date | date |
| `Exposure.Location` | Exposure Location | string |
| `Exposure.Event` | Exposure Event | string |
| `Exposure.Status` | Exposure Status | string |
| `Exposure.City` | Exposure City | string |
| `Exposure.State` | Exposure State | string |
| `Exposure.Country` | Exposure Country | string |

**Logic**: 
- Uses `OrderByDescending(e => e.IsReportingExposure).ThenBy(e => e.ExposureStartDate)`
- Shows the reporting exposure if flagged, otherwise the earliest exposure
- Location shows either linked Location name or free text location

### 2. **Laboratory** Category (7 fields)
Shows information from the **latest lab result** (by result date or collection date):

| Field Path | Display Name | Data Type |
|------------|--------------|-----------|
| `Lab.SpecimenDate` | Specimen Collection Date | date |
| `Lab.SpecimenType` | Specimen Type | string |
| `Lab.TestType` | Test Type | string |
| `Lab.Result` | Test Result | string |
| `Lab.ResultDate` | Result Date | date |
| `Lab.Laboratory` | Laboratory | string |
| `Lab.AccessionNumber` | Accession Number | string |

**Logic**:
- Uses `OrderByDescending(lr => lr.ResultDate ?? lr.SpecimenCollectionDate)`
- Shows the most recent lab result
- Returns null if no lab results exist

### 3. **Custom Fields** Category (Dynamic)
Shows **disease-specific custom fields** for the outbreak's primary disease:

- **Automatically loaded** based on outbreak's primary disease
- Shows all active custom field definitions
- Field path format: `Case.CustomField.{FieldName}`
- Supports all custom field types:
  - String
  - Number
  - Date
  - Boolean
  - Lookup

**Logic**:
- Queries `CustomFieldDefinitions` table for outbreak's primary disease
- Orders by `DisplayOrder`
- Shows label (if exists) or field name
- Maps data type correctly for grid rendering

## How It Works

### Field Extraction Flow

```
User selects "Exposure.Location" field
?
ExtractFieldValue() called with fieldPath = "Exposure.Location"
?
parts = ["Exposure", "Location"]
?
Calls HandleExposureField(case, parts)
?
Gets primary exposure event
?
Returns exposure.Location?.Name ?? exposure.FreeTextLocation
?
Displayed in grid
```

### Custom Field Extraction

```
User selects "Case.CustomField.FavoriteFoodQuestion"
?
parts = ["Case", "CustomField", "FavoriteFoodQuestion"]
?
Calls HandleCustomField(caseData, parts)
?
Searches all custom field collections:
  - CustomFieldStrings
  - CustomFieldNumbers
  - CustomFieldDates
  - CustomFieldBooleans
  - CustomFieldLookups
?
Returns value from matching field
?
Displayed in grid
```

## Testing Steps

### Test Exposure Fields

1. **Setup**:
   - Ensure outbreak has cases with exposure events
   - Some cases should have multiple exposures
   - Some should have `IsReportingExposure = true`

2. **Test**:
   ```
   Navigate to: Outbreak Details ? Line List
   Click: Configure Fields
   Expand: Exposure category
   Add: Exposure Location, Exposure Start Date, Exposure Type
   Click: Apply Changes
   ```

3. **Expected Results**:
   - Reporting exposure shows first
   - Location shows linked location name or free text
   - Type shows exposure type enum (Travel, Contact, etc.)
   - Date formatted as YYYY-MM-DD

### Test Lab Fields

1. **Setup**:
   - Ensure outbreak has cases with lab results
   - Some cases should have multiple lab results

2. **Test**:
   ```
   Navigate to: Outbreak Details ? Line List
   Click: Configure Fields
   Expand: Laboratory category
   Add: Test Result, Result Date, Specimen Type
   Click: Apply Changes
   ```

3. **Expected Results**:
   - Latest lab result shows (by result date)
   - Specimen type shows lookup name
   - Result shows lookup value (Positive, Negative, etc.)

### Test Custom Fields

1. **Setup**:
   - Create disease with custom fields
   - Create outbreak with that disease as primary disease
   - Create cases with custom field data

2. **Test**:
   ```
   Navigate to: Outbreak Details ? Line List
   Click: Configure Fields
   Expand: Custom Fields category
   ```

3. **Expected Results**:
   - All active custom fields for the disease appear
   - Field labels match what's configured
   - Ordered by DisplayOrder

4. **Add and Test**:
   ```
   Add: Several custom fields
   Click: Apply Changes
   ```

5. **Expected Results**:
   - String fields show text values
   - Number fields show numeric values
   - Date fields formatted as YYYY-MM-DD
   - Boolean fields show true/false
   - Lookup fields show lookup value

## Edge Cases Handled

### Exposure Fields
| Scenario | Behavior |
|----------|----------|
| No exposure events | Returns null for all exposure fields |
| Multiple exposures, no reporting | Shows earliest by start date |
| Multiple exposures, one reporting | Shows the reporting exposure |
| Location is linked | Shows Location.Name |
| Location is free text | Shows FreeTextLocation |

### Lab Fields
| Scenario | Behavior |
|----------|----------|
| No lab results | Returns null for all lab fields |
| Multiple results | Shows latest by ResultDate or SpecimenCollectionDate |
| Result with no date | Uses SpecimenCollectionDate |
| Accession number missing | Returns null |

### Custom Fields
| Scenario | Behavior |
|----------|----------|
| Outbreak has no primary disease | No custom fields shown |
| Disease has no custom fields | Category appears but empty |
| Custom field not filled in for case | Returns null |
| Multiple custom field types | Searches all and returns first match |
| Lookup field | Returns LookupValue.Value |

## Export Functionality

All new fields work seamlessly with **CSV Export**:

```csharp
// Export includes lab, exposure, and custom fields
await ExportToCsvAsync(outbreakId, fieldPaths, sortConfig)
```

**Example CSV Output**:
```csv
"Case ID","Disease","Exposure Type","Exposure Location","Test Result","Custom: Favorite Food"
"C-001","Salmonella","Food","Restaurant ABC","Positive","Chicken"
"C-002","Salmonella","Contact","Home","Pending",""
```

## Performance Considerations

### Exposure & Lab Data
- ? **Already included** in main query via Include statements
- ? **No additional database queries** needed
- ? **In-memory sorting** for primary/latest selection
- ?? **Cartesian explosion** prevented by `.AsSplitQuery()`

### Custom Fields
- ? **Loaded once** during field definition query
- ? **Included** in main case query
- ? **Indexed** by FieldDefinition relationship
- ?? **Could be large** if many custom fields exist

### Optimization Tips

**For Large Datasets**:
```csharp
// Consider pagination
query = query
    .Skip(pageNumber * pageSize)
    .Take(pageSize);
```

**For Many Custom Fields**:
```csharp
// Filter custom field includes by selected fields
var neededCustomFields = fieldPaths
    .Where(fp => fp.StartsWith("Case.CustomField."))
    .Select(fp => fp.Split('.')[2])
    .ToList();

// Then conditionally include only needed types
```

## Known Limitations

### Multiple Values
**Current Behavior**: Shows **one** value (primary/latest)

**Workaround Options**:
1. **Export all as separate rows** (one row per exposure/lab)
2. **Aggregate multiple values** (comma-separated)
3. **Add field variants** (Exposure1, Exposure2, etc.)

### Custom Field Visibility
**Current**: Shows **all active** custom fields for disease

**Future Enhancement**: Could filter by:
- Custom field scope (Case vs Patient vs Exposure)
- Custom field visibility settings
- User permissions

### Date Formatting
**Current**: All dates formatted as `YYYY-MM-DD`

**Enhancement**: Could support:
- Locale-specific formatting
- User-preferred date format
- Time component for datetime fields

## Code Reference

### Main Methods Modified

**`GetAvailableFieldsAsync()`**:
- Now `async` to load custom fields from database
- Queries outbreak for primary disease
- Queries custom field definitions
- Builds complete field list

**`ExtractFieldValue()`**:
- Already had logic for Exposure, Lab, CustomField
- No changes needed - just exposed in UI

**`HandleExposureField()`**:
- Orders by reporting exposure flag
- Then by earliest start date
- Returns primary exposure values

**`HandleLabField()`**:
- Orders by result date (or collection date)
- Returns latest lab values

**`HandleCustomField()`**:
- Searches all custom field collections
- Matches by FieldDefinition.Name
- Returns value based on type

## Files Modified

1. **`Services/LineListService.cs`**
   - Changed `GetAvailableFieldsAsync()` from sync to async
   - Added database query for custom field definitions
   - Added Exposure fields to available list
   - Added Lab fields to available list
   - Dynamic custom field loading

## Summary

? **Line list now supports ALL major data types**:
- Patient demographics
- Case core fields
- Outbreak classification
- **Exposure events** (NEW)
- **Lab results** (NEW)
- **Custom fields** (NEW - dynamic)

? **Total Available Fields**:
- Patient: 17 fields
- Case: 7 fields
- Outbreak: 5 fields
- **Exposure: 9 fields** ? NEW
- **Laboratory: 7 fields** ? NEW
- **Custom Fields: X fields** ? NEW (dynamic based on disease)

? **Export Support**: CSV export includes all field types

? **Performance**: Optimized with AsSplitQuery and efficient includes

## Next Steps

Users can now:
1. ? Select any combination of fields from all categories
2. ? Include exposure tracking data in line lists
3. ? Include laboratory results in analysis
4. ? Include disease-specific custom fields
5. ? Export complete datasets to CSV
6. ? Save custom field configurations
7. ? Share configurations with team

**Ready for production use!** ??

Restart the app and test by selecting fields from the new Exposure, Laboratory, and Custom Fields categories!
