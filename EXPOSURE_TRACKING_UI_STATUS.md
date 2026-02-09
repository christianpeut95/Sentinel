# Exposure Tracking System - UI Implementation Status

## ? FILES CREATED

### Lookup Management - Location Types
1. `Pages/Settings/Lookups/LocationTypes.cshtml` - Index/list page ?
2. `Pages/Settings/Lookups/LocationTypes.cshtml.cs` - Index page model ?
3. `Pages/Settings/Lookups/CreateLocationType.cshtml` - Create page ?
4. `Pages/Settings/Lookups/CreateLocationType.cshtml.cs` - Create page model ?
5. `Pages/Settings/Lookups/EditLocationType.cshtml.cs` - Edit page model ?

### Lookup Management - Event Types
6. `Pages/Settings/Lookups/EventTypes.cshtml` - Index/list page ?
7. `Pages/Settings/Lookups/EventTypes.cshtml.cs` - Index page model ?

## ?? FILES NEEDED (Follow Same Pattern)

### Lookup Management - Remaining Files
- `Pages/Settings/Lookups/EditLocationType.cshtml` - Edit page view
- `Pages/Settings/Lookups/CreateEventType.cshtml` - Create page view
- `Pages/Settings/Lookups/CreateEventType.cshtml.cs` - Create page model
- `Pages/Settings/Lookups/EditEventType.cshtml` - Edit page view  
- `Pages/Settings/Lookups/EditEventType.cshtml.cs` - Edit page model

### Location Management (Main Feature)
- `Pages/Locations/Index.cshtml` - List all locations with search/filter
- `Pages/Locations/Index.cshtml.cs` - Page model with geocoding status display
- `Pages/Locations/Create.cshtml` - Create new location with address geocoding
- `Pages/Locations/Create.cshtml.cs` - Page model with geocoding service integration
- `Pages/Locations/Edit.cshtml` - Edit location, re-geocode option
- `Pages/Locations/Edit.cshtml.cs` - Page model
- `Pages/Locations/Details.cshtml` - View location details, show linked events/exposures
- `Pages/Locations/Details.cshtml.cs` - Page model
- `Pages/Locations/Delete.cshtml` - Delete confirmation
- `Pages/Locations/Delete.cshtml.cs` - Page model with safety checks

### Event Management  
- `Pages/Events/Index.cshtml` - List events with filters
- `Pages/Events/Index.cshtml.cs` - Page model
- `Pages/Events/Create.cshtml` - Create event (select location, dates, attendees)
- `Pages/Events/Create.cshtml.cs` - Page model
- `Pages/Events/Edit.cshtml` - Edit event
- `Pages/Events/Edit.cshtml.cs` - Page model
- `Pages/Events/Details.cshtml` - View event, show linked cases/exposures
- `Pages/Events/Details.cshtml.cs` - Page model
- `Pages/Events/Delete.cshtml` - Delete confirmation
- `Pages/Events/Delete.cshtml.cs` - Page model

### Case Integration - Exposure Tracking
- `Pages/Cases/Exposures/Create.cshtml` - Add exposure to case
- `Pages/Cases/Exposures/Create.cshtml.cs` - Page model (smart form adapts to ExposureType)
- `Pages/Cases/Exposures/Edit.cshtml` - Edit exposure
- `Pages/Cases/Exposures/Edit.cshtml.cs` - Page model
- `Pages/Cases/Exposures/Delete.cshtml.cs` - Delete exposure (inline)

### Partial Views
- `Pages/Cases/_ExposuresTab.cshtml` - Tab content for Case Details page
- `Pages/Cases/_ExposureCard.cshtml` - Display single exposure (reusable)
- `Pages/Locations/_LocationSelector.cshtml` - Location picker with search
- `Pages/Events/_EventSelector.cshtml` - Event picker with filters

### API Endpoints (Optional - for dynamic UI)
- `Pages/API/Locations/Search.cshtml.cs` - JSON endpoint for location autocomplete
- `Pages/API/Events/Search.cshtml.cs` - JSON endpoint for event autocomplete
- `Pages/API/Cases/Search.cshtml.cs` - JSON endpoint for case search (contact tracing)

## ?? FEATURES TO IMPLEMENT

### 1. Location Management
**Key Features:**
- Search/filter by name, type, organization
- Show geocoding status (Success/Failed/Pending) with icons
- Bulk geocode action for pending addresses
- Map view showing all locations (optional enhancement)
- Show count of linked events and exposures
- Prevent deletion if location has events/exposures

**Geocoding Integration:**
```csharp
// In Create/Edit page models
private readonly IGeocodingService _geocodingService;

// When address changes
if (!string.IsNullOrEmpty(Location.Address))
{
    var (lat, lon, status) = await _geocodingService.GeocodeAddressAsync(Location.Address);
    Location.Latitude = lat;
    Location.Longitude = lon;
    Location.GeocodingStatus = status;
    Location.LastGeocoded = DateTime.UtcNow;
}
```

### 2. Event Management
**Key Features:**
- Search by name, location, date range, event type
- Calendar view showing events (optional)
- Show attendee count and linked case count
- Calculate attack rate: (linked cases / estimated attendees) * 100
- Filter by date range for outbreak investigation
- Link to location details
- Show all cases exposed at this event

### 3. Exposure Tracking on Case Pages
**Key Features:**
- Tabbed interface on Case Details page
- "Add Exposure" button opens modal or new page
- Smart form that changes fields based on ExposureType selection:
  - **Event**: Show event selector, dates auto-fill from event
  - **Location**: Show location selector + free-text option
  - **Contact**: Show case search, contact type dropdown
  - **Travel**: Show country selector, date range
- Status badges (Potential/UnderInvestigation/Confirmed/RuledOut)
- Timeline view of exposures ordered by date
- Quick actions: Edit, Delete, Change Status
- Investigation notes section

**Exposure Form Logic:**
```javascript
// exposure-form.js
document.getElementById('exposureType').addEventListener('change', function() {
    const type = this.value;
    hideAllFields();
    
    if (type === '1') { // Event
        showEventFields();
    } else if (type === '2') { // Location
        showLocationFields();
    } else if (type === '3') { // Contact
        showContactFields();
    } else if (type === '4') { // Travel
        showTravelFields();
    }
});
```

### 4. Outbreak Investigation Features
**Dashboard Page** (`Pages/Outbreaks/Dashboard.cshtml`):
- Cluster detection: Events with 2+ cases
- Location hotspots: Locations mentioned in multiple cases
- Contact network graph (cases linked via RelatedCaseId)
- Date range filter
- Disease filter
- Export to CSV/Excel

**Common Exposures Query:**
```csharp
// Find locations visited by multiple cases in investigation
var commonLocations = await _context.ExposureEvents
    .Where(e => caseIds.Contains(e.CaseId) && e.LocationId != null)
    .GroupBy(e => e.LocationId)
    .Select(g => new {
        Location = g.Key,
        CaseCount = g.Select(e => e.CaseId).Distinct().Count(),
        Cases = g.Select(e => e.CaseId).Distinct().ToList()
    })
    .Where(x => x.CaseCount >= 2)
    .OrderByDescending(x => x.CaseCount)
    .ToListAsync();
```

## ?? SEED DATA

Create `Migrations/ManualScripts/SeedExposureTrackingData.sql`:

```sql
-- Seed LocationTypes
INSERT INTO LocationTypes (Name, Description, IsHighRisk, DisplayOrder, IsActive)
VALUES
('Healthcare Facility', 'Hospitals, clinics, aged care facilities', 1, 1, 1),
('Education', 'Schools, universities, childcare centers', 0, 2, 1),
('Retail', 'Shopping centers, stores, markets', 0, 3, 1),
('Hospitality', 'Restaurants, cafes, hotels, bars', 0, 4, 1),
('Residential', 'Private homes, apartments, residential care', 0, 5, 1),
('Public Space', 'Parks, beaches, playgrounds, streets', 0, 6, 1),
('Transport', 'Airports, trains, buses, taxis, flights', 0, 7, 1),
('Religious', 'Churches, mosques, temples, places of worship', 0, 8, 1),
('Workplace', 'Offices, factories, construction sites', 0, 9, 1),
('Other', 'Other location types', 0, 10, 1);

-- Seed EventTypes
INSERT INTO EventTypes (Name, Description, DisplayOrder, IsActive)
VALUES
('Party', 'Birthday parties, celebrations', 1, 1),
('Wedding', 'Wedding ceremonies and receptions', 2, 1),
('Funeral', 'Funeral services and wakes', 3, 1),
('Conference', 'Conferences, seminars, workshops', 4, 1),
('Concert', 'Concerts, performances, shows', 5, 1),
('Festival', 'Community festivals, fairs', 6, 1),
('Religious Service', 'Religious services and gatherings', 7, 1),
('Sports Event', 'Sports games, matches, tournaments', 8, 1),
('School Event', 'School assemblies, camps, events', 9, 1),
('Meeting', 'Meetings, gatherings, social events', 10, 1),
('Other', 'Other event types', 11, 1);
```

## ?? UI PATTERNS TO FOLLOW

### Standard Page Layout
```razor
@page
@model YourModel

@{
    ViewData["Title"] = "Page Title";
}

<!-- Breadcrumb -->
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-page="/Index">Home</a></li>
        <li class="breadcrumb-item"><a asp-page="/ParentPage">Parent</a></li>
        <li class="breadcrumb-item active">Current Page</li>
    </ol>
</nav>

<!-- Page Header -->
<div class="d-flex justify-content-between align-items-center mb-3">
    <h2><i class="bi bi-icon me-2"></i>Page Title</h2>
    <div>
        <!-- Action buttons -->
    </div>
</div>

<!-- Success/Error Messages -->
@if (TempData["SuccessMessage"] != null) { ... }
@if (TempData["ErrorMessage"] != null) { ... }

<!-- Main Content Card -->
<div class="card shadow-sm">
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

### Icon Usage
- Locations: `bi-geo-alt-fill`, `bi-geo-alt`, `bi-map`
- Events: `bi-calendar-event`, `bi-calendar3-event`
- Exposures: `bi-diagram-3`, `bi-share`
- High Risk: `bi-exclamation-triangle` with `text-warning`
- Success: `bi-check-circle` with `text-success`
- Error: `bi-exclamation-triangle` with `text-danger`
- Info: `bi-info-circle` with `text-info`

### Color Coding
- **Success/Active**: `badge bg-success`, `text-success`
- **Danger/Inactive**: `badge bg-secondary`, `text-muted`
- **Warning/High Risk**: `badge bg-warning text-dark`
- **Info/Counts**: `badge bg-info`
- **Status - Potential**: `badge bg-secondary`
- **Status - Under Investigation**: `badge bg-warning text-dark`
- **Status - Confirmed**: `badge bg-danger`
- **Status - Ruled Out**: `badge bg-success`

## ?? NAVIGATION UPDATES NEEDED

### Settings Index Page
Add to `Pages/Settings/Index.cshtml`:

```razor
<div class="col-md-6 col-lg-4 mb-4">
    <div class="card h-100 shadow-sm">
        <div class="card-body">
            <h5 class="card-title">
                <i class="bi bi-geo-alt-fill text-primary me-2"></i>
                Exposure Tracking
            </h5>
            <p class="card-text text-muted">
                Manage locations, events, and exposure types for outbreak investigation.
            </p>
            <div class="list-group list-group-flush">
                <a asp-page="/Locations/Index" class="list-group-item list-group-item-action">
                    <i class="bi bi-geo-alt me-2"></i>Locations
                </a>
                <a asp-page="/Events/Index" class="list-group-item list-group-item-action">
                    <i class="bi bi-calendar-event me-2"></i>Events
                </a>
                <a asp-page="/Settings/Lookups/LocationTypes" class="list-group-item list-group-item-action">
                    <i class="bi bi-tags me-2"></i>Location Types
                </a>
                <a asp-page="/Settings/Lookups/EventTypes" class="list-group-item list-group-item-action">
                    <i class="bi bi-tags me-2"></i>Event Types
                </a>
            </div>
        </div>
    </div>
</div>
```

### Main Navigation
Add to `Pages/Shared/_Layout.cshtml` navigation:

```html
<li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
        <i class="bi bi-diagram-3 me-1"></i>Investigation
    </a>
    <ul class="dropdown-menu">
        <li><a class="dropdown-item" asp-page="/Locations/Index">
            <i class="bi bi-geo-alt me-2"></i>Locations
        </a></li>
        <li><a class="dropdown-item" asp-page="/Events/Index">
            <i class="bi bi-calendar-event me-2"></i>Events
        </a></li>
        <li><hr class="dropdown-divider"></li>
        <li><a class="dropdown-item" asp-page="/Outbreaks/Dashboard">
            <i class="bi bi-exclamation-triangle me-2"></i>Outbreak Dashboard
        </a></li>
    </ul>
</li>
```

## ?? NEXT STEPS

1. **Apply SQL Migration**: Run `AddExposureTrackingSystem.sql` on database
2. **Run Seed Data**: Execute seed script to populate lookups
3. **Complete Lookup Pages**: Finish Edit pages for LocationType and EventType (use Create as template)
4. **Build Location Management**: Create full CRUD for Locations (most important)
5. **Build Event Management**: Create full CRUD for Events
6. **Integrate with Cases**: Add Exposures tab to Case Details page
7. **Create Exposure Forms**: Build smart exposure entry form
8. **Test Geocoding**: Ensure address geocoding works when creating/editing locations
9. **Add Validation**: Ensure proper validation on all forms
10. **Build Outbreak Dashboard**: Create investigation dashboard (optional but powerful)

## ?? TESTING CHECKLIST

- [ ] Create LocationTypes via UI
- [ ] Create EventTypes via UI
- [ ] Create Location with address, verify geocoding
- [ ] Create Event linked to Location
- [ ] Add Event-based exposure to Case
- [ ] Add Location-based exposure to Case
- [ ] Add Contact-based exposure (link two cases)
- [ ] Add Travel-based exposure
- [ ] View Case Details with exposures listed
- [ ] Edit exposure status (Potential ? Confirmed)
- [ ] View Event Details showing all linked cases
- [ ] View Location Details showing events and exposures
- [ ] Search locations by name/type
- [ ] Search events by date range
- [ ] Test deletion: Location with events should fail
- [ ] Test deletion: Event with exposures should fail

## ?? TIPS FOR COMPLETION

1. **Copy-Paste Pattern**: Use the created files as templates. Most pages follow the same structure.
2. **Search & Replace**: When creating similar pages, search/replace model names (LocationType ? EventType).
3. **Geocoding**: The `IGeocodingService` already exists. Just inject and call in Location Create/Edit.
4. **Partial Views**: Create reusable `_ExposuresTab.cshtml` to avoid code duplication.
5. **JavaScript**: Create `exposure-form.js` for smart form field showing/hiding.
6. **Validation**: Use `[Required]`, `[StringLength]` attributes already on models.
7. **Authorization**: Add `[Authorize(Policy = "...")]` to page models as needed.

---

The foundation is built! The models, database, and audit tracking are complete. Now it's UI assembly following these patterns. ??
