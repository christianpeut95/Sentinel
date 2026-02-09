# Task Management System - Quick Start Guide

## Run the Migration

```bash
cd Surveillance-MVP
dotnet ef database update
```

## Seed Example Data

Execute the SQL script:
```sql
-- File: Surveillance-MVP/Migrations/ManualScripts/SeedTaskTemplateConfiguration.sql
```

This creates example tasks for:
- Measles (isolation, contact tracing)
- Meningococcal (prophylactic antibiotics)
- COVID-19 (daily symptom monitoring)
- Tuberculosis (contact investigation)
- Salmonella (food history - cascades to subtypes)
- Legionellosis (water system investigation)
- Pertussis (vulnerable contact identification)

## Usage in Your Code

### When Creating a Case

```csharp
// In Cases/Create.cshtml.cs
public async Task<IActionResult> OnPostAsync()
{
    // ... save case ...
    await _context.SaveChangesAsync();
    
    // Create tasks automatically based on disease configuration
    await _taskService.CreateTasksForCase(Input.CaseId, TaskTrigger.OnCaseCreation);
    
    return RedirectToPage("./Details", new { id = Input.CaseId });
}
```

### When Lab Result Confirms Diagnosis

```csharp
// In LabResults/Create.cshtml.cs or Edit
if (labResult.IsConfirmatory)
{
    await _taskService.CreateTasksForCase(caseId, TaskTrigger.OnLabConfirmation);
}
```

### Get Tasks for Display

```csharp
// In Cases/Details.cshtml.cs
public async Task OnGetAsync(Guid id)
{
    Case = await _context.Cases
        .Include(c => c.Patient)
        .Include(c => c.Disease)
        .FirstOrDefaultAsync(c => c.Id == id);
    
    // Get all tasks for this case
    Tasks = await _taskService.GetTasksForCase(id);
    
    // Or get only pending tasks
    PendingTasks = await _taskService.GetTasksForCase(id, CaseTaskStatus.Pending);
    
    // Get task statistics
    TaskStats = await _taskService.GetTaskStatistics(id);
}
```

### Complete a Task

```csharp
// In your task completion handler
public async Task<IActionResult> OnPostCompleteTaskAsync(Guid taskId, string notes)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    await _taskService.CompleteTask(taskId, notes, userId);
    
    return RedirectToPage();
}
```

### Get My Tasks (Dashboard)

```csharp
// In Dashboard page
public async Task OnGetAsync()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    MyTasks = await _taskService.GetTasksForUser(userId, CaseTaskStatus.Pending);
    TasksDueToday = await _taskService.GetTasksDueToday();
    OverdueTasks = await _taskService.GetOverdueTasks();
}
```

## Configure Tasks for a Disease (Admin)

```csharp
// Assign a task template to a disease
await _taskService.AssignTaskTemplate(
    diseaseId: measlesId,
    taskTemplateId: isolationTaskId,
    applyToChildren: true,      // Apply to child diseases
    allowChildOverride: true    // Let children customize
);

// View all tasks that will apply to a disease
var preview = await _taskService.PreviewTasksForDisease(diseaseId);
Console.WriteLine($"Total tasks: {preview.TotalTaskCount}");
Console.WriteLine($"Inherited: {preview.InheritedTaskCount}");
Console.WriteLine($"Direct: {preview.DirectTaskCount}");

// Create a child override
await _taskService.CreateChildOverride(
    childDiseaseId: salmonellaTyphiId,
    inheritedTaskTemplateId: foodHistoryTaskId,
    new TaskTemplateOverride
    {
        CustomInstructions = "Focus on travel-related food sources",
        Priority = TaskPriority.Urgent,
        DueDaysFromOnset = 5
    }
);
```

## Task Properties Reference

### Task Categories
- **Isolation**: Patient must isolate
- **Medication**: Take medication (e.g., prophylactic antibiotics)
- **Monitoring**: Daily checks (e.g., symptom monitoring)
- **Survey**: Complete questionnaire
- **LabTest**: Get tested
- **Education**: Read educational materials
- **ContactTracing**: Investigator identifies contacts
- **FollowUp**: Schedule appointment

### Task Priorities
- **Low**: Can wait
- **Medium**: Standard priority
- **High**: Important, complete soon
- **Urgent**: Immediate action required (e.g., meningococcal prophylaxis)

### Task Status
- **Pending**: Not started
- **InProgress**: Started but not complete
- **Completed**: Done
- **Cancelled**: No longer needed
- **Overdue**: Past due date
- **WaitingForPatient**: Staff completed their part, waiting on patient

### Task Triggers
- **OnCaseCreation**: Auto-create when case is created
- **OnContactIdentification**: Auto-create when contact is identified
- **OnLabConfirmation**: Auto-create when lab confirms diagnosis
- **OnSymptomOnset**: Auto-create when symptoms recorded
- **OnExposureRecorded**: Auto-create when exposure documented
- **Manual**: Only created manually by investigator

## Common Queries

```csharp
// Get overdue tasks
var overdue = await _taskService.GetOverdueTasks();

// Get tasks due today
var dueToday = await _taskService.GetTasksDueToday();

// Get tasks for a specific user
var userTasks = await _taskService.GetTasksForUser(userId);

// Get task statistics
var stats = await _taskService.GetTaskStatistics(caseId);
// stats.TotalTasks, stats.PendingTasks, stats.CompletedTasks, 
// stats.OverdueTasks, stats.UrgentTasks

// Get all applicable task templates for a disease (including inherited)
var templates = await _taskService.GetApplicableTaskTemplates(diseaseId);
foreach (var t in templates)
{
    Console.WriteLine($"{t.Template.Name} - " +
        $"IsInherited: {t.IsInherited} - " +
        $"Source: {t.SourceDiseaseName}");
}
```

## Background Job (Optional)

Add a background job to mark overdue tasks:

```csharp
// Run daily at midnight
public class TaskMaintenanceJob
{
    private readonly ITaskService _taskService;
    
    public async Task Execute()
    {
        // Mark tasks as overdue
        await _taskService.MarkTasksOverdue();
        
        // Generate recurring task instances (if needed)
        await _taskService.GenerateRecurringTaskInstances(DateTime.Today);
    }
}
```

## Example: Measles Case Workflow

1. **Investigator creates Measles case**
   - System auto-creates "Isolation" task (due 4 days from rash onset)
   - System auto-creates "Urgent Contact Tracing" task (due 1 day from notification)

2. **Investigator assigns isolation task to patient**
   ```csharp
   var task = await _context.CaseTasks.FindAsync(isolationTaskId);
   task.AssignedToUserId = patientUserId; // If patient has portal access
   await _context.SaveChangesAsync();
   ```

3. **Investigator completes contact tracing task**
   ```csharp
   await _taskService.CompleteTask(
       contactTracingTaskId,
       "Identified 15 contacts: 5 household, 7 school, 3 healthcare. All notified.",
       currentUserId
   );
   ```

4. **Patient confirms isolation (if patient portal)**
   ```csharp
   await _taskService.CompleteTask(
       isolationTaskId,
       "Confirmed. Remaining in isolation at home until [date].",
       patientUserId
   );
   ```

5. **View task completion on case details**
   - ? Isolation (Completed by patient on [date])
   - ? Contact Tracing (Completed by investigator on [date])

## Example: Salmonella with Hierarchy

**Scenario**: Food history questionnaire configured on parent "Salmonella" disease

1. **Parent Configuration** (Salmonella)
   - Food History Questionnaire task
   - ApplyToChildren = true
   - Auto-create on case creation

2. **Child Disease** (Salmonella Enteritidis)
   - Automatically inherits Food History Questionnaire
   - Uses parent's instructions
   - No override

3. **Another Child** (Salmonella Typhi)
   - Inherits Food History Questionnaire
   - Has override with custom instructions:
     "Focus on travel-related food and water"
   - Higher priority (Urgent vs High)

4. **When creating cases:**
   - Salmonella Enteritidis case ? Gets standard food history task
   - Salmonella Typhi case ? Gets travel-focused food history task
   - Both triggered automatically on case creation

## Key Points

- ? Tasks are automatically created based on disease configuration
- ? Tasks cascade from parent diseases to children
- ? Children can override task details (instructions, priority, due dates)
- ? Support for recurring tasks (daily symptom checks)
- ? Flexible assignment (patient, investigator, or anyone)
- ? Complete task tracking and audit trail
- ? Ready for survey/questionnaire integration

## Next: Build the UI

Now that the backend is complete, you can build:
1. Task list component for case details page
2. Dashboard widgets for "My Tasks" and "Overdue Tasks"
3. Task completion modal
4. Disease configuration UI for admin
