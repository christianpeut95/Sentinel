# Quick Testing Guide - DateTime Entity Fix

## Pre-Test: Refresh the Page
Since JavaScript files were modified, you need to hard refresh:
- **Windows**: `Ctrl + Shift + R` or `Ctrl + F5`
- **Mac**: `Cmd + Shift + R`

## Test 1: Create a DateTime Entity via Quick-Add
**Steps**:
1. Click into any timeline textarea
2. Type: `..`
3. Select: **🕐 Time** from the menu
4. Click: **🌅 Morning** (or choose "☀️ Afternoon", "🌙 Evening", etc.)

**Expected Result**:
- Text "morning" inserted into textarea
- "morning" highlighted in **pink/magenta** color
- Entity appears in right sidebar under "DateTime" section with 🕐 icon

## Test 2: Check Recent Entities (PRIMARY FIX TEST)
**Steps**:
1. After Test 1, type `..` again anywhere in the textarea
2. Look at the autocomplete menu

**Expected Result**:
- You should see a "Recent:" section
- **"morning"** (or whatever time you added) should appear with 🕐 icon
- Format: `🕐 morning`

**Before Fix**: Time entities would NOT appear here ❌
**After Fix**: Time entities SHOULD appear here ✅

## Test 3: Test Specific Time Entry
**Steps**:
1. Type: `..`
2. Select: **🕐 Time**
3. Enter specific time:
   - Hour: `3`
   - Minute: `30`
   - Select: `PM`
4. Click: **✓ Add**

**Expected Result**:
- Text "3:30 PM" inserted
- "3:30 PM" highlighted in pink
- Appears in sidebar under DateTime section

## Test 4: Test Relationship Syntax
**Steps**:
1. Type: `went to McDonald's @3PM.`
2. Wait for auto-parsing (1-2 seconds)

**Expected Result**:
- "McDonald's" highlighted in green (location)
- "3PM" highlighted in pink (time)
- Right panel shows relationship: John → McDonald's @ 3PM
- Relationship timeline visualization updates

## Test 5: Verify Recent Time Entity
**Steps**:
1. After Test 4, type `..` somewhere else
2. Check autocomplete menu

**Expected Result**:
- "3PM" appears in "Recent:" section with 🕐 icon
- Can click it to insert again

## Debugging if Tests Fail

### If Entity is NOT Highlighted
1. Open browser console (F12)
2. Type: `window.timelineEntry.entryEntities`
3. Check if your DateTime entity exists in the array
4. Verify it has:
   - `entityType: 5`
   - `entityTypeName: "DateTime"`
   - `startPosition` and `endPosition` values

### If Entity Doesn't Appear in Recent Menu
1. Open console
2. Type: `window.timelineEntry.entityQuickAdd.getRecentEntitiesFromSession()`
3. Look for DateTime entities in the output
4. Check if they have `entityTypeName: "DateTime"` (not "Time")

### If Relationship Doesn't Create
1. Check console for errors
2. Type: `window.timelineEntry.entryRelationships`
3. Verify relationships array contains your time relationship

## Expected Console Output

When you type text and it parses entities, you should see:
```
[EntityQuickAdd] Built entity: {entityType: 5, entityTypeName: "DateTime", rawText: "morning", ...}
[TimelineEntry] Refreshing highlights for 1 manual entities
```

When you type `..` after creating a time entity:
```
[EntityQuickAdd] Found 3 recent entities  // Should include your time entity
```

## Color Reference
- **Person**: Blue (#4A90E2)
- **Location**: Green (#50C878)
- **Event**: Purple (#9B59B6)
- **Transport**: Orange (#FF8C42)
- **DateTime**: Pink/Magenta (#e83e8c) ← This is what you're testing!
- **Duration**: Gray (#808080)

## Success Criteria

✅ All 5 tests pass
✅ DateTime entities appear in recent menu
✅ DateTime entities highlight in pink
✅ DateTime entities show in sidebar with 🕐 icon
✅ No console errors

## If Something Doesn't Work

1. **Hard refresh** the page (Ctrl+Shift+R)
2. Clear browser cache if needed
3. Check console for JavaScript errors
4. Verify hot reload applied changes (check file modification times)
5. If using hot reload, try stopping debugging and restarting

## Report Back

Please test and report:
1. Which tests passed ✅
2. Which tests failed ❌
3. Any console errors
4. Screenshots if helpful

The primary test is **Test 2** - that's where the main bug was!
