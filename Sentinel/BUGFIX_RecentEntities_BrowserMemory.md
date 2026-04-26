# Bug Fix: Recent Entities Browser Memory + Periodic Auto-Save

## Issues Fixed

### Issue 1: Recent Entity Insertion Not Working
**Problem**: Selecting a recent entity from the ".." menu didn't insert text in the textarea.

**Root Cause**: 
- `getRecentEntitiesFromSession()` method didn't exist - caused runtime error
- `insertRecentEntity()` expected API `EntitySuggestion` structure but received session entity structure
- Data structure mismatch: `displayText` vs `rawText`, `recordId` vs `id`

**Solution**:
- ✅ Implemented `getRecentEntitiesFromSession()` to scan `entryEntities` arrays from browser memory
- ✅ Simplified `insertRecentEntity()` to just insert text (like typing it manually)
- ✅ Parser will then detect and highlight the entity automatically
- ✅ Deduplicates entities by type:rawText key
- ✅ Returns top 5 entities sorted by type priority (Person, Location, Transport, etc.)

### Issue 2: Auto-Save Too Slow / Workflow Mismatch
**Problem**: Database auto-save after every entity insertion caused delays during live patient interviews.

**Root Cause**:
- Per-entity auto-save triggered network call after each insertion/update
- Network latency (100-500ms) interrupted contact tracer's interview flow
- User needs to "focus on recording the history not saving"

**Solution**:
- ✅ Removed auto-save calls from `insertEntityIntoText()` and `updateEntityInText()`
- ✅ Implemented periodic background saves every 30 seconds
- ✅ Added `performQuietSave()` method that saves without blocking UI
- ✅ Enhanced page unload handler to save one final time before leaving
- ✅ Kept subtle "Saved" indicator on button for user feedback
- ✅ Manual "Save Draft" button still available as safety override

## Technical Changes

### `wwwroot/js/timeline/entity-quick-add.js`

**Added: `getRecentEntitiesFromSession()` method**
```javascript
getRecentEntitiesFromSession() {
    // Scans all entryEntities arrays
    // Deduplicates by type:rawText
    // Sorts by type priority
    // Returns top 5 entities
}
```

**Modified: `insertRecentEntity()` method**
- Changed from building full entity object to simple text insertion
- Removes ".." trigger
- Inserts entity rawText at cursor position
- Triggers parser to detect and highlight entity
- Much simpler, more reliable

**Removed: Auto-save calls**
- Line 1338: Removed `this.timelineEntry.autoSave()` after entity insertion
- Line 1407: Removed `this.timelineEntry.autoSave()` after entity update

### `wwwroot/js/timeline/timeline-entry.js`

**Added: Periodic Auto-Save System**
```javascript
startPeriodicAutoSave()    // Called in init() - setInterval every 30s
stopPeriodicAutoSave()     // Cleanup method
performQuietSave()         // Background save without UI interruption
```

**Modified: Constructor**
- Added `periodicSaveInterval` property
- Updated comment for `autoSaveTimer` (deprecated in favor of periodic)

**Modified: `init()` method**
- Calls `startPeriodicAutoSave()` after initialization
- Enhanced `beforeunload` handler to save one final time

**Existing: `autoSave()` method**
- Still exists for backwards compatibility
- No longer called automatically after entity operations
- Can be called manually if needed

## Workflow Improvements

### Before (Problematic)
1. User types ".." → Menu appears
2. User selects recent entity → **BLOCKS** while saving to database (100-500ms)
3. User adds another entity → **BLOCKS** while saving again
4. Repeat for each entity → **Multiple interruptions during interview**

### After (Optimized)
1. User types ".." → Menu appears **instantly** (browser memory search)
2. User selects recent entity → Inserts **immediately** (0ms)
3. User adds more entities → All **instant** (no blocking)
4. System saves in background every 30 seconds → **No interruptions**
5. Final save on page unload → **Data safety**

## Live Interview Benefits

✅ **Zero latency** - Recent entities from browser memory (instant search)
✅ **No interruptions** - Background saves every 30 seconds (non-blocking)
✅ **Data safety** - Periodic saves + page unload save + manual button
✅ **Better UX** - Contact tracer can focus on patient, not UI delays
✅ **Workflow aligned** - Technical implementation matches interview process

## Testing Checklist

- [ ] Type ".." and verify recent entities appear instantly
- [ ] Select recent entity and verify it inserts as text
- [ ] Verify parser detects and highlights the inserted entity
- [ ] Add multiple entities quickly without delays
- [ ] Check "Saved" indicator appears after ~30 seconds
- [ ] Verify manual "Save Draft" button still works
- [ ] Test page unload triggers final save
- [ ] Verify entities persist after browser refresh

## Data Flow

```
User Action (Add Entity)
    ↓
Browser Memory (entryEntities[entryId])  ← Instant
    ↓
[30 Second Timer]
    ↓
Background Save to Database  ← Non-blocking
    ↓
Subtle "Saved" Indicator  ← No workflow interruption
```

## Browser Memory Structure

```javascript
this.timelineEntry.entryEntities = {
    "entry_123": [
        {
            id: "entity_456",
            rawText: "Grandmother",
            entityType: 1,
            entityTypeName: "Person",
            personId: 789,
            confirmed: true,
            startPosition: 45,
            endPosition: 56
        },
        {
            id: "entity_457",
            rawText: "Sushi Train",
            entityType: 2,
            entityTypeName: "Location",
            locationId: 234,
            confirmed: true,
            startPosition: 100,
            endPosition: 111
        }
    ],
    "entry_124": [...]
}
```

## Future Enhancements

- Consider session storage backup for browser crash recovery
- Add visual indicator for "saving in progress" (non-blocking)
- Implement smarter deduplication (fuzzy matching for similar names)
- Track actual entity usage frequency for better sorting
- Configurable auto-save interval (30s default)

## Related Files

- `wwwroot/js/timeline/entity-quick-add.js` - Recent entity menu and insertion
- `wwwroot/js/timeline/timeline-entry.js` - Periodic auto-save orchestration
- `wwwroot/js/timeline/entity-autocomplete.js` - Automatic parser-detected entities (unchanged)
- `Services/EntityMemoryService.cs` - Persistent cross-session storage (future use)
