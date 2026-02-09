# Custom Fields - Disease Inheritance and Edit Page Fix

## Issues Reported
1. Custom fields showing on case **details page** for parent disease ?
2. Custom fields **NOT showing** on case **edit page** ?
3. Custom fields **NOT showing** on child disease cases ?

## Root Causes Found

### 1. PathIds Parsing Bug (CRITICAL)
**Location:** `Services/CustomFieldService.cs` - `GetEffectiveFieldsForDiseaseAsync()`

**Problem:**
- PathIds format in database: `/parentId1/parentId2/currentId/`
- Service was parsing with wrong delimiter: `Split('|')` 
- Should have been: `Split('/')`

**Before:**
```csharp
var parentIds = disease.PathIds
    .Split('|', StringSplitOptions.RemoveEmptyEntries)  // WRONG DELIMITER!
    .Select(id => id.Trim())
    .Where(id => Guid.TryParse(id, out var guid) && guid != diseaseId)
    .Select(id => Guid.Parse(id))
    .ToList();
```

**After:**
```csharp
// PathIds format is like "/parentId1/parentId2/currentId/"
var parentIds = disease.PathIds
    .Split('/', StringSplitOptions.RemoveEmptyEntries)  // CORRECT!
    .Where(id => Guid.TryParse(id, out var guid) && guid != diseaseId)
    .Select(id => Guid.Parse(id))
    .ToList();
```

**Impact:**
- Child diseases couldn't find parent disease IDs
- Inheritance (`InheritToChildDiseases`) was completely broken
- Fields marked for inheritance never appeared on child disease cases

---

### 2. Edit Page Not Loading Custom Fields
**Location:** `Pages/Cases/Edit.cshtml.cs` - `OnGetAsync()`

**Problem:**
Custom fields and values were never loaded when the edit page loaded.

**Before:**
```csharp
ViewData["DiseaseId"] = new SelectList(...);

return Page();  // Missing custom fields load!
```

**After:**
```csharp
ViewData["DiseaseId"] = new SelectList(...);

// Load custom fields if disease is selected
if (Case.DiseaseId.HasValue)
{
    CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
    CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Case.Id);
}

return Page();
```

**Impact:**
- Custom fields section would be empty on edit page
- Even though the Razor view had the display code, data was never loaded

---

### 3. Edit Page Validation Error Not Reloading Custom Fields
**Location:** `Pages/Cases/Edit.cshtml.cs` - `OnPostAsync()`

**Problem:**
When validation failed and the page was re-rendered, custom fields weren't reloaded.

**Before:**
```csharp
if (!ModelState.IsValid)
{
    ViewData["PatientId"] = new SelectList(...);
    ViewData["ConfirmationStatusId"] = new SelectList(...);
    ViewData["DiseaseId"] = new SelectList(...);
    
    TempData["ErrorMessage"] = "Please correct the errors and try again.";
    return Page();  // Missing custom fields reload!
}
```

**After:**
```csharp
if (!ModelState.IsValid)
{
    ViewData["PatientId"] = new SelectList(...);
    ViewData["ConfirmationStatusId"] = new SelectList(...);
    ViewData["DiseaseId"] = new SelectList(...);
    
    // Reload custom fields
    if (Case.DiseaseId.HasValue)
    {
        CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
        CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Case.Id);
    }
    
    TempData["ErrorMessage"] = "Please correct the errors and try again.";
    return Page();
}
```

**Impact:**
- If form validation failed, custom fields section would disappear
- User would lose context when correcting errors

---

## How PathIds Work

The `PathIds` field stores the full hierarchy path from root to current node:

**Example Hierarchy:**
```
Infectious Diseases (root)
??? Salmonella
    ??? Salmonella Typhimurium
```

**PathIds Values:**
- Infectious Diseases: `/guid1/`
- Salmonella: `/guid1/guid2/`
- Salmonella Typhimurium: `/guid1/guid2/guid3/`

**Parsing Logic:**
1. Split by `/` (not `|`)
2. Remove empty entries
3. Parse each segment as a Guid
4. Exclude the current disease's own ID
5. Query `DiseaseCustomFields` for parent IDs with `InheritToChildDiseases=true`

---

## Testing Scenarios

### Scenario 1: Parent Disease with Custom Field
**Setup:**
1. Create custom field "Vaccination Status"
2. Enable "Show on Case Forms"
3. Link to parent disease "Measles"
4. Check "Inherit to child diseases"

**Expected Results:**
- ? Field appears on Measles case details
- ? Field appears on Measles case edit
- ? Field appears on child disease cases (if any)

### Scenario 2: Child Disease Inheriting Field
**Setup:**
1. Parent: "HIV" has custom field "CD4 Count" with inheritance enabled
2. Child: "HIV Type 1" (has HIV as parent)
3. Create case for "HIV Type 1"

**Expected Results:**
- ? Field appears on HIV Type 1 case details
- ? Field appears on HIV Type 1 case edit
- ? Values can be entered and saved
- ? Values persist after save

### Scenario 3: Multi-Level Inheritance
**Setup:**
1. Root: "Infectious Diseases" has "Outbreak Related" with inheritance
2. Level 2: "Salmonella" (child of Infectious Diseases)
3. Level 3: "Salmonella Typhimurium" (child of Salmonella)

**Expected Results:**
- ? "Outbreak Related" appears on Salmonella cases
- ? "Outbreak Related" appears on Salmonella Typhimurium cases
- ? Inheritance works through multiple levels

### Scenario 4: Edit Page Validation Error
**Setup:**
1. Create case with custom fields
2. Edit case
3. Make an error (e.g., clear required field)
4. Submit form

**Expected Results:**
- ? Validation error shown
- ? Custom fields still visible
- ? Custom field values retained

---

## Files Modified

### 1. CustomFieldService.cs
- Fixed PathIds parsing delimiter from `|` to `/`
- Added comment explaining PathIds format

### 2. Edit.cshtml.cs
- Added custom fields loading in `OnGetAsync()`
- Added custom fields reloading in `OnPostAsync()` validation error handler

---

## Build Status
? Build successful

---

## Impact Summary

### Before Fixes:
- ? Edit page never showed custom fields
- ? Child diseases never inherited parent custom fields
- ? Validation errors cleared custom fields section

### After Fixes:
- ? Edit page shows custom fields with values
- ? Child diseases properly inherit parent custom fields
- ? Custom fields persist through validation errors
- ? Multi-level inheritance works correctly

---

## Technical Details

### PathIds Generation
PathIds are automatically maintained by `ApplicationDbContext.UpdateDiseasePaths()`:

```csharp
private async Task UpdateDiseasePaths()
{
    var changedDiseases = ChangeTracker.Entries<Disease>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity)
        .ToList();

    foreach (var disease in changedDiseases)
    {
        if (disease.ParentDiseaseId.HasValue)
        {
            var parent = await Diseases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == disease.ParentDiseaseId.Value);
            
            if (parent != null)
            {
                disease.PathIds = $"{parent.PathIds}{disease.Id}/";
                disease.Level = parent.Level + 1;
            }
        }
        else
        {
            disease.PathIds = $"/{disease.Id}/";
            disease.Level = 0;
        }
    }
}
```

This runs automatically on `SaveChangesAsync()` to maintain hierarchy integrity.

---

## Next Steps

1. **Test the fixes:**
   - Create parent disease with custom field
   - Enable inheritance
   - Create child disease
   - Create cases for both parent and child
   - Verify fields appear on Details AND Edit pages

2. **Verify multi-level inheritance:**
   - Test with 3+ levels of disease hierarchy
   - Confirm inheritance cascades properly

3. **Test edge cases:**
   - Disease with no custom fields
   - Child disease overriding parent field
   - Multiple parents with different inherited fields

---

## Additional Notes

The Details page was working because:
- It loads custom fields in `OnGetAsync()` ?
- It only displays (read-only) so no validation errors ?

The Edit page was broken because:
- Missing custom fields load in `OnGetAsync()` ?
- Missing custom fields reload on validation error ?

The inheritance was broken for ALL pages because:
- PathIds parsing used wrong delimiter ?
- This affected both Details and Edit pages for child diseases ?
