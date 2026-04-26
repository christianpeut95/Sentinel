# Feature: Recent Entities in Quick-Add Menu

## Overview
Users can now quickly reuse recently-used entities (like "Grandmother") directly from the ".." menu without having to retype them or fill out forms.

## Implementation

### Menu Structure
When typing "..", users now see:
1. **Recent Entities** (top 3-5 most recently used):
   - 3 most recent people (e.g., 👤 Grandmother)
   - 2 most recent locations (e.g., 📍 Sushi Train Welland Plaza)
   - Styled with `.entity-quick-recent` CSS class (bold, signal color)
   
2. **Separator**: "or choose type" (visual divider)

3. **Entity Type Buttons**: All 6 entity types (Person, Location, Event, Transport, DateTime, Duration)

### User Workflow
1. Type ".." anywhere in the textarea
2. Menu appears showing recent entities at the top
3. **Option A - Reuse Recent**: 
   - Navigate to a recent entity (arrow keys or mouse)
   - Press Enter or click
   - Entity is immediately inserted at cursor position
   - No form required!

4. **Option B - Create New**:
   - Navigate to an entity type
   - Press Enter or click
   - Form appears for creating new entity

### Code Changes

#### wwwroot/js/timeline/entity-quick-add.js

**New Method: `insertRecentEntity(entityData)`** (lines 1426-1497)
- Converts recent entity data to full entity object
- Preserves all entity properties (personId, locationId, coordinates, etc.)
- Removes the ".." trigger
- Inserts entity using existing `insertEntityIntoText()` method
- Closes Tippy popup
- Automatically triggers parser to update highlights

**Enhanced Tribute Menu** (lines 55-100)
- Loads recent entities via `loadRecentEntities()` API call
- Shows top 3 recent people + top 2 recent locations
- Uses icon emojis from `getIconForRecordType()`:
  - 👤 Person
  - 📍 Location
  - 🚌 Transport
  - 📅 Event
- HTML labels with CSS styling for visual distinction
- Separator with "or choose type" text

**Menu Template Updates** (lines 101-112)
- Returns raw HTML for recent items (already contains styling)
- Maintains CSS class wrapping for entity types
- Separator shows "or choose type" message

### Data Flow
1. User types ".." → Tribute triggers
2. `values` callback → `loadRecentEntities()` fetches from API
3. Recent entities from `/api/timeline/memory/{caseId}` combined with entity types
4. Menu rendered with recent entities at top
5. User selects recent entity → `selectTemplate` calls `insertRecentEntity()`
6. Entity inserted → Parser runs → Highlighting updates → Entity panel shows

### CSS Styling
Uses existing CSS from `entity-quick-add.css`:
- `.entity-quick-recent`: Bold, signal color for recent entities
- `.tribute-separator`: Centered, non-interactive divider
- `.entity-quick-type`: Standard styling for entity type options

### API Integration
- **Endpoint**: `/api/timeline/memory/{caseId}`
- **Returns**: Recent entities grouped by type (people, locations, transports, events, datetimes)
- **Sorted**: By usage frequency/recency (API handles sorting)
- **Limited**: Top 3 people, top 2 locations shown in menu

## Benefits

1. **Speed**: One-click insertion of frequently-used entities
2. **Consistency**: Reuses exact same entities (names, IDs, relationships)
3. **No Typing Errors**: Eliminates typos when reusing entities
4. **Keyboard Optimized**: Full keyboard navigation support
5. **Context Aware**: Shows entities used in this specific case

## Example Usage

**Scenario**: Contact tracer interviewing patient about multiple exposures to grandmother

**First Time**:
1. Type: "visited .. "
2. Select "Person" from menu
3. Fill form: Name = "Grandmother", Relationship = "Grandmother"
4. Submit

**Subsequent Times**:
1. Type: "lunch with .. "
2. See "👤 Grandmother" at top of menu
3. Press Enter or click
4. "Grandmother" immediately inserted!
5. Continue typing: "lunch with Grandmother at .."
6. See "📍 Sushi Train Welland Plaza" in menu if used before
7. Select and insert instantly

## Future Enhancements

Possible improvements:
- Allow customizing number of recent entities shown (currently hardcoded 3 + 2)
- Add recent events, transports, datetimes to menu
- Show entity usage count or last-used timestamp
- Filter recent entities by search text
- Pin favorite entities to always show
- Cross-case recent entities (if appropriate for workflow)

## Technical Notes

- Recent entities are case-specific (fetched per `caseId`)
- Entity data includes all necessary fields for immediate insertion
- No additional API calls needed when inserting recent entity
- Parser still runs after insertion to ensure highlighting is correct
- Entity merging logic ensures manually-added entities aren't overwritten by parser
