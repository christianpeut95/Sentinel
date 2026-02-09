# ? Task Management UI - Lookup Pages Complete

## What Was Created

**Task Template Management UI** - Full CRUD interface for managing task templates (the lookup/configuration pages only).

### Files Created:

1. **`Pages/Settings/Lookups/TaskTemplates.cshtml`** - List all task templates
2. **`Pages/Settings/Lookups/TaskTemplates.cshtml.cs`** - Page model for list and delete
3. **`Pages/Settings/Lookups/CreateTaskTemplate.cshtml`** - Create new task template form
4. **`Pages/Settings/Lookups/CreateTaskTemplate.cshtml.cs`** - Page model for create

### Settings Index Updated:

- Added "Task Management" card with link to Task Templates

---

## Features Implemented

### Task Templates List Page (`/Settings/Lookups/TaskTemplates`)

**Features:**
- ? Display all task templates in a table
- ? Filter by:
  - Category (Isolation, Medication, Monitoring, etc.)
  - Trigger Type (On Case Creation, On Lab Confirmation, etc.)
  - Status (Active/Inactive)
- ? Shows key information:
  - Name & description
  - Category badge
  - Trigger badge
  - Priority badge (color-coded: Urgent=red, High=yellow, Medium=blue, Low=gray)
  - Applicable to (Cases/Contacts/Both)
  - Recurring status
  - Active/Inactive status
- ? Actions:
  - Edit button
  - Details button (placeholder - not yet created)
  - Delete button with confirmation modal
- ? Client-side filtering (no page refresh)
- ? Success messages after create/delete

### Create Task Template Page (`/Settings/Lookups/CreateTaskTemplate`)

**Comprehensive Form with Sections:**

#### 1. **Basic Information**
- Name (required)
- Category (dropdown: Isolation, Medication, Monitoring, Survey, etc.)
- Description (optional)

#### 2. **Task Configuration**
- Default Priority (Low, Medium, High, Urgent)
- Trigger Type (On Case Creation, Manual, etc.)
- Assignment Type (Patient, Investigator, Anyone)
- Applicable To (Cases Only, Contacts Only, or Both)
- Inheritance Behavior (how it applies to child diseases)
- Requires Evidence (checkbox)

#### 3. **Timing & Due Dates**
- Due Calculation Method (From Symptom Onset, From Notification, etc.)
- Due Days (number of days)

#### 4. **Recurrence (Optional)**
- Is Recurring checkbox
- When checked, shows:
  - Recurrence Pattern (Daily, Twice Daily, Weekly, Every Other Day)
  - Recurrence Count (number of times)
  - Recurrence Duration (in days)
- JavaScript shows/hides recurrence options

#### 5. **Instructions**
- Instructions (text area) - What users should do
- Completion Criteria (optional) - What defines completion

#### 6. **Status**
- Is Active checkbox (default checked)

**Form Validation:**
- All required fields validated
- ASP.NET validation messages
- Client-side validation with jQuery

---

## Navigation

### From Settings Index:
1. Click "Settings" in navbar
2. Scroll to "Task Management" card
3. Click "Task Templates"

### From Task Templates List:
- Click "Create Task Template" button ? Goes to create form
- Click Edit icon ? Goes to edit form (not yet created)
- Click Details icon ? Goes to details page (not yet created)
- Click Delete icon ? Shows confirmation modal

---

## What's NOT Yet Created

### Still To Do:

1. **Edit Task Template Page**
   - `/Settings/Lookups/EditTaskTemplate`
   - Similar form to Create, but pre-populated

2. **Task Template Details Page**
   - `/Settings/Lookups/TaskTemplateDetails`
   - Read-only view of all template properties
   - Show which diseases use this template

3. **Disease Task Assignment UI**
   - Within Disease Details/Edit pages
   - Assign/unassign task templates to diseases
   - Configure auto-creation settings
   - Create child overrides

4. **Case/Contact Task List UI**
   - View tasks on Case Details page
   - Complete/cancel tasks
   - Add ad-hoc tasks
   - Task statistics widget

5. **Dashboard Widgets**
   - "My Tasks" widget
   - "Tasks Due Today"
   - "Overdue Tasks"

---

## Database Seeding

The seed script creates example templates:
- Measles isolation
- Meningococcal prophylaxis
- COVID-19 daily symptom checks
- Salmonella food history
- And more...

**To load seed data:**
```bash
# Execute: Surveillance-MVP/Migrations/ManualScripts/SeedTaskTemplateConfiguration.sql
```

---

## Usage Examples

### Creating a Task Template

**Example 1: Measles Isolation**
```
Name: Measles Isolation
Category: Isolation
Trigger: On Case Creation
Priority: High
Applies To: Cases Only
Due Days: 4 (from symptom onset)
Instructions: Remain in isolation until 4 days after rash onset...
Is Active: Yes
```

**Example 2: Daily Symptom Check**
```
Name: Daily Symptom Check - COVID Contact
Category: Monitoring
Trigger: On Contact Identification
Priority: High
Applies To: Contacts Only
Is Recurring: Yes
Recurrence Pattern: Daily
Recurrence Duration: 14 days
Instructions: Check temperature twice daily...
Is Active: Yes
```

**Example 3: Food History Questionnaire**
```
Name: Food History Questionnaire
Category: Survey
Trigger: On Case Creation
Priority: High
Applies To: Cases Only
Due Days: 7 (from notification)
Inheritance: Inherit to all descendants
Instructions: Document all food consumed 3 days before onset...
Is Active: Yes
```

---

## Filtering Examples

The list page supports real-time filtering:

**Show only Isolation tasks:**
- Category filter ? Select "Isolation"
- Shows: Measles Isolation, Generic Isolation, etc.

**Show only tasks for Lab Confirmation:**
- Trigger filter ? Select "On Lab Confirmation"
- Shows: TB Contact Investigation, etc.

**Show only Inactive templates:**
- Status filter ? Select "Inactive Only"

**Combine filters:**
- Category: Monitoring
- Trigger: On Contact Identification
- Status: Active Only
- Result: Shows active monitoring tasks for contacts

---

## Styling & UX

**Bootstrap 5 components:**
- Cards for organization
- Badges for categories, triggers, priorities (color-coded)
- Buttons with icons (Font Awesome)
- Responsive table
- Modals for delete confirmation
- Form validation styling
- Success alerts

**Icons:**
- Category badges (color-coded by type)
- Priority badges (danger for urgent, warning for high)
- Recurring icon (checkmark)
- Active/Inactive badges
- Action buttons (edit, view, delete)

**Responsive:**
- Mobile-friendly form
- Tables scroll horizontally on small screens
- Filter options stack on mobile

---

## Testing Checklist

- [x] Build successful
- [ ] Navigate to `/Settings/Lookups/TaskTemplates`
- [ ] View empty list
- [ ] Click "Create Task Template"
- [ ] Fill out form for Measles Isolation task
- [ ] Submit form
- [ ] Verify redirect to list with success message
- [ ] Verify new template appears in list
- [ ] Filter by category
- [ ] Filter by trigger
- [ ] Test delete with confirmation
- [ ] Load seed data script
- [ ] Verify seed templates appear
- [ ] Filter shows correct results

---

## Next Steps

### Phase 1: Complete Task Template CRUD
1. Create `EditTaskTemplate.cshtml` + page model
2. Create `TaskTemplateDetails.cshtml` + page model

### Phase 2: Disease Task Assignment
3. Add "Tasks" tab to Disease Details page
4. Show inherited and direct tasks
5. Add "Assign Task" button
6. Create child override form

### Phase 3: Case Task UI
7. Add "Tasks" tab to Case Details page
8. Show tasks for case (pending, completed, overdue)
9. Add "Complete Task" button/modal
10. Add "Add Task" button for ad-hoc tasks
11. Show task statistics (X pending, Y completed)

### Phase 4: Dashboard Widgets
12. Create "My Tasks" widget component
13. Create "Tasks Due Today" widget
14. Create "Overdue Tasks" widget
15. Add to main dashboard

---

## Summary

? **Task Template Lookup Pages: COMPLETE**
- List page with filtering
- Create page with comprehensive form
- Delete functionality with confirmation
- Integrated into Settings navigation

? **Still To Build:**
- Edit & Details pages
- Disease task assignment UI
- Case task viewing & completion UI
- Dashboard widgets

**Current Status:** Basic administrative lookup pages complete. Users can now create and manage task templates. Next phase is connecting templates to diseases and cases.
