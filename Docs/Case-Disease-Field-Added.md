# Disease Field Added to Case Management ?

## Summary

Added a **Disease dropdown field** to Case Create and Edit pages with hierarchical disease display.

## Changes Made

### 1. **Case Create Page** (`Pages/Cases/Create.cshtml`)
- ? Added Disease dropdown after Confirmation Status
- ? Shows hierarchical structure with visual indicators (?)
- ? Includes helper text

### 2. **Case Create PageModel** (`Pages/Cases/Create.cshtml.cs`)
- ? Added `ViewBag.DiseaseId` population in `OnGetAsync()`
- ? Added same in `OnPostAsync()` for validation errors
- ? Loads only active diseases
- ? Sorts by Level ? DisplayOrder ? Name
- ? Displays hierarchy with special characters

### 3. **Case Edit Page** (`Pages/Cases/Edit.cshtml`)
- ? Added Disease dropdown (same as Create)
- ? Pre-selects current disease value

### 4. **Case Edit PageModel** (`Pages/Cases/Edit.cshtml.cs`)
- ? Added `ViewBag.DiseaseId` population in `OnGetAsync()`
- ? Added same in `OnPostAsync()` for validation errors

### 5. **Case Details Page** (`Pages/Cases/Details.cshtml`)
- ? Added Disease display in Case Information section
- ? Shows disease name in bold
- ? Shows "Notifiable" badge if applicable
- ? Shows Code and Export Code
- ? Shows "-" if no disease assigned

### 6. **Case Details PageModel** (`Pages/Cases/Details.cshtml.cs`)
- ? Added `.Include(c => c.Disease)` to load disease data

### 7. **Case Index Page** (`Pages/Cases/Index.cshtml`)
- ? Added "Disease" column to table
- ? Shows disease name
- ? Shows notifiable badge icon if applicable
- ? Shows "-" if no disease

### 8. **Case Index PageModel** (`Pages/Cases/Index.cshtml.cs`)
- ? Added `.Include(c => c.Disease)` to load disease data

## How It Works

### Hierarchical Display

The dropdown shows diseases with visual hierarchy:

```
-- Select Disease --
Salmonella
? Salmonella Typhimurium
?? Salmonella Typhimurium 9
Campylobacter
? Campylobacter jejuni
```

Each level is indented with `?` characters, making parent-child relationships clear.

### Code Implementation

**Loading Diseases:**
```csharp
ViewData["DiseaseId"] = new SelectList(
    await _context.Diseases
        .Where(d => d.IsActive)
        .OrderBy(d => d.Level)
        .ThenBy(d => d.DisplayOrder)
        .ThenBy(d => d.Name)
        .Select(d => new { 
            d.Id, 
            DisplayName = new string('?', d.Level) + " " + d.Name 
        })
        .ToListAsync(),
    "Id", "DisplayName");
```

**Dropdown HTML:**
```razor
<div class="mb-3">
    <label asp-for="Case.DiseaseId" class="form-label">Disease</label>
    <select asp-for="Case.DiseaseId" class="form-select" asp-items="ViewBag.DiseaseId">
        <option value="">-- Select Disease --</option>
    </select>
    <span asp-validation-for="Case.DiseaseId" class="text-danger"></span>
    <small class="form-text text-muted">Select the disease for this case</small>
</div>
```

## Usage

### Creating a Case
1. Navigate to **Cases ? Create New Case**
2. Select Patient
3. **Select Disease** from dropdown (hierarchical)
4. Enter dates and confirmation status
5. Click "Create Case"

### Editing a Case
1. Navigate to case details
2. Click "Edit"
3. **Change Disease** if needed (current value is pre-selected)
4. Click "Save Changes"

### Viewing Cases

**Index Page:**
- New "Disease" column shows disease name
- Notifiable diseases show a bell icon badge

**Details Page:**
- Disease section shows:
  - Disease name (bold)
  - "Notifiable" badge if applicable
  - Code and Export Code
  - "-" if no disease assigned

## Features

### ? Hierarchical Display
- Visual hierarchy with `?` characters
- Clear parent-child relationships
- Sorted by level and display order

### ? Active Diseases Only
- Only shows diseases marked as "Active"
- Inactive diseases are hidden

### ? Notifiable Indicator
- Shows warning badge for notifiable diseases
- Helps identify diseases requiring official reporting

### ? Nullable Field
- Disease is **optional** (nullable)
- Existing cases without diseases remain valid
- Shows "-" if no disease selected

### ? Eager Loading
- Disease data is included in queries
- No N+1 query problems
- Follows `.github/copilot-instructions.md` best practices

## Query Examples

### Get all Salmonella cases (any type)
```csharp
var salmonellaId = "guid-of-salmonella";
var cases = await _context.Cases
    .Include(c => c.Patient)
    .Include(c => c.Disease)
    .Where(c => c.Disease != null && 
                c.Disease.PathIds.Contains($"/{salmonellaId}/"))
    .ToListAsync();
```

This returns:
- Direct Salmonella cases
- Salmonella Typhimurium cases
- Salmonella Typhimurium 9 cases
- Any other Salmonella subtypes

**No recursion needed!** Path-based querying makes it simple.

## Display Examples

### Create/Edit Dropdown
```
???????????????????????????????????
? -- Select Disease --            ?
? Salmonella                      ?
? ? Salmonella Typhimurium        ?
? ?? Salmonella Typhimurium 9     ?
? Campylobacter                   ?
? ? Campylobacter jejuni          ?
???????????????????????????????????
```

### Index Table
```
| Case ID  | Patient      | Disease                    | Date of Onset |
|----------|--------------|----------------------------|---------------|
| C-2025-1 | John Smith   | Salmonella Typhimurium ?? | 15 Jan 2025   |
| C-2025-2 | Jane Doe     | Campylobacter jejuni      | 14 Jan 2025   |
```

### Details Page
```
Disease: Salmonella Typhimurium ?? Notifiable
         Code: SAL-TYP | Export: A02.0
```

## Build Status
? **Build successful**  
? **All pages updated**  
? **Ready to use**

## Testing Checklist

1. ? Create new case with disease
2. ? Create new case without disease (optional)
3. ? Edit case and change disease
4. ? View case details with disease
5. ? View case list with disease column
6. ? Verify hierarchy displays correctly
7. ? Verify notifiable badges appear
8. ? Verify validation works

## Next Steps

### Optional Enhancements

**1. Make Disease Required** (after data migration)
If you want to require disease for all cases:
```csharp
// In Models/Case.cs
[Required]
public Guid? DiseaseId { get; set; }
```

**2. Add Disease Filter to Case Index**
Add dropdown filter to show only specific diseases:
```csharp
// Add to Index.cshtml.cs
public Guid? FilterDiseaseId { get; set; }

public async Task OnGetAsync(Guid? diseaseId)
{
    FilterDiseaseId = diseaseId;
    
    var query = _context.Cases
        .Include(c => c.Patient)
        .Include(c => c.Disease);
    
    if (diseaseId.HasValue)
    {
        query = query.Where(c => c.Disease != null && 
            c.Disease.PathIds.Contains($"/{diseaseId}/"));
    }
    
    Cases = await query.ToListAsync();
}
```

**3. Add Disease Statistics to Dashboard**
Show case counts by disease:
```csharp
var stats = await _context.Cases
    .Where(c => c.Disease != null)
    .GroupBy(c => c.Disease!.Name)
    .Select(g => new { Disease = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .Take(10)
    .ToListAsync();
```

## Files Modified

- ? `Pages/Cases/Create.cshtml`
- ? `Pages/Cases/Create.cshtml.cs`
- ? `Pages/Cases/Edit.cshtml`
- ? `Pages/Cases/Edit.cshtml.cs`
- ? `Pages/Cases/Details.cshtml`
- ? `Pages/Cases/Details.cshtml.cs`
- ? `Pages/Cases/Index.cshtml`
- ? `Pages/Cases/Index.cshtml.cs`

## Summary

? **Disease dropdown added to Case Create/Edit**  
? **Hierarchical display with visual indicators**  
? **Disease shown in Case Index and Details**  
? **Notifiable badges displayed**  
? **Eager loading follows best practices**  
? **Build successful - Ready to use!**

The Case management system now fully integrates with the Disease hierarchy!
