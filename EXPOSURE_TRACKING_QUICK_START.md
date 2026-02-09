# Exposure Tracking - Quick Implementation Guide

## ?? What's Done
? Models, enums, database migration  
? Service layer (IExposureRequirementService)  
? Disease Edit page (Exposure Tracking tab)  
? Disease Create Wizard (Step 5: Exposure)  
? Seed data script for common diseases  

## ?? What to Do Next: Case Forms

### Step 1: Modify Cases/Create.cshtml.cs

Add to the top of the class:
```csharp
private readonly IExposureRequirementService _exposureRequirementService;

// Add to constructor:
IExposureRequirementService exposureRequirementService

// Assign in constructor:
_exposureRequirementService = exposureRequirementService;
```

Add property:
```csharp
public Disease? DiseaseRequirements { get; set; }
public bool ShouldPromptForExposure { get; set; }
```

In OnGetAsync, after loading case:
```csharp
if (Case.DiseaseId.HasValue)
{
    DiseaseRequirements = await _exposureRequirementService
        .GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
    ShouldPromptForExposure = await _exposureRequirementService
        .ShouldPromptForExposureAsync(Case.DiseaseId.Value);
}
```

In OnPostAsync, before _context.SaveChangesAsync():
```csharp
// Validate exposure if required
if (Case.DiseaseId.HasValue)
{
    var requirements = await _exposureRequirementService
        .GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
    
    if (requirements != null && 
        (requirements.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
         requirements.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired))
    {
        var isComplete = await _exposureRequirementService
            .ValidateExposureCompletenessAsync(Case);
        
        if (!isComplete)
        {
            ModelState.AddModelError("", 
                "Exposure data is required for this disease. Please add at least one exposure.");
            // Reload dropdowns and return Page();
        }
    }
}
```

### Step 2: Modify Cases/Create.cshtml

Add after case form fields, before the submit button:
```html
<!-- Exposure Section (Conditional) -->
@if (Model.ShouldPromptForExposure && Model.DiseaseRequirements != null)
{
    <div class="card mb-3 border-warning" id="exposureSection">
        <div class="card-header bg-warning bg-opacity-10">
            <h5 class="mb-0">
                <i class="bi bi-geo-alt me-2"></i>Exposure Information
                @if (Model.DiseaseRequirements.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
                     Model.DiseaseRequirements.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired)
                {
                    <span class="badge bg-danger ms-2">Required</span>
                }
            </h5>
        </div>
        <div class="card-body">
            @if (!string.IsNullOrWhiteSpace(Model.DiseaseRequirements.ExposureGuidanceText))
            {
                <div class="alert alert-info">
                    <i class="bi bi-info-circle me-2"></i>
                    @Model.DiseaseRequirements.ExposureGuidanceText
                </div>
            }
            
            <div class="d-flex justify-content-between align-items-center">
                <p class="mb-0">
                    @if (Model.DiseaseRequirements.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired)
                    {
                        <text>Travel history and exposure locations are required for this disease.</text>
                    }
                    else if (Model.DiseaseRequirements.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion)
                    {
                        <text>Specific exposure location(s) must be documented.</text>
                    }
                    else
                    {
                        <text>Please add exposure information if relevant.</text>
                    }
                </p>
                <a asp-page="/Cases/Exposures/Create" 
                   asp-route-caseId="@Model.Case.Id" 
                   class="btn btn-outline-primary btn-sm">
                    <i class="bi bi-plus-circle me-1"></i>Add Exposure
                </a>
            </div>
        </div>
    </div>
}
```

Add JavaScript at the bottom:
```javascript
<script>
$(document).ready(function() {
    // When disease changes, check if exposure section should show
    $('#Case_DiseaseId').on('change', function() {
        var diseaseId = $(this).val();
        if (diseaseId) {
            // Call an API endpoint to check requirements
            $.get('/api/diseases/' + diseaseId + '/exposure-requirements', function(data) {
                if (data.shouldPrompt) {
                    $('#exposureSection').slideDown();
                } else {
                    $('#exposureSection').slideUp();
                }
            });
        } else {
            $('#exposureSection').slideUp();
        }
    });
});
</script>
```

### Step 3: Create API Endpoint (Optional but recommended)

In Program.cs, add minimal API endpoint:
```csharp
app.MapGet("/api/diseases/{id:guid}/exposure-requirements", 
    async (Guid id, IExposureRequirementService service) =>
{
    var disease = await service.GetRequirementsForDiseaseAsync(id);
    var shouldPrompt = await service.ShouldPromptForExposureAsync(id);
    
    return Results.Json(new 
    { 
        shouldPrompt = shouldPrompt,
        mode = disease?.ExposureTrackingMode.ToString(),
        guidanceText = disease?.ExposureGuidanceText,
        isRequired = disease?.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
                     disease?.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired
    });
});
```

### Step 4: Repeat for Cases/Edit.cshtml

Same changes as Create, but also add:
- Display existing exposures
- Show completeness status
- Quick "Add Exposure" if none exist

## ?? Test Cases

1. **Measles (LocalSpecificRegion):**
   - Create case ? Should show exposure section with REQUIRED badge
   - Try to save without exposure ? Should show validation error
   - Add exposure ? Should save successfully

2. **Malaria (OverseasAcquired):**
   - Create case ? Should emphasize travel history
   - Guidance text should mention country requirement

3. **Influenza (Optional):**
   - Create case ? Should NOT show exposure section automatically
   - Can still manually add exposure via Exposures tab

4. **Ross River (LocallyAcquired with DefaultToResidential):**
   - Create case ? Should show exposure section
   - Check if can pre-populate with patient address

## ?? Checklist

- [ ] Inject IExposureRequirementService into Cases/Create
- [ ] Add DiseaseRequirements property
- [ ] Load requirements in OnGet
- [ ] Add validation in OnPost
- [ ] Add exposure section to Create.cshtml
- [ ] Add JavaScript for dynamic show/hide
- [ ] Test with Measles (required exposure)
- [ ] Test with Influenza (optional exposure)
- [ ] Repeat for Cases/Edit
- [ ] Run seed data script
- [ ] Test end-to-end workflow

## ?? UI Reference

The exposure section should look similar to the existing exposure forms in:
- `Pages/Cases/Exposures/Create.cshtml`
- `Pages/Cases/Exposures/Edit.cshtml`

Key UI elements:
- Warning/Info card border
- Badge for "Required" vs "Optional"
- Guidance text alert box
- "Add Exposure" button linking to full exposure form

## ?? Related Files

**Service:**
- `Services/IExposureRequirementService.cs`
- `Services/ExposureRequirementService.cs`

**Models:**
- `Models/Lookups/Disease.cs` (ExposureTrackingMode property)
- `Models/ExposureEvent.cs` (IsDefaultedFromResidentialAddress)
- `Models/ExposureEnums.cs` (ExposureTrackingMode enum)

**Existing Exposure Pages (for reference):**
- `Pages/Cases/Exposures/Create.cshtml`
- `Pages/Cases/Exposures/Edit.cshtml`

**Configuration:**
- `Program.cs` (Service registration already done)

---

**Quick Start:** Begin with Cases/Create.cshtml.cs, add the service injection and requirements loading.
