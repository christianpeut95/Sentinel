# ?? EXPOSURE TRACKING - FINAL PUSH

## ? WHAT'S COMPLETE (30 FILES)

- ? LocationTypes CRUD (6 files)
- ? EventTypes CRUD (6 files)
- ? Locations CRUD (10 files) + geocoding
- ? Events CRUD (8 files) + attack rates
- ? Navigation integrated (sidebar + settings)
- ? Build successful
- ? **85% Complete**

---

## ?? FINAL 15% - CASE INTEGRATION

### What Needs to be Done:

Add **Exposures section** to Case Details page, allowing users to:
1. View all exposures for a case
2. Add new exposures (Event/Location/Contact/Travel)
3. Edit exposures
4. Track investigation status
5. See timeline of exposures

### Files to Create/Modify (4-6 files, 2-3 hours):

1. **Modify: `Pages/Cases/Details.cshtml`**
   - Add new card section for "Exposures & Contact Tracing"
   - Show list of exposures with type badges
   - "Add Exposure" button
   - Display exposure timeline

2. **Modify: `Pages/Cases/Details.cshtml.cs`**
   - Load ExposureEvents for the case
   - Add handler for deleting exposures

3. **Create: `Pages/Cases/Exposures/Create.cshtml`**
   - Form to add new exposure
   - Dropdown to select exposure type
   - Dynamic fields based on type:
     - Event: Select event from dropdown
     - Location: Select location or free text
     - Contact: Search for related case
     - Travel: Country selector + dates
   - Exposure dates
   - Investigation status
   - Notes

4. **Create: `Pages/Cases/Exposures/Create.cshtml.cs`**
   - Handle form submission
   - Validation
   - Create ExposureEvent record

5. **Create: `Pages/Cases/Exposures/Edit.cshtml + .cs`** (Optional but recommended)
   - Similar to Create
   - Pre-populate fields

---

## ?? IMPLEMENTATION PLAN

### Step 1: Modify Case Details Page (30 min)

Add Exposures section after Lab Results:

```html
<!-- Exposures & Contact Tracing -->
<div class="card shadow-sm mb-3">
    <div class="card-header bg-light fw-semibold d-flex justify-content-between">
        <span><i class="bi bi-diagram-3 me-2"></i>Exposures (@Model.Exposures.Count)</span>
        <a asp-page="/Cases/Exposures/Create" asp-route-caseId="@Model.Case.Id" class="btn btn-sm btn-success">
            <i class="bi bi-plus-circle"></i> Add Exposure
        </a>
    </div>
    <div class="card-body">
        @if (Model.Exposures.Any())
        {
            <!-- Timeline of exposures -->
        }
        else
        {
            <div class="text-center py-4">
                <i class="bi bi-diagram-3 fs-1 text-muted"></i>
                <p class="text-muted">No exposures recorded.</p>
            </div>
        }
    </div>
</div>
```

### Step 2: Update Page Model (15 min)

```csharp
public IList<ExposureEvent> Exposures { get; set; } = default!;

// In OnGetAsync:
Exposures = await _context.ExposureEvents
    .Include(e => e.Event).ThenInclude(e => e.Location)
    .Include(e => e.Location)
    .Include(e => e.RelatedCase).ThenInclude(c => c.Patient)
    .Include(e => e.EventType)
    .Where(e => e.CaseId == id)
    .OrderByDescending(e => e.ExposureStartDate)
    .ToListAsync();
```

### Step 3: Create Exposure Form (1 hour)

Smart form with JavaScript to show/hide fields based on exposure type selected.

### Step 4: Test End-to-End (30 min)

---

## ?? DELIVERABLES

When complete, users will be able to:

1. ? **View exposures** on case details page
2. ? **Add exposures** via smart form
3. ? **Track investigation status** (Unknown ? Potential ? Investigating ? Confirmed/Ruled Out)
4. ? **Link to events** and see all cases at same event
5. ? **Link to locations** for location-based exposures
6. ? **Link to other cases** for contact tracing
7. ? **Record travel** with countries and dates
8. ? **Timeline view** of all exposures
9. ? **Calculate attack rates** for events
10. ? **Full audit trail** of all changes

---

## ?? CURRENT STATUS

**Locations & Events:** ? 100% Complete  
**Case Integration:** ? 0% Complete  
**Overall System:** ? 85% Complete

**Ready to finish the last 15%!** ??
