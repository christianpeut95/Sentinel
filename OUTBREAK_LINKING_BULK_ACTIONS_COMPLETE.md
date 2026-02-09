# Outbreak Case/Contact Linking & Bulk Actions - Complete

## Overview
Built a comprehensive case/contact linking system with saved search queries and bulk task/survey assignment capabilities for outbreak investigations.

## New Pages Created

### 1. Link Cases Page (`/Outbreaks/LinkCases`)
**Purpose:** Search for and link cases/contacts to outbreaks with saved query support

#### Key Features:

**Search Panel (Left Sidebar - Sticky)**
- **Filter Options:**
  - Record Type (All / Cases Only / Contacts Only)
  - Disease selection
  - Date range (onset or notification)
  - Patient name search
  - Case ID search
  
- **Search Query Management:**
  - Save current search criteria with name
  - Auto-link toggle (automatically link new matches)
  - List of saved searches with match counts
  - Click saved search to reload and run
  
**Results Panel (Right)**
- Displays up to 100 matching records
- **Excludes already linked cases** to prevent duplicates
- Shows:
  - Type badge (Case/Contact)
  - Case ID (clickable to open in new tab)
  - Patient name
  - Disease
  - Relevant date (onset for cases, notification for contacts)
- Select all / Clear selection buttons
- Checkbox selection for individual records
- Classification dropdown (applies to cases only)
- Bulk link selected records button

**Workflow:**
1. Set search criteria
2. Click "Search"
3. Review results (automatically excludes linked cases)
4. Select cases/contacts to link
5. Choose classification if needed
6. Click "Link Selected to Outbreak"
7. Optionally save search for repeated use

### 2. Bulk Actions Page (`/Outbreaks/BulkActions`)
**Purpose:** Assign tasks or surveys to multiple cases/contacts simultaneously

#### Key Features:

**Selected Records Display**
- Shows all selected cases/contacts in table
- Type, Case ID, Patient, Disease
- Confirmation before executing action

**Action Selection**
- Radio toggle between:
  - **Assign Task** - Create same task for everyone
  - **Assign Survey** - Request survey completion from everyone
  
**Template Selection**
- Dynamic dropdown based on action type
- Shows all available Task Templates or Survey Templates
- Help text explaining the action

**Execution**
- Creates individual tasks for each person
- Logs to outbreak timeline
- Redirects to outbreak details with success message

**Helper Information Cards**
- Use case examples for cases vs contacts
- Common scenarios (isolation, monitoring, surveys)

## Backend Implementation

### LinkCasesModel

**Properties:**
- `Outbreak` - Current outbreak
- `SavedQueries` - List of saved search queries for this outbreak
- `SearchResults` - Matching cases/contacts from search
- `Criteria` - Current search parameters
- `SavedQueryName` - Name for saving query
- `AutoLink` - Whether to auto-link new matches

**Handlers:**
- `OnGetAsync(id, queryId)` - Load page, optionally run saved query
- `OnPostSearchAsync(id)` - Execute search with criteria
- `OnPostSaveQueryAsync(id)` - Save search criteria as reusable query
- `OnPostLinkCasesAsync(id, selectedCaseIds, classification)` - Link selected cases

**Search Logic:**
```csharp
- Filters: CaseType, Disease, Date Range, Patient Name, Case ID
- Excludes already linked cases from outbreak
- Orders by onset/notification date
- Limits to 100 results
- Eager loads Patient and Disease for display
```

### BulkActionsModel

**Properties:**
- `Outbreak` - Current outbreak
- `SelectedRecords` - Cases/contacts to act upon
- `TaskTemplates` - Available task templates
- `SurveyTemplates` - Available survey templates
- `ActionType` - "task" or "survey"
- `TemplateId` - Selected template
- `CaseIds` - List of selected case IDs (passed via query string)

**Handlers:**
- `OnGetAsync(id, caseIds)` - Parse case IDs and load selected records
- `OnPostAsync(id)` - Execute bulk action (task or survey assignment)

**Integration:**
- Calls `IOutbreakService.BulkAssignTaskAsync()` for tasks
- Calls `IOutbreakService.BulkAssignSurveyAsync()` for surveys
- Both create timeline entries automatically

### SearchCriteria Model
```csharp
public class SearchCriteria
{
    public CaseType? CaseType { get; set; }
    public Guid? DiseaseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PatientName { get; set; }
    public string? CaseId { get; set; }
}
```

Serialized to JSON for storage in `OutbreakSearchQuery.QueryJson`

## Updated Pages

### Details Page Updates

**Link Buttons Updated:**
- Cases tab "Link Case" ? redirects to LinkCases page
- Contacts tab "Link Contact" ? redirects to LinkCases page
- Dropdown menu "Link Case/Contact" ? redirects to LinkCases page
- Empty state buttons ? redirect to LinkCases page

**Bulk Action Buttons:**
- Now functional with `onclick` handlers
- Cases: `bulkActionsCases()` - collects selected case checkboxes
- Contacts: `bulkActionsContacts()` - collects selected contact checkboxes
- Both redirect to BulkActions page with comma-separated case IDs

**JavaScript Functions:**
```javascript
function bulkActionsCases() {
    // Collect checked case IDs
    // Redirect to /Outbreaks/BulkActions?id={id}&caseIds={csv}
}

function bulkActionsContacts() {
    // Collect checked contact IDs  
    // Redirect to /Outbreaks/BulkActions?id={id}&caseIds={csv}
}
```

## Search Query Storage

### OutbreakSearchQuery Model
```csharp
- QueryName - User-friendly name
- QueryJson - Serialized SearchCriteria
- IsAutoLink - Auto-link new matches flag
- LastRunDate - When last executed
- LastRunMatchCount - Results from last run
- IsActive - Soft delete support
```

### Benefits of Saved Queries
1. **Repeated Use** - Common searches saved for quick access
2. **Consistency** - Same criteria applied over time
3. **Auto-Link** - Automatically link new cases matching criteria
4. **Audit Trail** - Track what queries are used
5. **Team Collaboration** - Share effective search strategies

### Example Saved Queries
- "Confirmed Cases Last 30 Days"
- "All Gastro Contacts for Monitoring"
- "Recent Suspect Cases Needing Classification"
- "Event Attendees - Auto Link"

## User Workflows

### Workflow 1: Manual Case Linking
1. Navigate to outbreak details
2. Click "Link Case/Contact" (button or dropdown)
3. Set search filters (type, disease, dates, etc.)
4. Click "Search"
5. Review results (excludes already linked)
6. Select desired cases/contacts
7. Choose classification for cases
8. Click "Link Selected to Outbreak"
9. Return to outbreak details

### Workflow 2: Save Search for Repeated Use
1. On Link Cases page, set useful search criteria
2. Click "Search" to verify results
3. Enter name in "Save This Search" section
4. Toggle "Auto-link" if desired
5. Click "Save Query"
6. Query appears in "Saved Searches" list
7. Click saved search anytime to re-run

### Workflow 3: Bulk Task Assignment
1. On outbreak details, Cases or Contacts tab
2. Check boxes for desired records
3. Click "Bulk Actions" button
4. Review selected records
5. Choose "Assign Task"
6. Select task template from dropdown
7. Click "Execute Bulk Action"
8. Tasks created for all selected people
9. Timeline entry logged

### Workflow 4: Bulk Survey Assignment
1. Select multiple cases/contacts (checkboxes)
2. Click "Bulk Actions"
3. Choose "Assign Survey"
4. Select survey template
5. Execute action
6. Survey tasks created for everyone
7. People can complete via "My Tasks"

## Integration Points

### With Outbreak Service
- `LinkCaseAsync()` - Creates OutbreakCase record
- `CreateSearchQueryAsync()` - Saves query
- `GetSearchQueriesAsync()` - Loads saved queries
- `BulkAssignTaskAsync()` - Task assignment
- `BulkAssignSurveyAsync()` - Survey assignment
- `AddTimelineEventAsync()` - Auto-logging

### With Task System
- Bulk actions create CaseTask records
- Integrated with existing task templates
- Tasks appear in "My Tasks" dashboard
- Survey tasks link to survey system

### With Timeline
- Case linked events
- Search query saved events
- Bulk action events
- All with case counts and descriptions

## UI/UX Enhancements

### Visual Design
- **Link Cases**: Sticky search panel, clean results table
- **Bulk Actions**: Clear two-step process (select ? execute)
- **Color Coding**: Cases (red), Contacts (blue) consistent throughout
- **Icons**: Intuitive icons for all actions
- **Empty States**: Helpful guidance when no results

### Responsive Design
- Search panel sticks on scroll
- Tables responsive with horizontal scroll
- Works on tablet and desktop

### User Feedback
- Success messages after linking
- Error messages if issues
- Disabled buttons when nothing selected
- Selection count display
- Confirmation dialogs for critical actions

## Data Flow

### Link Cases Flow
```
User selects criteria ? 
Query database ? 
Filter by criteria ? 
Exclude linked cases ?
Display results ?
User selects records ?
Create OutbreakCase links ?
Log to timeline ?
Redirect to details
```

### Saved Query Flow
```
User saves search ?
Serialize criteria to JSON ?
Store in OutbreakSearchQuery ?
Display in saved list ?
User clicks saved query ?
Deserialize JSON ?
Populate form ?
Auto-execute search ?
Show results
```

### Bulk Action Flow
```
User selects cases on details page ?
Click bulk action button ?
Collect selected IDs ?
Navigate to Bulk Actions with IDs ?
Display selected records ?
User chooses action & template ?
Service creates individual tasks ?
Log to timeline ?
Return to details page
```

## Testing Checklist

### Link Cases
- [ ] Navigate from outbreak details
- [ ] Search by different criteria combinations
- [ ] Verify already linked cases excluded
- [ ] Select and link single case
- [ ] Select and link multiple cases
- [ ] Assign classification to cases
- [ ] Save search query with name
- [ ] Load and run saved query
- [ ] Toggle auto-link on saved query

### Bulk Actions
- [ ] Select cases from Cases tab
- [ ] Click bulk actions button
- [ ] Verify correct records shown
- [ ] Assign task template
- [ ] Verify tasks created (check My Tasks)
- [ ] Select contacts from Contacts tab
- [ ] Assign survey template
- [ ] Verify survey tasks created
- [ ] Check timeline for bulk action events

### Integration
- [ ] Verify linked cases appear in Cases tab
- [ ] Verify linked contacts appear in Contacts tab
- [ ] Check case count updates
- [ ] Verify timeline entries logged
- [ ] Test from different outbreak scenarios

## Performance Considerations

- Search limited to 100 results
- Indexes on CaseType, DiseaseId, DateOfOnset, DateOfNotification
- Efficient exclusion of already linked cases
- Eager loading of related data (Patient, Disease)
- Minimal database roundtrips

## Future Enhancements (Not Implemented)

1. **Auto-Link Scheduler** - Background job to run auto-link queries
2. **Advanced Search** - More filter options (location, symptoms, lab results)
3. **Search Result Export** - Download matching cases as CSV
4. **Bulk Classification Change** - Update classifications in bulk
5. **Query Sharing** - Share saved queries with team
6. **Search Analytics** - Track which queries are most effective

## File Structure

```
Pages/Outbreaks/
??? Index.cshtml
??? Create.cshtml
??? Details.cshtml         # ? Updated with bulk action buttons
??? Edit.cshtml
??? LinkCases.cshtml       # ? NEW - Search and link interface
??? LinkCases.cshtml.cs    # ? NEW - Search logic & query management
??? BulkActions.cshtml     # ? NEW - Bulk task/survey assignment
??? BulkActions.cshtml.cs  # ? NEW - Bulk action handler
```

## Summary

The outbreak case/contact linking system is now **fully functional** with:

? **Comprehensive search** with multiple filter options
? **Saved search queries** for repeated use and auto-linking
? **Smart exclusion** of already linked cases
? **Classification assignment** for cases
? **Bulk task assignment** to multiple records
? **Bulk survey assignment** to multiple records
? **Timeline integration** for audit trail
? **User-friendly interface** with clear workflows

This provides outbreak investigators with powerful tools to:
- Quickly find and link relevant cases/contacts
- Save effective search strategies
- Coordinate response activities at scale
- Maintain consistency across investigations

**Next Priorities:**
1. Team member management modal
2. Case definition builder
3. Enhanced statistics and visualizations
4. Auto-link scheduler for saved queries
