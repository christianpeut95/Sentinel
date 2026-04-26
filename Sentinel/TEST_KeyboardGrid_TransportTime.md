# Quick Testing Guide - Keyboard Navigation (Transport/Time Forms)

## Pre-Test: Refresh
Hard refresh to load new JavaScript:
- **Windows**: `Ctrl + Shift + R` or `Ctrl + F5`
- **Mac**: `Cmd + Shift + R`

## Test 1: Transport Form Arrow Navigation ⭐ PRIMARY TEST

**Steps**:
1. Type `..` in any timeline textarea
2. Select "🚌 Transport" from menu
3. **Don't touch the mouse!**
4. Notice: 🚌 Bus is already focused (blue outline + glow)
5. Press `→` (right arrow)
6. Expected: Focus moves to 🚆 Train
7. Press `↓` (down arrow)
8. Expected: Focus moves to 🚕 Taxi (below Train)
9. Press `←` (left arrow)
10. Expected: Focus moves to 🚗 Car
11. Press `Enter`
12. Expected: Car selected, details input appears and is focused

**Success Criteria**:
- ✅ Arrow keys move focus
- ✅ Focus visible (blue outline + glow effect)
- ✅ Enter selects without mouse
- ✅ Details input auto-focused after selection
- ✅ Can type details immediately

## Test 2: Time Form Navigation

**Steps**:
1. Type `..`
2. Select "🕐 Time"
3. Grid appears: 🌅 Morning, ☀️ Afternoon, 🌙 Evening, 🌃 Night
4. Press `→` three times
5. Expected: Focus moves Morning → Afternoon → Evening → Night
6. Press `Enter`
7. Expected: "Night" selected and entity created immediately

## Test 3: Home/End Keys

**Steps**:
1. Type `..` → "🚌 Transport"
2. Press `End`
3. Expected: Focus jumps to 🚶 Walk (last button)
4. Press `Home`
5. Expected: Focus jumps to 🚌 Bus (first button)

## Test 4: Vertical Navigation

**Transport Grid Layout**:
```
[🚌 Bus]    [🚆 Train]
[🚗 Car]    [🚕 Taxi]
[✈️ Flight] [🚶 Walk]
```

**Steps**:
1. Start at 🚌 Bus (top-left)
2. Press `↓`
3. Expected: Move to 🚗 Car (directly below)
4. Press `↓` again
5. Expected: Move to ✈️ Flight (directly below)
6. Press `↓` again
7. Expected: Wrap to 🚌 Bus (top of same column)

## Test 5: Tab Key Behavior

**Steps**:
1. Type `..` → "🚌 Transport"
2. Press `Tab` (don't press arrow keys)
3. Expected: Focus exits the grid and moves to "Cancel" button
4. Press `Shift + Tab`
5. Expected: Focus returns to grid, 🚌 Bus focused (first item)

**Why This Matters**: Grid acts as single tab stop (ARIA best practice)

## Test 6: Space Bar Selection

**Steps**:
1. Type `..` → "🚌 Transport"
2. Press `→` twice (now on 🚗 Car)
3. Press `Space` (not Enter)
4. Expected: Car selected, details input appears

## Test 7: Keyboard Hint Visibility

**Steps**:
1. Type `..` → "🚌 Transport"
2. Look at the label above the button grid
3. Expected: "Type: *(Arrow keys to navigate)*" shown in gray italic text

**Purpose**: Visual cue for keyboard-only users

## Test 8: Duration Form (5 buttons)

**Grid Layout**:
```
[⏱️ Quick]    [🕐 Short]    [🕒 Medium]
[🕔 Extended] [🕗 All day]
```

**Steps**:
1. Type `..` → "⏱️ Duration"
2. Press `→` → `→` (now on Medium)
3. Press `↓`
4. Expected: Move to All day (below Medium)
5. Press `←`
6. Expected: Move to Extended (left of All day)

## Test 9: Screen Reader Test (If Available)

**With NVDA or JAWS**:
1. Type `..` → "🚌 Transport"
2. Screen reader should announce:
   - "Type, radiogroup, arrow keys to navigate"
   - "bus, radio button, not checked, 1 of 6"
3. Press `→`
4. Should announce: "train, radio button, not checked, 2 of 6"
5. Press `Enter`
6. Should announce: "train, radio button, checked, 2 of 6"

## Test 10: Focus Visibility (Accessibility)

**Steps**:
1. Type `..` → "🚌 Transport"
2. Press `→` multiple times
3. Watch the focus indicator

**Expected Visual**:
- **Thick blue outline** (3px)
- **Blue glow** around button (box-shadow)
- **Outline offset** (2px gap)
- **High contrast** (easily visible)

**Fail Criteria** (old version):
- Thin outline (2px)
- No glow
- Outline touching border
- Hard to see

## Debugging Tests

### If Arrow Keys Don't Work:

**Check Console**:
```javascript
// Open Console (F12)
// Look for this message:
[GridNav] Grid has 6 items in 2 columns
[GridNav] Keyboard navigation setup complete for .transport-btn
```

**Manual Test**:
```javascript
// In console:
const grid = document.querySelector('.transport-grid');
const buttons = grid.querySelectorAll('.transport-btn');

// Check tabindex pattern
buttons.forEach((btn, i) => {
    console.log(`Button ${i}: tabindex=${btn.tabIndex}`);
});
// Should show: 0, -1, -1, -1, -1, -1
```

### If Focus Not Visible:

**Check CSS Applied**:
```javascript
// In console, focus a button then:
const focused = document.activeElement;
console.log(window.getComputedStyle(focused).outline);
// Should show: "rgb(0, 122, 255) solid 3px" (or similar)
```

### If Grid Layout Wrong:

**Check Column Detection**:
```javascript
// In console:
const grid = document.querySelector('.transport-grid');
const styles = window.getComputedStyle(grid);
console.log(styles.gridTemplateColumns);
// Should show: "1fr 1fr" (2 columns)
```

## Performance Test

**Rapid Navigation**:
1. Type `..` → "🚌 Transport"
2. Rapidly press arrow keys: `→→→←←←↓↓↑↑`
3. Expected: Smooth focus movement, no lag, no errors

**Spam Test**:
1. Hold down `→` key
2. Expected: Focus cycles through buttons smoothly
3. No console errors
4. No memory leaks

## Regression Tests

### Ensure Old Functionality Still Works:

- [ ] Click buttons with mouse still works
- [ ] Tribute `..` menu still appears
- [ ] Entity submission still works
- [ ] Details input still works
- [ ] Cancel button still works
- [ ] Back button (←) still works

## Pass/Fail Criteria

### ✅ PASS if:
- All arrow keys move focus correctly
- Enter/Space select items
- Home/End jump to first/last
- Tab exits grid (single tab stop)
- Focus visible at all times
- No console errors
- Screen reader announces correctly (if tested)

### ❌ FAIL if:
- Arrow keys don't respond
- Focus disappears
- Tab stops on each button (should be one stop)
- Enter/Space don't select
- Console shows JavaScript errors
- Focus indicator not visible

## Expected Behavior Summary

| Action | Result |
|--------|--------|
| Open form | First button focused |
| `→` | Move right |
| `←` | Move left |
| `↓` | Move down one row |
| `↑` | Move up one row |
| `Home` | Jump to first |
| `End` | Jump to last |
| `Enter` | Select and submit |
| `Space` | Select and submit |
| `Tab` | Exit grid |
| `Shift+Tab` | Return to grid |
| `Escape` | Close form |

## Report Back

Please test and report:
1. Which tests passed ✅
2. Which tests failed ❌
3. Browser used
4. Any console errors
5. Screenshots of focus indicators (if helpful)

**Primary test**: Test 1 (Transport Form Arrow Navigation) - this is the core functionality!
