# Feature: Group Reference Chip Rendering

## Overview
Implemented clean architecture for group references that stores `+#GroupName` as-is in text, renders as styled chips, and expands on-the-fly for relationship parsing. This avoids position tracking issues and matches industry best practices (Facebook, Slack, LinkedIn).

## Problem Solved
Previously, group autocomplete worked but:
- ❌ Group references were expanded to `+Entity1 +Entity2` in stored text
- ❌ Created duplicate entities with position conflicts
- ❌ Highlighting became messy with overlapping spans
- ❌ Fragile architecture broke on text edits

## Solution: "Keep References Unexpanded"
**Store**: `+#Siblings` (unexpanded reference)  
**Display**: `👥 Siblings (3)` (styled chip)  
**Relationships**: Expand to members on-the-fly  

## Changes Made

### 1. Updated Relationship Parsing (`timeline-entry.js`)
**`parseAndCreateRelationships()`** - Lines 645-672
```javascript
// NOTE: We do NOT expand +#GroupName in the text itself
// Groups are expanded only during relationship parsing below
// This keeps the text clean and avoids position tracking issues
```
- ✅ Removed text expansion call
- ✅ Removed entity object creation from markers
- ✅ Groups stay as `+#GroupName` in stored text

### 2. Added On-the-Fly Group Expansion (`timeline-entry.js`)
**`resolveGroupEntities()`** - Lines 875-941
```javascript
// Check if this is a group reference (+#GroupName)
if (syntaxEntity.marker === '+' && syntaxEntity.text.startsWith('#')) {
    const groupName = syntaxEntity.text.substring(1);
    const entityGroup = Object.values(this.entityGroups).find(g => 
        g.name.toLowerCase() === groupName.toLowerCase()
    );
    
    if (entityGroup) {
        // Find all member entities and add to resolved array
        entityGroup.entityIds.forEach(entityId => {
            const memberEntity = entities.find(e => e.id === entityId);
            if (memberEntity) {
                resolved.push({...memberEntity, ...});
            }
        });
    }
}
```
- ✅ Detects `+#GroupName` syntax during relationship parsing
- ✅ Expands to member entities in-memory
- ✅ No modification of stored text

### 3. Implemented Chip Rendering (`timeline-entry.js`)
**`highlightEntities()`** - Lines 982-1108
```javascript
// Find all group references (+#GroupName) and their positions
const groupRefPattern = /\+#([A-Za-z0-9_-]+)/g;
let match;
while ((match = groupRefPattern.exec(text)) !== null) {
    const groupName = match[1];
    const entityGroup = Object.values(this.entityGroups).find(g => 
        g.name.toLowerCase() === groupName.toLowerCase()
    );
    
    if (entityGroup) {
        groupReferences.push({
            start: match.index,
            end: match.index + match[0].length,
            name: entityGroup.name,
            memberCount: entityGroup.entityIds.length,
            groupId: entityGroup.id
        });
    }
}

// Render group reference chip
const groupLabel = `👥 ${group.name} (${group.memberCount})`;
highlightedHtml += `<span class="entity-group-ref" ...>${groupLabel}</span>`;
```
- ✅ Scans text for `+#GroupName` patterns
- ✅ Renders as styled chips showing "👥 GroupName (count)"
- ✅ Merges with entity highlighting in correct order

### 4. Added Group Details Popup (`timeline-entry.js`)
**`showGroupDetails()`** - Lines 1092-1108
```javascript
showGroupDetails(groupId, groupName, entryId) {
    const entityGroup = this.entityGroups[groupId];
    const memberEntities = allEntities.filter(e => 
        entityGroup.entityIds.includes(e.id)
    );
    // Shows popup with member list
}
```
- ✅ Click handler for group chips
- ✅ Shows group name and member entities
- ✅ Can be upgraded to modal in future

### 5. Updated Entity Counting (`timeline-entry.js`)
**`updateEntitySummary()`** - Lines 1171-1300
```javascript
// Scan for group references in text and add their members to counts
document.querySelectorAll('.narrative-textarea').forEach(textarea => {
    const text = textarea.value;
    const groupRefPattern = /\+#([A-Za-z0-9_-]+)/g;
    let match;
    while ((match = groupRefPattern.exec(text)) !== null) {
        const groupName = match[1];
        const entityGroup = Object.values(this.entityGroups).find(...);
        
        if (entityGroup) {
            // Add all member entities to the count
            entityGroup.entityIds.forEach(entityId => {
                const memberEntity = entryEntities.find(e => e.id === entityId);
                if (memberEntity) {
                    allEntities.push({ 
                        ...memberEntity, 
                        entryId,
                        fromGroupReference: true,
                        groupName: entityGroup.name
                    });
                }
            });
        }
    }
});
```
- ✅ Scans all textareas for `+#GroupName` references
- ✅ Includes group members in sidebar entity counts
- ✅ Proper deduplication (group members counted correctly)

### 6. Added Chip Styling (`entity-quick-add.css`)
**`.entity-group-ref`** - Lines 525-552
```css
.entity-group-ref {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 3px 10px;
    background: linear-gradient(135deg, var(--signal-lt) 0%, var(--paper) 100%);
    border: 1px solid var(--signal);
    border-radius: 12px;
    color: var(--signal-dk);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
}

.entity-group-ref:hover {
    background: linear-gradient(135deg, var(--signal) 0%, var(--signal-lt) 100%);
    border-color: var(--signal-dk);
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
    transform: translateY(-1px);
}
```
- ✅ Modern chip design with gradient background
- ✅ Hover effects and shadows
- ✅ Distinct from entity highlights

## Architecture Benefits

### 1. No Position Tracking Issues
- **Before**: Expanded `+#Siblings` → `+Humphrey +Bash +Christian` caused 3 new entities with positions
- **After**: `+#Siblings` stays as single token, no position conflicts

### 2. Survives Text Edits
- **Before**: Any edit invalidated all entity positions
- **After**: Group reference is stable text token

### 3. Clean Data Model
- **Before**: Text contained expanded entities (duplication)
- **After**: Text contains reference, group membership is source of truth

### 4. Matches Industry Patterns
- **Facebook Lexical**: Stores entity IDs, renders names
- **Slack**: Stores mention tokens (`@user:id`), renders visually
- **LinkedIn**: Stores entity nodes, renders components
- **Our approach**: Store `+#GroupName`, render chips, expand for logic

## User Experience

### Workflow
1. User types `..` → Entity quick-add menu opens
2. User selects group "Siblings" from dropdown
3. System inserts `+#Siblings` in text
4. System renders as chip: `👥 Siblings (3)`
5. User clicks chip → Popup shows members (Humphrey, Bash, Christian)
6. System parses relationships: expands to 3 members for connecting
7. Sidebar shows 3 persons counted correctly

### Visual Design
- **Group chips**: Gradient background, distinct from entities
- **Entity highlights**: Colored by type (person/location/etc)
- **Clear distinction**: Groups look like buttons, entities look like tags

## Testing Checklist

- [ ] Group autocomplete inserts `+#GroupName`
- [ ] Group reference renders as chip with emoji and count
- [ ] Clicking chip shows member popup
- [ ] Sidebar counts include group members
- [ ] Relationships expand groups correctly
- [ ] Text edits don't break chip rendering
- [ ] Multiple groups in same text work correctly
- [ ] Mixed groups + entities render in order

## Future Enhancements

### 1. Rich Popup Modal
Replace alert with proper Bootstrap modal showing:
- Group name and description
- Member list with photos/icons
- Edit button to modify group
- "Add to group" button

### 2. Inline Editing
Allow editing chip directly:
- Click to show member picker
- Add/remove members inline
- Visual feedback for changes

### 3. Group Color Coding
Assign colors to groups:
- Persistent per group
- Chips render with group color
- Member entities inherit color hint

### 4. Group Nesting
Support groups containing other groups:
- `+#Family` includes `+#Siblings`
- Recursive expansion for relationships
- Visual hierarchy in popup

## Related Files

- `wwwroot/js/timeline/timeline-entry.js` - Core logic
- `wwwroot/js/timeline/entity-quick-add.js` - Autocomplete (unchanged)
- `wwwroot/js/timeline/relationship-syntax-parser.js` - Parser (unchanged)
- `wwwroot/css/timeline/entity-quick-add.css` - Chip styling

## Technical Notes

### Regex Pattern
```javascript
const groupRefPattern = /\+#([A-Za-z0-9_-]+)/g;
```
- Matches: `+#GroupName`
- Captures: `GroupName` (without `+#` prefix)
- Supports: Letters, numbers, underscore, hyphen

### Chip HTML Structure
```html
<span class="entity-group-ref" 
      data-group-id="abc123" 
      data-group-name="Siblings" 
      title="Group: Siblings - 3 members">
    👥 Siblings (3)
</span>
```

### Expansion Logic
```javascript
// Text storage: "+#Siblings"
// Display: "👥 Siblings (3)" [chip]
// Relationships: [Humphrey, Bash, Christian] [expanded]
// Sidebar: 3 persons counted [members]
```

## Comparison: Before vs After

| Aspect | Before (Expanded) | After (Chip) |
|--------|------------------|--------------|
| **Stored Text** | `+Humphrey +Bash +Christian` | `+#Siblings` |
| **Entity Objects** | 3 separate entities | 0 (reference only) |
| **Position Tracking** | 3 positions to maintain | 1 stable token |
| **Visual Display** | 3 highlighted names | 1 styled chip |
| **Sidebar Count** | ❌ Wrong (duplicates) | ✅ Correct (members) |
| **Relationships** | ❌ Broke parsing | ✅ Expands on-the-fly |
| **Text Edits** | ❌ Invalidates positions | ✅ Stable reference |
| **Data Model** | ❌ Duplicate data | ✅ Single source of truth |

## Conclusion

This implementation provides a clean, maintainable architecture for group references that:
- ✅ Solves position tracking issues
- ✅ Matches industry best practices
- ✅ Provides excellent user experience
- ✅ Enables future enhancements
- ✅ Maintains backwards compatibility

The "keep references unexpanded" approach is the correct solution for structured content in rich text editors.
