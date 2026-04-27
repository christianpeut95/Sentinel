# Bug Fix: Location Entity Autocomplete Keyboard Navigation

## Issue Summary
When adding a location entity in the natural language entry interface, the Google Places dropdown had several keyboard navigation issues:

1. **Arrow keys moved cursor instead of navigating**: ArrowUp/ArrowDown moved the text cursor to beginning/end of input instead of navigating results
2. **Input field didn't update during navigation**: Users could see highlights in the dropdown but the input field didn't show which place was selected
3. **Enter key didn't display place name**: When pressing Enter, the place was selected but the name was NOT displayed in the textbox, causing it to be interpreted as a new entity
4. **Couldn't type while navigating**: Users couldn't continue typing or use backspace while navigating through results

## Root Cause
The input field's default behavior for ArrowUp/ArrowDown (moving cursor position) was not being prevented, and updating `input.value` during navigation was triggering the `input` event, causing the search to re-run and clearing the navigation state.

## Changes Made

### JavaScript (`wwwroot/js/timeline/entity-quick-add.js`)

#### 1. Added Navigation State Flag
```javascript
let isNavigating = false; // Flag to prevent input events during navigation
```

This prevents the `input` event handler from treating programmatic value changes as new user searches.

#### 2. Enhanced Input Event Handler
- Now checks `isNavigating` flag before processing
- Clears all highlights when user actually types
- Resets flag after checking

#### 3. Completely Rewrote Arrow Key Navigation
**Critical fixes:**
- Added `e.preventDefault()` AND `e.stopPropagation()` to prevent default cursor movement
- Set `isNavigating = true` before updating `input.value`
- Properly manages highlight class on results
- Saves `lastSearchQuery` only when first pressing ArrowDown
- Restores original search text when pressing ArrowUp from first result
- Added `scrollIntoView` to ensure highlighted items are visible

**ArrowDown behavior:**
1. First press: saves search query, selects first result, updates input
2. Subsequent presses: moves down through results, updates input each time
3. At last result: does nothing (doesn't wrap)

**ArrowUp behavior:**
1. From any result except first: moves up, updates input
2. From first result: removes highlight, restores original search text
3. When no selection: does nothing

#### 4. Simplified Result Item Event Handlers
- Removed all keyboard handlers from individual results (they were conflicting)
- Now only have click handlers
- All keyboard navigation happens from the input field

#### 5. Enhanced Enter Key Handling
```javascript
if (e.key === 'Enter' && results.length > 0) {
    if (selectedIndex >= 0 && places[selectedIndex]) {
        e.preventDefault();
        e.stopPropagation();
        input.value = places[selectedIndex].displayName;
        this.currentState.selectedPlace = places[selectedIndex];
        this.submitEntity();
    }
}
```

### CSS (`wwwroot/css/timeline/entity-quick-add.css`)

Added `.highlight` class styling to visually indicate keyboard selection:
```css
.place-result.highlight {
    background: var(--signal-lt);
    box-shadow: inset 3px 0 0 var(--signal);
}
```

## User Experience Improvements

### Before
1. Type "cinem" in location search → results appear
2. Press ArrowDown → cursor jumps to end of "cinem", results still visible but nothing highlighted
3. Press ArrowDown again → cursor stays at end
4. Press Enter → "cinem" is inserted as text (wrong!)

### After
1. Type "cinem" in location search → results appear
2. Press ArrowDown → input updates to "Cinema City", first result highlighted
3. Press ArrowDown again → input updates to "Cinema Complex", second result highlighted  
4. Press Enter → "Cinema City" (the currently selected place) is inserted with full metadata
5. Can press ArrowUp to go back to original "cinem" search text
6. Can start typing again at any time to refine search

## Technical Details

### Key Fix #1: Prevent Default Cursor Movement
```javascript
if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    if (results.length === 0) return;
    e.preventDefault();
    e.stopPropagation();
    // ... rest of navigation logic
}
```

### Key Fix #2: Prevent Input Event During Navigation
```javascript
// In input event handler:
if (isNavigating) {
    isNavigating = false;
    return; // Don't process as a new search
}

// Before updating input value:
isNavigating = true;
input.value = places[selectedIndex].displayName;
```

### Key Fix #3: Save Search Query at Right Time
```javascript
if (selectedIndex === -1) {
    // First time pressing down - save search query NOW
    lastSearchQuery = input.value;
    selectedIndex = 0;
}
```

### Key Fix #4: Remove Result Item Keyboard Handlers
Previously, keyboard events were handled on both the input AND individual results, causing conflicts. Now all keyboard navigation happens exclusively from the input field's `keydown` handler.

## Testing Recommendations

1. **Basic Navigation**: 
   - Type "cinema"
   - Press ArrowDown
   - Verify: input shows first place name, first result highlighted
   - Press ArrowDown again
   - Verify: input shows second place name, second result highlighted

2. **Enter Key**: 
   - Navigate to a result with arrows
   - Press Enter
   - Verify: place name displays in main textarea with correct metadata

3. **Return to Search**: 
   - Type "cinema", press ArrowDown twice (now at 2nd result)
   - Press ArrowUp twice
   - Verify: input shows original "cinema" search text, no highlights

4. **Continue Typing**: 
   - Type "cine", press ArrowDown (shows "Cinema City")
   - Type additional letter "m"
   - Verify: input now shows "cinem", search re-runs, highlights cleared

5. **Escape**: 
   - Type and navigate
   - Press Escape
   - Verify: results cleared, input cleared

6. **Scroll Into View**:
   - Type search with many results
   - Press ArrowDown multiple times
   - Verify: highlighted item always visible (scrolls automatically)

## Files Changed

- `wwwroot/js/timeline/entity-quick-add.js` - Enhanced keyboard navigation logic with navigation flag and event prevention
- `wwwroot/css/timeline/entity-quick-add.css` - Added `.highlight` class styling

## Related Features

This fix improves the keyboard-optimized natural language entry system for contact tracing workflows, specifically the location entity quick-add feature documented in:
- `DOCS_KeyboardNavigation_EntityQuickAdd.md`
- `DOCS_NaturalLanguageExposureEntry.md`
- `DOCS_EntityAutocomplete_GooglePlaces.md`
