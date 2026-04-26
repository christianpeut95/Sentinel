# BUGFIX: Entity Autocomplete Keyboard Navigation

## Problem
When the natural language parser automatically recognizes entities (e.g., typing "Cinema City"), a popup menu appears to link/confirm the entity. This menu had **keyboard navigation bugs**:
1. Arrow keys and Enter didn't work at all (missing methods)
2. Google Places search results were not keyboard-navigable (separate container system)

## Root Causes

### Issue 1: Missing Keyboard Navigation Methods
The `EntityAutocomplete` class was **missing keyboard navigation methods** that `TimelineEntry` was trying to call:
- `navigate(direction)` - Move selection up/down with arrow keys
- `acceptSuggestion()` - Accept selected item with Enter/Tab
- `updateSelection()` - Visual highlight for keyboard-selected item

### Issue 2: Focus Management Problem
When the autocomplete popup opens for a **Location** entity, it displays a Google Places search input that **auto-focuses**. This means:
- Textarea loses focus
- Textarea's keydown handlers don't fire
- Arrow keys and Enter have no effect

### Issue 3: Dual Container Architecture (CRITICAL)
Google Places search results were rendered in a **separate container** with a **separate data structure**:

```
.entity-autocomplete dropdown
  ├─ Search input
  ├─ .autocomplete-search-results ← Google Places (data-place-index, NOT in this.suggestions[])
  ├─ Recent entities (data-index, IN this.suggestions[])
  └─ Other suggestions (data-index, IN this.suggestions[])
```

**Problem:**
- Regular suggestions → `this.suggestions[]` → `data-index` → ✅ Keyboard works
- Google Places results → **separate array & container** → `data-place-index` → ❌ Keyboard doesn't work

## Solution

### Part 1: Added Missing Methods
Added three keyboard navigation methods to `EntityAutocomplete` class:

**`navigate(direction)`** - Moves `selectedIndex` up/down with wraparound  
**`updateSelection()`** - Highlights selected item and scrolls into view  
**`acceptSuggestion()`** - Links entity to selected record (handles both regular and Google Places results)

### Part 2: Document-Level Keyboard Handler
Instead of relying on textarea focus, added a **document-level keydown handler** that works regardless of focus:

- **Arrow Down** from search input → Blurs search, selects first suggestion
- **Arrow Up/Down** → Navigate all suggestions
- **Arrow Up** at top → Return to search input for editing
- **Enter/Tab** → Accept selected suggestion (ignored if in search input)
- **Escape** → Close dropdown

### Part 3: Unified Container Architecture ⭐ **KEY FIX**
Completely refactored `performGooglePlacesSearch()` to **inject results into `this.suggestions[]`** instead of a separate container:

```javascript
async performGooglePlacesSearch(query) {
    const results = await fetch(...);

    // Remove old Google Places results
    this.suggestions = this.suggestions.filter(s => !s.isGooglePlaceResult);

    // Add new results to TOP of suggestions
    results.forEach(place => {
        this.suggestions.unshift({
            displayText: place.displayName,
            address: place.address,
            recordType: 'Place',
            isGooglePlaceResult: true,  // Flag for identification
            placeData: place,            // Store full place object
            score: 1.0
        });
    });

    // Re-render entire dropdown
    this.updateDropdownContent();
    this.selectedIndex = 0;
    this.updateSelection();
}
```

**Benefits:**
- ✅ All items in single array → Keyboard navigation works everywhere
- ✅ Consistent `data-index` system → No confusion
- ✅ Google Places results appear at top (most relevant)
- ✅ Recent entities still preserved below
- ✅ No duplicate rendering code

### Part 4: Updated acceptSuggestion()
Modified to handle both regular suggestions and Google Places results:

```javascript
acceptSuggestion() {
    const selectedSuggestion = this.suggestions[this.selectedIndex];

    if (selectedSuggestion.isGooglePlaceResult && selectedSuggestion.placeData) {
        // Handle Google Place
        const place = selectedSuggestion.placeData;
        this.currentEntity.placeDetails = {
            address: place.address,
            latitude: place.latitude,
            longitude: place.longitude,
            // ... etc
        };
    } else {
        // Handle regular suggestion (from memory/recent)
        this.currentEntity.linkedRecordId = selectedSuggestion.recordId;
        // ... etc
    }
}
```

## Keyboard Flow

**When autocomplete opens:**
1. Search input is pre-filled with entity text (e.g., "Cinema City")
2. Search input has focus (cursor at end)
3. Initial suggestions shown: Recent entities, "Use quick-add" option

**User types in search input:**
1. After 300ms debounce, `performGooglePlacesSearch()` fires
2. Google Places results fetched
3. Results injected at **top** of `this.suggestions[]`
4. Dropdown re-renders with "Search Results" section at top
5. First result auto-selected

**User can:**
- **↓** Arrow Down → Blur search, select first result
- **↑↓** Arrow keys → Navigate all items (Google Places + Recent + Actions)
- **↑** Arrow Up (at top) → Return to search input
- **Enter/Tab** → Accept selected item
- **Escape** → Close dropdown
- **Type** → Edit search query (triggers new search)

## Files Modified
- `wwwroot/js/timeline/entity-autocomplete.js`
  - Added `navigate()`, `updateSelection()`, `acceptSuggestion()` methods
  - Added document-level keyboard handler in `createDropdown()`
  - Refactored `performGooglePlacesSearch()` to use unified suggestions array
  - Updated `updateDropdownContent()` to remove separate search results container
  - Removed obsolete `selectGooglePlace()` and `hideSearchResults()` methods

## Testing Steps
1. Hot reload or restart debug session
2. Go to Cases/Exposures/NaturalEntry page
3. Type a location name naturally (e.g., "Cinema City")
4. When the autocomplete popup appears:
   - **Search input has focus** - Should see cursor blinking
   - **Type more** → Should see "Search Results" section update
   - Press **↓** → First Google Places result should highlight
   - Press **↑↓** → Selection should move through ALL items (search + recent + actions)
   - Press **Enter** → Should accept selection, link entity, close dropdown
   - Press **Escape** → Should close dropdown

## Related Fixes
This is separate from the manual entity entry fix (`.` trigger → Location → Google Places search) which uses the entity-quick-add.js system with Tribute.js and Tippy.js.

## Architecture Decision
**Why unified container?**
- Simpler code (one rendering path, one data structure)
- Natural keyboard navigation (one selectedIndex for everything)
- Better UX (consistent highlighting, no modal confusion)
- Easier to maintain (no synchronization between two systems)
- Performance (single DOM update, not dual containers)

The alternative (dual navigation with index mapping) would have been more complex and error-prone.
