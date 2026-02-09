# ? Task System Enhancements - Task Types & Disease Autocomplete

## What Was Added

### 1. **Task Type Lookup Table** (Replaces TaskCategory Enum)

**New Model:** `TaskType` (Models/Lookups/TaskType.cs)
- Customizable task type categories
- Icon and color configuration
- Display order for sorting
- Active/Inactive status

**Benefits:**
- ? Fully customizable by users
- ? No code changes needed to add new types
- ? Visual customization (icons, colors)
- ? Can be localized/renamed per organization

### 2. **Disease Selection with Autocomplete**

**Added to CreateTaskTemplate:**
- Disease autocomplete field (jQuery UI)
- Optional - can create generic tasks or disease-specific tasks
- Automatically creates DiseaseTaskTemplate assignment when disease selected
- Uses existing `/api/diseases/search` endpoint

**Benefits:**
- ? Easy to link template to specific disease
- ? Auto-creates disease assignment on template creation
- ? Optional - can still create generic templates

## Files Created

### Models
- ? `Models/Lookups/TaskType.cs` - New lookup table model

### UI Pages - Task Types Management
- ? `Pages/Settings/Lookups/TaskTypes.cshtml` - List all task types
- ? `Pages/Settings/Lookups/TaskTypes.cshtml.cs` - Page model
- ? `Pages/Settings/Lookups/CreateTaskType.cshtml` - Create form
- ? `Pages/Settings/Lookups/CreateTaskType.cshtml.cs` - Create logic

### Seed Data
- ? `Migrations/ManualScripts/SeedDefaultTaskTypes.sql` - 10 default task types

## Files Modified

### Models
- ? `Models/TaskTemplate.cs` - Changed from `TaskCategory` enum to `TaskTypeId` FK
- ? `Models/CaseTask.cs` - Changed from `TaskCategory` enum to `TaskTypeId` FK

### Database
- ? `Data/ApplicationDbContext.cs` - Added TaskTypes DbSet and relationships

### Services
- ? `Services/TaskService.cs` - Updated to use TaskTypeId instead of Category

### UI Pages - Task Templates
- ? `Pages/Settings/Lookups/CreateTaskTemplate.cshtml` - Added disease autocomplete + task type dropdown
- ? `Pages/Settings/Lookups/CreateTaskTemplate.cshtml.cs` - Added disease assignment logic + task type loading
- ? `Pages/Settings/Lookups/TaskTemplates.cshtml` - Shows task type + associated diseases
- ? `Pages/Settings/Lookups/TaskTemplates.cshtml.cs` - Loads task types and disease assignments

### Settings
- ? `Pages/Settings/Index.cshtml` - Added "Task Types" link

## Database Changes Required

### Migration Steps:

1. **Create Migration:**
```bash
dotnet ef migrations add AddTaskTypeAndDiseaseToTaskTemplate
```

2. **Review Migration** - It should:
- Create `TaskTypes` table
- Add `TaskTypeId` column to `TaskTemplates`
- Add `TaskTypeId` column to `CaseTasks`
- Remove `Category` column from both tables
- Add FK relationships
- Add indexes

3. **Apply Migration:**
```bash
dotnet ef database update
```

4. **Seed Default Task Types:**
```sql
-- Execute: Migrations/ManualScripts/SeedDefaultTaskTypes.sql
```

## Default Task Types Seeded

1. **Isolation** (Red) - Patient isolation and quarantine
2. **Medication** (Blue) - Medication administration
3. **Monitoring** (Light Blue) - Symptom checks and monitoring
4. **Survey/Questionnaire** (Yellow) - Completing questionnaires
5. **Laboratory Test** (Green) - Specimen collection and testing
6. **Education** (Gray) - Providing information
7. **Contact Tracing** (Dark) - Identifying contacts
8. **Follow-Up** (Light Blue) - Follow-up appointments
9. **Documentation** (Gray) - Required documentation
10. **Notification** (Yellow) - Notifying authorities

Each has:
- Bootstrap icon
- Color class for badges
- Display order
- Short code

## Updated Workflows

### Creating a Task Template

**Before:**
1. Fill out name, description
2. Select **Category** from dropdown (hardcoded enum)
3. Configure other settings
4. Submit

**After:**
1. Fill out name, description
2. Select **Task Type** from dropdown (loaded from database)
3. **(NEW)** Optionally type disease name in autocomplete field
4. Configure other settings
5. Submit
6. If disease selected, auto-creates disease assignment

### Managing Task Types

**New Workflow:**
1. Go to Settings ? Task Management ? Task Types
2. View all task types
3. Create new task type:
   - Name (e.g., "Vaccination")
   - Code (e.g., "VAX")
   - Description
   - Icon (Bootstrap or Font Awesome)
   - Color (bg-primary, bg-success, etc.)
   - Display Order
   - Active/Inactive
4. Edit or delete existing types
5. Task types appear in Task Template dropdown immediately

## Task Templates List Enhancements

**Now Shows:**
- Task Name
- **Task Type** (with icon and color badge)
- **Associated Diseases** (shows up to 2, with "+X more" if applicable)
- Trigger
- Priority
- Applies To
- Recurring
- Status
- Actions

**Filtering:**
- By Task Type (replaces Category filter)
- By Trigger
- By Status

## Key Features

### 1. **Task Type is Customizable**
- No code changes to add new types
- Can customize icons, colors, names
- Can localize names per organization
- Display order controls sorting

### 2. **Disease Assignment is Simpler**
- Type disease name ? autocomplete suggests matches
- Select disease ? automatically linked
- Or leave blank for generic template
- Shows linked diseases in template list

### 3. **Backwards Compatible**
- Old CategoryEnum removed
- New TaskType lookup more flexible
- Migration handles conversion
- Seed provides equivalent defaults

### 4. **Better UX**
- Visual task type badges with icons
- Disease autocomplete saves clicks
- Clear indication of generic vs disease-specific templates
- Task type management separate from templates

## Testing Checklist

- [ ] Run migration successfully
- [ ] Seed default task types
- [ ] Navigate to Settings ? Task Types
- [ ] View 10 default task types
- [ ] Create new task type "Vaccination"
- [ ] Edit existing task type
- [ ] Go to Task Templates ? Create
- [ ] Select "Isolation" task type from dropdown
- [ ] Type "Measles" in disease field
- [ ] See autocomplete suggestions
- [ ] Select Measles
- [ ] See disease badge appear
- [ ] Submit form
- [ ] Verify template created with Isolation type
- [ ] Verify disease assignment created
- [ ] View template list - see Measles linked

## Migration Command

```bash
# From solution root
dotnet ef migrations add AddTaskTypeAndDiseaseToTaskTemplate --project Surveillance-MVP

# Review the generated migration file

# Apply migration
dotnet ef database update --project Surveillance-MVP

# Seed default task types
# Execute SQL: Surveillance-MVP/Migrations/ManualScripts/SeedDefaultTaskTypes.sql
```

## Benefits Summary

? **Flexibility**: Task types fully customizable without code changes  
? **Usability**: Disease autocomplete simplifies template creation  
? **Visibility**: See which diseases use each template  
? **Organization**: Color-coded task type badges  
? **Scalability**: Easy to add new task types as needed  
? **Localization**: Task type names can be changed per organization  

## Status

**Code Complete:** ?  
**Build Successful:** ?  
**Migration Created:** ? (needs to be run)  
**Database Updated:** ? (needs migration + seed)  
**Testing:** ? (ready for testing after migration)

## Next Steps

1. **Run the migration**
2. **Seed default task types**
3. **Test creating task templates with disease selection**
4. **Test customizing task types**
5. **Build Edit Task Template page** (if needed)
