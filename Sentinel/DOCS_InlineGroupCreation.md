# Inline Group Creation Feature

## Overview
Automatically create entity groups during timeline entry without any separate UI. Use natural syntax like `#Family(..John ..Mary ..Sue)` to instantly create reusable groups.

## Syntax

### Inline Group Creation
```
#GroupName(..entity1 ..entity2 @entity3)
```

Examples:
- `#Family(..John ..Mary ..Sue)` - Creates "Family" group with 3 people
- `#Siblings(..Tom ..Jerry)` - Creates "Siblings" group with 2 people  
- `#Colleagues(..Dr. Smith ..Nurse Jane)` - Creates "Colleagues" group

### Using Existing Groups
```
@#GroupName
```

Examples:
- `went to hospital with @#Family @2PM.` - Expands to all Family members
- `met @#Colleagues at cafe.` - Expands to all Colleagues

## How It Works

### 1. Detection Phase
When user types `#GroupName(...entities...)`:

```javascript
// Pattern: #GroupName(..entity1 +entity2 ..entity3)
const inlineGroupPattern = /#(\w+)\(([^)]+)\)/g;
```

### 2. Entity Resolution
System parses entities within parentheses:
- Strips operators: `..john` → `john`, `+mary` → `mary`
- Matches against existing entities in current entry
- Uses fuzzy matching (case-insensitive, substring matching)
- Collects entity IDs (prefers `sourceEntityId` for deduplication)

### 3. API Group Creation
POSTs to `/api/timeline/groups`:
```json
{
  "caseId": "guid",
  "name": "Family",
  "entityIds": ["entity_1", "entity_2", "entity_3"]
}
```

### 4. Inline Expansion
Replaces inline syntax with individual entity markers:
```
#Family(..John ..Mary) → @John @Mary
```

### 5. Relationship Parsing
Continues with normal relationship syntax parsing:
```
visited hospital #Family(..John ..Mary) @2PM.
↓
visited hospital @John @Mary @2PM.
↓ (parsed relationships)
- John AT_LOCATION hospital @2PM
- Mary AT_LOCATION hospital @2PM
```

## Implementation Details

### Timeline Entry Flow
```javascript
async parseAndCreateRelationships(entryId, text) {
    // 1. Process inline group creation first
    text = await this.processInlineGroupCreation(text, entryId);
    
    // 2. Expand existing group references
    text = this.expandGroupReferences(text);
    
    // 3. Parse relationships normally
    const parsed = this.syntaxParser.parse(text);
    // ... create relationships
}
```

### Group Creation Logic
```javascript
async processInlineGroupCreation(text, entryId) {
    // Find all #GroupName(...) patterns
    const inlineGroupPattern = /#(\w+)\(([^)]+)\)/g;
    const matches = [...text.matchAll(inlineGroupPattern)];
    
    for (let match of matches) {
        const groupName = match[1];
        const entitiesText = match[2];
        
        // Parse entities within parentheses
        const entityPattern = /(\.\.\w+|[@>]\s*\w+)/g;
        const entityNames = [...entitiesText.matchAll(entityPattern)]
            .map(m => m[0].replace(/^(\.\.|[@>])\s*/, ''));

        // Resolve entity IDs from current entry
        const entityIds = this.resolveEntityIds(entityNames);

        // Create via API
        await fetch('/api/timeline/groups', {
            method: 'POST',
            body: JSON.stringify({ caseId, name: groupName, entityIds })
        });

        // Expand inline
        const expansion = entityNames.map(name => `@${name}`).join(' ');
        text = text.replace(match[0], expansion);
    }
    
    return text;
}
```

### API Endpoints

#### GET `/api/timeline/groups/{caseId}`
Returns all entity groups for a case:
```json
{
  "group_id_1": {
    "id": "group_id_1",
    "name": "Family",
    "entityIds": ["entity_1", "entity_2"],
    "createdDate": "2026-04-22T12:00:00Z"
  }
}
```

#### POST `/api/timeline/groups`
Creates or updates a group:
```json
Request:
{
  "caseId": "guid",
  "name": "Family",
  "entityIds": ["entity_1", "entity_2"]
}

Response:
{
  "id": "new_group_id",
  "name": "Family",
  "entityIds": ["entity_1", "entity_2"],
  "entityCount": 2
}
```

#### DELETE `/api/timeline/groups/{caseId}/{groupId}`
Deletes a group:
```json
Response:
{
  "success": true
}
```

## Data Model

### EntityGroup
```csharp
public class EntityGroup
{
    public string Id { get; set; }           // Unique identifier
    public Guid CaseId { get; set; }         // Case this group belongs to
    public string Name { get; set; }         // Group name (e.g., "Family")
    public List<string> EntityIds { get; set; } // Entity IDs in group
    public DateTime CreatedDate { get; set; }
    public string? Description { get; set; }
}
```

### CaseTimelineData Extension
```csharp
public class CaseTimelineData
{
    // ... existing properties
    
    /// <summary>
    /// Entity groups for quick reference (e.g., #Family, #Colleagues)
    /// </summary>
    public Dictionary<string, EntityGroup> EntityGroups { get; set; } = new();
}
```

## User Experience

### Workflow Example
1. User types: `visited hospital with ..John ..Mary ..Sue @2PM.`
2. System creates entities: John (Person), Mary (Person), Sue (Person), hospital (Location), 2PM (DateTime)
3. User decides to group: edits text to `visited hospital with #Family(..John ..Mary ..Sue) @2PM.`
4. System:
   - Detects `#Family(..John ..Mary ..Sue)`
   - Creates group "Family" with 3 members
   - Expands inline: `visited hospital with +John +Mary +Sue @2PM.`
   - Parses relationships: John/Mary/Sue AT_LOCATION hospital @2PM
5. Later entries: user types `+#Family` → auto-expands to `+John +Mary +Sue`

### Benefits
- **No context switching** - Create groups inline during interview transcription
- **Natural syntax** - Feels like note-taking, not data entry
- **Instant reuse** - Use `+#GroupName` immediately in next entry
- **Self-documenting** - Group name appears in text, making it readable

## Edge Cases

### Duplicate Group Names
If group name already exists, system **updates** the entity list:
```javascript
// First time
#Family(..John ..Mary) → creates Family group

// Later (different entities)
#Family(..Tom ..Sue) → updates Family group (replaces John, Mary with Tom, Sue)
```

### Unresolved Entities
If entity name doesn't match any existing entity:
```javascript
#Family(..Unknown ..John) → only John resolved
// Group created with John only, "Unknown" skipped with console warning
```

### Empty Groups
If no entities resolve:
```javascript
#Family(..Nobody) → no matching entities
// Group creation skipped, console warning, inline syntax left unchanged
```

### Nested Operators
All operators work within parentheses:
```javascript
#Team(..John +Mary @office) → resolves all 3 entities
// + and @ operators stripped during parsing
```

## Console Logging

All operations logged for debugging:

```javascript
[TimelineEntry] Processing inline group creation: #Family(..John ..Mary)
[TimelineEntry] Created group "Family" with 2 entities
[TimelineEntry] Expanded #Family(..John ..Mary) to: +John +Mary
```

Errors also logged:
```javascript
[TimelineEntry] No entities found in group definition: #Empty()
[TimelineEntry] No matching entities found for group: Family
```

## Testing

### Manual Test Cases

1. **Basic Creation**
   ```
   Input: visited hospital #Family(..John ..Mary) @2PM.
   Expected: Group created, expanded to +John +Mary, relationships parsed
   ```

2. **Multiple Groups**
   ```
   Input: met #Colleagues(..Dr. Smith ..Nurse Jane) at #Locations(..clinic ..pharmacy)
   Expected: Both groups created and expanded
   ```

3. **Group Reference**
   ```
   Input: went shopping with +#Family
   Expected: Expands to previously created Family members
   ```

4. **Update Existing Group**
   ```
   Input: #Family(..Tom ..Sue)
   Expected: Family group updated with new members
   ```

5. **Empty Group**
   ```
   Input: #Empty()
   Expected: No group created, syntax remains unchanged
   ```

### API Test Cases

1. **Create Group**
   ```bash
   curl -X POST http://localhost:5000/api/timeline/groups \
     -H "Content-Type: application/json" \
     -d '{"caseId":"guid","name":"Family","entityIds":["e1","e2"]}'
   ```

2. **Get Groups**
   ```bash
   curl http://localhost:5000/api/timeline/groups/{caseId}
   ```

3. **Delete Group**
   ```bash
   curl -X DELETE http://localhost:5000/api/timeline/groups/{caseId}/{groupId}
   ```

## Future Enhancements

### Tribute.js Autocomplete
Show existing groups when user types `#`:
```javascript
// In entity-quick-add.js Tribute config
values: async (text, cb) => {
    if (text.startsWith('#')) {
        // Fetch groups from API
        const groups = await fetch(`/api/timeline/groups/${caseId}`);
        // Show group names in autocomplete menu
    }
}
```

### Group Management UI
Optional sidebar panel for:
- Viewing all groups
- Editing group membership
- Renaming groups
- Deleting groups

### Group Metadata
Add additional fields:
- `Type` (family, colleagues, medical_team, etc.)
- `Color` for visualization
- `Description` text
- `Tags` for categorization

### Group Relationships
Support group-to-entity relationships:
```
#Family AT_LOCATION hospital
// Creates relationships for all Family members to hospital
```

## Files Modified

### JavaScript
- `wwwroot/js/timeline/relationship-syntax-parser.js` - Updated documentation
- `wwwroot/js/timeline/timeline-entry.js` - Added `processInlineGroupCreation()` method
- `wwwroot/js/timeline/timeline-entry.js` - Made `parseAndCreateRelationships()` async

### C# Backend
- `Models/Timeline/EntityGroup.cs` - **NEW** - Group data models
- `Models/Timeline/CaseTimelineData.cs` - Added `EntityGroups` property
- `Controllers/Api/TimelineEntryApiController.cs` - Added 3 new endpoints (GET, POST, DELETE groups)

## Related Documentation
- [DOCS_KeyboardNavigation_EntityQuickAdd.md](DOCS_KeyboardNavigation_EntityQuickAdd.md)
- [DOCS_EntityAutocomplete_GooglePlaces.md](DOCS_EntityAutocomplete_GooglePlaces.md)
- [IMPLEMENTATION_SUMMARY_Relationships.md](IMPLEMENTATION_SUMMARY_Relationships.md)
- [DOCS_NaturalLanguageExposureEntry.md](DOCS_NaturalLanguageExposureEntry.md)
