# Outbreak Statistics & Epidemic Curve - Complete

## Overview
Added comprehensive summary statistics and an interactive epidemic curve to the outbreak details page for enhanced outbreak visualization and analysis.

## Features Added

### 1. **Summary Statistics Cards** (Top of Page)
**Replaces old single-row cards with detailed classification breakdown:**

- **Confirmed Cases** (Red) - Count with check-circle icon
- **Probable Cases** (Yellow) - Count with exclamation icon  
- **Suspect Cases** (Gray) - Count with question icon
- **Total Contacts** (Blue) - Count with people icon

### 2. **Demographics Card**

**Age Distribution:**
- Median age (calculated from patient DOB)
- Age range (min - max)
- Shows "No age data available" if no DOB data

**Sex Distribution:**
- Visual progress bars for each sex
- Counts with badges
- Percentages calculated from total cases
- Categories:
  - Male (Blue)
  - Female (Pink #e91e63)
  - Other (Gray) - only shows if > 0
  - Unknown (Light gray border) - only shows if > 0

### 3. **Epidemic Curve** (Interactive Chart)

**Stacked Bar Chart showing:**
- X-axis: Date (date of onset OR date of notification if onset not available)
- Y-axis: Number of cases
- Stacked by classification:
  - Confirmed (Red)
  - Probable (Yellow)
  - Suspect (Gray)
  - Unclassified (Light gray)

**Chart Features:**
- Interactive tooltips on hover
- Legend showing all classifications
- Responsive sizing
- Proper date formatting
- Step size of 1 for case counts
- Starts at zero

## Backend Implementation

### Updated Models

**OutbreakStatistics Class:**
```csharp
public class OutbreakStatistics
{
    // Existing
    public int TotalCases { get; set; }
    public int ConfirmedCases { get; set; }
    public int ProbableCases { get; set; }
    public int SuspectCases { get; set; }
    public int TotalContacts { get; set; }
    public int TeamMemberCount { get; set; }
    public int DaysSinceStart { get; set; }
    
    // NEW - Demographics
    public double? MedianAge { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public int OtherSexCount { get; set; }
    public int UnknownSexCount { get; set; }
    
    // NEW - Epidemic Curve Data
    public List<EpiCurveDataPoint> EpiCurveData { get; set; } = new();
}

public class EpiCurveDataPoint
{
    public DateTime Date { get; set; }
    public int ConfirmedCount { get; set; }
    public int ProbableCount { get; set; }
    public int SuspectCount { get; set; }
    public int UnclassifiedCount { get; set; }
}
```

### Updated OutbreakService.GetStatisticsAsync()

**Demographics Calculation:**
```csharp
// Get patients with age data
var patientsWithAge = cases
    .Where(c => c.Case?.Patient?.DateOfBirth != null)
    .Select(c => new { Age = CalculateAge(...), Sex = ... })
    .ToList();

// Calculate median
var ages = patientsWithAge.Select(p => p.Age).OrderBy(a => a).ToList();
double? medianAge = count % 2 == 0 
    ? (ages[count/2-1] + ages[count/2]) / 2.0 
    : ages[count/2];
```

**Epidemic Curve Data:**
```csharp
var epiCurveData = cases
    .GroupBy(c => (c.Case?.DateOfOnset ?? c.Case?.DateOfNotification ?? c.LinkedDate).Date)
    .Select(g => new EpiCurveDataPoint
    {
        Date = g.Key,
        ConfirmedCount = g.Count(c => c.Classification == CaseClassification.Confirmed),
        ProbableCount = g.Count(c => c.Classification == CaseClassification.Probable),
        SuspectCount = g.Count(c => c.Classification == CaseClassification.Suspect),
        UnclassifiedCount = g.Count(c => !c.Classification.HasValue)
    })
    .OrderBy(d => d.Date)
    .ToList();
```

**Sex Counts:**
```csharp
MaleCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == 1),
FemaleCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == 2),
OtherSexCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId > 2),
UnknownSexCount = cases.Count(c => c.Case?.Patient?.SexAtBirthId == null)
```

## Frontend Implementation

### Chart.js Integration

**Added to _Layout.cshtml:**
```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
```

**Epidemic Curve Script (Details.cshtml):**
```javascript
const epiData = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.Statistics.EpiCurveData));

new Chart(ctx, {
    type: 'bar',
    data: {
        labels: dates.map(d => d.toLocaleDateString()),
        datasets: [
            { label: 'Confirmed', data: confirmedData, backgroundColor: 'rgba(220, 53, 69, 0.8)' },
            { label: 'Probable', data: probableData, backgroundColor: 'rgba(255, 193, 7, 0.8)' },
            { label: 'Suspect', data: suspectData, backgroundColor: 'rgba(108, 117, 125, 0.8)' },
            { label: 'Unclassified', data: unclassifiedData, backgroundColor: 'rgba(206, 212, 218, 0.8)' }
        ]
    },
    options: {
        responsive: true,
        scales: {
            x: { stacked: true },
            y: { stacked: true, beginAtZero: true, ticks: { stepSize: 1 } }
        }
    }
});
```

### Visual Layout

**Statistics Cards (Row 1):**
```
[Confirmed] [Probable] [Suspect] [Contacts]
   (Red)     (Yellow)    (Gray)     (Blue)
```

**Analytics Row (Row 2):**
```
[Demographics Card - 4 cols]  [Epidemic Curve - 8 cols]
     Age + Sex stats              Stacked bar chart
```

## Data Flow

### Page Load:
```
1. Load outbreak ? GetStatisticsAsync()
2. Calculate demographics from Patient.DateOfBirth
3. Group cases by date (onset/notification/linked)
4. Count by classification per date
5. Serialize to JSON for Chart.js
6. Render statistics cards
7. Render chart on DOM ready
```

### Date Priority for Epidemic Curve:
```
1. Case.DateOfOnset (preferred - symptom onset)
2. Case.DateOfNotification (fallback - when reported)
3. OutbreakCase.LinkedDate (last resort - when linked to outbreak)
```

## Visual Design

### Color Scheme:
- **Confirmed**: `#dc3545` (Red) - Most definitive
- **Probable**: `#ffc107` (Yellow) - Clinical certainty
- **Suspect**: `#6c757d` (Gray) - Under investigation
- **Unclassified**: `#ced4da` (Light gray) - Needs review
- **Male**: `#0d6efd` (Blue)
- **Female**: `#e91e63` (Pink)

### Card Styling:
- Icon badges with opacity backgrounds
- Colored borders matching classification
- Progress bars for sex distribution
- Responsive grid (4 columns ? 2 ? 1 on mobile)

## Use Cases

### For Epidemiologists:
1. **Quick Assessment** - Classification breakdown at a glance
2. **Demographic Profile** - Age/sex characteristics of outbreak
3. **Temporal Trends** - Epidemic curve shows outbreak trajectory
4. **Case Distribution** - Stacked bars show classification composition over time

### For Public Health Response:
1. **Resource Allocation** - Age range informs targeted interventions
2. **Outbreak Phase** - Curve shape indicates if expanding, plateau, or declining
3. **Classification Progress** - Unclassified cases highlight investigation gaps
4. **Sex-based Patterns** - Identify if outbreak affects specific demographics

## Statistics Calculated

### Age Statistics:
- **Median Age**: Middle value of sorted ages (handles even/odd counts)
- **Min/Max Age**: Range of affected population
- **Calculation**: `DateTime.UtcNow.Year - DateOfBirth.Year`

### Sex Distribution:
- **Male Count**: SexAtBirthId == 1
- **Female Count**: SexAtBirthId == 2
- **Other Count**: SexAtBirthId > 2
- **Unknown Count**: SexAtBirthId == null
- **Percentages**: (Count / TotalCases) * 100

### Epidemic Curve:
- **Grouping**: By date (onset > notification > linked)
- **Stacking**: Confirmed + Probable + Suspect + Unclassified
- **Ordering**: Chronological (oldest to newest)

## Testing

### Test Scenarios:

1. **Empty Outbreak** ? Shows zeros, no chart data
2. **No Age Data** ? Shows "No age data available"
3. **All One Sex** ? Progress bar at 100%, others at 0%
4. **Mixed Classifications** ? Stacked bars show all colors
5. **Same Day Cases** ? Single bar with multiple classifications stacked
6. **Span Multiple Days** ? Multiple bars showing temporal progression

### Example Outbreak:
```
10 cases over 2 weeks
- 5 Confirmed (3 Male, 2 Female)
- 3 Probable (2 Male, 1 Female)
- 2 Suspect (1 Male, 1 Female)
Age range: 25-65, Median: 42
```

Result: 3 colored stat cards, demographics showing 60% male / 40% female, epidemic curve with 14 bars (some days may have multiple cases stacked).

## Technical Notes

### Performance:
- Statistics calculated once per page load
- Chart.js renders client-side (no server rendering)
- Epidemic curve data pre-aggregated in backend
- Minimal database queries (reuses existing GetOutbreakCasesAsync)

### Responsive Behavior:
- Cards stack on mobile (col-md-3 ? full width)
- Chart maintains aspect ratio
- Demographics card stacks with curve on tablet
- Progress bars scale to container width

### Browser Compatibility:
- Chart.js 4.4.1 supports all modern browsers
- Canvas element required (IE11+ supported by Chart.js)
- Date formatting uses native `toLocaleDateString()`

## Future Enhancements

1. **Interactive Filtering**
   - Click legend to hide/show classifications
   - Date range selector
   - Filter by age group or sex

2. **Additional Charts**
   - Age pyramid
   - Geographic distribution map
   - Attack rate by demographic

3. **Export Options**
   - Download chart as PNG
   - Export data to CSV
   - Generate PDF report

4. **Real-time Updates**
   - WebSocket integration
   - Auto-refresh epidemic curve
   - Live case count updates

5. **Comparative Analytics**
   - Compare multiple outbreaks
   - Overlay historical data
   - Benchmark against expected curves

## Files Modified

```
Surveillance-MVP/
??? Services/
?   ??? IOutbreakService.cs           # ? Added demographics & epi curve properties
?   ??? OutbreakService.cs            # ? Calculate stats in GetStatisticsAsync()
??? Pages/
?   ??? Outbreaks/
?   ?   ??? Details.cshtml            # ? New stats cards, demographics, chart
?   ??? Shared/
?       ??? _Layout.cshtml            # ? Added Chart.js CDN
```

## Summary

? **Summary statistics** with classification breakdown
? **Demographics analysis** (age + sex distribution)
? **Epidemic curve** with stacked bar chart
? **Interactive visualization** using Chart.js
? **Responsive design** for all screen sizes
? **Color-coded** by classification status
? **Smart date selection** (onset > notification > linked)
? **Real-time calculation** from linked cases

The outbreak details page now provides **comprehensive epidemiological visualization** to support outbreak investigation and public health decision-making! ????

Investigators can quickly assess the outbreak's temporal progression, demographic profile, and classification distribution at a glance.
