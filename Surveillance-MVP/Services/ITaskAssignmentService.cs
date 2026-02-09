using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public interface ITaskAssignmentService
    {
        /// <summary>
        /// Auto-assigns the next available interview task to a user using round-robin
        /// </summary>
        Task<CaseTask?> AssignNextTaskAsync(string userId);

        /// <summary>
        /// Auto-assigns a specific task to the next available worker
        /// </summary>
        Task<bool> AutoAssignTaskAsync(Guid taskId, TaskAssignmentMethod method = TaskAssignmentMethod.AutoRoundRobin);

        /// <summary>
        /// Manually assigns a task to a specific user (supervisor assignment)
        /// </summary>
        Task<bool> ManuallyAssignTaskAsync(Guid taskId, string userId, string assignedByUserId);

        /// <summary>
        /// Reassigns a task from one user to another or back to the pool
        /// </summary>
        Task<bool> ReassignTaskAsync(Guid taskId, string? newUserId, string reassignedByUserId, string reason);

        /// <summary>
        /// Escalates a task after max attempts reached
        /// </summary>
        Task<bool> EscalateTaskAsync(Guid taskId, string reason);

        /// <summary>
        /// Gets unassigned interview tasks in queue
        /// </summary>
        Task<List<CaseTask>> GetUnassignedInterviewTasksAsync();

        /// <summary>
        /// Gets assigned tasks for an interview worker
        /// </summary>
        Task<List<CaseTask>> GetAssignedTasksForWorkerAsync(string userId);

        /// <summary>
        /// Gets all currently assigned interview tasks (for supervisor view)
        /// </summary>
        Task<List<CaseTask>> GetAllAssignedInterviewTasksAsync();

        /// <summary>
        /// Gets available interview workers (with language filter optional)
        /// </summary>
        Task<List<ApplicationUser>> GetAvailableWorkersAsync(string? languageRequired = null);

        /// <summary>
        /// Logs a call attempt for a task
        /// </summary>
        Task<TaskCallAttempt> LogCallAttemptAsync(Guid taskId, string userId, CallOutcome outcome, string? notes = null, int? durationSeconds = null, DateTime? nextCallback = null);

        /// <summary>
        /// Gets call attempt history for a task
        /// </summary>
        Task<List<TaskCallAttempt>> GetCallAttemptsAsync(Guid taskId);

        /// <summary>
        /// Gets worker statistics
        /// </summary>
        Task<WorkerStatistics> GetWorkerStatisticsAsync(string userId, DateTime? fromDate = null);

        /// <summary>
        /// Gets supervisor dashboard data
        /// </summary>
        Task<SupervisorDashboardData> GetSupervisorDashboardAsync();

        /// <summary>
        /// Sets worker availability status
        /// </summary>
        Task<bool> SetWorkerAvailabilityAsync(string userId, bool available);
        
        /// <summary>
        /// Gets paginated assigned interview tasks with filters (optimized for hundreds of tasks)
        /// </summary>
        Task<(List<CaseTask> Tasks, int TotalCount)> GetAssignedInterviewTasksPaginatedAsync(
            int pageNumber,
            int pageSize,
            string? workerId = null,
            string? priority = null,
            string? searchTerm = null,
            string? sortBy = "Priority",
            string? sortOrder = "asc");
        
        /// <summary>
        /// Gets lightweight supervisor dashboard summary (counts only, no full task lists)
        /// </summary>
        Task<SupervisorDashboardData> GetSupervisorDashboardSummaryAsync();
        
        /// <summary>
        /// Gets all interview workers for dropdowns
        /// </summary>
        Task<List<ApplicationUser>> GetAllInterviewWorkersAsync();
    }

    public class WorkerStatistics
    {
        public string UserId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public int CallsToday { get; set; }
        public int SuccessfulCallsToday { get; set; }
        public double CompletionRate { get; set; }
        public double AverageDurationSeconds { get; set; }
        public List<string> LanguagesSpoken { get; set; } = new();
        public bool IsAvailable { get; set; }
    }

    public class SupervisorDashboardData
    {
        public int UnassignedTaskCount { get; set; }
        public int EscalatedTaskCount { get; set; }
        public int ActiveWorkerCount { get; set; }
        public int TotalTasksToday { get; set; }
        public int CompletedTasksToday { get; set; }
        public List<WorkerStatistics> WorkerStats { get; set; } = new();
        public List<CaseTask> EscalatedTasks { get; set; } = new();
        public List<CaseTask> UnassignedTasks { get; set; } = new();
        public Dictionary<string, int> LanguageCoverage { get; set; } = new();
    }
}
