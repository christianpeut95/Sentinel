# Disease Categories - Quick Implementation Guide

## Issue
1. ? "Manage Categories" button links to non-existent pages
2. ? Disease Create/Edit missing category dropdown

## What's Needed

### DiseaseCategory CRUD Pages (5 files each - .cshtml and .cshtml.cs)
All in `Pages/Settings/DiseaseCategories/`:
1. ? Index - List categories (CREATED)
2. ? Create - Add new category
3. ? Edit - Modify category
4. ? Details - View category with disease count
5. ? Delete - Remove category (check for diseases first)

### Disease Forms Update
Add category dropdown to:
1. ? `Pages/Settings/Diseases/Create.cshtml[.cs]`
2. ? `Pages/Settings/Diseases/Edit.cshtml[.cs]`

## Quick Copy-Paste Solutions

### 1. DiseaseCategory Create Page

**Create.cshtml:**
```razor
@page
@model Surveillance_MVP.Pages.Settings.DiseaseCategories.CreateModel
@{
    ViewData["Title"] = "Create Category";
}

<h2><i class="bi bi-plus-circle me-2"></i>Create Disease Category</h2>
<hr />

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

            <div class="mb-3">
                <label asp-for="Category.Name" class="form-label"></label>
                <input asp-for="Category.Name" class="form-control" />
                <span asp-validation-for="Category.Name" class="text-danger"></span>
            </div>

            <div class="mb-3">
                <label asp-for="Category.ReportingId" class="form-label"></label>
                <input asp-for="Category.ReportingId" class="form-control" />
                <span asp-validation-for="Category.ReportingId" class="text-danger"></span>
                <small class="form-text text-muted">Unique identifier for reporting (e.g., RPT-001)</small>
            </div>

            <div class="mb-3">
                <label asp-for="Category.Description" class="form-label"></label>
                <textarea asp-for="Category.Description" class="form-control" rows="3"></textarea>
                <span asp-validation-for="Category.Description" class="text-danger"></span>
            </div>

            <div class="mb-3">
                <label asp-for="Category.DisplayOrder" class="form-label"></label>
                <input asp-for="Category.DisplayOrder" class="form-control" type="number" />
                <span asp-validation-for="Category.DisplayOrder" class="text-danger"></span>
            </div>

            <div class="mb-3 form-check">
                <input asp-for="Category.IsActive" class="form-check-input" type="checkbox" checked />
                <label asp-for="Category.IsActive" class="form-check-label"></label>
            </div>

            <div class="mt-3">
                <button type="submit" class="btn btn-primary"><i class="bi bi-check-circle me-1"></i>Create</button>
                <a asp-page="Index" class="btn btn-outline-secondary ms-2"><i class="bi bi-arrow-left me-1"></i>Back to List</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

**Create.cshtml.cs:**
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.DiseaseCategories
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public DiseaseCategory Category { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _context.DiseaseCategories.AnyAsync(c => c.Name == Category.Name))
            {
                ModelState.AddModelError("Category.Name", "A category with this name already exists.");
                return Page();
            }

            if (await _context.DiseaseCategories.AnyAsync(c => c.ReportingId == Category.ReportingId))
            {
                ModelState.AddModelError("Category.ReportingId", "A category with this Reporting ID already exists.");
                return Page();
            }

            _context.DiseaseCategories.Add(Category);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Category '{Category.Name}' has been created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
```

### 2. Add Category Dropdown to Disease Create

**In Create.cshtml, add after Description field:**
```razor
<div class="mb-3">
    <label asp-for="Disease.DiseaseCategoryId" class="form-label">Category</label>
    <select asp-for="Disease.DiseaseCategoryId" class="form-select" asp-items="ViewBag.CategoryId">
        <option value="">-- No Category --</option>
    </select>
    <span asp-validation-for="Disease.DiseaseCategoryId" class="text-danger"></span>
    <small class="form-text text-muted">Optional: Organize disease into a category for reporting</small>
</div>
```

**In Create.cshtml.cs, add to OnGetAsync():**
```csharp
ViewData["CategoryId"] = new SelectList(
    await _context.DiseaseCategories
        .Where(c => c.IsActive)
        .OrderBy(c => c.DisplayOrder)
        .ThenBy(c => c.Name)
        .ToListAsync(),
    "Id", "Name");
```

**Also add to OnPostAsync() error handling:**
```csharp
ViewData["CategoryId"] = new SelectList(
    await _context.DiseaseCategories
        .Where(c => c.IsActive)
        .OrderBy(c => c.DisplayOrder)
        .ThenBy(c => c.Name)
        .ToListAsync(),
    "Id", "Name");
```

### 3. Sample Categories to Create

Use these examples:

| Name | Reporting ID | Description | Display Order |
|------|--------------|-------------|---------------|
| STI/BBV | RPT-001 | Sexually Transmitted & Blood Borne Viruses | 1 |
| Food Borne Diseases | RPT-002 | Diseases transmitted through contaminated food | 2 |
| Vaccine Preventable Diseases | RPT-003 | Diseases preventable by vaccination | 3 |
| Respiratory Diseases | RPT-004 | Diseases affecting the respiratory system | 4 |
| Vector Borne Diseases | RPT-005 | Diseases transmitted by vectors (mosquitoes, ticks, etc.) | 5 |
| Zoonotic Diseases | RPT-006 | Diseases transmitted from animals to humans | 6 |

## Build Order

1. ? Index pages created
2. Create the Create pages (copy code above)
3. Create Edit pages (similar to Create, but with pre-populated data)
4. Create Details pages (read-only view)
5. Create Delete pages (with safety checks)
6. Update Disease Create/Edit forms
7. Test the workflow

## Expected Workflow After Completion

1. Settings ? Diseases ? **Manage Categories**
2. Create categories (STI/BBV, Food Borne, etc.)
3. Settings ? Diseases ? **Create New Disease**
4. Select category from dropdown
5. Save disease
6. View diseases grouped by category on Index page

## Files Status

? Index.cshtml
? Index.cshtml.cs
? Create.cshtml (copy code above)
? Create.cshtml.cs (copy code above)
? Edit.cshtml (similar pattern)
? Edit.cshtml.cs (similar pattern)
? Details.cshtml (similar pattern)
? Details.cshtml.cs (similar pattern)
? Delete.cshtml (similar pattern)
? Delete.cshtml.cs (similar pattern)
? Update Disease Create/Edit forms

Everything is ready - just needs the remaining CRUD pages created following the patterns shown!
