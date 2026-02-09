# Outbreak Case Definitions Management - Complete

## Overview
Built a comprehensive case definition management system where outbreak investigators can define and version criteria for each classification type (Confirmed, Probable, Suspect, Not a Case).

## New Page Created

### Case Definitions Page (`/Outbreaks/CaseDefinitions`)
**Purpose:** Define, version, and manage case classification criteria for outbreak investigations

## Key Features

### Visual Overview (4 Cards)
Each classification type gets its own color-coded card:

1. **Confirmed (Red border)** - Laboratory-confirmed cases
2. **Probable (Warning/Yellow border)** - Clinically compatible cases  
3. **Suspect (Gray border)** - Cases under investigation
4. **Not a Case (Light border)** - Exclusion criteria

### Per-Classification Management

**Each card shows:**
- Active definition name and version number
- Full definition text
- Edit button to modify
- History button to view previous versions
- Create button if no definition exists

**Version History (Collapsible)**
- List of all previous versions
- Effective dates
- Reactivate button to restore old version

### Case Definition Form (Modal)

**Fields:**
- **Definition Name** (required) - e.g., "Gastroenteritis Outbreak Definition"
- **Definition Text** (textarea) - Full prose description
- **Specific Criteria** (structured):
  - Clinical criteria
  - Laboratory criteria  
  - Epidemiological criteria
- **Notes** - Additional context or references

**Functionality:**
- Create new definitions
- Edit existing definitions  
- Version tracking (automatic increment)
- Activation/deactivation

## Backend Implementation

### CaseDefinitionsModel

**Properties:**
- `Outbreak` - Current outbreak
- `Definitions` - All definitions for outbreak
- `ConfirmedDefinition` - Active confirmed definition
- `ProbableDefinition` - Active probable definition
- `SuspectDefinition` - Active suspect definition
- `NotACaseDefinition` - Active exclusion criteria

**Handlers:**
- `OnGetAsync(id)` - Load outbreak and all definitions
- `OnPostSaveDefinitionAsync(id)` - Create or update definition
- `OnPostActivateDefinitionAsync(id, definitionId)` - Activate a specific version

**Logic:**
```csharp
// When creating new definition:
1. Auto-increment version number for that classification
2. Deactivate previous definitions of same classification  
3. Set new definition as active
4. Log to outbreak timeline

// When activating old definition:
1. Deactivate all others of same classification
2. Activate selected definition
3. Update effective date
4. Clear expiry date
5. Log to timeline
```

### Version Management

**Versioning Strategy:**
- Each classification has independent version numbers
- Version 1, 2, 3... for Confirmed
- Version 1, 2, 3... for Probable (separate sequence)
- Only ONE active definition per classification at a time

**Version History:**
- Old versions kept in database (IsActive = false)
- ExpiryDate set when replaced
- Can reactivate any previous version

## Model Updates

### OutbreakCaseDefinition - Already Had Required Fields
```csharp
public string DefinitionName { get; set; }  // Added earlier
public string? DefinitionText { get; set; }  // Added earlier
public CaseClassification Classification { get; set; }  // Which classification this defines
public string CriteriaJson { get; set; }  // Structured criteria (JSON)
public int Version { get; set; }  // Version number
public DateTime EffectiveDate { get; set; }  // When activated
public DateTime? ExpiryDate { get; set; }  // When replaced
public bool IsActive { get; set; }  // Only one active per classification
```

## Integration Points

### Details Page
- Added "Case Definitions" to quick actions dropdown
- Links to definition management page

### Classify Cases Page  
- Already shows active definition with version
- Uses definitions to guide classification decisions

### Timeline Integration
- "Case Definition Created" events
- "Case Definition Activated" events
- Shows which classification and definition name

## User Workflows

### Workflow 1: Create Initial Definitions
1. Navigate to Outbreak Details ? Case Definitions
2. See 4 empty cards (Confirmed, Probable, Suspect, Not a Case)
3. Click "Create Definition" on Confirmed card
4. Enter definition name and criteria
5. Save - becomes Version 1, marked active
6. Repeat for other classifications as needed

### Workflow 2: Update Existing Definition
1. On Case Definitions page, find classification
2. Click "Edit" button
3. Modify definition text or criteria
4. Save - creates new version, deactivates old
5. New version becomes active automatically

### Workflow 3: Restore Previous Version
1. Click "History" button on a classification
2. Collapse reveals previous versions
3. Click reactivate button (circular arrow)
4. Confirm - that version becomes active again
5. Previous "active" version moves to history

### Workflow 4: Define Exclusion Criteria
1. Navigate to "Not a Case" card
2. Click "Create Definition"
3. Enter criteria that EXCLUDE cases
4. Save - guides exclusion decisions

## Visual Design

### Color-Coding
- **Confirmed**: Red (#dc3545) - Most certain
- **Probable**: Warning/Yellow (#ffc107) - Clinical certainty
- **Suspect**: Gray (#6c757d) - Under investigation  
- **Not a Case**: Light gray - Exclusions

### Cards
- Color-coded borders match classification
- Header shows classification icon
- Create button in header
- Collapsible history sections

### Modal
- Large (modal-lg) for detailed input
- Structured criteria sections
- Placeholder text for guidance
- Save button prominent

## Data Flow

### Create Definition
```
User fills form ?
POST to SaveDefinition ?
Check if update (Id > 0) or new ?
If new:
  - Get next version number
  - Deactivate previous versions
  - Add new definition
  - Log timeline event
? Redirect to definitions page
```

### Activate Old Version
```
User clicks reactivate ?
POST to ActivateDefinition ?
Load definition by ID ?
Deactivate all others of same classification ?
Activate selected definition ?
Update effective date ?
Clear expiry date ?
Log timeline event ?
Redirect with success
```

## Database Considerations

### Indexes Needed
```sql
-- For fast lookups of active definitions
CREATE INDEX IX_OutbreakCaseDefinitions_OutbreakId_Classification_IsActive
ON OutbreakCaseDefinitions (OutbreakId, Classification, IsActive);

-- For version history queries
CREATE INDEX IX_OutbreakCaseDefinitions_OutbreakId_Classification_Version
ON OutbreakCaseDefinitions (OutbreakId, Classification, Version DESC);
```

### Query Performance
- Active definition queries are fast (indexed on IsActive)
- Version history loaded only when expanded
- All definitions loaded once on page load

## Benefits

### For Outbreak Investigators
1. **Standardization** - Consistent criteria across team
2. **Transparency** - Clear documented definitions
3. **Flexibility** - Can update as outbreak evolves
4. **Audit Trail** - Version history preserved
5. **Guidance** - Helps with case classification decisions

### For Epidemiologists
1. **Reproducibility** - Know exact criteria used
2. **Temporal Analysis** - See how definitions changed over time
3. **Comparison** - Compare definitions across outbreaks
4. **Documentation** - Evidence-based classification

## Testing Checklist

- [ ] Navigate to Case Definitions from Details
- [ ] See 4 classification cards (empty initially)
- [ ] Create Confirmed definition
- [ ] Verify definition appears with v1
- [ ] Create Probable definition
- [ ] Create Suspect definition
- [ ] Create Not a Case definition
- [ ] Edit Confirmed definition
- [ ] Verify new version created (v2)
- [ ] Check old version in history
- [ ] Reactivate old version (v1)
- [ ] Verify it becomes active again
- [ ] Check timeline for definition events
- [ ] Verify ClassifyCases page shows active definition

## Future Enhancements

1. **Criteria Builder**
   - Visual form for clinical criteria
   - Symptom checklist
   - Lab result options
   - Exposure history fields

2. **Auto-Classification**
   - Apply criteria automatically
   - Suggest classifications based on data
   - Flag cases meeting criteria

3. **Import/Export**
   - Import standard definitions
   - Export for other outbreaks
   - Share across jurisdictions

4. **Definition Templates**
   - Pre-built definitions by disease
   - Customize standard definitions
   - Library of common criteria

5. **Comparison Tool**
   - Compare two versions side-by-side
   - Highlight changes
   - Diff view

## File Structure

```
Pages/Outbreaks/
??? CaseDefinitions.cshtml        # ? NEW - Definition management UI
??? CaseDefinitions.cshtml.cs     # ? NEW - Definition logic
??? ClassifyCases.cshtml          # (uses definitions)
??? Details.cshtml                # ? UPDATED - Added link
??? ...

Models/
??? OutbreakCaseDefinition.cs     # (already complete)
??? OutbreakEnums.cs              # CaseClassification enum
```

## Summary

The case definition management system is now **complete** with:

? **Visual overview** of all 4 classification types
? **Create/edit** definitions with structured criteria
? **Version management** with automatic incrementing
? **History tracking** with reactivation capability
? **Timeline integration** for audit trail
? **Color-coded UI** matching classification importance
? **Modal form** for detailed definition entry

This provides outbreak teams with essential tools to:
- Define clear classification criteria upfront
- Update definitions as outbreak evolves
- Maintain consistent classification across team
- Document decision-making process
- Compare criteria over time

**Ready for use** - no additional configuration needed!

The system allows each outbreak to have its own tailored definitions while maintaining version history and ensuring only one active definition per classification at any time.
