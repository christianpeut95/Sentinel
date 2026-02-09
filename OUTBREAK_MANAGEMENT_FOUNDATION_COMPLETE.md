# Outbreak Management System - Implementation Complete

## Overview
Built a comprehensive Outbreak Management System that enables public health teams to investigate disease outbreaks, track cases and contacts, manage investigation teams, and coordinate response activities.

## Core Features Implemented

### 1. **Outbreak Entity**
- Name, description, status (Active, Monitoring, Resolved, Closed)
- Start/end dates
- Single primary location OR event (1:1 relationship)
- Primary disease association
- Lead investigator assignment
- Soft delete support

### 2. **Team Management**
- Add/remove team members with roles:
  - Lead Investigator
  - Investigator
  - Data Manager
  - Lab Liaison
  - Communications Officer
  - Team Member
- Track assignment/removal dates and users
- Active/inactive status

### 3. **Case & Contact Tracking**
- Link both Cases (CaseType.Case) and Contacts (CaseType.Contact) to outbreaks
- Track classification for cases: Confirmed, Probable, Suspect, Not a Case
- Contacts don't have classifications (nullable)
- Track link method: Manual, Auto-Suggested, Search Query
- Support for unlinking with reason tracking
- Separate views for cases vs contacts (graphically separated in UI)

### 4. **Case Definitions**
- Version-controlled case definitions
- Separate definitions for each classification level
- Criteria stored as JSON for flexibility
- Effective/expiry dates for temporal tracking
- Notes field for additional context

### 5. **Timeline Tracking**
- Automatic timeline events for:
  - Outbreak declared
  - Cases/contacts added or removed
  - Team members added or removed
  - Case definitions updated
  - Bulk actions performed
  - Status changes
- Manual timeline entries supported
- Links to related cases or notes

### 6. **Search Queries**
- Save search criteria as JSON
- Auto-link mode to automatically add matching cases
- Track last run date and match count
- Use for case finding and surveillance

### 7. **Integration with Existing Systems**
- **Notes Model**: Extended to support OutbreakId for file attachments and notes
- **Location Model**: Single primary location per outbreak
- **Event Model**: Single primary event per outbreak
- **Case Model**: Uses existing Case.Type (Case/Contact)
- **Patient Model**: Access via Case.Patient relationship
- **Task System**: Ready for bulk task assignment (stubbed)
- **Survey System**: Ready for bulk survey assignment (stubbed)

## Database Schema

### Tables Created
1. **Outbreaks** - Main outbreak entity
2. **OutbreakTeamMembers** - Investigation team membership
3. **OutbreakCaseDefinitions** - Versioned case criteria
4. **OutbreakCases** - Junction table linking cases/contacts to outbreaks
5. **OutbreakTimeline** - Activity log and milestones
6. **OutbreakSearchQueries** - Saved search criteria

### Key Relationships
- Outbreak ? Disease (Many-to-One, optional)
- Outbreak ? Location (Many-to-One, optional)
- Outbreak ? Event (Many-to-One, optional)
- Outbreak ? ApplicationUser (Lead Investigator)
- OutbreakCase ? Case (includes both cases and contacts)
- Note ? Outbreak (for files and documentation)

## User Interface

### Pages Created
1. **`/Outbreaks/Index`** - List all outbreaks with status indicators
2. **`/Outbreaks/Create`** - Declare new outbreak form
3. **Navigation** - Added to sidebar with danger badge

### Settings Integration
- Added "Outbreak Management" section to Settings page
- Red border card for visibility
- Links to outbreak management

## Service Layer

### IOutbreakService / OutbreakService
- CRUD operations for outbreaks
- Team member management
- Case/contact linking and unlinking
- Case definition versioning
- Timeline event tracking
- Search query management
- Statistics calculation
- Bulk operations (stubbed for future implementation)

### Statistics Provided
- Total cases and contacts
- Cases by classification (Confirmed/Probable/Suspect)
- Team member count
- Days since outbreak start
- Last case/contact linked dates

## Key Design Decisions

### 1. Single Location or Event
- Simplified from many-to-many to 1:1
- Outbreak is tied to ONE primary location OR event
- Matches real-world outbreak investigation patterns

### 2. Unified Case/Contact Handling
- Both stored in same OutbreakCases table
- Differentiated by Case.Type property
- UI will show separate tabs/sections
- Classification only applies to cases, not contacts

### 3. Reuse Existing Notes Model
- No separate OutbreakFile model
- Notes model extended with OutbreakId
- Consistent with rest of application
- Supports both text notes and file attachments

### 4. Bulk Operations Stubbed
- BulkAssignTaskAsync and BulkAssignSurveyAsync created
- Currently create timeline events only
- Ready for full implementation with proper type handling
- Avoided complexity of Guid/int conversion issues

### 5. Timeline Auto-Tracking
- Automatic events for all major actions
- Provides audit trail
- Supports investigation documentation
- Links to related entities

## Migration Applied
- Migration `20260208050523_AddOutbreakManagementSystem` applied successfully
- All tables and indexes created
- Foreign keys established
- Ready for use

## Next Steps for Full Implementation

### High Priority
1. **Details Page** - Outbreak dashboard with:
   - Statistics cards
   - Cases tab with classification filters
   - Contacts tab (separate from cases)
   - Team members list
   - Timeline view
   - Files/notes section

2. **Link Cases/Contacts** - Interface to:
   - Search and manually link cases/contacts
   - View suggested cases from saved searches
   - Bulk link multiple cases
   - Reclassify cases (Suspect ? Probable ? Confirmed)

3. **Case Definition Builder** - UI to:
   - Create/edit case definitions with criteria
   - Version management
   - Apply definitions to classify cases

4. **Bulk Actions** - Complete implementation:
   - Select multiple cases/contacts
   - Assign tasks from templates
   - Assign surveys
   - Change classifications in bulk
   - Send notifications

### Medium Priority
5. **Search Query Builder** - Visual interface for:
   - Building case search criteria
   - Saving queries
   - Running queries to find matches
   - Auto-link toggle

6. **Visualization** - Charts and maps:
   - Epidemic curve
   - Geographic distribution (if location data available)
   - Timeline visualization
   - Case count trends

7. **Reports** - Generate outbreak reports:
   - Summary statistics
   - Case listings
   - Team activity
   - Export to PDF/Excel

### Low Priority
8. **Email Notifications** - Notify team members of:
   - Assignment to outbreak
   - New cases added
   - Status changes
   - Tasks assigned

9. **Outbreak Templates** - Save configurations for:
   - Common outbreak types
   - Standard team structures
   - Default case definitions

## Files Modified/Created

### Models
- `Models/OutbreakEnums.cs` ?
- `Models/Outbreak.cs` ?
- `Models/OutbreakTeamMember.cs` ?
- `Models/OutbreakCaseDefinition.cs` ?
- `Models/OutbreakCase.cs` ?
- `Models/OutbreakTimeline.cs` ?
- `Models/OutbreakSearchQuery.cs` ?
- `Models/Note.cs` (updated) ?

### Services
- `Services/IOutbreakService.cs` ?
- `Services/OutbreakService.cs` ?

### Data Layer
- `Data/ApplicationDbContext.cs` (updated) ?
- `Program.cs` (service registration) ?

### Pages
- `Pages/Outbreaks/Index.cshtml` ?
- `Pages/Outbreaks/Index.cshtml.cs` ?
- `Pages/Outbreaks/Create.cshtml` ?
- `Pages/Outbreaks/Create.cshtml.cs` ?

### Navigation
- `Pages/Shared/_Layout.cshtml` (updated) ?
- `Pages/Settings/Index.cshtml` (updated) ?

### Database
- Migration created and applied ?

## Testing Checklist

- [ ] Navigate to Settings ? Outbreak Management
- [ ] Click "Outbreaks" from sidebar
- [ ] Declare a new outbreak
- [ ] Associate with a disease, location, and lead investigator
- [ ] View outbreak in list
- [ ] Check database tables created correctly

## Summary
The foundation for the Outbreak Management System is complete with all models, services, database tables, and basic UI pages created. The system is designed to handle real-world outbreak investigation workflows with proper separation of cases and contacts, team coordination, and activity tracking. Ready for enhancement with details page and bulk action features.
