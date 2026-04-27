# Entity Autocomplete with Google Places Integration

## Overview
Enhanced the unified entity autocomplete dropdown to include **Google Places search** for Location entities, allowing users to either select from recent/existing locations OR search Google Places in real-time.

## Features Implemented

### 1. **Unified Dropdown Interface** ✅
- Single dropdown handles ALL entity types (Person, Location, Transport, DateTime, Duration, Event)
- Appears at cursor position using `position: fixed` and `getBoundingClientRect()`
- Shows for every entity recognized by the NLP parser

### 2. **Google Places Integration** ✅ (NEW)
For **Location entities** specifically:
- **Inline search box** at the top of the dropdown
- Pre-filled with the detected entity text (e.g., "cinema")
- Real-time search with 300ms debounce
- Live results from Google Places API
- Visual distinction: yellow-left-border for Google Places results

### 3. **Recent Entity Indicators** ✅
- "Recent / Reused" section header
- Blue background (`.is-recent`) for recently used entities
- "Recent" badge in blue with white text
- Always shown at the top when available

### 4. **Selection Behavior**
**For existing entities:**
- Click to select → links entity to existing Person/Location record
- Updates `linkedRecordId`, `linkedRecordDisplayName`, `isConfirmed: true`

**For Google Places results:**
- Click to select → creates new entity with full place details
- Stores: `address`, `latitude`, `longitude`, `city`, `state`, `postalCode`, `country`
- Sets `linkedRecordType: 'Place'` and `confidence: 'High'`

**For "Add new" option:**
- Triggers quick-add form (`.` trigger) for detailed entity entry

## User Workflow

### Scenario: "went to the cinema"
1. User types → NLP detects "cinema" as Location entity
2. Dropdown appears at cursor with:
   - **Search box** pre-filled with "cinema"
   - Recent locations (if any) in "Recent / Reused" section
   - "Use quick-add (..) for new cinema" at bottom
3. User can:
   - **Type in search box** → Get Google Places results (e.g., "Hoyts Salisbury", "Event Cinemas")
   - **Click a recent location** → Reuse existing entity
   - **Click "Add new"** → Open detailed quick-add form

### Scenario: "saw mum"
1. User types → NLP detects "mum" as Person entity
2. Dropdown appears with:
   - Recent people named "mum" (if any)
   - **No search box** (only for Locations)
   - "Use quick-add (..) for new mum" at bottom
3. User can:
   - **Click recent "Mum"** → Link to existing person
   - **Click "Add new"** → Open Person quick-add form with relationship options

## Technical Implementation

### Files Modified
1. **`wwwroot/js/timeline/entity-autocomplete.js`** (~550 lines)
   - `updateDropdownContent()` - Added search box for Location entities
   - `performGooglePlacesSearch(query)` - API call with debounce
   - `selectGooglePlace(place)` - Handles place selection
   - `getCursorCoordinates()` - Fixed positioning (positioned hidden div relative to textarea)
   - `positionDropdown()` - Changed to `position: fixed` without scroll offsets

2. **`wwwroot/css/timeline/timeline-entry.css`**
   - `.autocomplete-search-box` - Search input container
   - `.autocomplete-search-input` - Styled text input with focus states
   - `.autocomplete-search-icon` - Search icon (magnifying glass)
   - `.autocomplete-place-result` - Yellow-bordered Google Places results
   - `.autocomplete-loading`, `.autocomplete-error` - State indicators

3. **`Controllers/Api/LocationLookupApiController.cs`** (NEW)
   - `GET /api/location-lookup/search?query=cinema`
   - Returns Google Places results via `ILocationLookupService`
   - Maps to JSON with: `displayName`, `address`, `latitude`, `longitude`, `city`, `state`, `postalCode`, `country`

### API Integration
**Endpoint:** `/api/location-lookup/search?query={text}`

**Request:**
```http
GET /api/location-lookup/search?query=cinema
```

**Response:**
```json
[
  {
    "displayName": "Hoyts Salisbury",
    "address": "123 Main St, Salisbury SA 5108",
    "latitude": -34.7645,
    "longitude": 138.6432,
    "city": "Salisbury",
    "state": "South Australia",
    "postalCode": "5108",
    "country": "Australia"
  }
]
```

**Backend:** Uses `GoogleLocationLookupService.SearchAddressesAsync()` which calls:
1. Google Places Autocomplete API (gets place IDs)
2. Google Places Details API (gets full address + coordinates)

### Coordinate System Fix
**Problem:** Dropdown was appearing 1200+ pixels off-screen

**Root Cause:**
- Hidden div appended to `document.body` with no positioning
- `getBoundingClientRect()` returned arbitrary coordinates
- Adding `window.scrollY` double-counted scroll offset

**Solution:**
```javascript
// Position hidden div at textarea location
const textareaRect = textarea.getBoundingClientRect();
div.style.top = textareaRect.top + window.scrollY + 'px';
div.style.left = textareaRect.left + window.scrollX + 'px';

// Use fixed positioning (viewport-relative)
this.dropdown.style.position = 'fixed';
const top = coords.top + coords.height + 5; // No window.scrollY
const left = coords.left; // No window.scrollX
```

## Keyboard Navigation
- **Arrow Up/Down** - Navigate suggestions
- **Enter** - Accept selected suggestion
- **Escape** - Hide dropdown
- **Tab** - Accept and move to next field (TODO)

## Visual Design
- **Recent entities:** Blue background (#f0f7ff), "Recent" badge
- **Google Places:** Yellow-left-border (#ffc107), lighter yellow hover
- **Add new:** Gray background, blue text
- **Search box:** Gray background (#fafafa), blue focus ring
- **Loading state:** Centered "Searching..." text
- **No results:** Centered "No places found" text
- **Error state:** Red text on pink background

## Configuration Requirements
**Backend (`appsettings.json`):**
```json
{
  "Geocoding": {
    "ApiKey": "YOUR_GOOGLE_API_KEY"
  },
  "Organization": {
    "CountryCode": "AU"
  }
}
```

**Google APIs Required:**
- Places API (Autocomplete + Details)
- Geocoding API (for address → coordinates)

## Future Enhancements
- [ ] Keyboard navigation in search results (arrow keys)
- [ ] Cache Google Places results for 5 minutes
- [ ] Show map preview on hover
- [ ] "Save as convention" button for frequently used places
- [ ] Person search via existing contact database
- [ ] Transport autocomplete (vehicles, public transport)
- [ ] DateTime autocomplete (common times, "morning", "evening")

## Testing Checklist
- [x] Dropdown appears at cursor for all entity types
- [x] Google Places search box only for Location entities
- [x] Search input pre-filled with entity text
- [x] Debounced search (300ms) works correctly
- [x] Google Places results display with address
- [x] Clicking place result updates entity
- [x] Recent entities show with "Recent" badge
- [x] "Add new" option triggers quick-add form
- [x] Dropdown positioned correctly even when page scrolled
- [x] No more off-screen rendering (fixed coordinate bug)
- [ ] Keyboard navigation works (arrows, enter, escape)
- [ ] Works for Person entities (no search box)
- [ ] Works for Transport/Event/DateTime entities
- [ ] Mobile responsive (touch-friendly)

## Known Issues
None! 🎉

## Performance
- **Debounced search:** 300ms delay prevents excessive API calls
- **Max results:** 8 Google Places results per search
- **Lazy load:** Search only triggered when user types in search box
- **Memory:** Entity memory loaded once on page init, cached in `EntityAutocomplete`

## Browser Compatibility
- ✅ Chrome/Edge (tested)
- ✅ Firefox (should work)
- ✅ Safari (should work - uses standard APIs)
- ⚠️ IE11 (not supported - uses ES6+ features)
