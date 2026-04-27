# Keyboard-Optimized Entity Quick-Add Documentation

## Overview

The Entity Quick-Add feature has been enhanced to provide a keyboard-optimized workflow that shows recently used entities first when the `..` trigger is typed. This allows power users to quickly insert frequently-used entities without repeatedly selecting entity types or using the mouse.

## User Workflow

### Basic Usage: Recent Entities

1. **Type `..`** in the timeline entry textarea
2. **Recent entities appear** in a dropdown menu:
   - ⭐ Mum (Person)
   - ⭐ work (Location)
   - ⭐ Dr. Smith (Person)
   - ─────────────────
   - ➕ Choose entity type...

3. **Navigate with keyboard**:
   - ↑/↓ Arrow keys to navigate
   - Enter to select
   - Escape to cancel

4. **Selected entity inserted** at cursor position with proper spacing

### Fallback: Choose Entity Type

If no suitable recent entity is available:

1. Press ↓ to "➕ Choose entity type..."
2. Press Enter
3. Entity type menu appears (currently defaults to Person form)
4. Select entity type or use form

### Entity Type Forms with Recent Entities

When an entity type is selected (Person, Location, etc.):

1. **Recent entities shown first**:
   - Top 3 recent people (Person form)
   - Top 2 recent locations (Location form)
   - Click or press Enter/Space to select

2. **OR** enter new entity manually

3. **Full keyboard navigation**:
   - Tab through fields
   - Enter to submit
   - Escape to close
   - Back button (←) to return to type selection

## Technical Implementation

### Files Modified

#### 1. `wwwroot/js/timeline/entity-quick-add.js`

**Tribute.js Configuration (lines 26-104)**:
```javascript
this.tribute = new Tribute({
    trigger: '..',
    values: async (text, cb) => {
        // Load recent entities
        await this.loadRecentEntities();
        
        const items = [];
        
        // Top 3 people + top 2 locations
        const recentPeople = (this.recentEntities.people || []).slice(0, 3);
        const recentLocations = (this.recentEntities.locations || []).slice(0, 2);
        const allRecent = [...recentPeople, ...recentLocations];
        
        // Format with ⭐ emoji
        allRecent.forEach(entity => {
            items.push({
                key: `recent_${entity.recordId}`,
                label: `⭐ ${entity.displayText}`,
                value: 'RecentEntity',
                entityData: entity,
                isRecent: true
            });
        });
        
        // Add separator and "Choose type" option
        items.push({
            key: 'separator',
            label: '─────────────────',
            value: 'separator',
            disabled: true
        });
        
        items.push({
            key: 'choose_type',
            label: '➕ Choose entity type...',
            value: 'ChooseType',
            isTypeChoice: true
        });
        
        cb(items);
    },
    selectTemplate: (item) => {
        setTimeout(() => {
            if (item.original.isRecent) {
                this.insertRecentEntity(item.original.entityData);
            } else if (item.original.isTypeChoice) {
                this.showEntityTypeMenu();
            } else if (item.original.value && !item.original.disabled) {
                this.showEntityForm(item.original.value);
            }
        }, 0);
        return '';
    },
    // ... menuItemTemplate, etc.
});
```

**New Methods**:

1. **`getIconForRecordType(recordType)`** (lines 119-132):
   - Returns emoji icon for entity type
   - Used in menu display
   - Supports: Person (👤), Location (📍), Transport (🚌), Event (📅), DateTime (🕐), Duration (⏱️)

2. **`insertRecentEntity(entityData)`** (lines 134-199):
   - Removes `..` trigger
   - Inserts entity text with proper spacing
   - Positions cursor after entity
   - Triggers parse for entity highlighting
   - Shows success toast
   - Refocuses textarea

3. **`showEntityTypeMenu()`** (lines 201-232):
   - Fallback when "Choose type" selected
   - Currently shows Person form (can be enhanced with type-specific recent browsing)
   - Placeholder for future keyboard shortcut handling (P/L/E/T)

#### 2. `wwwroot/css/timeline/entity-quick-add.css`

**New Styles** (lines 40-61):

```css
/* Recent Entity Styling */
.entity-quick-recent {
    font-size: 14px;
    font-weight: 600;
    color: var(--signal-dk);
}

/* Separator Styling */
.tribute-separator {
    display: block;
    font-size: 12px;
    color: var(--earth);
    text-align: center;
    padding: var(--spacing-1) 0;
    pointer-events: none;
    user-select: none;
}

.tribute-container li:has(.tribute-separator) {
    cursor: default;
    background: transparent !important;
}

/* Choose Type Option */
.entity-quick-choice {
    font-size: 14px;
    font-weight: 500;
    color: var(--signal-dk);
}
```

**Keyboard Focus** (lines 31-34):
```css
.tribute-container li:hover,
.tribute-container li.highlight {
    background: var(--signal-lt);
}
```

### Data Flow

1. **User types `..`** → Tribute.js triggers
2. **`values` function called** → Async loads recent entities from `/api/timeline/memory/{caseId}`
3. **Menu built** → Top 3 people + top 2 locations + separator + "Choose type"
4. **User navigates** → Arrow keys move `.highlight` class (built into Tribute.js)
5. **User selects** → Enter key triggers `selectTemplate`
6. **Branch on item type**:
   - `isRecent` → `insertRecentEntity()` → Insert text, trigger parse, show toast
   - `isTypeChoice` → `showEntityTypeMenu()` → Show entity type form
   - Regular type → `showEntityForm()` → Show detailed form with recent entities

### Entity Memory API

**Endpoint**: `/api/timeline/memory/{caseId}`

**Response Structure**:
```json
{
  "people": [
    {
      "recordId": 123,
      "displayText": "Mum",
      "recordType": "Person",
      "normalizedValue": "Mum",
      "metadata": { "relationship": "family" }
    }
  ],
  "locations": [
    {
      "recordId": 456,
      "displayText": "work",
      "recordType": "Location",
      "normalizedValue": "work",
      "address": "123 Main St"
    }
  ],
  "transports": [...],
  "events": [...],
  "datetimes": [...]
}
```

### Keyboard Shortcuts

**Tribute.js Menu**:
- ↑ / ↓ : Navigate menu items
- Enter: Select highlighted item
- Escape: Close menu

**Entity Forms** (Tippy popover):
- Tab: Cycle through fields
- Shift+Tab: Cycle backwards
- Enter: Submit form (when in input field)
- Escape: Close form
- Buttons: Space or Enter to activate

**Recent Entity Items**:
- Tab: Focus next item
- Enter or Space: Select entity
- Already implemented in `attachFormHandlers()` (lines 736-748)

## Benefits

1. **Faster Data Entry**: No need to select entity type for frequently-used entities
2. **Keyboard-Only Operation**: Complete workflow without mouse
3. **Power User Optimized**: Shows most recent/relevant entities first
4. **Contextual**: Recent entities from the current case only
5. **Fallback Options**: Still easy to add new entities manually

## Future Enhancements

Possible improvements for `showEntityTypeMenu()`:

1. **Type-Specific Recent Browsing**:
   - Show recent people when "P" pressed
   - Show recent locations when "L" pressed
   - Include search/filter input

2. **Keyboard Shortcuts**:
   - `..P` → Person form
   - `..L` → Location form
   - `..E` → Event form
   - `..T` → Transport form

3. **Smart Sorting**:
   - Track entity usage frequency
   - Show most-used entities first
   - Consider recency + frequency score

4. **Cross-Case Recent Entities**:
   - Option to show entities from other cases
   - Filtered by current disease/jurisdiction

## Testing Checklist

- [x] Tribute.js triggers when `..` typed
- [x] Recent entities load async from API
- [x] Menu shows top 3 people + top 2 locations
- [x] Separator line renders correctly
- [x] "Choose entity type" option at bottom
- [x] Arrow keys navigate menu (Tribute.js built-in)
- [x] Enter selects highlighted item
- [x] Escape closes menu
- [x] Recent entity inserts at cursor with proper spacing
- [x] Entity highlighting triggers after insertion
- [x] Toast notification shows success message
- [x] Textarea refocuses after insertion
- [x] Entity type forms show recent entities
- [x] Recent items clickable and keyboard-accessible
- [x] CSS focus states work (`:hover` and `.highlight`)
- [x] Build compiles without errors

## Accessibility

- **Keyboard Navigation**: Full keyboard support, no mouse required
- **Screen Readers**: Menu items have proper ARIA roles (built into Tribute.js)
- **Focus Management**: Focus returns to textarea after operations
- **Visual Feedback**: `.highlight` class provides clear focus indication
- **Touch Support**: Menu items work with touch on mobile (if applicable)

## Related Documentation

- [Entity Autocomplete (Inline Suggestions)](DOCS_EntityAutocomplete_GooglePlaces.md)
- [Natural Language Entry Overview](DOCS_NaturalLanguageExposureEntry.md)
- [Entity Quick-Add Forms](DOCS_KeyboardNavigation_EntityQuickAdd.md)

## Conclusion

The keyboard-optimized entity quick-add feature significantly improves workflow efficiency for contact tracing interviews by:

1. Reducing repetitive type selection for common entities
2. Enabling fast, keyboard-only data entry
3. Providing contextual suggestions from case history
4. Maintaining full accessibility and usability

The implementation leverages Tribute.js's robust keyboard navigation and extends it with recent entity memory from the backend API, creating a seamless power-user experience.
