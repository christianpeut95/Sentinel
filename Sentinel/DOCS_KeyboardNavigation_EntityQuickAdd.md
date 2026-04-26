# Keyboard Navigation - Entity Quick-Add Feature

## Overview

The Entity Quick-Add feature is fully keyboard accessible, allowing users to interact with all forms and controls without using a mouse. This document describes all keyboard shortcuts and navigation patterns.

## Global Keyboard Shortcuts

### In Textarea
- **`.` (dot)** - Trigger the entity type dropdown menu
- **`↑` `↓`** - Navigate entity type options in Tribute.js menu
- **`Enter`** - Select highlighted entity type and open form
- **`Esc`** - Close entity type menu

### In Forms
- **`Esc`** - Close current form and return focus to textarea
- **`Tab`** - Move to next focusable element (inputs, buttons, selects)
- **`Shift+Tab`** - Move to previous focusable element
- **`Enter`** - Submit form (when focused on input with value)

## Form-Specific Keyboard Navigation

### Person Form
**Recent People:**
- `Tab` to navigate to recent person items
- `Enter` or `Space` to select a recent person
- Automatically inserts entity and closes form

**Manual Entry:**
- `Tab` to "Name" input field
- Type person's name
- `Tab` to "Relationship" dropdown
- `↑` `↓` to select relationship
- `Enter` to submit

### Location Form

**Quick Picks (Home/Work/School):**
- `Tab` to convention buttons
- `Enter` or `Space` to select
- Automatically inserts entity and closes form

**Recent Locations:**
- `Tab` to navigate to recent location items
- `Enter` or `Space` to select
- Automatically inserts entity and closes form

**Google Places Search:**
- `Tab` to "Search Google Places" input
- Type location name (minimum 2 characters)
- `↓` - Move focus from search box to first result
- `↑` `↓` - Navigate between place results
- `Enter` or `Space` - Select highlighted place
- **Location is saved as entity immediately upon selection**
- `↑` from first result - Return focus to search input
- `Esc` - Close form

**Manual Entry:**
- `Tab` to "Enter manually" input
- Type location name
- `Enter` or `Tab` to submit button
- `Enter` to submit (will need address confirmation later)

### Transport Form

**Type Selection:**
- `Tab` to transport type buttons (Bus, Train, Car, etc.)
- `Enter` or `Space` to select transport type
- Selected type shows visual feedback

**Details Entry:**
- After selecting type, details input appears
- `Tab` to details input
- Type details (e.g., "Bus 557", "QF123")
- `Enter` to submit

### Date/Time Form

**Quick Picks (Morning/Afternoon/Evening/Night):**
- `Tab` to time period buttons
- `Enter` or `Space` to select
- Automatically inserts entity and closes form

**Specific Time Entry:**
- `Tab` to hour input
- Type hour (1-12)
- `Tab` to minute input
- Type minutes (00-59)
- `Tab` to AM/PM dropdown
- `↑` `↓` to select AM/PM
- `Enter` to submit

**Vague Time Entry:**
- `Tab` to "Vague time" input
- Type approximate time (e.g., "around 3pm", "late morning")
- `Enter` to submit

### Duration Form

**Quick Picks:**
- `Tab` to duration buttons (Quick/Short/Medium/Extended/All day)
- `Enter` or `Space` to select
- Automatically inserts entity and closes form

**Custom Duration:**
- `Tab` to duration value input
- Type number
- `Tab` to unit dropdown
- `↑` `↓` to select unit (minutes/hours/days)
- `Enter` to submit

### Event Form

**Event Type:**
- `Tab` to event type dropdown
- `↑` `↓` to select type (Meeting/Festival/Party/etc.)

**Event Name:**
- `Tab` to name input
- Type event name
- `Enter` to submit

## Focus Management

### Automatic Focus
- When a form opens, focus automatically moves to the first input field
- When Google Places results appear, you can use `↓` to move into the results list
- When a form closes, focus returns to the textarea

### Focus Trap
- `Tab` navigation is contained within the active form
- Pressing `Tab` on the last element cycles back to the first element
- Pressing `Shift+Tab` on the first element cycles to the last element
- This prevents focus from escaping to page elements behind the form

### Visual Focus Indicators
All focusable elements show clear visual feedback:
- **Blue outline** (2px solid) around focused element
- **Background highlight** on hover and focus
- **Selected state** with darker border for chosen options

## Accessibility Features

### ARIA Attributes
- `role="button"` on clickable items (recent items, place results)
- `aria-label` on interactive elements describing their action
- `tabindex="0"` on all keyboard-focusable elements

### Keyboard-Only Operation
Every action that can be performed with a mouse can also be performed with the keyboard:
- ✅ Triggering entity menu (`.`)
- ✅ Selecting entity type (arrows + Enter)
- ✅ Navigating form fields (Tab)
- ✅ Selecting quick picks (Enter/Space)
- ✅ Searching Google Places (type + arrows + Enter)
- ✅ Submitting forms (Enter)
- ✅ Canceling forms (Escape)

## Best Practices for Users

### Efficient Workflows

**Using Quick Picks:**
1. Type `.` to open menu
2. Use `↓` to highlight desired entity type
3. Press `Enter` to open form
4. Press `Tab` once to reach quick pick buttons
5. Press `Enter` to select
6. Entity inserted automatically

**Using Google Places (Keyboard Only):**
1. Type `.` → select "Location" → press `Enter`
2. `Tab` past conventions/recent items to search box
3. Type location name (e.g., "Coles Salisbury")
4. Press `↓` to move into results
5. Press `Enter` on highlighted result
6. **Location saved automatically** - no additional steps needed

**Using Manual Input:**
1. Type `.` → select entity type → press `Enter`
2. `Tab` to input field
3. Type value
4. Press `Enter` to submit

### Common Keyboard Sequences

**Add person with relationship:**
```
. → ↓ (to Person) → Enter → Type "John" → Tab → ↓ (to Friend) → Enter
```

**Add location from Google:**
```
. → ↓↓ (to Location) → Enter → Tab Tab → Type "Coles" → ↓ → Enter
(Location saved immediately)
```

**Add transport:**
```
. → ↓↓↓ (to Transport) → Enter → Tab (to Bus) → Enter → Tab → Type "557" → Enter
```

**Add time quickly:**
```
. → ↓↓↓↓ (to DateTime) → Enter → Tab (to Afternoon) → Enter
```

## Troubleshooting

### Focus Not Moving
- **Issue**: Pressing Tab doesn't move focus
- **Solution**: Form has focus trap enabled - Tab cycles within form only. Use Escape to exit.

### Can't Select Google Place
- **Issue**: Typing in search but can't select result
- **Solution**: After typing, press `↓` to move focus from search box to results, then `Enter` to select.

### Enter Key Not Submitting
- **Issue**: Pressing Enter in search box doesn't submit form
- **Solution**: This is intentional - use `↓` then `Enter` to select a place, or `Tab` to manual input, type name, then `Enter`.

### Visual Focus Not Visible
- **Issue**: Can't see which element is focused
- **Solution**: All elements should have blue outline when focused. Check browser settings or refresh page.

## Technical Implementation

### Key JavaScript Functions

**setupFormKeyboardNavigation(formContainer)**
- Handles Escape key to close form
- Handles Enter key to submit when in input fields
- Implements focus trap with Tab/Shift+Tab
- Adds tabindex to all interactive elements

**setupPlacesSearch(input, form)**
- Adds arrow key navigation to place results
- Adds Enter/Space handlers to select places
- Manages selectedIndex state for result highlighting
- Returns focus to input when pressing Up from first result

**Form Event Handlers**
- All buttons support both click and keydown (Enter/Space)
- Recent items and place results are focusable with tabindex="0"
- ARIA labels provide context for screen reader users

### CSS Focus Styles

```css
/* Standard focus indicator */
.element:focus {
    outline: 2px solid var(--signal);
    outline-offset: 2px;
    border-color: var(--signal);
    background: var(--signal-lt);
}

/* Place result focus with left border */
.place-result:focus {
    outline: none;
    background: var(--signal-lt);
    box-shadow: inset 3px 0 0 var(--signal);
}
```

## Compatibility

- **Chrome/Edge**: Full support ✅
- **Firefox**: Full support ✅
- **Safari**: Full support ✅
- **Screen Readers**: JAWS, NVDA, VoiceOver supported ✅

## Future Enhancements

- **Autocomplete history**: Remember frequently used entities
- **Fuzzy search**: Match partial strings with Fuse.js
- **Voice dictation**: Speak entity values (Phase 3)
- **Custom keyboard shortcuts**: User-configurable hotkeys

## Support

For keyboard navigation issues, verify:
1. JavaScript console for errors
2. Browser focus is on textarea/form
3. No browser extensions blocking keyboard events
4. Focus indicators are visible in browser DevTools

Contact development team if issues persist.
