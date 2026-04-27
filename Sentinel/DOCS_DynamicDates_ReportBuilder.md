# Dynamic Dates in Report Builder

## Overview
Dynamic date filtering allows reports to use relative date references (e.g., "Today", "Past 7 Days") instead of hardcoded static dates. This is essential for automation, scheduled reports, and recurring queries that need to stay current without manual updates.

## Implementation Details

### Database Schema
**Table:** `ReportFilters`

New columns added via migration `20260405121236_AddDynamicDatesToReportFilter`:
- `IsDynamicDate` (bit, NOT NULL, default: false) - Flag indicating dynamic date usage
- `DynamicDateType` (nvarchar(50), NULL) - Type of dynamic date (e.g., "Today", "PastDays")
- `DynamicDateOffset` (int, NULL) - Numeric offset for relative dates (e.g., 7 for "Past 7 Days")
- `DynamicDateOffsetUnit` (nvarchar(20), NULL) - Unit for offset ("Days", "Weeks", "Months", "Years")

### Backend Services

#### DynamicDateResolver
**Location:** `Services/Reporting/DynamicDateResolver.cs`

Resolves dynamic date specifications to actual `DateTime` values at query execution time.

**Supported Dynamic Date Types:**
- `Today` - Current date at midnight
- `Yesterday` - Previous day
- `Tomorrow` - Next day
- `StartOfWeek` - Monday of current week
- `EndOfWeek` - Sunday of current week
- `StartOfMonth` - First day of current month
- `EndOfMonth` - Last day of current month
- `StartOfYear` - January 1st of current year
- `EndOfYear` - December 31st of current year
- `PastDays` - X days ago (requires offset)
- `NextDays` - X days from now (requires offset)
- `PastWeeks` - X weeks ago (requires offset)
- `NextWeeks` - X weeks from now (requires offset)
- `PastMonths` - X months ago (requires offset)
- `NextMonths` - X months from now (requires offset)

**Example Usage:**
```csharp
var resolver = new DynamicDateResolver();

// Simple dynamic date
var today = resolver.ResolveDate("Today"); // 2026-04-05 00:00:00

// Dynamic date with offset
var past7Days = resolver.ResolveDate("PastDays", offset: 7); // 2026-03-29 00:00:00

// Dynamic date with custom offset unit
var past2Weeks = resolver.ResolveDate("PastWeeks", offset: 2); // 2026-03-22 00:00:00
```

#### ReportDataService Integration
**Location:** `Services/Reporting/ReportDataService.cs`

The `BuildWhereClause` method detects dynamic date filters and resolves them before building LINQ expressions:

```csharp
if (filter.IsDynamicDate && (dataType == "DateTime" || dataType == "Date" || dataType == "DateOnly"))
{
    var resolvedDate = _dynamicDateResolver.ResolveDate(
        filter.DynamicDateType ?? "Today",
        filter.DynamicDateOffset,
        filter.DynamicDateOffsetUnit
    );
    value = resolvedDate.ToString("yyyy-MM-dd");
}
```

### Frontend UI

#### Date Filter UI Components
**Location:** `Pages/Reports/Builder.cshtml`

When a date field is selected for filtering, the UI provides:

1. **Mode Toggle** (Radio buttons)
   - Static Date: Traditional date picker
   - Dynamic Date: Dropdown selector for dynamic date types

2. **Dynamic Date Type Selector** (Dropdown)
   - Displays when "Dynamic Date" is selected
   - Options: Today, Yesterday, Past X Days, Next X Months, etc.

3. **Offset Input** (Shown for offset-based types)
   - Number input: Quantity (e.g., 7)
   - Unit selector: Days, Weeks, Months, Years

#### Filter Data Collection
The `getFilters()` method in the report builder collects dynamic date settings:

```javascript
if (dynamicRadio) {
    isDynamicDate = true;
    dynamicDateType = el.querySelector('.filter-dynamic-type')?.value || 'Today';
    
    const offsetValue = el.querySelector('.filter-dynamic-offset-value')?.value;
    if (offsetValue) {
        dynamicDateOffset = parseInt(offsetValue);
        dynamicDateOffsetUnit = el.querySelector('.filter-dynamic-offset-unit')?.value || 'Days';
    }
}
```

#### Saved Report Restoration
When editing an existing report, dynamic date settings are restored:

```javascript
if (filter.IsDynamicDate) {
    dynamicRadio.checked = true;
    dynamicTypeSelect.value = filter.DynamicDateType;
    offsetInput.value = filter.DynamicDateOffset;
}
```

## Use Cases

### Automation & Scheduled Reports
**Problem:** Automated reports with hardcoded date ranges become stale.

**Solution:** Use dynamic dates that recalculate on each execution.

**Example:** Daily COVID-19 case report
- Filter: `OnsetDate >= Past7Days`
- When run on 2026-04-05, shows cases from 2026-03-29 onwards
- When run on 2026-04-06, automatically shows cases from 2026-03-30 onwards

### Outbreak Dashboards
**Problem:** Dashboard showing "cases in the past 14 days" requires manual date updates.

**Solution:** Use `Past14Days` dynamic date filter.

**Example:** Active outbreak monitoring
- Filter: `OnsetDate >= PastWeeks(2)` AND `Status != Closed`
- Always shows cases from the past 2 weeks, regardless of when viewed

### Task Assignment Rules
**Problem:** Automated task creation needs to check "cases created today".

**Solution:** Use `Today` dynamic date for real-time filtering.

**Example:** Daily case review task creation
- Filter: `CreatedAt = Today`
- Automation rule: Create review task for each case created today

## Future Enhancements

### Planned Features
1. **Date Difference Calculations** (Not Yet Implemented)
   - Calculate days between two date fields
   - Example: "Days between OnsetDate and FirstLabDate > 7"
   - Requires calculated field support in report builder

2. **Time Component Support**
   - Current implementation uses midnight (00:00:00)
   - Future: Support time-of-day specifications (e.g., "Past 24 Hours")

3. **Custom Week Start Day**
   - Current: Weeks start on Monday
   - Future: Configurable week start day (Sunday, Monday, etc.)

4. **Fiscal Year Support**
   - Current: Calendar year (Jan 1 - Dec 31)
   - Future: Configurable fiscal year boundaries

5. **Natural Language Input**
   - Future UI redesign could support: "in the past 7 days", "last week", "this month"

## Migration to Automation System

When query builder is modularized for automation, outbreak matching, and task assignment:

1. **Shared Query Engine**
   - Extract `BuildWhereClause` logic to shared service
   - Reuse `DynamicDateResolver` across all systems

2. **Automation Rules**
   - Rule: "IF Case.OnsetDate = Today THEN Create ReviewTask"
   - Dynamic date resolves fresh on each automation execution

3. **Outbreak Case Matching**
   - Match cases: "OnsetDate in Past30Days AND Jurisdiction = X"
   - Dynamic criteria that updates daily

## Testing Guidelines

### Manual Testing Checklist
- [ ] Create report with date filter
- [ ] Toggle between Static and Dynamic date modes
- [ ] Select "Today" - verify current date is used
- [ ] Select "Past 7 Days" with offset 7 - verify 7 days ago
- [ ] Save report with dynamic date
- [ ] Reload report - verify dynamic settings restored
- [ ] Preview report - verify correct data returned
- [ ] Change system date - verify dynamic date recalculates

### Automated Test Scenarios
```csharp
[Fact]
public void ResolveDate_Today_ReturnsCurrentDate()
{
    var resolver = new DynamicDateResolver();
    var result = resolver.ResolveDate("Today");
    Assert.Equal(DateTime.Now.Date, result);
}

[Fact]
public void ResolveDate_Past7Days_Returns7DaysAgo()
{
    var resolver = new DynamicDateResolver();
    var result = resolver.ResolveDate("PastDays", offset: 7);
    var expected = DateTime.Now.Date.AddDays(-7);
    Assert.Equal(expected, result);
}
```

## Known Limitations

1. **Date Arithmetic in Filters**
   - Cannot currently express: "OnsetDate - LabDate > 7 days"
   - Workaround: Create calculated field or use collection queries

2. **Timezone Handling**
   - All dynamic dates use server timezone
   - Future: User-specific timezone support

3. **Between Operator**
   - Both start and end dates must be static OR both dynamic
   - Cannot mix: "Between Today AND 2026-12-31"

## API Contracts

### ReportFilter Model
```csharp
public class ReportFilter
{
    public string FieldPath { get; set; }
    public string Operator { get; set; }
    public string? Value { get; set; } // Empty for dynamic dates
    
    // Dynamic date properties
    public bool IsDynamicDate { get; set; }
    public string? DynamicDateType { get; set; }
    public int? DynamicDateOffset { get; set; }
    public string? DynamicDateOffsetUnit { get; set; }
}
```

### Report Preview API
```json
POST /api/reports/preview
{
  "filters": [
    {
      "fieldPath": "OnsetDate",
      "operator": "GreaterThanOrEqual",
      "value": "",
      "dataType": "DateTime",
      "isDynamicDate": true,
      "dynamicDateType": "PastDays",
      "dynamicDateOffset": 7,
      "dynamicDateOffsetUnit": "Days"
    }
  ]
}
```

## Related Files
- `Models/Reporting/ReportFilter.cs` - Data model
- `Services/Reporting/IDynamicDateResolver.cs` - Service interface
- `Services/Reporting/DynamicDateResolver.cs` - Service implementation
- `Services/Reporting/ReportDataService.cs` - Query builder integration
- `Pages/Reports/Builder.cshtml` - UI implementation
- `Migrations/20260405121236_AddDynamicDatesToReportFilter.cs` - Database migration

## Version History
- **v1.0.2-alpha (April 2026)**: Initial dynamic date implementation
  - Basic dynamic date types (Today, Yesterday, Past X Days, etc.)
  - UI toggle for static/dynamic dates
  - Backend resolution service
  - Database schema updates
