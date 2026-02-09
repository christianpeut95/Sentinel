using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public class TaskTemplateWithSource
    {
        public TaskTemplate Template { get; set; } = null!;
        public DiseaseTaskTemplate Assignment { get; set; } = null!;
        public bool IsInherited { get; set; }
        public string SourceDiseaseName { get; set; } = string.Empty;
        public Guid SourceDiseaseId { get; set; }
        public bool HasLocalOverride { get; set; }
    }

    public class TaskTemplateOverride
    {
        public TaskPriority? Priority { get; set; }
        public int? DueDaysFromOnset { get; set; }
        public string? CustomInstructions { get; set; }
        public bool? AutoCreate { get; set; }
    }

    public class TaskTemplatePreview
    {
        public List<TaskTemplateWithSource> TasksForCases { get; set; } = new();
        public List<TaskTemplateWithSource> TasksForContacts { get; set; } = new();
        public int TotalTaskCount { get; set; }
        public int InheritedTaskCount { get; set; }
        public int DirectTaskCount { get; set; }
    }

    public interface ITaskService
    {
        // === Hierarchy-Aware Task Retrieval ===

        /// <summary>
        /// Gets all applicable task templates for a disease, including inherited from parents
        /// </summary>
        Task<List<TaskTemplateWithSource>> GetApplicableTaskTemplates(Guid diseaseId);

        /// <summary>
        /// Gets task templates defined directly on this disease (not inherited)
        /// </summary>
        Task<List<TaskTemplate>> GetDirectTaskTemplates(Guid diseaseId);

        /// <summary>
        /// Gets task templates inherited from parent diseases
        /// </summary>
        Task<List<TaskTemplateWithSource>> GetInheritedTaskTemplates(Guid diseaseId);

        // === Template Configuration with Hierarchy ===

        /// <summary>
        /// Assigns a task template to a disease with hierarchy options
        /// </summary>
        Task<DiseaseTaskTemplate> AssignTaskTemplate(
            Guid diseaseId,
            Guid taskTemplateId,
            bool applyToChildren = true,
            bool allowChildOverride = true);

        /// <summary>
        /// Propagates task template to all child diseases
        /// </summary>
        Task PropagateTaskTemplateToChildren(Guid diseaseId, Guid taskTemplateId);

        /// <summary>
        /// Creates a child-specific override of an inherited task
        /// </summary>
        Task<DiseaseTaskTemplate> CreateChildOverride(
            Guid childDiseaseId,
            Guid inheritedTaskTemplateId,
            TaskTemplateOverride overrideSettings);

        /// <summary>
        /// Removes task template from disease and optionally from children
        /// </summary>
        Task RemoveTaskTemplate(Guid diseaseId, Guid taskTemplateId, bool removeFromChildren = false);

        // === Task Instance Creation (Hierarchy-Aware) ===

        /// <summary>
        /// Creates task instances for a case based on disease-specific and inherited templates
        /// </summary>
        Task<List<CaseTask>> CreateTasksForCase(Guid caseId, TaskTrigger trigger);

        /// <summary>
        /// Checks which task templates would apply to a disease (preview mode)
        /// </summary>
        Task<TaskTemplatePreview> PreviewTasksForDisease(Guid diseaseId);

        // === Recurring Task Management ===

        /// <summary>
        /// Creates recurring task instances for a parent task
        /// </summary>
        Task<List<CaseTask>> CreateRecurringTaskInstances(CaseTask parentTask);

        /// <summary>
        /// Generates recurring task instances for a specific date (background job)
        /// </summary>
        Task GenerateRecurringTaskInstances(DateTime forDate);

        // === Manual Task Management ===

        /// <summary>
        /// Creates an ad-hoc task not based on a template
        /// </summary>
        Task<CaseTask> CreateAdHocTask(CaseTask task);

        /// <summary>
        /// Updates an existing task
        /// </summary>
        Task<CaseTask> UpdateTask(Guid taskId, CaseTask updatedTask);

        /// <summary>
        /// Marks a task as complete
        /// </summary>
        Task<CaseTask> CompleteTask(Guid taskId, string? completionNotes, string userId);

        /// <summary>
        /// Cancels a task
        /// </summary>
        Task<CaseTask> CancelTask(Guid taskId, string cancellationReason, string userId);

        // === Task Queries ===

        /// <summary>
        /// Gets all tasks for a specific case
        /// </summary>
        Task<List<CaseTask>> GetTasksForCase(Guid caseId, CaseTaskStatus? status = null);

        /// <summary>
        /// Gets tasks assigned to a specific user
        /// </summary>
        Task<List<CaseTask>> GetTasksForUser(string userId, CaseTaskStatus? status = null);

        /// <summary>
        /// Gets all overdue tasks in the system
        /// </summary>
        Task<List<CaseTask>> GetOverdueTasks();

        /// <summary>
        /// Gets tasks due today
        /// </summary>
        Task<List<CaseTask>> GetTasksDueToday();

        /// <summary>
        /// Gets task statistics for a case
        /// </summary>
        Task<TaskStatistics> GetTaskStatistics(Guid caseId);

        // === Batch Operations ===

        /// <summary>
        /// Marks tasks as overdue (background job)
        /// </summary>
        Task MarkTasksOverdue();
    }

    public class TaskStatistics
    {
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int UrgentTasks { get; set; }
    }
}
