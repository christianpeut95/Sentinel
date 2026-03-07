using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Text.Json;

namespace Sentinel.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        // === Hierarchy-Aware Task Retrieval ===

        public async Task<List<TaskTemplateWithSource>> GetApplicableTaskTemplates(Guid diseaseId)
        {
            System.Diagnostics.Debug.WriteLine($"?? GetApplicableTaskTemplates called for diseaseId: {diseaseId}");
            
            var disease = await _context.Diseases
                .Include(d => d.ParentDisease)
                .FirstOrDefaultAsync(d => d.Id == diseaseId);

            if (disease == null)
            {
                System.Diagnostics.Debug.WriteLine($"? Disease {diseaseId} not found!");
                return new List<TaskTemplateWithSource>();
            }

            System.Diagnostics.Debug.WriteLine($"? Found disease: {disease.Name}, PathIds: {disease.PathIds ?? "(null)"}");

            var results = new List<TaskTemplateWithSource>();

            // 1. Get tasks directly assigned to this disease
            var directTasks = await _context.DiseaseTaskTemplates
                .Include(dt => dt.TaskTemplate)
                .Include(dt => dt.Disease)
                .Where(dt => dt.DiseaseId == diseaseId
                          && !dt.IsInherited
                          && dt.IsActive
                          && dt.TaskTemplate!.IsActive)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"?? Found {directTasks.Count} DIRECT task templates (not inherited)");

            results.AddRange(directTasks.Select(dt => new TaskTemplateWithSource
            {
                Template = dt.TaskTemplate!,
                Assignment = dt,
                IsInherited = false,
                SourceDiseaseName = disease.Name,
                SourceDiseaseId = disease.Id,
                HasLocalOverride = false
            }));

            // 2. Get inherited tasks from parent hierarchy
            if (!string.IsNullOrEmpty(disease.PathIds))
            {
                var ancestorIds = disease.PathIds
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .Reverse() // Start from immediate parent
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"?? PathIds found {ancestorIds.Count} ancestor IDs");

                foreach (var ancestorId in ancestorIds)
                {
                    System.Diagnostics.Debug.WriteLine($"  Checking ancestor: {ancestorId}");
                    
                    var inheritedTasks = await _context.DiseaseTaskTemplates
                        .Include(dt => dt.TaskTemplate)
                        .Include(dt => dt.Disease)
                        .Where(dt => dt.DiseaseId == ancestorId
                                  && dt.ApplyToChildren
                                  && dt.IsActive
                                  && !dt.IsInherited
                                  && dt.TaskTemplate!.IsActive)
                        .ToListAsync();

                    System.Diagnostics.Debug.WriteLine($"  Found {inheritedTasks.Count} inheritable tasks from ancestor {ancestorId}");

                    foreach (var inheritedTask in inheritedTasks)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Processing inheritable task: {inheritedTask.TaskTemplate!.Name}");
                        
                        // Check if child has already overridden this
                        var childOverride = await _context.DiseaseTaskTemplates
                            .FirstOrDefaultAsync(dt => dt.DiseaseId == diseaseId
                                                    && dt.TaskTemplateId == inheritedTask.TaskTemplateId
                                                    && dt.IsInherited);

                        if (childOverride != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"      ? Child override exists with AutoCreateOnCaseCreation={childOverride.AutoCreateOnCaseCreation}");
                        }

                        // Skip if already added as direct task
                        if (results.Any(r => r.Template.Id == inheritedTask.TaskTemplateId))
                        {
                            System.Diagnostics.Debug.WriteLine($"      ??  SKIPPED - already added as direct task");
                            continue;
                        }

                        // Check RestrictToSubDiseaseIds if specified
                        if (inheritedTask.TaskTemplate!.InheritanceBehavior == TaskInheritanceBehavior.Selective)
                        {
                            if (!string.IsNullOrEmpty(inheritedTask.TaskTemplate.RestrictToSubDiseaseIds))
                            {
                                var allowedIds = JsonSerializer.Deserialize<List<Guid>>(
                                    inheritedTask.TaskTemplate.RestrictToSubDiseaseIds);

                                if (allowedIds != null && !allowedIds.Contains(diseaseId))
                                {
                                    System.Diagnostics.Debug.WriteLine($"      ??  SKIPPED - RestrictToSubDiseaseIds filter");
                                    continue;
                                }
                            }
                        }

                        var assignmentToUse = childOverride ?? inheritedTask;
                        System.Diagnostics.Debug.WriteLine($"      ? ADDING inherited task - Using assignment: {(childOverride != null ? "child override" : "parent")}, AutoCreateOnCaseCreation={assignmentToUse.AutoCreateOnCaseCreation}");

                        results.Add(new TaskTemplateWithSource
                        {
                            Template = inheritedTask.TaskTemplate,
                            Assignment = assignmentToUse,
                            IsInherited = true,
                            SourceDiseaseName = inheritedTask.Disease!.Name,
                            SourceDiseaseId = inheritedTask.DiseaseId,
                            HasLocalOverride = childOverride != null
                        });
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"??  No PathIds set for disease {disease.Name} - can't inherit from parent!");
            }

            System.Diagnostics.Debug.WriteLine($"?? FINAL RESULT: {results.Count} total task templates for disease {disease.Name}");
            foreach (var r in results)
            {
                System.Diagnostics.Debug.WriteLine($"  - {r.Template.Name}: IsInherited={r.IsInherited}, AutoCreateOnCaseCreation={r.Assignment.AutoCreateOnCaseCreation}");
            }

            return results.OrderBy(t => t.Assignment.DisplayOrder).ToList();
        }

        public async Task<List<TaskTemplate>> GetDirectTaskTemplates(Guid diseaseId)
        {
            return await _context.DiseaseTaskTemplates
                .Include(dt => dt.TaskTemplate)
                .Where(dt => dt.DiseaseId == diseaseId && !dt.IsInherited && dt.IsActive)
                .Select(dt => dt.TaskTemplate!)
                .ToListAsync();
        }

        public async Task<List<TaskTemplateWithSource>> GetInheritedTaskTemplates(Guid diseaseId)
        {
            var allTemplates = await GetApplicableTaskTemplates(diseaseId);
            return allTemplates.Where(t => t.IsInherited).ToList();
        }

        // === Template Configuration with Hierarchy ===

        public async Task<DiseaseTaskTemplate> AssignTaskTemplate(
            Guid diseaseId,
            Guid taskTemplateId,
            bool applyToChildren = true,
            bool allowChildOverride = true)
        {
            var assignment = new DiseaseTaskTemplate
            {
                Id = Guid.NewGuid(),
                DiseaseId = diseaseId,
                TaskTemplateId = taskTemplateId,
                ApplyToChildren = applyToChildren,
                AllowChildOverride = allowChildOverride,
                IsInherited = false,
                AutoCreateOnCaseCreation = true,
                AutoCreateOnContactCreation = false,
                AutoCreateOnLabConfirmation = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.DiseaseTaskTemplates.Add(assignment);
            await _context.SaveChangesAsync();

            // Propagate to children if requested
            if (applyToChildren)
            {
                await PropagateTaskTemplateToChildren(diseaseId, taskTemplateId);
            }

            return assignment;
        }

        public async Task PropagateTaskTemplateToChildren(Guid diseaseId, Guid taskTemplateId)
        {
            var parentAssignment = await _context.DiseaseTaskTemplates
                .FirstOrDefaultAsync(dt => dt.DiseaseId == diseaseId
                                        && dt.TaskTemplateId == taskTemplateId);

            if (parentAssignment == null || !parentAssignment.ApplyToChildren)
                return;

            // Get all child diseases
            var children = await _context.Diseases
                .Where(d => d.PathIds.Contains($"/{diseaseId}/"))
                .ToListAsync();

            foreach (var child in children)
            {
                // Check if child already has this task (direct or inherited)
                var existing = await _context.DiseaseTaskTemplates
                    .FirstOrDefaultAsync(dt => dt.DiseaseId == child.Id
                                            && dt.TaskTemplateId == taskTemplateId);

                if (existing == null)
                {
                    // Create inherited assignment - COPY auto-create flags from parent
                    var childAssignment = new DiseaseTaskTemplate
                    {
                        Id = Guid.NewGuid(),
                        DiseaseId = child.Id,
                        TaskTemplateId = taskTemplateId,
                        IsInherited = true,
                        InheritedFromDiseaseId = diseaseId,
                        ApplyToChildren = false,
                        AllowChildOverride = parentAssignment.AllowChildOverride,
                        // ?? CRITICAL: Copy auto-create flags from parent!
                        AutoCreateOnCaseCreation = parentAssignment.AutoCreateOnCaseCreation,
                        AutoCreateOnContactCreation = parentAssignment.AutoCreateOnContactCreation,
                        AutoCreateOnLabConfirmation = parentAssignment.AutoCreateOnLabConfirmation,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.DiseaseTaskTemplates.Add(childAssignment);
                }
                else if (existing.IsInherited && existing.InheritedFromDiseaseId == diseaseId)
                {
                    // ?? NEW: Update existing inherited record to match parent settings
                    existing.AutoCreateOnCaseCreation = parentAssignment.AutoCreateOnCaseCreation;
                    existing.AutoCreateOnContactCreation = parentAssignment.AutoCreateOnContactCreation;
                    existing.AutoCreateOnLabConfirmation = parentAssignment.AutoCreateOnLabConfirmation;
                    existing.AllowChildOverride = parentAssignment.AllowChildOverride;
                    existing.ModifiedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<DiseaseTaskTemplate> CreateChildOverride(
            Guid childDiseaseId,
            Guid inheritedTaskTemplateId,
            TaskTemplateOverride overrideSettings)
        {
            var inheritedAssignment = await _context.DiseaseTaskTemplates
                .FirstOrDefaultAsync(dt => dt.DiseaseId == childDiseaseId
                                        && dt.TaskTemplateId == inheritedTaskTemplateId
                                        && dt.IsInherited);

            if (inheritedAssignment == null)
                throw new InvalidOperationException("Inherited task assignment not found");

            inheritedAssignment.OverridePriority = overrideSettings.Priority;
            inheritedAssignment.OverrideDueDays = overrideSettings.DueDaysFromOnset;
            inheritedAssignment.OverrideInstructions = overrideSettings.CustomInstructions;
            inheritedAssignment.OverrideAutoCreate = overrideSettings.AutoCreate;
            inheritedAssignment.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return inheritedAssignment;
        }

        public async Task RemoveTaskTemplate(Guid diseaseId, Guid taskTemplateId, bool removeFromChildren = false)
        {
            var assignment = await _context.DiseaseTaskTemplates
                .FirstOrDefaultAsync(dt => dt.DiseaseId == diseaseId && dt.TaskTemplateId == taskTemplateId);

            if (assignment != null)
            {
                _context.DiseaseTaskTemplates.Remove(assignment);

                if (removeFromChildren && assignment.ApplyToChildren)
                {
                    var childAssignments = await _context.DiseaseTaskTemplates
                        .Where(dt => dt.TaskTemplateId == taskTemplateId
                                  && dt.IsInherited
                                  && dt.InheritedFromDiseaseId == diseaseId)
                        .ToListAsync();

                    _context.DiseaseTaskTemplates.RemoveRange(childAssignments);
                }

                await _context.SaveChangesAsync();
            }
        }

        // === Task Instance Creation ===

        public async Task<List<CaseTask>> CreateTasksForCase(Guid caseId, TaskTrigger trigger)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseEntity?.DiseaseId == null)
                return new List<CaseTask>();

            // Get all applicable templates (including inherited)
            var applicableTemplates = await GetApplicableTaskTemplates(caseEntity.DiseaseId.Value);

            // Filter by trigger and case type
            var templatesToCreate = applicableTemplates
                .Where(t => t.Template.TriggerType == trigger
                         && (t.Template.ApplicableToType == null
                             || t.Template.ApplicableToType == caseEntity.Type))
                .Where(t => ShouldAutoCreate(t, caseEntity.Type))
                .ToList();

            var createdTasks = new List<CaseTask>();

            foreach (var templateWithSource in templatesToCreate)
            {
                var template = templateWithSource.Template;
                var assignment = templateWithSource.Assignment;

                // Use centralized task creation method
                var task = await CreateTaskFromTemplateAsync(
                    caseId,
                    caseEntity,
                    template,
                    assignment);

                createdTasks.Add(task);

                // Create recurring instances if needed
                if (template.IsRecurring)
                {
                    var recurringTasks = await CreateRecurringTaskInstances(task);
                    createdTasks.AddRange(recurringTasks);
                }
            }

            await _context.SaveChangesAsync();
            return createdTasks;
        }

        public async Task<List<CaseTask>> AutoCreateTasksForNewCase(Guid caseId)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseEntity?.DiseaseId == null)
                return new List<CaseTask>();

            // Get all applicable templates (including inherited)
            var applicableTemplates = await GetApplicableTaskTemplates(caseEntity.DiseaseId.Value);

            // Filter for auto-create templates
            var templatesToCreate = applicableTemplates
                .Where(t => t.Assignment.AutoCreateOnCaseCreation == true)
                .Where(t => t.Template.ApplicableToType == null || t.Template.ApplicableToType == caseEntity.Type)
                .ToList();

            var createdTasks = new List<CaseTask>();

            foreach (var templateWithSource in templatesToCreate)
            {
                var template = templateWithSource.Template;
                var assignment = templateWithSource.Assignment;

                // Use centralized task creation method
                var task = await CreateTaskFromTemplateAsync(
                    caseId,
                    caseEntity,
                    template,
                    assignment);

                createdTasks.Add(task);

                // Create recurring instances if needed
                if (template.IsRecurring)
                {
                    var recurringTasks = await CreateRecurringTaskInstances(task);
                    createdTasks.AddRange(recurringTasks);
                }
            }

            await _context.SaveChangesAsync();
            return createdTasks;
        }

        public async Task<List<CaseTask>> CreateRecurringTaskInstances(CaseTask parentTask)
        {
            var template = await _context.TaskTemplates.FindAsync(parentTask.TaskTemplateId);
            if (template == null || !template.IsRecurring || parentTask.DueDate == null)
                return new List<CaseTask>();

            var instances = new List<CaseTask>();
            int count = template.RecurrenceCount ?? 0;
            int durationDays = template.RecurrenceDurationDays ?? 0;

            if (count == 0 && durationDays == 0)
                return instances;

            DateTime currentDate = parentTask.DueDate.Value;
            int instanceCount = count > 0 ? count : (durationDays / GetRecurrenceIntervalDays(template.RecurrencePattern));

            for (int i = 1; i <= instanceCount; i++)
            {
                currentDate = GetNextRecurrenceDate(currentDate, template.RecurrencePattern);

                var instance = new CaseTask
                {
                    Id = Guid.NewGuid(),
                    CaseId = parentTask.CaseId,
                    TaskTemplateId = parentTask.TaskTemplateId,
                    ParentTaskId = parentTask.Id,
                    RecurrenceSequence = i,
                    Title = $"{parentTask.Title} (Day {i + 1})",
                    Description = parentTask.Description,
                    TaskTypeId = parentTask.TaskTypeId,
                    Priority = parentTask.Priority,
                    AssignmentType = parentTask.AssignmentType,
                    DueDate = currentDate,
                    Status = CaseTaskStatus.Pending,
                    IsInterviewTask = parentTask.IsInterviewTask,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CaseTasks.Add(instance);
                instances.Add(instance);
            }

            await _context.SaveChangesAsync();
            return instances;
        }

        public async Task GenerateRecurringTaskInstances(DateTime forDate)
        {
            // This would be called by a background job
            // Implementation depends on your background job system
            await Task.CompletedTask;
        }

        // === Manual Task Management ===

        public async Task<CaseTask> CreateTaskFromTemplateForCaseAsync(
            Guid caseId, 
            Guid taskTemplateId, 
            string? assignedToUserId = null)
        {
            var caseEntity = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseEntity == null)
                throw new ArgumentException($"Case {caseId} not found");

            var template = await _context.TaskTemplates
                .Include(tt => tt.TaskType)
                .FirstOrDefaultAsync(tt => tt.Id == taskTemplateId);

            if (template == null)
                throw new ArgumentException($"Task template {taskTemplateId} not found");

            // Get disease assignment if exists (for overrides)
            DiseaseTaskTemplate? assignment = null;
            if (caseEntity.DiseaseId.HasValue)
            {
                assignment = await _context.DiseaseTaskTemplates
                    .FirstOrDefaultAsync(dt => dt.DiseaseId == caseEntity.DiseaseId.Value 
                                            && dt.TaskTemplateId == taskTemplateId);
            }

            // Use centralized creation method
            var task = await CreateTaskFromTemplateAsync(
                caseId,
                caseEntity,
                template,
                assignment);

            // Set assigned user if provided
            if (!string.IsNullOrEmpty(assignedToUserId))
            {
                task.AssignedToUserId = assignedToUserId;
            }

            // Create recurring instances if needed
            if (template.IsRecurring)
            {
                await CreateRecurringTaskInstances(task);
            }

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<CaseTask> CreateAdHocTask(CaseTask task)
        {
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            _context.CaseTasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<CaseTask> UpdateTask(Guid taskId, CaseTask updatedTask)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.Priority = updatedTask.Priority;
            task.DueDate = updatedTask.DueDate;
            task.AssignedToUserId = updatedTask.AssignedToUserId;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<CaseTask> CompleteTask(Guid taskId, string? completionNotes, string userId)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            task.Status = CaseTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = userId;
            task.CompletionNotes = completionNotes;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<CaseTask> CancelTask(Guid taskId, string cancellationReason, string userId)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            task.Status = CaseTaskStatus.Cancelled;
            task.CancelledAt = DateTime.UtcNow;
            task.CancellationReason = cancellationReason;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return task;
        }

        // === Task Queries ===

        public async Task<List<CaseTask>> GetTasksForCase(Guid caseId, CaseTaskStatus? status = null)
        {
            var query = _context.CaseTasks
                .Include(t => t.TaskTemplate)
                    .ThenInclude(tt => tt!.TaskType)
                .Include(t => t.TaskTemplate)
                    .ThenInclude(tt => tt!.SurveyTemplate) // Include survey template
                .Include(t => t.TaskType) // For manual tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CompletedByUser)
                .Include(t => t.CallAttempts) // Include call attempts
                .Where(t => t.CaseId == caseId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            return await query.OrderBy(t => t.DueDate).ToListAsync();
        }

        public async Task<List<CaseTask>> GetTasksForUser(string userId, CaseTaskStatus? status = null)
        {
            var query = _context.CaseTasks
                .Include(t => t.TaskTemplate)
                    .ThenInclude(tt => tt!.TaskType)
                .Include(t => t.TaskType)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.CallAttempts) // Include call attempts
                .Where(t => t.AssignedToUserId == userId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            return await query.OrderBy(t => t.DueDate).ToListAsync();
        }

        public async Task<List<CaseTask>> GetOverdueTasks()
        {
            return await _context.CaseTasks
                .Include(t => t.Case)
                .ThenInclude(c => c!.Patient)
                .Where(t => t.Status == CaseTaskStatus.Pending
                         && t.DueDate.HasValue
                         && t.DueDate.Value.Date < DateTime.UtcNow.Date)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<List<CaseTask>> GetTasksDueToday()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.CaseTasks
                .Include(t => t.Case)
                .ThenInclude(c => c!.Patient)
                .Where(t => t.Status == CaseTaskStatus.Pending
                         && t.DueDate.HasValue
                         && t.DueDate.Value.Date == today)
                .OrderBy(t => t.Priority)
                .ToListAsync();
        }

        public async Task<TaskStatistics> GetTaskStatistics(Guid caseId)
        {
            var tasks = await _context.CaseTasks
                .Where(t => t.CaseId == caseId)
                .ToListAsync();

            return new TaskStatistics
            {
                TotalTasks = tasks.Count,
                PendingTasks = tasks.Count(t => t.Status == CaseTaskStatus.Pending),
                CompletedTasks = tasks.Count(t => t.Status == CaseTaskStatus.Completed),
                OverdueTasks = tasks.Count(t => t.Status == CaseTaskStatus.Overdue),
                UrgentTasks = tasks.Count(t => t.Priority == TaskPriority.Urgent && t.Status == CaseTaskStatus.Pending)
            };
        }

        public async Task<TaskTemplatePreview> PreviewTasksForDisease(Guid diseaseId)
        {
            var allTemplates = await GetApplicableTaskTemplates(diseaseId);

            return new TaskTemplatePreview
            {
                TasksForCases = allTemplates.Where(t => t.Template.ApplicableToType == null || t.Template.ApplicableToType == CaseType.Case).ToList(),
                TasksForContacts = allTemplates.Where(t => t.Template.ApplicableToType == null || t.Template.ApplicableToType == CaseType.Contact).ToList(),
                TotalTaskCount = allTemplates.Count,
                InheritedTaskCount = allTemplates.Count(t => t.IsInherited),
                DirectTaskCount = allTemplates.Count(t => !t.IsInherited)
            };
        }

        // === Batch Operations ===

        public async Task MarkTasksOverdue()
        {
            var overdueTasks = await _context.CaseTasks
                .Where(t => t.Status == CaseTaskStatus.Pending
                         && t.DueDate.HasValue
                         && t.DueDate.Value.Date < DateTime.UtcNow.Date)
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                task.Status = CaseTaskStatus.Overdue;
                task.ModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // === Helper Methods ===

        /// <summary>
        /// Centralized method for creating a task from a template
        /// This eliminates duplication across auto-create, manual, and trigger-based creation
        /// </summary>
        private async Task<CaseTask> CreateTaskFromTemplateAsync(
            Guid caseId,
            Case caseEntity,
            TaskTemplate template,
            DiseaseTaskTemplate? assignment = null,
            TaskPriority? overridePriority = null,
            int? overrideDueDays = null,
            string? overrideInstructions = null)
        {
            // Calculate due date with exposure support
            int? dueDays = overrideDueDays ?? assignment?.OverrideDueDays ?? template.DueDaysFromOnset;
            DateTime? dueDate = await CalculateDueDateWithExposure(
                caseId,
                dueDays,
                caseEntity.DateOfOnset,
                caseEntity.DateOfNotification,
                template.DueCalculationMethod);

            // Ensure TaskType navigation property is loaded for IsInterviewTask inheritance
            if (template.TaskType == null && template.TaskTypeId != Guid.Empty)
            {
                await _context.Entry(template).Reference(t => t.TaskType).LoadAsync();
            }
            bool isInterviewTask = template.IsInterviewTask || (template.TaskType?.IsInterviewTask ?? false);

            // Determine final values (prefer manual overrides, then assignment overrides, then template defaults)
            var finalPriority = overridePriority 
                ?? assignment?.OverridePriority 
                ?? template.DefaultPriority;
            
            var finalInstructions = overrideInstructions 
                ?? assignment?.OverrideInstructions 
                ?? template.Description;

            var task = new CaseTask
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                TaskTemplateId = template.Id,
                Title = template.Name,
                Description = finalInstructions,
                TaskTypeId = template.TaskTypeId,
                Priority = finalPriority,
                AssignmentType = template.AssignmentType,
                DueDate = dueDate,
                Status = CaseTaskStatus.Pending,
                IsInterviewTask = isInterviewTask,
                CreatedAt = DateTime.UtcNow
            };

            _context.CaseTasks.Add(task);
            
            return task;
        }

        private bool ShouldAutoCreate(TaskTemplateWithSource templateWithSource, CaseType caseType)
        {
            var assignment = templateWithSource.Assignment;

            if (caseType == CaseType.Case)
            {
                return assignment.OverrideAutoCreate
                    ?? assignment.AutoCreateOnCaseCreation;
            }
            else if (caseType == CaseType.Contact)
            {
                return assignment.OverrideAutoCreate
                    ?? assignment.AutoCreateOnContactCreation;
            }

            return false;
        }

        private DateTime? CalculateDueDate(
            int? dueDays,
            DateTime? onsetDate,
            DateTime? notificationDate,
            TaskDueCalculationMethod method)
        {
            if (!dueDays.HasValue)
                return null;

            DateTime? baseDate = method switch
            {
                TaskDueCalculationMethod.FromSymptomOnset => onsetDate,
                TaskDueCalculationMethod.FromNotificationDate => notificationDate,
                TaskDueCalculationMethod.FromTaskCreation => DateTime.UtcNow,
                _ => null
            };

            return baseDate?.AddDays(dueDays.Value);
        }

        private async Task<DateTime?> CalculateDueDateWithExposure(
            Guid caseId,
            int? dueDays,
            DateTime? onsetDate,
            DateTime? notificationDate,
            TaskDueCalculationMethod method)
        {
            if (!dueDays.HasValue)
                return null;

            DateTime? baseDate = null;

            switch (method)
            {
                case TaskDueCalculationMethod.FromSymptomOnset:
                    baseDate = onsetDate;
                    break;
                case TaskDueCalculationMethod.FromNotificationDate:
                    baseDate = notificationDate;
                    break;
                case TaskDueCalculationMethod.FromTaskCreation:
                    baseDate = DateTime.UtcNow;
                    break;
                case TaskDueCalculationMethod.FromEarliestExposure:
                    baseDate = await GetEarliestExposureDate(caseId);
                    break;
                case TaskDueCalculationMethod.FromLatestExposure:
                    baseDate = await GetLatestExposureDate(caseId);
                    break;
                default:
                    baseDate = null;
                    break;
            }

            return baseDate?.AddDays(dueDays.Value);
        }

        private async Task<DateTime?> GetEarliestExposureDate(Guid caseId)
        {
            var exposures = await _context.ExposureEvents
                .Where(e => e.ExposedCaseId == caseId && !e.IsDeleted)
                .ToListAsync();

            if (!exposures.Any())
                return null;

            return exposures.Min(e => e.ExposureStartDate);
        }

        private async Task<DateTime?> GetLatestExposureDate(Guid caseId)
        {
            var exposures = await _context.ExposureEvents
                .Where(e => e.ExposedCaseId == caseId && !e.IsDeleted)
                .ToListAsync();

            if (!exposures.Any())
                return null;

            return exposures.Max(e => e.ExposureEndDate ?? e.ExposureStartDate);
        }

        private int GetRecurrenceIntervalDays(RecurrencePattern? pattern)
        {
            return pattern switch
            {
                RecurrencePattern.Daily => 1,
                RecurrencePattern.TwiceDaily => 1,
                RecurrencePattern.EveryOtherDay => 2,
                RecurrencePattern.Weekly => 7,
                _ => 1
            };
        }

        private DateTime GetNextRecurrenceDate(DateTime currentDate, RecurrencePattern? pattern)
        {
            return pattern switch
            {
                RecurrencePattern.Daily => currentDate.AddDays(1),
                RecurrencePattern.TwiceDaily => currentDate.AddHours(12),
                RecurrencePattern.EveryOtherDay => currentDate.AddDays(2),
                RecurrencePattern.Weekly => currentDate.AddDays(7),
                _ => currentDate.AddDays(1)
            };
        }
    }
}
