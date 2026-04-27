# Report Builder Refactoring - Implementation Guide

## Problem
The Report Builder page (Pages/Reports/Builder.cshtml) contains ~2300 lines of embedded JavaScript mixed with Razor syntax (@:). This causes persistent syntax errors every time the file is edited because whitespace in Razor blocks is fragile.

## Solution  
**Structural refactor**: Extract all JavaScript into external files to permanently eliminate Razor/JS mixing.

## Files Created
1. **wwwroot/js/report-builder.js** - Core functions (filters, drag-drop, field management)
2. **wwwroot/js/report-builder-collections.js** - Collection query functions
3. **wwwroot/js/report-builder-actions.js** - Data collection, preview, save functions

## Implementation Steps

### Step 1: Update Builder.cshtml Scripts Section
Replace lines 253-2567 (the entire embedded `<script>` block) with:

```razor
@section Scripts {
<script src="https://cdn.webdatarocks.com/latest/webdatarocks.js"></script>
<script src="~/js/webdatarocks-helper.js"></script>
<script src="~/js/report-builder.js"></script>
<script src="~/js/report-builder-collections.js"></script>
<script src="~/js/report-builder-actions.js"></script>

<script>
// Initialize Report Builder with data from Razor model
document.addEventListener('DOMContentLoaded', () => {
    const savedReport = {
        reportId: @(Model.ReportId?.ToString() ?? "null"),
        entityType: '@Model.EntityType',
        fields: [
            @if (Model.ReportDefinition != null)
            {
                foreach (var field in Model.ReportDefinition.Fields.OrderBy(f => f.DisplayOrder))
                {
                    @:{
                        @:fieldPath: '@field.FieldPath',
                        @:displayName: '@field.DisplayName',
                        @:dataType: '@field.DataType',
                        @:isCustomField: @field.IsCustomField.ToString().ToLower(),
                        @:customFieldDefinitionId: @(field.CustomFieldDefinitionId?.ToString() ?? "null")
                    @:},
                }
            }
        ],
        filters: [
            @if (Model.ReportDefinition != null)
            {
                foreach (var filter in Model.ReportDefinition.Filters.OrderBy(f => f.DisplayOrder))
                {
                    var filterGroupId = filter.GroupId?.ToString() ?? "null";
                    var filterValue = filter.Value?.Replace("'", "\\'").Replace("\r", "").Replace("\n", "") ?? "";
                    var isDynamicDate = filter.IsDynamicDate.ToString().ToLower();
                    var dynamicDateType = filter.DynamicDateType?.Replace("'", "\\'") ?? "";
                    var dynamicDateOffset = filter.DynamicDateOffset?.ToString() ?? "null";
                    var dynamicDateOffsetUnit = filter.DynamicDateOffsetUnit?.Replace("'", "\\'") ?? "";
                    
                    @:{
                        @:fieldPath: '@filter.FieldPath',
                        @:operator: '@filter.Operator',
                        @:value: '@Html.Raw(filterValue)',
                        @:dataType: '@filter.DataType',
                        @:groupId: @filterGroupId,
                        @:isDynamicDate: @isDynamicDate,
                        @:dynamicDateType: '@dynamicDateType',
                        @:dynamicDateOffset: @dynamicDateOffset,
                        @:dynamicDateOffsetUnit: '@dynamicDateOffsetUnit'
                    @:},
                }
            }
        ],
        collectionQueries: @Html.Raw(Model.CollectionQueriesJson ?? "[]"),
        pivotConfiguration: '@Html.Raw(Model.ReportDefinition?.PivotConfiguration ?? "")'
    };
    
    // Store saved pivot config and report ID in ReportBuilder
    ReportBuilder.savedPivotConfiguration = savedReport.pivotConfiguration;
    ReportBuilder.reportId = savedReport.reportId;
    
    // Store original entity type for change detection
    document.getElementById('entityTypeSelector').dataset.originalValue = savedReport.entityType;
    document.getElementById('entityTypeSelector').dataset.reportId = savedReport.reportId;
    
    // Initialize ReportBuilder
    ReportBuilder.init(savedReport);
});
</script>
}
```

### Step 2: Test Drag-and-Drop
1. Open the Report Builder page
2. Try dragging a field from the left panel to "Selected Fields"
3. **Expected**: Field should be added successfully
4. **If broken**: Check browser console for JavaScript errors

### Step 3: Test Smart Filtering
1. Click "Add Filter"
2. Select a Date field (e.g., "Report Date")
3. **Expected**: Should show combined date dropdown with options like "is today", "is within the last 7 days", etc.
4. **If broken**: Check console errors

### Step 4: Test Collection Queries
1. Click "Add Query" in the "Collection Queries" section
2. Select a collection (e.g., "Lab Results")
3. Click "Add Sub-Filter"
4. Select a Date field in the sub-filter
5. **Expected**: Should show smart date filtering (same as Step 3)
6. **If broken**: Check console errors

### Step 5: Test Save/Preview
1. Add some fields
2. Click "Preview"
3. **Expected**: Should show data grid
4. Click "Save Report"
5. **Expected**: Should save successfully and redirect to Reports index

## Rollback Plan
If anything breaks:
1. Revert Builder.cshtml to the original version
2. Delete the three new JS files
3. Report the error details

## Benefits of This Refactor
✅ **Eliminates syntax corruption** - Pure JavaScript in .js files can't be corrupted by Razor formatting
✅ **Better IDE support** - JavaScript files get proper syntax highlighting and autocomplete
✅ **Easier maintenance** - Separate files are easier to navigate than 2300-line embedded script
✅ **Prevents future regressions** - No more "editing one thing breaks another" issues
✅ **Fixes original bug** - Smart filtering in collection sub-filters will work correctly

## Current Status
- ✅ Created external JavaScript files (report-builder.js, report-builder-collections.js, report-builder-actions.js)
- ⏳ Need to update Builder.cshtml to use external files
- ⏳ Need to test all functionality

## Next Steps
1. User reviews this guide
2. User or agent updates Builder.cshtml Scripts section
3. Test each feature (drag-drop, filters, collection queries, save/preview)
4. If all tests pass, refactor is complete!
