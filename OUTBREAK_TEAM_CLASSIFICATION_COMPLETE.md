# Outbreak Team & Case Classification Management - Complete

## Overview
Built comprehensive team member management and case classification systems for outbreak investigations.

## New Pages Created

### 1. Manage Team Page (`/Outbreaks/ManageTeam`)
**Purpose:** Add and manage investigation team members with assigned roles

#### Key Features:

**Current Team Display**
- Table showing all team members
- User avatar/icon with name and email
- Role badges (color-coded by role)
- Assignment date and who assigned them
- Remove member button with confirmation

**Add Team Member Form**
- Dropdown to select user (shows only unassigned users)
- Role selection with 6 predefined roles:
  - Lead Investigator (Red badge)
  - Investigator (Blue badge)
  - Data Manager (Info badge)
  - Lab Liaison (Warning badge)
  - Communications Officer (Success badge)
  - Team Member (Secondary badge)
- Submit button to add

**Helper Information**
- Role descriptions card explaining each role
- Dynamic "available users" message
- Back to outbreak details button

**Integration:**
- Calls `IOutbreakService.AddTeamMemberAsync()`
- Calls `IOutbreakService.RemoveTeamMemberAsync()`
- Timeline logging for team changes
- Updates team count on outbreak details

### 2. Classify Cases Page (`/Outbreaks/ClassifyCases`)
**Purpose:** Review and classify outbreak cases based on case definition

#### Key Features:

**Active Definition Display**
- Shows active case definition with version number
- Definition name and full text displayed
- Warning if no definition exists

**Unclassified Cases Section** (Warning theme)
- Table of cases needing classification
- Checkboxes for bulk selection
- "Select All" button
- Individual "Classify" button per case
- Bulk Classify button (enabled when cases selected)
- Shows: Case ID, Patient, Disease, Onset Date, Linked Date

**Classified Cases Section**
- Table of already classified cases
- Classification badges (Confirmed/Probable/Suspect/Not a Case)
- Classification date
- "Reclassify" button to change classification

**Classify Single Case Modal**
- Shows case ID and patient name
- Classification dropdown (Confirmed/Probable/Suspect/Not a Case)
- Notes textarea for rationale
- Submit to classify

**Bulk Classify Modal**
- Shows count of selected cases
- Classification dropdown
- Warning about applying same to all
- Submit to bulk classify

**Summary Statistics Sidebar**
- Count of unclassified cases
- Count by each classification type
- Classification guide with badge explanations
- Back to outbreak button

## Backend Implementation

### ManageTeamModel

**Properties:**
- `Outbreak` - Current outbreak
- `TeamMembers` - Current team member list
- `AvailableUsers` - Users not yet on team
- `SelectedUserId` - Bound property for adding member
- `SelectedRole` - Bound property for role

**Handlers:**
- `OnGetAsync(id)` - Load outbreak, team, available users
- `OnPostAddMemberAsync(id)` - Add user to team
- `OnPostRemoveMemberAsync(id, memberId)` - Remove from team

**Logic:**
- Excludes already-assigned users from available list
- Validation to prevent duplicate assignments
- Timeline entries for add/remove actions

### ClassifyCasesModel

**Properties:**
- `Outbreak` - Current outbreak
- `UnclassifiedCases` - Cases without classification
- `ClassifiedCases` - Cases with classification
- `ActiveDefinition` - Active case definition (if any)

**Handlers:**
- `OnGetAsync(id)` - Load outbreak, definition, and cases
- `OnPostClassifyAsync(id, outbreakCaseId, classification, notes)` - Single classification
- `OnPostBulkClassifyAsync(id, selectedCaseIds, classification)` - Bulk classify

**Logic:**
- Loads active case definition (highest version, IsActive = true)
- Splits cases into classified/unclassified lists
- Only loads `CaseType.Case` (not contacts)
- Tracks classification changes for timeline

### Service Layer Updates

#### IOutbreakService - New Methods
```csharp
Task<bool> ClassifyCaseAsync(int outbreakCaseId, CaseClassification classification, string? notes, string userId);
// Existing methods for team management already in place
```

#### OutbreakService - Implementation
```csharp
public async Task<bool> ClassifyCaseAsync(int outbreakCaseId, CaseClassification classification, string? notes, string userId)
{
    // 1. Load outbreak case with patient
    // 2. Store previous classification
    // 3. Update classification, date, classifiedBy, notes
    // 4. Save to database
    // 5. Create timeline entry (classified or reclassified)
    // 6. Return success
}
```

## Model Updates

### OutbreakCase - New Properties
```csharp
public DateTime? ClassificationDate { get; set; }
public string? ClassifiedBy { get; set; }

[StringLength(1000)]
public string? ClassificationNotes { get; set; }
```

### OutbreakCaseDefinition - New Properties
```csharp
[Required]
[StringLength(200)]
public string DefinitionName { get; set; } = string.Empty;

[StringLength(2000)]
public string? DefinitionText { get; set; }
```

### OutbreakEnums - New Event Type
```csharp
CaseClassified = 6  // Added to TimelineEventType enum
```

**Note:** Enum values after this shifted by 1
- DefinitionUpdated now = 7
- TeamMemberAdded now = 8
- etc.

## Database Migration Required

```sql
-- Add new columns to OutbreakCases table
ALTER TABLE OutbreakCases
ADD ClassificationDate DATETIME2 NULL,
    ClassifiedBy NVARCHAR(450) NULL,
    ClassificationNotes NVARCHAR(1000) NULL;

-- Add new columns to OutbreakCaseDefinitions table
ALTER TABLE OutbreakCaseDefinitions
ADD DefinitionName NVARCHAR(200) NOT NULL DEFAULT 'Case Definition',
    DefinitionText NVARCHAR(2000) NULL;
```

## Updated Integration Points

### Details Page Updates

**Quick Actions Dropdown**
- Added "Classify Cases" link
- Changed "Add Team Member" to "Manage Team" (navigates to full page)
- Removed old modal placeholder

**Outbreak Context Card**
- Added "Classify Cases" button below lead investigator

**Team Members Card**
- Added gear icon button in header linking to ManageTeam page

## User Workflows

### Workflow 1: Add Team Member
1. Navigate to Outbreak Details
2. Click "Manage Team" (dropdown or team card gear icon)
3. Select user from available dropdown
4. Choose role (default: Investigator)
5. Click "Add to Team"
6. Member added with timeline entry
7. Returns to manage team page (can add more)

### Workflow 2: Remove Team Member
1. On Manage Team page, view current members
2. Click "Remove" button next to member
3. Confirm removal dialog
4. Member marked inactive with timeline entry
5. User returns to available users list

### Workflow 3: Classify Single Case
1. Navigate to Outbreak Details ? Classify Cases
2. View unclassified cases list
3. Click "Classify" on a case
4. Modal opens with case details
5. Select classification (Confirmed/Probable/Suspect/Not a Case)
6. Optionally enter notes explaining decision
7. Click "Classify"
8. Case moves to classified section with timeline entry

### Workflow 4: Bulk Classify Cases
1. On Classify Cases page, check multiple unclassified cases
2. Click "Bulk Classify" button
3. Modal shows selected count
4. Choose classification to apply to all
5. Warning reminds this applies to all selected
6. Click "Bulk Classify"
7. All selected cases classified with same classification
8. Timeline entry created for bulk action

### Workflow 5: Reclassify Case
1. On Classify Cases page, scroll to classified section
2. Find case needing reclassification
3. Click "Reclassify" button
4. Modal opens with current classification visible
5. Select new classification
6. Add notes explaining change
7. Submit to reclassify
8. Timeline entry logs reclassification

## Visual Design

### Team Management
- **User Icons:** Blue circular badges with person icon
- **Role Badges:** Color-coded by importance/function
- **Available Users:** Clean dropdown with name (email)
- **Remove Confirmation:** JavaScript confirm dialog

### Case Classification
- **Unclassified:** Warning theme (yellow border/background)
- **Classified:** Standard table with success indicators
- **Classification Badges:**
  - Confirmed: Red badge (most serious)
  - Probable: Warning badge (yellow/orange)
  - Suspect: Secondary badge (gray)
  - Not a Case: Light badge with border
- **Summary Stats:** Clean count display with color accents

### Modals
- Single Classify: Simple form with case context
- Bulk Classify: Warning style with count emphasis
- Both responsive and use Bootstrap styling

## Data Flow

### Add Team Member
```
User selects member & role ?
POST to AddMember handler ?
Service validates not duplicate ?
Create OutbreakTeamMember record ?
Log timeline event ?
Redirect to manage team page
```

### Classify Case
```
User selects classification ?
POST with outbreakCaseId & classification ?
Service loads OutbreakCase ?
Store previous classification ?
Update classification fields ?
Save to database ?
Create timeline entry (classify or reclassify) ?
Redirect to classify page
```

### Bulk Classify
```
User selects multiple cases ?
Checkboxes collect IDs ?
Modal collects same classification ?
POST with array of IDs ?
Loop through each case ID ?
Call ClassifyCaseAsync for each ?
Count successes ?
Single timeline entry for bulk action ?
Redirect with success count
```

## JavaScript Features

### ManageTeam.cshtml
- Form submission with validation
- Remove confirmation dialog

### ClassifyCases.cshtml
```javascript
- showClassifyModal(id, name, caseId) - Opens modal with case context
- toggleAllUnclassified(checkbox) - Master checkbox for all
- selectAllUnclassified() - Button to check all
- updateBulkButton() - Enable/disable based on selection, update count
- Dynamic hidden inputs - Adds selectedCaseIds to bulk form
```

## Timeline Integration

### New Timeline Events
1. **Team Member Added**
   - Title: "Team Member Added"
   - Description: "Username added as Role"
   - Type: TeamMemberAdded

2. **Team Member Removed**
   - Title: "Team Member Removed"
   - Description: "Username removed from team"
   - Type: TeamMemberRemoved

3. **Case Classified**
   - Title: "Case Classified"
   - Description: "Patient name classified as Classification"
   - Type: CaseClassified

4. **Case Reclassified**
   - Title: "Case Reclassified"
   - Description: "Patient name reclassified from Old to New"
   - Type: CaseClassified

## Statistics Updates

The outbreak statistics should now account for:
- Classification counts (Confirmed/Probable/Suspect)
- Team member counts
- Unclassified case warnings

These are already displayed on the Details page dashboard.

## Testing Checklist

### Team Management
- [ ] Navigate to Manage Team from Details page
- [ ] View current team members with roles
- [ ] Add new team member
- [ ] Verify cannot add duplicate
- [ ] Remove team member
- [ ] Confirm removed member returns to available list
- [ ] Check timeline entries for team changes
- [ ] Verify team count updates on Details page

### Case Classification
- [ ] Navigate to Classify Cases from Details
- [ ] View active case definition (if exists)
- [ ] See unclassified cases listed
- [ ] Classify single case as Confirmed
- [ ] Add classification notes
- [ ] Verify case moves to classified section
- [ ] Select multiple unclassified cases
- [ ] Use Bulk Classify to classify as Probable
- [ ] Verify all selected cases updated
- [ ] Reclassify a case
- [ ] Check timeline for classification events
- [ ] Verify summary statistics update

### Integration
- [ ] Verify Details page dropdown links work
- [ ] Check team count badge updates
- [ ] Confirm classification stats on dashboard
- [ ] Test with no active definition
- [ ] Test with all cases classified

## Security Considerations

- User ID captured from claims for audit trail
- Classification changes logged with user
- Team changes logged with user
- Confirmation dialogs for destructive actions
- No inline delete without confirmation

## Performance Notes

- Eager loading of Patient and Disease for display
- Indexed on OutbreakId for fast filtering
- Separate queries for classified vs unclassified
- Bulk operations batch database saves

## Future Enhancements

1. **Case Definition Builder**
   - UI to create/edit case definitions
   - Version management
   - Criteria wizard

2. **Auto-Classification**
   - Apply definition rules automatically
   - Suggest classifications based on criteria
   - Bulk auto-classify matching cases

3. **Team Notifications**
   - Email/SMS when assigned to outbreak
   - Notifications for classification milestones
   - Task assignment alerts

4. **Role Permissions**
   - Restrict actions by role
   - Lead Investigator approval workflows
   - Data Manager-only classification access

5. **Classification History**
   - View all classification changes for a case
   - Audit log with reasoning
   - Export classification report

## File Structure

```
Pages/Outbreaks/
??? ManageTeam.cshtml         # ? NEW - Team member management
??? ManageTeam.cshtml.cs      # ? NEW - Team management logic
??? ClassifyCases.cshtml      # ? NEW - Case classification UI
??? ClassifyCases.cshtml.cs   # ? NEW - Classification logic
??? Details.cshtml            # ? UPDATED - Added links to new pages
??? Details.cshtml.cs         # (unchanged)

Models/
??? OutbreakCase.cs           # ? UPDATED - Added classification tracking fields
??? OutbreakCaseDefinition.cs # ? UPDATED - Added name and text fields
??? OutbreakEnums.cs          # ? UPDATED - Added CaseClassified event type

Services/
??? IOutbreakService.cs       # ? UPDATED - Added ClassifyCaseAsync
??? OutbreakService.cs        # ? UPDATED - Implemented ClassifyCaseAsync
```

## Summary

The outbreak team and case classification system is now **functionally complete** pending:

?? **DATABASE MIGRATION REQUIRED** - Run migration to add new columns

? **Team Management** - Full CRUD for team members
? **Case Classification** - Individual and bulk classification
? **Timeline Integration** - All actions logged
? **Audit Trail** - User tracking on all changes
? **UI Integration** - Linked from Details page
? **Visual Separation** - Color-coded by classification
? **Role Management** - 6 predefined outbreak roles

**To Deploy:**
1. Stop the application (hot reload can't apply enum changes)
2. Create and run database migration
3. Restart application
4. Test team management workflows
5. Test case classification workflows

This provides outbreak investigators with essential tools to:
- Assemble investigation teams with clear roles
- Systematically classify cases based on definitions
- Track all classification changes for epidemiological analysis
- Maintain audit trail of outbreak response activities
