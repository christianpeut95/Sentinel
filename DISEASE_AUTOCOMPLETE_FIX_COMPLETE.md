# Disease Autocomplete Fix for Task Template Create Page ?

## Issue Identified
The disease autocomplete field on the Task Template Create page (`/Settings/Lookups/CreateTaskTemplate`) was not working.

## Root Cause
The JavaScript autocomplete was calling `/api/diseases/search` endpoint, but **this endpoint did not exist** in Program.cs.

## Solution Applied ?

### Added Disease Search API Endpoint
**File Modified:** `Surveillance-MVP/Program.cs`

Added new minimal API endpoint after the organizations search endpoint:

```csharp
// API endpoint for disease autocomplete
app.MapGet("/api/diseases/search", async (string term, ApplicationDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var diseases = await context.Diseases
        .Where(d => d.IsActive && d.Name.Contains(term))
        .OrderBy(d => d.Level)
        .ThenBy(d => d.DisplayOrder)
        .ThenBy(d => d.Name)
        .Take(20)
        .Select(d => new
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            Level = d.Level,
            ParentDiseaseId = d.ParentDiseaseId
        })
        .ToListAsync();

    return Results.Json(diseases);
});
```

## How It Works

### Request
```
GET /api/diseases/search?term=measles
```

### Response
```json
[
  {
    "id": "guid-here",
    "name": "Measles",
    "code": "MEAS",
    "level": 0,
    "parentDiseaseId": null
  },
  ...
]
```

### JavaScript Integration
The existing JavaScript in `CreateTaskTemplate.cshtml` already expects this format:

```javascript
$("#diseaseAutocomplete").autocomplete({
    source: function(request, response) {
        $.ajax({
            url: "/api/diseases/search",
            data: { term: request.term },
            success: function(data) {
                response(data.map(function(item) {
                    return {
                        label: item.name,
                        value: item.name,
                        id: item.id
                    };
                }));
            }
        });
    },
    minLength: 2,
    select: function(event, ui) {
        $("#diseaseId").val(ui.item.id);
        $("#diseaseNameBadge").text(ui.item.label);
        $("#diseaseDisplay").show();
        return false;
    }
});
```

## Features

### Endpoint Capabilities
- ? Searches by disease name (case-insensitive, contains)
- ? Only returns active diseases
- ? Ordered by hierarchy (Level ? DisplayOrder ? Name)
- ? Limits to 20 results
- ? Returns essential fields: Id, Name, Code, Level, ParentDiseaseId

### UI Features
- ? Type to search (minimum 2 characters)
- ? Disease badge display on selection
- ? Hidden field stores disease ID
- ? Remove button to clear selection
- ? Optional field (not required)

## Testing

### To Test
1. Navigate to **Settings ? Lookups ? Task Templates ? Create Task Template**
2. Scroll to the "Disease" field
3. Type a disease name (e.g., "measles", "covid", "tb")
4. ? Autocomplete suggestions should appear
5. Select a disease
6. ? Disease badge should display
7. Click "Remove" button
8. ? Disease selection should clear

### Expected Behavior
- **Before fix:** No suggestions appeared, console showed 404 error
- **After fix:** Suggestions appear, disease can be selected

## Build Status
? **Build Successful** - No compilation errors

## Files Modified
1. ? `Surveillance-MVP/Program.cs` - Added `/api/diseases/search` endpoint

## Files Already Correct
- ? `Surveillance-MVP/Pages/Settings/Lookups/CreateTaskTemplate.cshtml` - JavaScript already configured correctly
- ? `Surveillance-MVP/Pages/Settings/Lookups/CreateTaskTemplate.cshtml.cs` - Model already has DiseaseId property

## Impact
- **Task Template Creation** - Can now link task templates to specific diseases via autocomplete
- **No Breaking Changes** - Existing functionality unchanged
- **Consistent with Other Endpoints** - Follows same pattern as `/api/events/search`, `/api/locations/search`, etc.

## Similar Endpoints (For Reference)
The project now has these autocomplete endpoints:
- `/api/address-suggest` - Address autocomplete (Google Places)
- `/api/countries/search` - Country search
- `/api/cases/search` - Case search with patient info
- `/api/events/search` - Event search
- `/api/locations/search` - Location search
- `/api/organizations/search` - Organization search
- **`/api/diseases/search` ? NEW** - Disease search

## Next Steps
? **COMPLETE** - Disease autocomplete now functional

Optional enhancements (not required):
- Add disease hierarchy display in suggestions (show parent disease)
- Add disease code in autocomplete label
- Add icon/color for disease severity

---

*Fix Applied: February 6, 2026*  
*Build Status: ? SUCCESS*  
*Status: ? COMPLETE*
