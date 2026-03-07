using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Text.Json;

namespace Sentinel.Services
{
    public class TaskAssignmentService : ITaskAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskAssignmentService> _logger;

        public TaskAssignmentService(
            ApplicationDbContext context,
            ILogger<TaskAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CaseTask?> AssignNextTaskAsync(string userId)
        {
            var worker = await _context.Users.FindAsync(userId);
            if (worker == null || !worker.AvailableForAutoAssignment)
            {
                _logger.LogWarning("User {UserId} not available for auto-assignment", userId);
                return null;
            }

            var currentAssignedCount = await _context.CaseTasks
                .CountAsync(t => t.AssignedToUserId == userId && 
                                (t.Status == CaseTaskStatus.Pending ||
                                 t.Status == CaseTaskStatus.InProgress ||
                                 t.Status == CaseTaskStatus.WaitingForPatient));

            if (currentAssignedCount >= worker.CurrentTaskCapacity)
            {
                _logger.LogInformation("User {UserId} at capacity ({CurrentCount}/{MaxCapacity})", 
                    userId, currentAssignedCount, worker.CurrentTaskCapacity);
                return null;
            }

            var unassignedTask = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId == null && 
                           t.Status == CaseTaskStatus.Pending)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (unassignedTask != null)
            {
                unassignedTask.AssignedToUserId = userId;
                unassignedTask.AssignmentMethod = TaskAssignmentMethod.AutoRoundRobin;
                unassignedTask.AutoAssignedAt = DateTime.UtcNow;
                unassignedTask.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Auto-assigned task {TaskId} to user {UserId}", unassignedTask.Id, userId);
                
                return unassignedTask;
            }

            return null;
        }

        public async Task<bool> AutoAssignTaskAsync(Guid taskId, TaskAssignmentMethod method = TaskAssignmentMethod.AutoRoundRobin)
        {
            var task = await _context.CaseTasks
                .Include(t => t.Case)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null || task.AssignedToUserId != null)
            {
                return false;
            }

            ApplicationUser? selectedWorker = null;

            if (method == TaskAssignmentMethod.AutoLanguageMatch && !string.IsNullOrEmpty(task.LanguageRequired))
            {
                var workers = await GetAvailableWorkersAsync(task.LanguageRequired);
                selectedWorker = workers.OrderBy(w => _context.CaseTasks
                    .Count(t => t.AssignedToUserId == w.Id && 
                               (t.Status == CaseTaskStatus.Pending ||
                                t.Status == CaseTaskStatus.InProgress ||
                                t.Status == CaseTaskStatus.WaitingForPatient)))
                    .FirstOrDefault();
            }
            else
            {
                var workers = await GetAvailableWorkersAsync();
                selectedWorker = workers.OrderBy(w => _context.CaseTasks
                    .Count(t => t.AssignedToUserId == w.Id && 
                               (t.Status == CaseTaskStatus.Pending ||
                                t.Status == CaseTaskStatus.InProgress ||
                                t.Status == CaseTaskStatus.WaitingForPatient)))
                    .FirstOrDefault();
            }

            if (selectedWorker == null)
            {
                _logger.LogWarning("No available workers to assign task {TaskId}", taskId);
                return false;
            }

            task.AssignedToUserId = selectedWorker.Id;
            task.AssignmentMethod = method;
            task.AutoAssignedAt = DateTime.UtcNow;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Auto-assigned task {TaskId} to user {UserId} using {Method}", 
                taskId, selectedWorker.Id, method);

            return true;
        }

        public async Task<bool> ManuallyAssignTaskAsync(Guid taskId, string userId, string assignedByUserId)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            task.AssignedToUserId = userId;
            task.AssignmentMethod = TaskAssignmentMethod.SupervisorAssignment;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} manually assigned to {UserId} by {SupervisorId}", 
                taskId, userId, assignedByUserId);

            return true;
        }

        public async Task<bool> ReassignTaskAsync(Guid taskId, string? newUserId, string reassignedByUserId, string reason)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
            {
                return false;
            }

            var oldUserId = task.AssignedToUserId;
            task.AssignedToUserId = newUserId;
            task.ModifiedAt = DateTime.UtcNow;

            if (newUserId == null)
            {
                task.Status = CaseTaskStatus.Pending;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} reassigned from {OldUser} to {NewUser} by {SupervisorId}. Reason: {Reason}",
                taskId, oldUserId ?? "unassigned", newUserId ?? "unassigned", reassignedByUserId, reason);

            return true;
        }

        public async Task<bool> EscalateTaskAsync(Guid taskId, string reason)
        {
            var task = await _context.CaseTasks.FindAsync(taskId);
            if (task == null)
            {
                return false;
            }

            task.EscalationLevel++;
            task.Priority = TaskPriority.High;
            task.AssignedToUserId = null;
            task.Status = CaseTaskStatus.Pending;
            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogWarning("Task {TaskId} escalated to level {Level}. Reason: {Reason}", 
                taskId, task.EscalationLevel, reason);

            return true;
        }

        public async Task<List<CaseTask>> GetUnassignedInterviewTasksAsync()
        {
            return await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Disease)
                .Include(t => t.TaskType)
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId == null && 
                           t.Status == CaseTaskStatus.Pending)
                .OrderBy(t => t.EscalationLevel)
                .ThenBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CaseTask>> GetAssignedTasksForWorkerAsync(string userId)
        {
            return await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Disease)
                .Include(t => t.TaskType)
                .Include(t => t.TaskTemplate)
                .Include(t => t.CallAttempts)
                .Where(t => t.AssignedToUserId == userId && 
                           (t.Status == CaseTaskStatus.Pending || 
                            t.Status == CaseTaskStatus.InProgress ||
                            t.Status == CaseTaskStatus.WaitingForPatient))
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<List<CaseTask>> GetAllAssignedInterviewTasksAsync()
        {
            _logger.LogInformation("GetAllAssignedInterviewTasksAsync called");
            
            var tasks = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Disease)
                .Include(t => t.TaskType)
                .Include(t => t.AssignedToUser)
                .Include(t => t.CallAttempts)
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId != null &&
                           (t.Status == CaseTaskStatus.Pending || 
                            t.Status == CaseTaskStatus.InProgress ||
                            t.Status == CaseTaskStatus.WaitingForPatient))
                .OrderBy(t => t.AssignedToUser!.FirstName)
                .ThenBy(t => t.Priority)
                .ToListAsync();
            
            _logger.LogInformation("Found {Count} assigned interview tasks", tasks.Count);
            
            return tasks;
        }

        public async Task<List<ApplicationUser>> GetAvailableWorkersAsync(string? languageRequired = null)
        {
            var query = _context.Users
                .Where(u => u.IsInterviewWorker && u.AvailableForAutoAssignment);

            if (!string.IsNullOrEmpty(languageRequired))
            {
                query = query.Where(u => u.PrimaryLanguage == languageRequired || 
                                        (u.LanguagesSpokenJson != null && u.LanguagesSpokenJson.Contains(languageRequired)));
            }

            var workers = await query.ToListAsync();

            var workersWithCapacity = new List<ApplicationUser>();
            foreach (var worker in workers)
            {
                var assignedCount = await _context.CaseTasks
                    .CountAsync(t => t.AssignedToUserId == worker.Id && 
                                    (t.Status == CaseTaskStatus.Pending ||
                                     t.Status == CaseTaskStatus.InProgress ||
                                     t.Status == CaseTaskStatus.WaitingForPatient));

                if (assignedCount < worker.CurrentTaskCapacity)
                {
                    workersWithCapacity.Add(worker);
                }
            }

            return workersWithCapacity;
        }

        public async Task<TaskCallAttempt> LogCallAttemptAsync(
            Guid taskId, 
            string userId, 
            CallOutcome outcome, 
            string? notes = null, 
            int? durationSeconds = null, 
            DateTime? nextCallback = null)
        {
            var task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                throw new InvalidOperationException($"Task {taskId} not found");
            }

            var attempt = new TaskCallAttempt
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                AttemptedByUserId = userId,
                AttemptedAt = DateTime.UtcNow,
                Outcome = outcome,
                Notes = notes,
                DurationSeconds = durationSeconds,
                NextCallbackScheduled = nextCallback,
                PhoneNumberCalled = task.Case?.Patient?.MobilePhone ?? task.Case?.Patient?.HomePhone
            };

            _context.TaskCallAttempts.Add(attempt);

            task.CurrentAttemptCount++;
            task.LastCallAttempt = DateTime.UtcNow;

            if (outcome == CallOutcome.Completed)
            {
                task.Status = CaseTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = userId;
            }
            else if (task.CurrentAttemptCount >= task.MaxCallAttempts)
            {
                await EscalateTaskAsync(taskId, $"Max attempts ({task.MaxCallAttempts}) reached. Last outcome: {outcome}");
            }
            else if (outcome == CallOutcome.CallBackRequested && nextCallback.HasValue)
            {
                task.Status = CaseTaskStatus.WaitingForPatient;
            }
            else
            {
                task.Status = CaseTaskStatus.InProgress;
            }

            task.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Call attempt logged for task {TaskId} by {UserId}. Outcome: {Outcome}", 
                taskId, userId, outcome);

            return attempt;
        }

        public async Task<List<TaskCallAttempt>> GetCallAttemptsAsync(Guid taskId)
        {
            return await _context.TaskCallAttempts
                .Include(a => a.AttemptedByUser)
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.AttemptedAt)
                .ToListAsync();
        }

        public async Task<WorkerStatistics> GetWorkerStatisticsAsync(string userId, DateTime? fromDate = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            var startDate = fromDate ?? DateTime.UtcNow.Date;

            var assignedTasks = await _context.CaseTasks
                .Where(t => t.AssignedToUserId == userId)
                .ToListAsync();

            var todaysCalls = await _context.TaskCallAttempts
                .Where(a => a.AttemptedByUserId == userId && a.AttemptedAt >= startDate)
                .ToListAsync();

            var languages = new List<string>();
            if (!string.IsNullOrEmpty(user.PrimaryLanguage))
            {
                languages.Add(user.PrimaryLanguage);
            }
            if (!string.IsNullOrEmpty(user.LanguagesSpokenJson))
            {
                try
                {
                    var additionalLanguages = JsonSerializer.Deserialize<List<string>>(user.LanguagesSpokenJson);
                    if (additionalLanguages != null)
                    {
                        languages.AddRange(additionalLanguages);
                    }
                }
                catch { }
            }

            var completedTasks = assignedTasks.Count(t => t.Status == CaseTaskStatus.Completed);
            var totalTasks = assignedTasks.Count;

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = user.Email ?? "Unknown User";
            }

            return new WorkerStatistics
            {
                UserId = userId,
                WorkerName = displayName,
                TasksAssigned = totalTasks,
                TasksCompleted = completedTasks,
                TasksInProgress = assignedTasks.Count(t => 
                    t.Status == CaseTaskStatus.InProgress || 
                    t.Status == CaseTaskStatus.Pending ||
                    t.Status == CaseTaskStatus.WaitingForPatient),
                CallsToday = todaysCalls.Count,
                SuccessfulCallsToday = todaysCalls.Count(c => c.Outcome == CallOutcome.Completed),
                CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0,
                AverageDurationSeconds = todaysCalls.Where(c => c.DurationSeconds.HasValue)
                    .Select(c => c.DurationSeconds!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                LanguagesSpoken = languages.Distinct().ToList(),
                IsAvailable = user.AvailableForAutoAssignment
            };
        }

        public async Task<SupervisorDashboardData> GetSupervisorDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;

            var unassignedTasks = await GetUnassignedInterviewTasksAsync();
            var escalatedTasks = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Disease)
                .Where(t => t.IsInterviewTask && t.EscalationLevel > 0)
                .OrderByDescending(t => t.EscalationLevel)
                .ToListAsync();

            var workers = await _context.Users
                .Where(u => u.IsInterviewWorker)
                .ToListAsync();

            var workerStats = new List<WorkerStatistics>();
            foreach (var worker in workers)
            {
                workerStats.Add(await GetWorkerStatisticsAsync(worker.Id, today));
            }

            var todaysTasks = await _context.CaseTasks
                .Where(t => t.IsInterviewTask && t.CreatedAt >= today)
                .ToListAsync();

            var languageCoverage = new Dictionary<string, int>();
            foreach (var worker in workers)
            {
                if (!string.IsNullOrEmpty(worker.PrimaryLanguage))
                {
                    if (!languageCoverage.ContainsKey(worker.PrimaryLanguage))
                    {
                        languageCoverage[worker.PrimaryLanguage] = 0;
                    }
                    languageCoverage[worker.PrimaryLanguage]++;
                }

                if (!string.IsNullOrEmpty(worker.LanguagesSpokenJson))
                {
                    try
                    {
                        var languages = JsonSerializer.Deserialize<List<string>>(worker.LanguagesSpokenJson);
                        if (languages != null)
                        {
                            foreach (var lang in languages)
                            {
                                if (!languageCoverage.ContainsKey(lang))
                                {
                                    languageCoverage[lang] = 0;
                                }
                                languageCoverage[lang]++;
                            }
                        }
                    }
                    catch { }
                }
            }

            return new SupervisorDashboardData
            {
                UnassignedTaskCount = unassignedTasks.Count,
                EscalatedTaskCount = escalatedTasks.Count,
                ActiveWorkerCount = workers.Count(w => w.AvailableForAutoAssignment),
                TotalTasksToday = todaysTasks.Count,
                CompletedTasksToday = todaysTasks.Count(t => t.Status == CaseTaskStatus.Completed),
                WorkerStats = workerStats,
                EscalatedTasks = escalatedTasks,
                UnassignedTasks = unassignedTasks,
                LanguageCoverage = languageCoverage
            };
        }

        public async Task<bool> SetWorkerAvailabilityAsync(string userId, bool available)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.AvailableForAutoAssignment = available;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Worker {UserId} availability set to {Available}", userId, available);
            return true;
        }

        public async Task<bool> SkipTaskAsync(Guid taskId, string userId)
        {
            var task = await _context.CaseTasks
                .FirstOrDefaultAsync(t => t.Id == taskId && t.AssignedToUserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found or not assigned to user {UserId}", taskId, userId);
                return false;
            }

            // Unassign the task and put it back in the pool
            task.AssignedToUserId = null;
            task.AutoAssignedAt = null;
            task.AssignmentMethod = TaskAssignmentMethod.Manual;
            task.ModifiedAt = DateTime.UtcNow;

            // Log the skip action
            _logger.LogInformation("User {UserId} skipped task {TaskId}. Task returned to pool.", userId, taskId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(List<CaseTask> Tasks, int TotalCount)> GetAssignedInterviewTasksPaginatedAsync(
            int pageNumber,
            int pageSize,
            string? workerId = null,
            string? priority = null,
            string? searchTerm = null,
            string? sortBy = "Priority",
            string? sortOrder = "asc")
        {
            _logger.LogInformation(
                "GetAssignedInterviewTasksPaginatedAsync: Page {Page}, Size {Size}, Worker {Worker}, Priority {Priority}, Search {Search}", 
                pageNumber, pageSize, workerId, priority, searchTerm);
            
            // Base query with optimized includes
            var query = _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Include(t => t.AssignedToUser)
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId != null &&
                           (t.Status == CaseTaskStatus.Pending || 
                            t.Status == CaseTaskStatus.InProgress ||
                            t.Status == CaseTaskStatus.WaitingForPatient))
                .AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(workerId))
            {
                query = query.Where(t => t.AssignedToUserId == workerId);
            }
            
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var priorityEnum))
            {
                query = query.Where(t => t.Priority == priorityEnum);
            }
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(t => 
                    t.Title.ToLower().Contains(searchLower) ||
                    (t.Case!.Patient!.GivenName + " " + t.Case.Patient.FamilyName).ToLower().Contains(searchLower));
            }
            
            // Get total count before pagination
            var totalCount = await query.CountAsync();
            
            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "priority" => sortOrder == "desc" 
                    ? query.OrderByDescending(t => t.Priority) 
                    : query.OrderBy(t => t.Priority),
                "worker" => sortOrder == "desc"
                    ? query.OrderByDescending(t => t.AssignedToUser!.LastName)
                    : query.OrderBy(t => t.AssignedToUser!.LastName),
                "attempts" => sortOrder == "desc"
                    ? query.OrderByDescending(t => t.CurrentAttemptCount)
                    : query.OrderBy(t => t.CurrentAttemptCount),
                "lastcall" => sortOrder == "desc"
                    ? query.OrderByDescending(t => t.LastCallAttempt)
                    : query.OrderBy(t => t.LastCallAttempt),
                _ => query.OrderBy(t => t.Priority).ThenBy(t => t.AssignedToUser!.LastName)
            };
            
            // Apply pagination
            var tasks = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsSplitQuery() // Avoid cartesian explosion
                .ToListAsync();
            
            _logger.LogInformation("Found {Count} tasks (total {Total})", tasks.Count, totalCount);
            
            return (tasks, totalCount);
        }

        public async Task<SupervisorDashboardData> GetSupervisorDashboardSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;

            // Get counts only (much faster than loading full objects)
            var unassignedCount = await _context.CaseTasks
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId == null && 
                           t.Status == CaseTaskStatus.Pending)
                .CountAsync();
            
            var escalatedCount = await _context.CaseTasks
                .Where(t => t.IsInterviewTask && t.EscalationLevel > 0)
                .CountAsync();
            
            var activeWorkerCount = await _context.Users
                .Where(u => u.IsInterviewWorker && u.AvailableForAutoAssignment)
                .CountAsync();
            
            var todaysTasks = await _context.CaseTasks
                .Where(t => t.IsInterviewTask && t.CreatedAt >= today)
                .Select(t => new { t.Status })
                .ToListAsync();
            
            // Get escalated tasks (limited to top 20)
            var escalatedTasks = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Where(t => t.IsInterviewTask && t.EscalationLevel > 0)
                .OrderByDescending(t => t.EscalationLevel)
                .ThenBy(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();
            
            // Get unassigned tasks (limited to top 50)
            var unassignedTasks = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Where(t => t.IsInterviewTask && 
                           t.AssignedToUserId == null && 
                           t.Status == CaseTaskStatus.Pending)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();
            
            // Get worker statistics (optimized - no task lists)
            var workers = await _context.Users
                .Where(u => u.IsInterviewWorker)
                .ToListAsync();
            
            var workerStats = new List<WorkerStatistics>();
            
            // Batch query for all worker task counts
            var workerTaskCounts = await _context.CaseTasks
                .Where(t => t.AssignedToUserId != null)
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new
                {
                    WorkerId = g.Key,
                    TotalAssigned = g.Count(),
                    InProgress = g.Count(t => t.Status == CaseTaskStatus.Pending || 
                                             t.Status == CaseTaskStatus.InProgress ||
                                             t.Status == CaseTaskStatus.WaitingForPatient),
                    Completed = g.Count(t => t.Status == CaseTaskStatus.Completed)
                })
                .ToDictionaryAsync(x => x.WorkerId!);
            
            // Batch query for today's calls
            var todaysCallCounts = await _context.TaskCallAttempts
                .Where(a => a.AttemptedAt >= today)
                .GroupBy(a => a.AttemptedByUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalCalls = g.Count(),
                    Successful = g.Count(a => a.Outcome == CallOutcome.Completed)
                })
                .ToDictionaryAsync(x => x.UserId);
            
            foreach (var worker in workers)
            {
                var taskStats = workerTaskCounts.GetValueOrDefault(worker.Id);
                var callStats = todaysCallCounts.GetValueOrDefault(worker.Id);
                
                var languages = new List<string>();
                if (!string.IsNullOrEmpty(worker.PrimaryLanguage))
                    languages.Add(worker.PrimaryLanguage);
                
                if (!string.IsNullOrEmpty(worker.LanguagesSpokenJson))
                {
                    try
                    {
                        var additionalLangs = JsonSerializer.Deserialize<List<string>>(worker.LanguagesSpokenJson);
                        if (additionalLangs != null)
                            languages.AddRange(additionalLangs);
                    }
                    catch { }
                }
                
                var totalTasks = taskStats?.TotalAssigned ?? 0;
                var completedTasks = taskStats?.Completed ?? 0;
                
                workerStats.Add(new WorkerStatistics
                {
                    UserId = worker.Id,
                    WorkerName = $"{worker.FirstName} {worker.LastName}".Trim(),
                    TasksAssigned = totalTasks,
                    TasksCompleted = completedTasks,
                    TasksInProgress = taskStats?.InProgress ?? 0,
                    CallsToday = callStats?.TotalCalls ?? 0,
                    SuccessfulCallsToday = callStats?.Successful ?? 0,
                    CompletionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0,
                    AverageDurationSeconds = 0,
                    LanguagesSpoken = languages.Distinct().ToList(),
                    IsAvailable = worker.AvailableForAutoAssignment
                });
            }
            
            return new SupervisorDashboardData
            {
                UnassignedTaskCount = unassignedCount,
                EscalatedTaskCount = escalatedCount,
                ActiveWorkerCount = activeWorkerCount,
                TotalTasksToday = todaysTasks.Count,
                CompletedTasksToday = todaysTasks.Count(t => t.Status == CaseTaskStatus.Completed),
                WorkerStats = workerStats,
                EscalatedTasks = escalatedTasks,
                UnassignedTasks = unassignedTasks,
                LanguageCoverage = new Dictionary<string, int>()
            };
        }

        public async Task<(List<CaseTask> Tasks, int TotalCount)> GetUnassignedInterviewTasksPaginatedAsync(
            int pageNumber,
            int pageSize,
            string? priority = null,
            string? searchTerm = null,
            string? language = null)
        {
            var query = _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Where(t => t.IsInterviewTask &&
                           t.AssignedToUserId == null &&
                           t.Status == CaseTaskStatus.Pending)
                .AsQueryable();

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var priorityEnum))
                query = query.Where(t => t.Priority == priorityEnum);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(lower) ||
                    (t.Case!.Patient!.GivenName + " " + t.Case.Patient.FamilyName).ToLower().Contains(lower));
            }

            if (!string.IsNullOrEmpty(language))
                query = query.Where(t => t.LanguageRequired == language);

            var totalCount = await query.CountAsync();
            var tasks = await query
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsSplitQuery()
                .ToListAsync();

            return (tasks, totalCount);
        }

        public async Task<(List<CaseTask> Tasks, int TotalCount)> GetEscalatedInterviewTasksPaginatedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null)
        {
            var query = _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c!.Patient)
                .Where(t => t.IsInterviewTask && t.EscalationLevel > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(lower) ||
                    (t.Case!.Patient!.GivenName + " " + t.Case.Patient.FamilyName).ToLower().Contains(lower));
            }

            var totalCount = await query.CountAsync();
            var tasks = await query
                .OrderByDescending(t => t.EscalationLevel)
                .ThenBy(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsSplitQuery()
                .ToListAsync();

            return (tasks, totalCount);
        }

        public async Task<List<ApplicationUser>> GetAllInterviewWorkersAsync()
        {
            return await _context.Users
                .Where(u => u.IsInterviewWorker)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
        }
    }
}
