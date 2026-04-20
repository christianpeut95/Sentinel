# Natural Language Exposure Entry - Feature Documentation

## Overview

The Natural Language Exposure Entry feature allows contact tracers to enter exposure timeline data using natural language instead of traditional forms. This significantly speeds up data entry during phone interviews with patients.

## Access the Feature

1. Navigate to a Case Details page
2. Click "Exposures" tab
3. Click "Natural Entry" button (or navigate to `/Cases/Exposures/NaturalEntry?caseId={guid}`)

Alternatively, you can use the traditional form-based entry by clicking "Traditional Form".

## How to Use

### Basic Entry

1. **Click "Add Day"** to create a new timeline entry for a specific date
2. **Select the date** using the date picker
3. **Type naturally** in the text area, as if taking notes during a phone call

**Example:**
```
went to work with mum around 3pm, then took the 557 bus to Coles Salisbury
```

### Entity Recognition

As you type, the system automatically detects and highlights:

- **People** (blue underline): "mum", "John", "sister"
- **Locations** (green underline): "Coles", "work", "school"
- **Transport** (orange underline): "bus 557", "train", "car"
- **Events** (purple underline): "festival", "wedding", "concert"
- **Times** (pink underline): "3pm", "morning", "around 2:30"
- **Durations** (gray underline): "all afternoon", "quick visit"

### Convention Names

Use shorthand for familiar places:
- **"work"** → Links to patient's employer
- **"home"** → Patient's residential address
- **"school"** → Patient's or their children's school
- **"gym", "church", "shops", "cinema", "pool", "park"** → Prompts for specific location details

### Autocomplete

When you type a recognized entity, an autocomplete dropdown appears with:
- **Previously mentioned people/locations** from this case (highest priority)
- **Google Places suggestions** for locations (medium priority)
- **"+ Add new"** option to create a new entity

**Keyboard shortcuts:**
- `↑` `↓` - Navigate suggestions
- `Tab` or `Enter` - Accept suggestion
- `Esc` - Close dropdown
- Keep typing to ignore and continue

### Uncertainty & Notes

The system automatically detects:
- **Uncertainty**: "I think", "maybe", "around", "not sure"
- **Corrections**: "actually", "wait no", "correction"
- **Protective measures**: "wearing mask", "outdoors", "social distancing"
- **Memory gaps**: "can't remember", "don't recall"

These are flagged for follow-up during review.

### Multiple Days

- **Add Day** - Creates a new date block
- **Copy Previous Day** - Duplicates the last entry for recurring activities

### Saving

- **Save Draft** - Saves your work without review (can return later)
- **Save & Review** - Saves and navigates to review mode for classification

## Review Mode (Future Phase)

After saving, you'll enter Review Mode where you can:
- Complete missing details (full names, phone numbers)
- Classify contacts (Close/Casual/Household)
- Confirm locations with addresses
- Add risk assessments
- Generate traditional `ExposureEvent` records

## Map Visualization

The right panel shows:
- **Entity Summary** - Count of people, locations detected
- **Map** - Pins for confirmed locations with coordinates
- Status badges (✓ Complete, ⚠️ Partial, ❌ Needs follow-up)

## Complex Scenarios

### Chain Visits
```
went to 5 different shops at Westfield - Coles, Kmart, Chemist Warehouse, Target, and H&M
```
→ Creates multiple location entities grouped under "Westfield"

### Group Variations
```
Thursday: with the friends' three kids
Friday: went to the pool with only the oldest 2
```
→ System tracks subset relationships

### Multi-Stop Journeys
```
on the way to the shops we stopped at mum's place
```
→ Creates waypoint: Home → Mum's Place → Shops

## Data Storage

- **Prototype**: JSON files in `wwwroot/data/timeline-entries/`
- **Format**: `{CaseId}_timeline.json`
- **Backups**: Auto-created on save in `wwwroot/data/timeline-backups/`

**Future**: Migration to database tables for production use.

## Technical Architecture

### Backend Services

- **`NaturalLanguageParserService`** - Regex-based entity extraction
- **`TimelineStorageService`** - JSON file management
- **`EntityMemoryService`** - Autocomplete memory with caching

### API Endpoints

- `POST /api/timeline/parse` - Parse narrative text
- `GET /api/timeline/{caseId}` - Load timeline
- `POST /api/timeline/save` - Save timeline
- `GET /api/timeline/memory/{caseId}` - Get autocomplete data
- `POST /api/timeline/copy-day` - Duplicate day entry
- `POST /api/timeline/convention` - Add location shortcut
- `DELETE /api/timeline/{caseId}` - Delete timeline

### Frontend Components

- **`timeline-entry.js`** - Main orchestrator
- **`entity-autocomplete.js`** - VSCode-style dropdown
- **`map-visualization.js`** - Leaflet map integration
- **`timeline-entry.css`** - UI styling

## Known Limitations (Prototype)

- ✓ **Google Places API** integrated via existing `/api/address-suggest` endpoint
- ⚠️ **Single-user editing** - No concurrent write protection
- ⚠️ **Manual backup** - No automated archiving
- ⚠️ **100 entry limit** per case (file size management)
- ⚠️ **English only** - Non-English languages not yet supported

## Future Enhancements

### Phase 2 (Database Migration)
- Migrate JSON storage to SQL tables
- Add versioning and audit trails
- Concurrent editing support

### Phase 3 (Advanced Features)
- ML-based entity extraction (improve accuracy)
- Multi-language support
- Voice dictation mode
- Cross-case clustering (outbreak detection)
- Mobile/tablet optimized UI

## Troubleshooting

### Entities not being highlighted
- **Check**: Are you typing in the textarea? Highlighting appears after 300ms delay
- **Try**: Type at least 3 characters for most entities

### Autocomplete not appearing
- **Check**: Entity must be recognized first (look for underline)
- **Try**: Press `Tab` to manually trigger

### Map not showing locations
- **Cause**: Locations need lat/long coordinates
- **Fix**: Confirm locations via Google Places autocomplete

### Save button disabled
- **Check**: User has `Permission.Exposure.Create` permission
- **Try**: Contact system administrator for permission

## Example Workflow

**Phone call scenario:**
```
Operator: "Can you tell me where you've been in the last week?"

Patient: "On Monday I went to work with my husband, then we picked up our kids from school around 3pm. Tuesday I can't really remember. Wednesday I went to Coles Salisbury with mum, we were wearing masks. Thursday was just at home sick. Friday I went to the doctor at Salisbury Medical Centre."

Operator types:
---
Monday: went to work with my husband, picked up kids from school around 3pm

Tuesday: can't remember

Wednesday: went to Coles Salisbury with mum, wearing masks

Thursday: at home sick

Friday: went to doctor at Salisbury Medical Centre
---
```

**System detects:**
- People: husband (link to contact), mum, kids
- Locations: work (convention → ABC Hospital), school, Coles Salisbury, home, Salisbury Medical Centre
- Times: 3pm
- Uncertainty: "can't really remember" (Tuesday marked as memory gap)
- Protective measures: "wearing masks" (tagged on Wednesday)

**Operator clicks "Save & Review"**, then in review mode:
- Adds husband's full name: "Michael Smith"
- Confirms school: "Salisbury East Primary"
- Classifies contacts:
  - Husband: Close Contact (Household)
  - Mum: Close Contact (Prolonged indoor)
  - Kids: Household Contact

**Result:** 5 days of exposure data entered in ~2 minutes (vs 8-10 minutes with traditional forms)

## Support

For questions or issues, contact the development team or refer to:
- GitHub repository: [Sentinel](https://github.com/christianpeut95/Sentinel)
- Main documentation: `README.md`
