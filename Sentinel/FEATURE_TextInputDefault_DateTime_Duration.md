# Feature: Text Input Default for DateTime and Duration Entities

## Summary
Updated the DateTime and Duration entity creation forms to display a text input field as the default option with keyboard navigation support. Suggestions are now displayed below the input field instead of being the primary UI.

## Changes Made

### 1. Updated `renderDateTimeForm()` in `entity-quick-add.js`
- **Before**: Displayed preset time options (Morning, Noon, etc.) as clickable menu items with "Custom time..." as the last option
- **After**: 
  - Text input field at the top with placeholder "Type time (e.g., 3pm, 14:30)..."
  - Common time suggestions displayed below under "Common times" section label
  - Removed "Custom time..." option (no longer needed since text input is default)

### 2. Updated `renderDurationForm()` in `entity-quick-add.js`
- **Before**: Displayed preset duration options (30 minutes, 1 hour, etc.) as clickable menu items with "Custom duration..." as the last option
- **After**:
  - Text input field at the top with placeholder "Type duration (e.g., 2 hours, 30min)..."
  - Common duration suggestions displayed below under "Common durations" section label
  - Removed "Custom duration..." option (no longer needed since text input is default)

### 3. Enhanced Enter Key Handler in `attachMenuHandlers()`
Added intelligent Enter key handling for DateTime and Duration inputs:
```javascript
else if (e.key === 'Enter') {
    e.preventDefault();
    const query = searchInput.value.trim();
    
    // Handle DateTime and Duration direct text entry
    if (menuType === 'datetime' && query) {
        // User typed a time (e.g., "3pm", "14:30") and pressed Enter
        this.createEntity({
            type: 'DateTime',
            value: query,
            metadata: {}
        });
        return;
    } else if (menuType === 'duration' && query) {
        // User typed a duration (e.g., "2 hours", "30min") and pressed Enter
        this.createEntity({
            type: 'Duration',
            value: query,
            metadata: {}
        });
        return;
    }
}
```

### 4. Added CSS Styling for Section Labels
Added `.menu-section-label` style in `entity-quick-add.css`:
```css
.menu-section-label {
    padding: 8px 12px 4px;
    font-size: 11px;
    font-weight: 600;
    color: #858585;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    pointer-events: none;
    user-select: none;
}
```

## User Experience Improvements

### Keyboard Navigation
1. **Focus on Text Input**: When the menu opens, focus is automatically placed on the text input field
2. **Arrow Down**: Navigate from text input to first suggestion
3. **Arrow Up/Down**: Navigate through suggestions
4. **Enter Key**: 
   - With text in input: Creates entity with the typed value
   - With suggestion selected: Creates entity with the selected preset
5. **Escape**: Closes the menu and removes the ".." trigger

### Workflow
**Before**:
1. Type `..3pm`
2. See list of presets with "Custom time..." at bottom
3. Click "Custom time..." to get a text input
4. Type actual time
5. Submit

**After**:
1. Type `..3pm`
2. Text input is immediately available with focus
3. Type time (e.g., "3pm", "14:30", "9:00 AM")
4. Press Enter to create entity
5. **OR** Press Arrow Down to select a preset suggestion

### Preformatted Time Examples
The placeholders now provide clear examples:
- **DateTime**: "Type time (e.g., 3pm, 14:30)..."
- **Duration**: "Type duration (e.g., 2 hours, 30min)..."

## Technical Details

### Data Attributes
- Text inputs have `data-menu-type="datetime"` or `data-menu-type="duration"`
- This allows the Enter key handler to distinguish between different entity types

### Suggestions Structure
```html
<div class="entity-menu">
    <input type="text" class="menu-search" placeholder="..." data-menu-type="datetime">
    <div class="menu-items">
        <div class="menu-section-label">Common times</div>
        <!-- Preset suggestions -->
    </div>
</div>
```

## Benefits
1. **Faster Entry**: No extra click needed to access custom text input
2. **Keyboard Friendly**: Fully navigable with keyboard (Enter, Arrow keys, Escape)
3. **Discoverable**: Suggestions are still visible and easy to select
4. **Flexible**: Users can type any format they prefer while seeing common options
5. **Consistent**: Matches the pattern used for Person and Location entities

## Testing Checklist
- [ ] Type `..` and select DateTime - verify text input has focus
- [ ] Type a time and press Enter - verify entity is created
- [ ] Press Arrow Down from input - verify first suggestion is highlighted
- [ ] Press Enter on a suggestion - verify preset time is used
- [ ] Repeat for Duration entities
- [ ] Verify Escape key closes menu
- [ ] Verify text input placeholders are helpful
- [ ] Verify section labels ("Common times", "Common durations") are displayed

## Related Files
- `wwwroot/js/timeline/entity-quick-add.js` - Form rendering and keyboard handlers
- `wwwroot/css/timeline/entity-quick-add.css` - Section label styling

## Date
2026-04-06
