# Outbreak Details & Dashboard - Implementation Complete

## Overview
Built a comprehensive outbreak Details/Dashboard page that serves as the central hub for outbreak investigation. Features include statistics overview, separate tabs for cases and contacts (graphically separated), team management, and timeline tracking.

## Pages Created

### 1. Details Page (`Pages/Outbreaks/Details.cshtml`)
**Route:** `/Outbreaks/Details?id={outbreakId}`

#### Features:
- **Header Section**
  - Outbreak name with danger icon
  - Status badge (Active/Monitoring/Resolved/Closed) with color coding
  - Start/end dates
  - Description
  - Quick actions dropdown (Edit, Link Case, Add Team, Generate Report)

- **Statistics Dashboard** (4 cards)
  1. **Total Cases** - Shows confirmed/probable/suspect breakdown
  2. **Contacts** - Number under monitoring
  3. **Investigation Team** - Active team member count
  4. **Duration** - Days since outbreak start

- **Context Cards Row**
  - **Outbreak Context Card**
    - Primary disease (with badge)
    - Primary location (clickable link to location details)
    - Primary event (clickable link to event details)
    - Lead investigator
  
  - **Investigation Team Card**
    - List of team members with roles
    - Assignment dates
    - Remove member button (inline form)
    - Shows first 5, with "View all" link if more

#### Tab Navigation
Four main tabs with badge counts:

1. **Cases Tab** (Red theme)
   - Checkbox selection for bulk actions
   - Table showing:
     - Patient name (clickable to case details)
     - Classification (Confirmed/Probable/Suspect badges)
     - Disease
     - Onset date
     - Linked date
     - Link method
   - Actions: View case, Unlink
   - Bulk action button (enabled when cases selected)
   - Empty state with "Link First Case" button

2. **Contacts Tab** (Blue theme)
   - **Graphically separated from Cases** (different color scheme)
   - Checkbox selection for bulk actions
   - Table showing:
     - Contact name (clickable to case details)
     - Disease
     - Notification date (not onset)
     - Linked date
     - Link method
   - Actions: View contact, Unlink
   - Bulk action button (enabled when contacts selected)
   - Empty state with "Link First Contact" button

3. **Timeline Tab**
   - Chronological list of investigation events
   - Color-coded icons by event type:
     - Outbreak declared (red flag)
     - Case/contact added (blue person-plus)
     - Definition updated (yellow document)
     - Team member added (green people)
   - Shows event title, description, and timestamp
   - "Add Event" button for manual entries

4. **Files & Notes Tab**
   - Placeholder for file/note management
   - Integration with Notes model

#### Modal Placeholders
- **Link Case Modal** - For adding cases/contacts (to be implemented)
- **Add Team Member Modal** - For team assignment (to be implemented)

#### JavaScript Features
- **Checkbox Management**
  - Select all cases/contacts
  - Individual checkbox tracking
  - Bulk action button enable/disable based on selection
  
- **Unlink Functionality**
  - Confirmation dialog
  - Reason prompt
  - Form submission to server

### 2. Edit Page (`Pages/Outbreaks/Edit.cshtml`)
**Route:** `/Outbreaks/Edit?id={outbreakId}`

#### Features:
- Update outbreak name
- Change status (Active/Monitoring/Resolved/Closed)
- Update description
- Set start/end dates
- Change primary disease
- Switch location or event (mutually exclusive with JavaScript enforcement)
- Reassign lead investigator
- Audit fields preserved (CreatedDate, CreatedBy)

#### Helper Cards
- **Editing Guidelines** - Best practices
- **Status Guide** - Explanation of each status with color coding

## Backend Implementation

### DetailsModel (Details.cshtml.cs)
**Properties:**
- `Outbreak` - Main outbreak entity
- `Statistics` - Calculated statistics
- `Cases` - List of linked cases
- `Contacts` - List of linked contacts
- `TeamMembers` - Active team members
- `Timeline` - Chronological events

**Methods:**
- `OnGetAsync(int id)` - Loads all outbreak data
- `OnPostUnlinkCaseAsync(int id, int outbreakCaseId, string reason)` - Removes case/contact link
- `OnPostRemoveTeamMemberAsync(int id, int memberId)` - Removes team member

### EditModel (Edit.cshtml.cs)
**Properties:**
- `Outbreak` - Bound model for editing
- Select lists for Diseases, Locations, Events, Users, Statuses

**Methods:**
- `OnGetAsync(int id)` - Loads outbreak for editing
- `OnPostAsync()` - Saves changes
- `LoadSelectListsAsync()` - Populates dropdowns

## Key Design Features

### 1. Graphical Separation of Cases vs Contacts
- **Cases Tab**: Red theme (#dc3545)
  - Border-danger class
  - Red header background
  - Virus icon
  - Shows case-specific data (onset date, classification)
  
- **Contacts Tab**: Blue theme (#0dcaf0)
  - Border-info class
  - Blue header background
  - People icon
  - Shows contact-specific data (notification date, no classification)

### 2. Statistics Integration
Uses `OutbreakStatistics` from service:
```csharp
- TotalCases
- ConfirmedCases, ProbableCases, SuspectCases
- TotalContacts
- TeamMemberCount
- DaysSinceStart
- LastCaseLinkedDate, LastContactLinkedDate
```

### 3. Timeline Visualization
- Vertical timeline with connecting line
- Circular markers with icons
- Event type determines icon and color
- Chronological ordering (newest first)
- Links to related entities where applicable

### 4. Bulk Actions Ready
- Checkbox infrastructure in place
- JavaScript tracks selections
- Buttons enable/disable dynamically
- Ready for bulk task/survey assignment implementation

## Navigation Updates

### Updated Index Page
- Added "View Details" eye icon button to each outbreak row
- Links to Details page with outbreak ID

## CSS Enhancements

### Timeline Styles
```css
.timeline - Container with left padding
.timeline::before - Vertical connecting line
.timeline-marker - Circular icons with borders
```

## Integration Points

### Existing System Integration
1. **Cases** - Links to Case Details page
2. **Patients** - Displays patient names via Case.Patient
3. **Diseases** - Shows and allows selection of diseases
4. **Locations** - Links to location details
5. **Events** - Links to event details
6. **Users** - Shows investigator and team member names
7. **Notes** - Ready for file/note attachment (tab placeholder)

### Service Layer
All data loading uses `IOutbreakService`:
- `GetByIdAsync()` - Main outbreak data with includes
- `GetStatisticsAsync()` - Calculated metrics
- `GetOutbreakCasesAsync()` - Filtered by CaseType.Case
- `GetOutbreakContactsAsync()` - Filtered by CaseType.Contact
- `GetTeamMembersAsync()` - Active members only
- `GetTimelineAsync()` - Ordered events
- `UnlinkCaseAsync()` - Remove case/contact link

## User Experience Flow

### 1. View Outbreak
1. Navigate to Outbreaks ? Click outbreak name or eye icon
2. See overview statistics at a glance
3. Review outbreak context (disease, location/event, lead)
4. Quick view of team members

### 2. Manage Cases
1. Click "Cases" tab
2. Review all cases with classifications
3. Select cases for bulk actions
4. Unlink individual cases with reason

### 3. Manage Contacts
1. Click "Contacts" tab (visually distinct from cases)
2. Review contacts under monitoring
3. Select contacts for bulk actions
4. Unlink individual contacts

### 4. Review Timeline
1. Click "Timeline" tab
2. See chronological investigation history
3. Visual indicators for different event types
4. Add manual timeline events

### 5. Edit Outbreak
1. Click "Edit" button in header
2. Update any outbreak details
3. Changes logged to timeline automatically
4. Redirect back to details page

## Next Steps for Full Implementation

### High Priority
1. **Link Case/Contact Modal** - Interface to search and link cases
   - Search by case ID, patient name
   - Filter by disease
   - Show case type (Case vs Contact)
   - Classification selection for cases
   - Link method tracking

2. **Bulk Actions Implementation**
   - Modal for selecting action (Task or Survey)
   - Template selection
   - Preview affected people
   - Execute and show results

3. **Team Management Modal**
   - User search/selection
   - Role assignment dropdown
   - Add to team with timeline entry

### Medium Priority
4. **Case Definition Management**
   - Tab or section for definitions
   - Create/edit interface
   - Version history
   - Apply to classify cases

5. **Files & Notes Integration**
   - Use existing Notes model
   - File upload
   - Note creation
   - Category/tag filtering

6. **Reports**
   - Summary report generation
   - Case listing export
   - Timeline export
   - Statistics visualization

### Low Priority
7. **Visualizations**
   - Epidemic curve chart (cases over time)
   - Map if location data available
   - Network diagram for case-contact relationships

8. **Search Queries**
   - Save search criteria
   - Run to find matching cases
   - Auto-link option
   - Suggested cases section

## Testing Checklist

- [ ] Navigate to outbreak details from index
- [ ] View statistics cards
- [ ] Switch between tabs (Cases, Contacts, Timeline, Files)
- [ ] Select individual cases/contacts with checkboxes
- [ ] Click "Select All" for cases and contacts
- [ ] Verify bulk action buttons enable/disable
- [ ] Attempt to unlink a case (with reason prompt)
- [ ] View timeline events
- [ ] Click "Edit" to update outbreak
- [ ] Change status and verify color changes
- [ ] Update dates and description
- [ ] Switch between location and event (mutual exclusivity)
- [ ] Save changes and verify redirect
- [ ] Check success message display
- [ ] View team members in both overview and full list

## File Structure

```
Pages/Outbreaks/
??? Index.cshtml              # List of outbreaks
??? Index.cshtml.cs
??? Create.cshtml             # Declare new outbreak
??? Create.cshtml.cs
??? Details.cshtml            # ? NEW - Dashboard/Details
??? Details.cshtml.cs         # ? NEW - Page model
??? Edit.cshtml               # ? NEW - Edit form
??? Edit.cshtml.cs            # ? NEW - Edit handler
```

## Summary

The outbreak Details/Dashboard is now functional with:
- ? Comprehensive statistics overview
- ? **Graphical separation between Cases and Contacts** (different colors/themes)
- ? Team member display and management hooks
- ? Timeline visualization with auto-tracking
- ? Edit functionality
- ? Bulk action infrastructure (checkboxes, buttons)
- ? Unlink case/contact with reason tracking
- ? Integration with existing models and services

**Ready for:**
- Link case/contact interface
- Bulk task/survey assignment
- Team member management modal
- Case definition builder
- File/note management integration

The foundation is solid and follows established patterns from the rest of your surveillance application!
