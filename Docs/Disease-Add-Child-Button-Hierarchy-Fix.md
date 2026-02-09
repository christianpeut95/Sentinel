# Disease Management - Add Child Button & Hierarchy Fix ?

## Summary

Added **"Create & Add Child"** button to Disease pages and **fixed the hierarchy display** issue (replaced "?" characters with proper em-dash "—").

## Issues Fixed

### 1. **Hierarchy Character Display Issue**
**Problem:** Child diseases showed "?" character instead of proper hierarchy indicator.

**Root Cause:** The Unicode character '?' (U+2514) wasn't encoding properly in some contexts.

**Solution:** Changed to em-dash '—' (U+2014) which displays correctly across all browsers and encodings.

### 2. **Missing Child Creation Workflow**
**Problem:** No quick way to add child diseases after creating a parent.

**Solution:** Added "Create & Add Child" button and "Add Child Disease" button on Details page.

## Changes Made

### 1. **Disease Index Page** (`Pages/Settings/Diseases/Index.cshtml`)
**Before:**
```csharp
<span class="text-muted me-1">@(new string('?', item.Level))</span>
```

**After:**
```csharp
<span class="text-muted me-1">@(new string('—', item.Level))</span>
```

Now displays:
```
Salmonella
— Salmonella Typhimurium
—— Salmonella Typhimurium 9
```

### 2. **Disease Create Page** (`Pages/Settings/Diseases/Create.cshtml`)
Added two submit buttons:

**Before:**
```html
<button type="submit" class="btn btn-primary">
    <i class="bi bi-check-circle me-1"></i>Create
</button>
```

**After:**
```html
<button type="submit" name="action" value="create" class="btn btn-primary">
    <i class="bi bi-check-circle me-1"></i>Create
</button>
<button type="submit" name="action" value="createAndAddChild" class="btn btn-success ms-2">
    <i class="bi bi-plus-circle me-1"></i>Create & Add Child
</button>
```

### 3. **Disease Create PageModel** (`Pages/Settings/Diseases/Create.cshtml.cs`)

**Added parameter to OnGetAsync:**
```csharp
public async Task<IActionResult> OnGetAsync(Guid? parentId)
{
    await LoadParentDiseases();
    
    // If parentId is provided, pre-select it
    if (parentId.HasValue)
    {
        Disease = new Disease { ParentDiseaseId = parentId.Value };
    }
    
    return Page();
}
```

**Updated OnPostAsync to handle button action:**
```csharp
public async Task<IActionResult> OnPostAsync(string action)
{
    // ... validation code ...
    
    _context.Diseases.Add(Disease);
    await _context.SaveChangesAsync();
    TempData["SuccessMessage"] = $"Disease '{Disease.Name}' has been created successfully.";
    
    // If "Create & Add Child" was clicked, redirect to create page with parent set
    if (action == "createAndAddChild")
    {
        return RedirectToPage("./Create", new { parentId = Disease.Id });
    }
    
    return RedirectToPage("./Index");
}
```

### 4. **Disease Details Page** (`Pages/Settings/Diseases/Details.cshtml`)
Added "Add Child Disease" button:

```html
<a asp-page="./Create" asp-route-parentId="@Model.Disease.Id" class="btn btn-success">
    <i class="bi bi-plus-circle me-1"></i>Add Child Disease
</a>
```

### 5. **Fixed All Dropdowns**
Updated hierarchy display in all dropdown loading code:

**Files Changed:**
- `Pages/Settings/Diseases/Create.cshtml.cs`
- `Pages/Settings/Diseases/Edit.cshtml.cs`
- `Pages/Cases/Create.cshtml.cs` (2 places)
- `Pages/Cases/Edit.cshtml.cs` (2 places)

**Before:**
```csharp
DisplayName = new string('?', d.Level) + " " + d.Name
```

**After:**
```csharp
DisplayName = new string('—', d.Level) + " " + d.Name
```

## How It Works

### Workflow 1: Create & Add Child Button

1. **Create Parent Disease:**
   - Navigate to Settings ? Diseases ? Create New
   - Fill in disease details (e.g., "Salmonella")
   - Click **"Create & Add Child"** button

2. **Automatic Redirect:**
   - Disease is saved
   - Page redirects to Create page again
   - **Parent is pre-selected** automatically
   - User can immediately add child (e.g., "Salmonella Typhimurium")

3. **Continue Adding Children:**
   - Click "Create & Add Child" again
   - Keeps adding children in hierarchy

### Workflow 2: Add Child from Details

1. **View Disease Details:**
   - Navigate to Settings ? Diseases
   - Click on any disease to view details

2. **Click "Add Child Disease":**
   - Green button at top of details page
   - Redirects to Create page
   - **Parent is pre-selected** to current disease

3. **Fill Child Details:**
   - Enter child disease information
   - Parent is already set
   - Click Create or Create & Add Child

## Visual Display Examples

### Index Page Hierarchy
**Now Displays:**
```
Name                          | Code    | Export Code | Notifiable | Active
------------------------------|---------|-------------|------------|--------
Campylobacter                 | CAMP    | A04         | ?? Yes     | ? Active
— Campylobacter jejuni        | CAMP-J  | A04.0       | ?? Yes     | ? Active
Salmonella                    | SAL     | A02         | ?? Yes     | ? Active
— Salmonella Typhimurium      | SAL-TYP | A02.0       | ?? Yes     | ? Active
—— Salmonella Typhimurium 9   | SAL-TYP-9| A02.0.9    | ?? Yes     | ? Active
```

### Dropdown Display
**Parent Disease Dropdown:**
```
???????????????????????????????
? -- Root Level Disease --    ?
? Campylobacter               ?
? — Campylobacter jejuni      ?
? Salmonella                  ?
? — Salmonella Typhimurium    ?
???????????????????????????????
```

### Case Disease Dropdown (with Select2)
When creating/editing a case:
```
Type: "salm"
Results:
  Salmonella
  — Salmonella Typhimurium
  —— Salmonella Typhimurium 9
```

## Button Actions

### "Create" Button (Blue)
- Saves disease
- Redirects to Index page
- **Use when:** Done adding diseases

### "Create & Add Child" Button (Green)
- Saves disease
- Redirects back to Create page
- Pre-selects current disease as parent
- **Use when:** Want to add child immediately

### "Add Child Disease" Button (on Details page)
- Opens Create page
- Pre-selects current disease as parent
- **Use when:** Viewing a disease and want to add sub-type

## Example Usage Scenarios

### Scenario 1: Building Complete Hierarchy
1. Click "Create New" for Diseases
2. Enter "Salmonella"
3. Click **"Create & Add Child"** ?
4. Page reloads with "Salmonella" selected as parent
5. Enter "Salmonella Typhimurium"
6. Click **"Create & Add Child"** ?
7. Page reloads with "Salmonella Typhimurium" selected as parent
8. Enter "Salmonella Typhimurium 9"
9. Click "Create" (done with this branch)

**Result:** Complete 3-level hierarchy created quickly!

### Scenario 2: Adding Child Later
1. Navigate to Disease Details for "Campylobacter"
2. Click **"Add Child Disease"** ?
3. Page opens with "Campylobacter" pre-selected
4. Enter "Campylobacter coli"
5. Click "Create"

### Scenario 3: Adding Multiple Siblings
1. Create "Salmonella Enteritidis" with parent "Salmonella"
2. Click **"Create & Add Child"** (or just "Create")
3. Change name to "Salmonella Paratyphi"
4. Parent is still "Salmonella" ?
5. Click "Create"

## Character Encoding Notes

### Why Em-Dash (—) Works Better

| Character | Unicode | Display | Issues |
|-----------|---------|---------|---------|
| ? (Box Drawing) | U+2514 | ? Shows as "?" | Font-dependent, encoding issues |
| — (Em-Dash) | U+2014 | ? Shows correctly | Universal support, web-safe |
| • (Bullet) | U+2022 | ? Alternative | Less visual hierarchy |
| ? (Box T) | U+251C | ? Similar issues | Font-dependent |

**Em-dash advantages:**
- ? Supported in all browsers
- ? Displays correctly in all fonts
- ? UTF-8 safe
- ? No encoding issues
- ? Clear visual hierarchy

## Files Modified

### View Pages (Razor)
- ? `Pages/Settings/Diseases/Index.cshtml`
- ? `Pages/Settings/Diseases/Create.cshtml`
- ? `Pages/Settings/Diseases/Details.cshtml`

### PageModels (C#)
- ? `Pages/Settings/Diseases/Create.cshtml.cs`
- ? `Pages/Settings/Diseases/Edit.cshtml.cs`
- ? `Pages/Cases/Create.cshtml.cs`
- ? `Pages/Cases/Edit.cshtml.cs`

## Testing Checklist

### Hierarchy Display
1. ? Index page shows "—" instead of "?"
2. ? Parent dropdown shows "—" correctly
3. ? Case disease dropdown shows "—" correctly
4. ? Multiple levels display correctly (—, ——, ———)

### Create & Add Child Button
1. ? Button appears on Create page
2. ? Click saves disease and redirects to Create
3. ? Parent is pre-selected on redirect
4. ? Can add multiple children in sequence
5. ? Regular "Create" button still works

### Add Child from Details
1. ? Button appears on Details page
2. ? Click redirects to Create page
3. ? Parent is pre-selected
4. ? Can create child successfully

## Build Status
? **Build successful**  
? **All hierarchy characters fixed**  
? **Add child buttons working**  
? **Ready to use!**

## Quick Start

### Create a Disease Hierarchy
```
1. Settings ? Diseases ? Create New
2. Enter root disease (e.g., "Salmonella")
3. Click "Create & Add Child" ??
4. Enter child (e.g., "Salmonella Typhimurium")
5. Click "Create & Add Child" again ??
6. Enter grandchild (e.g., "Salmonella Typhimurium 9")
7. Click "Create"
```

### Add Child to Existing Disease
```
1. Settings ? Diseases ? Click disease name
2. Click "Add Child Disease" ??
3. Enter child details
4. Click "Create"
```

## Summary

? **Hierarchy display fixed** - No more "?" characters  
? **"Create & Add Child" button added** - Quick workflow  
? **"Add Child Disease" button on Details** - Easy access  
? **Parent pre-selection working** - Seamless UX  
? **All dropdowns updated** - Consistent display  
? **Build successful** - Ready to use!

The Disease management system now provides an intuitive workflow for building hierarchical disease structures!
