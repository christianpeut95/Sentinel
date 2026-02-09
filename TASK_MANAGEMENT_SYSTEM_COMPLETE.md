# Task Management System - Implementation Complete

## Overview
Implemented a comprehensive task/action tracking system for the Surveillance-MVP application with full disease hierarchy support. This system allows public health officials to manage tasks related to cases and contacts, with automatic task generation based on disease type.

## Components Created

### 1. Models

#### **TaskEnums.cs**
- `TaskCategory`: Isolation, Medication, Monitoring, Survey, LabTest, Education, ContactTracing, FollowUp
- `TaskTrigger`: OnCaseCreation, OnContactIdentification, OnLabConfirmation, OnSymptomOnset, OnExposureRecorded, Manual
- `TaskAssignmentType`: Patient, Investigator, Anyone
- `CaseTaskStatus`: Pending, InProgress, Completed, Cancelled, Overdue, WaitingForPatient
- `TaskPriority`: Low, Medium, High, Urgent
- `TaskDueCalculationMethod`: FromSymptomOnset, FromNotificationDate, FromContactDate, FromTaskCreation
- `RecurrencePattern`: Daily, TwiceDaily, Weekly, EveryOtherDay
- `TaskInheritanceBehavior`: Inherit, NoInheritance, Selective

#### **TaskTemplate.cs**
Reusable task templates that define:
- Task details (name, description, instructions)
- Triggering conditions (when to create the task)
- Timing (due dates, recurrence)
- Assignment (who should complete it)
- Hierarchy behavior (inheritance to child diseases)
- Survey integration (for future questionnaires)

#### **CaseTask.cs**
Actual task instances assigned to cases:
- Links to Case and TaskTemplate
- Tracking status, completion, and cancellation
- Assignment to specific users
- Evidence/attachment support
- Recurrence tracking (parent/child tasks)

#### **DiseaseTaskTemplate.cs**
Junction table managing disease-task relationships with hierarchy support:
- Direct assignments vs inherited assignments
- Propagation to child diseases
- Child-specific overrides (priority, instructions, due dates)
- Auto-creation settings per trigger

### 2. Services

#### **ITaskService.cs**
Comprehensive interface including:
- Hierarchy-aware task template retrieval
- Task template configuration and propagation
- Task instance creation (automatic and manual)
- Recurring task management
- Task queries and statistics
- Batch operations

#### **TaskService.cs**
Full implementation of ITaskService with:
- **GetApplicableTaskTemplates()**: Walks disease hierarchy to get all applicable tasks (direct + inherited)
- **AssignTaskTemplate()**: Assigns a task to a disease with cascade options
- **PropagateTaskTemplateToChildren()**: Propagates tasks to all child diseases
- **CreateChildOverride()**: Allows child diseases to customize inherited tasks
- **CreateTasksForCase()**: Automatically creates task instances based on triggers
- **CreateRecurringTaskInstances()**: Generates recurring task instances (e.g., daily symptom checks)
- Task management: Complete, Cancel, Update
- Queries: GetTasksForCase, GetTasksForUser, GetOverdueTasks, GetTasksDueToday
- Statistics: GetTaskStatistics

### 3. Database

#### **ApplicationDbContext.cs**
Added DbSets and configurations:
- TaskTemplates
- CaseTasks
- DiseaseTaskTemplates
- Proper indexes for performance
- Foreign key relationships with cascading rules

#### **Migration: AddTaskManagementSystem**
Generated migration file creates:
- TaskTemplates table
- CaseTasks table
- DiseaseTaskTemplates table
- All necessary indexes and relationships

### 4. Seed Data

#### **SeedTaskTemplateConfiguration.sql**
Example task configurations for common diseases:

**Measles:**
- Isolation task (4 days after rash onset)
- Urgent contact tracing task

**Meningococcal Disease:**
- Prophylactic antibiotics for close contacts (urgent)

**COVID-19:**
- Daily symptom monitoring for contacts (14 days, recurring)

**Tuberculosis:**
- Contact investigation (after lab confirmation)

**Salmonella:**
- Food history questionnaire (cascades to all Salmonella subtypes)

**Legionellosis:**
- Water system exposure investigation

**Pertussis:**
- Identify vulnerable contacts (infants, pregnant women)

**Generic:**
- Outbreak questionnaire (manual trigger)
- Generic isolation task

## Key Features

### 1. Disease Hierarchy Support
- Tasks configured on parent diseases automatically apply to child diseases
- Uses existing `PathIds` field to walk hierarchy
- Child diseases can override inherited tasks with custom:
  - Instructions
  - Priority
  - Due dates
  - Auto-creation settings

### 2. Flexible Triggering
Tasks can be created:
- Automatically on case creation
- Automatically on contact identification
- Automatically on lab confirmation
- Manually by investigators
- Based on other triggers (symptom onset, exposure recorded)

### 3. Recurring Tasks
Support for recurring tasks (e.g., daily symptom checks):
- Configurable patterns: Daily, Twice Daily, Weekly, Every Other Day
- Duration-based or count-based recurrence
- Parent-child task relationships

### 4. Assignment & Tracking
- Assign tasks to patients, investigators, or anyone
- Track status: Pending, In Progress, Completed, Cancelled, Overdue
- Priority levels: Low, Medium, High, Urgent
- Completion notes and evidence/attachments
- Audit trail of who completed/cancelled tasks

### 5. Smart Due Date Calculation
Due dates calculated from:
- Symptom onset date
- Notification date
- Contact date
- Task creation date

### 6. Future Survey Integration
- TaskTemplate has `SurveyTemplateId` field
- CaseTask has `SurveyResponseId` field
- Ready for questionnaire/survey system integration

## Usage Examples

### Example 1: Assign a Task to a Parent Disease (Cascades to Children)
```csharp
await _taskService.AssignTaskTemplate(
    salmonellaId, 
    foodHistoryQuestionnaireTaskId, 
    applyToChildren: true,      // Apply to all Salmonella subtypes
    allowChildOverride: true    // Allow subtypes to customize
);
```

### Example 2: Child Disease Overrides Instructions
```csharp
await _taskService.CreateChildOverride(
    salmonellaTyphiId,
    foodHistoryQuestionnaireTaskId,
    new TaskTemplateOverride
    {
        CustomInstructions = "Focus on travel-related food and water sources. Salmonella Typhi is primarily travel-acquired.",
        Priority = TaskPriority.Urgent
    }
);
```

### Example 3: Automatically Create Tasks When Case is Created
```csharp
// In Cases/Create.cshtml.cs after saving case
await _taskService.CreateTasksForCase(newCase.Id, TaskTrigger.OnCaseCreation);
```

### Example 4: Get All Tasks for a Case
```csharp
var allTasks = await _taskService.GetTasksForCase(caseId);
var pendingTasks = await _taskService.GetTasksForCase(caseId, CaseTaskStatus.Pending);
```

### Example 5: Complete a Task
```csharp
await _taskService.CompleteTask(
    taskId, 
    "Prophylactic antibiotics administered. Prescription: Rifampicin 600mg x2 days.", 
    currentUserId
);
```

### Example 6: View Task Statistics
```csharp
var stats = await _taskService.GetTaskStatistics(caseId);
// stats.PendingTasks, stats.CompletedTasks, stats.OverdueTasks, stats.UrgentTasks
```

## Next Steps

### Immediate:
1. Run the migration: `dotnet ef database update`
2. Execute the seed script: `SeedTaskTemplateConfiguration.sql`
3. Test task creation on case creation

### Phase 2 - UI Implementation:
1. **Case Details Page - Tasks Tab:**
   - List all tasks for the case
   - Filter by status, category, priority
   - Quick actions: Complete, Cancel, Add Note
   - Timeline view

2. **Dashboard Widgets:**
   - "My Tasks" - tasks assigned to current user
   - "Overdue Tasks" - system-wide overdue tasks
   - "Tasks Due Today"

3. **Task Management Modal:**
   - Create ad-hoc tasks
   - Edit existing tasks
   - Upload evidence/attachments
   - Complete/cancel tasks

4. **Disease Configuration UI:**
   - Manage task templates for each disease
   - View inherited tasks
   - Create child overrides
   - Preview which tasks will be created

### Phase 3 - Advanced Features:
1. **Notifications:**
   - Email/SMS reminders for due tasks
   - Alerts for overdue tasks
   - Notifications to supervisors

2. **Background Jobs:**
   - Auto-mark overdue tasks (daily job)
   - Generate recurring task instances
   - Send reminder notifications

3. **Survey Integration:**
   - Link tasks to survey/questionnaire system
   - Auto-complete task when survey submitted
   - Store survey responses with tasks

4. **Reporting:**
   - Task completion rates by disease
   - Average time to completion
   - Overdue task trends
   - Staff workload analysis

## Database Schema

```sql
TaskTemplates
??? Id (PK)
??? Name
??? Description
??? Category (enum)
??? DefaultPriority (enum)
??? TriggerType (enum)
??? ApplicableToType (Case/Contact)
??? Timing fields (DueDaysFromOnset, etc.)
??? Recurrence fields
??? InheritanceBehavior (enum)
??? Instructions

CaseTasks
??? Id (PK)
??? CaseId (FK)
??? TaskTemplateId (FK)
??? Title
??? Description
??? Status (enum)
??? Priority (enum)
??? AssignedToUserId (FK)
??? DueDate
??? CompletedAt
??? CompletedByUserId (FK)
??? ParentTaskId (FK, for recurrence)
??? RecurrenceSequence

DiseaseTaskTemplates
??? Id (PK)
??? DiseaseId (FK)
??? TaskTemplateId (FK)
??? IsInherited
??? InheritedFromDiseaseId (FK)
??? ApplyToChildren
??? AllowChildOverride
??? Override fields (Priority, Instructions, DueDays)
??? Auto-create settings
```

## Service Registration

Already added to `Program.cs`:
```csharp
builder.Services.AddScoped<Surveillance_MVP.Services.ITaskService, Surveillance_MVP.Services.TaskService>();
```

## Important Notes

1. **Naming Convention:** The enum is named `CaseTaskStatus` (not `TaskStatus`) to avoid conflicts with `System.Threading.Tasks.TaskStatus`.

2. **Hierarchy Logic:** Uses the existing `Disease.PathIds` field to efficiently walk the disease hierarchy without recursive queries.

3. **Soft Delete Compatibility:** CaseTasks don't have soft delete, but they reference Cases which do. Global query filter on Cases is respected.

4. **Performance:** Appropriate indexes added for common queries (status, due date, case ID, user ID).

5. **Extensibility:** Survey fields are placeholders for future integration. Evidence fields use JSON for file IDs.

## Testing Checklist

- [ ] Create a case for Measles ? Verify isolation and contact tracing tasks created
- [ ] Create a contact for Meningococcal ? Verify prophylaxis task created
- [ ] Create a case for Salmonella Enteritidis (child) ? Verify food history task inherited
- [ ] Override task settings for a child disease ? Verify custom instructions used
- [ ] Complete a task ? Verify status, completion date, and user recorded
- [ ] Create recurring task ? Verify instances generated
- [ ] Query tasks by status ? Verify filtering works
- [ ] Check task statistics ? Verify counts accurate

## Files Created/Modified

### New Files:
- `Surveillance-MVP/Models/TaskEnums.cs`
- `Surveillance-MVP/Models/TaskTemplate.cs`
- `Surveillance-MVP/Models/CaseTask.cs`
- `Surveillance-MVP/Models/DiseaseTaskTemplate.cs`
- `Surveillance-MVP/Services/ITaskService.cs`
- `Surveillance-MVP/Services/TaskService.cs`
- `Surveillance-MVP/Migrations/ManualScripts/SeedTaskTemplateConfiguration.sql`
- `Surveillance-MVP/Migrations/[timestamp]_AddTaskManagementSystem.cs`

### Modified Files:
- `Surveillance-MVP/Data/ApplicationDbContext.cs` (added DbSets and configurations)
- `Surveillance-MVP/Program.cs` (registered ITaskService)

## Success Criteria Met

? Task templates with disease-specific configuration
? Hierarchy support with inheritance and overrides
? Automatic task creation based on triggers
? Recurring task support
? Flexible assignment (patient/investigator/anyone)
? Status tracking and completion
? Evidence/attachment support (structure in place)
? Future survey integration (structure in place)
? Comprehensive service layer
? Example seed data for common diseases
? Database migration created
? Service registered in DI container

## Conclusion

The Task Management System is now fully implemented and ready for testing. The system provides a robust foundation for managing public health workflows with strong support for disease hierarchy, flexible configuration, and future extensibility. The next step is to run the migration, seed the data, and begin building the UI components.
